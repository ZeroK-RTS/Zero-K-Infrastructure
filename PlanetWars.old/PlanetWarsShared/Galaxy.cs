#region using

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using PlanetWarsShared.Events;
using PlanetWarsShared.Springie;

#endregion

namespace PlanetWarsShared
{
    [Serializable]
    public class Galaxy
    {
        #region Properties

        public List<SpaceFleet> Fleets = new List<SpaceFleet>();
        public string OffensiveFactionInternal;

        public Galaxy()
        {
            Factions = new List<Faction>();
            Links = new List<Link>();
            MapNames = new List<string>();
            Events = new List<Event>();
            Planets = new List<Planet>();
            Players = new List<Player>();
        }

        public List<Faction> Factions { get; set; }
        public List<Link> Links { get; set; }
        public List<string> MapNames { get; set; }
        public List<Event> Events { get; set; }

        [XmlIgnore]
        public Faction OffensiveFaction
        {
            get
            {
                if (string.IsNullOrEmpty(OffensiveFactionInternal)) {
                    return Factions[Round%Factions.Count()];
                } else {
                    return GetFaction(OffensiveFactionInternal);
                }
            }
        }

        public List<Planet> Planets { get; set; }
        public List<Player> Players { get; set; }
        public int Round { get; set; }
        public int Turn { get; set; }

        public void SwapUnusedPlanets()
        {
            foreach (var f in Factions) {
                double min = int.MaxValue;
                double max = int.MinValue;

                Player best = null;
                Player worst = null;

                foreach (var p in Players.Where(x => x.FactionName == f.Name)) {
                    double pval = p.RankPoints;
                    if (GetPlanet(p) == null && pval > max) {
                        // best without planet
                        max = pval;
                        best = p;
                    }
                    if (GetPlanet(p) != null && pval < min) {
                        // worst with planet
                        min = pval;
                        worst = p;
                    }
                }

                if (best != null && worst != null && best != worst && min < max) {
                    // give worst's planet to best

                    GetPlanet(worst).OwnerName = best.Name; // transfer
                    best.HasChangedMap = false; // can change map again

                    // add event
                    Events.Add(
                        new PlanetOwnerChangedEvent(
                            DateTime.Now, this, worst.Name, best.Name, f.Name, GetPlanet(best.Name).ID));
                }
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Advances galaxy to next round - loads new layout from file but keeps players. Warning modifies original galaxy
        /// </summary>
        public Galaxy AdvanceRound(string newRoundTemplate)
        {
            var gal = FromFile(newRoundTemplate);
            gal.Players = Players;
            gal.Round = Round + 1;

            // reset player stats and set next commanderinchief
            foreach (var player in gal.Players) {
                if (player.RankOrder == 0) {
                    player.Rank = Rank.CommanderInChief;
                    player.Clout++;
                } else {
                    player.Rank = Rank.Commander;
                }
                player.Victories = 0;
                player.MetalEarned = 0;
                player.Defeats = 0;
                player.HasChangedMap = false;
            }

            // swap sides and set commanders in chief
            int cnt = gal.Factions.Count;
            for (int i = 0; i < cnt; i++) {
                gal.Factions[i].SpringSide = Factions[(i + 1)%cnt].SpringSide;
            }
            return gal;
        }

        /// <summary>
        /// Calculates Rank (enum) and RankOrder (number, 0 = best) for players. Ranks are "valid" within factions.
        /// </summary>
        public void CalculatePlayerRanks(out List<PlayerRankChangedEvent> changedRanks)
        {
            changedRanks = new List<PlayerRankChangedEvent>();
            double totalRp = 0;
            foreach (var p in Players) {
                double rp = p.MeasuredVictories - 0.5*p.MeasuredDefeats;
                if (rp <= 0) {
                    p.MeasuredDefeats = 0;
                    p.MeasuredVictories = 0;
                    rp = 0.01*(p.Victories + p.Defeats);
                }

                foreach (var a in p.Awards) {
                    if (a.Type == "pwn") {
                        rp += 0.2;
                    } else if (a.Type == "friend") {
                        rp -= 0.5;
                    } else {
                        rp += 0.1;
                    }
                }

                p.RankPoints = rp;
                totalRp += p.RankPoints;
            }

            foreach (var f in Factions) {
                double factionRp = 0;
                var plr = Players.Where(p => p.FactionName == f.Name).ToList();
                foreach (var p in plr) {
                    factionRp += p.RankPoints;
                }
                plr.Sort(Player.CompareTo);

                var RankLimits = new List<RankLimit>
                {
                    new RankLimit(Rank.CommanderInChief, 30, 1),
                    new RankLimit(Rank.FieldMarshall, 20, 2),
                    new RankLimit(Rank.General, 14, 3),
                    new RankLimit(Rank.Brigadier, 10, 4),
                    new RankLimit(Rank.Colonel, 7, int.MaxValue),
                    new RankLimit(Rank.LtColonel, 4, int.MaxValue),
                    new RankLimit(Rank.Major, 2, int.MaxValue),
                    new RankLimit(Rank.Captain, 1, int.MaxValue)
                };

                int cnt = 0;
                for (int i = plr.Count - 1; i >= 0; i--) {
                    plr[i].RankOrder = cnt++;

                    bool rankSet = false;
                    foreach (var rankLimit in RankLimits) {
                        double weightedRp = plr[i].RankPoints;
                        if (totalRp > 0) {
                            weightedRp = (weightedRp*Factions.Count*(1.0 - (factionRp/totalRp)));
                        }
                        if (rankLimit.Count > 0 && weightedRp > rankLimit.MinRankPoints) {
                            if (plr[i].Rank != rankLimit.Rank) {
                                var rankEvent = new PlayerRankChangedEvent(
                                    DateTime.Now, this, plr[i].Name, plr[i].Rank, rankLimit.Rank);
                                plr[i].Rank = rankLimit.Rank;

                                Events.Add(rankEvent);
                                changedRanks.Add(rankEvent);
                            }
                            rankLimit.Count--;
                            rankSet = true;
                            break;
                        }
                    }
                    if (!rankSet) {
                        if (plr[i].Rank != Rank.Commander) {
                            var rankEvent = new PlayerRankChangedEvent(
                                DateTime.Now, this, plr[i].Name, plr[i].Rank, Rank.Commander);
                            plr[i].Rank = Rank.Commander;

                            Events.Add(rankEvent);
                            changedRanks.Add(rankEvent);
                        }
                    }
                }
            }
        }

        public void CalculateBattleElo(BattleEvent e)
        {
            var teams =
                e.EndGameInfos.Where(p => !p.Spectator).GroupBy(p => p.OnVictoryTeam).OrderBy(g => g.Key).ToList();

            var loserCount = teams[0].Count();
            var winnerCount = teams[1].Count();

            var loserElo = teams[0].Average(p => GetPlayer(p.Name).Elo);
            var winnerElo = teams[1].Average(p => GetPlayer(p.Name).Elo);

            var eWin = 1 / (1 + Math.Pow(10, (loserElo - winnerElo) / 400));
            var eLose = 1 / (1 + Math.Pow(10, (winnerElo - loserElo) / 400));

            var scoreWin = 32 * (1 - eWin) / winnerCount;
            var scoreLose = 32 * (0 - eLose) / loserCount;

            foreach (var p in teams[0])
            {
                GetPlayer(p.Name).Elo += scoreLose;
            }

            foreach (var p in teams[1])
            {
                GetPlayer(p.Name).Elo += scoreWin;
            }

            bool is1v1 = e.AreUpgradesDisabled && loserCount == 1 && winnerCount == 1;
            if (is1v1) {
                var pLose = GetPlayer(teams[0].First().Name);
                var pWin = GetPlayer(teams[1].First().Name);
                loserElo = pLose.Elo1v1;
                winnerElo = pWin.Elo1v1;

                eWin = 1 / (1 + Math.Pow(10, (loserElo - winnerElo) / 400));
                eLose = 1 / (1 + Math.Pow(10, (winnerElo - loserElo) / 400));

                scoreWin = 32 * (1 - eWin) / winnerCount;
                scoreLose = 32 * (0 - eLose) / loserCount;

                pLose.Elo1v1 += scoreLose;
                pWin.Elo1v1 += scoreWin;
            }
        }


        public void RecalculateAllEloRanks()
        {
            foreach (var p in Players) {
                p.Elo = 1500;
                p.Elo1v1 = 1500;
            }

            foreach (var e in Events.OfType<BattleEvent>())
            {
                CalculateBattleElo(e);
            }
        }



        public Player GetCommanderInChief(IFaction faction)
        {
            return GetPlayers(faction).SingleOrDefault(p => p.IsCommanderInChief);
        }

        public Player[] GetCommandersInChief()
        {
            var commanders = Players.Where(p => p.IsCommanderInChief).ToArray();
            if (commanders.Length > 2) {
                throw new Exception("Too many commanders in chief.");
            }
            return commanders;
        }

        public IEnumerable<Planet> GetAttackOptions()
        {
            return GetAttackOptions(OffensiveFaction);
        }

        public ICollection<Link> GetAttackLinks(string factionName)
        {
            return GetLinks(GetPlanets(factionName), GetEnemyPlanets(factionName));
        }

        public ICollection<Link> GetAttackLinks(IFaction faction)
        {
            return GetAttackLinks(faction.Name);
        }

        public ICollection<Link> GetAttackLinks()
        {
            return GetAttackLinks(OffensiveFaction);
        }

        public ICollection<Planet> GetAttackOptions(IFaction faction)
        {
            var linkedPlanets = GetLinkedPlanets(GetPlanets(faction), GetEnemyPlanets(faction));
            var enemyLinkedPlanets = linkedPlanets.Where(p => GetFaction(p).Name != faction.Name).ToArray();

            // this will get planets that have fleets, unless enemy has 2 more fleets there
            List<Planet> invasiblePlanets = new List<Planet>();
            var planetFleets = Fleets.Where(f => f.Arrives <= Turn && f.TargetPlanetID >= 0 && GetPlanet(f.TargetPlanetID).FactionName != faction.Name).GroupBy(p => p.TargetPlanetID).ToArray(); // get fleets in orbits of planets, grouped by planets

            foreach (var pf in planetFleets) {
                var byFactions =
                    pf.GroupBy(x => GetPlayer(x.OwnerName).FactionName).OrderByDescending(x => x.Count()).ToList();
                
                var allyFleets = byFactions.Find(k => k.Key == faction.Name);
                if (allyFleets != null) {
                    if (byFactions[0].Key == faction.Name || byFactions[0].Count() < allyFleets.Count() + 2) {
                        invasiblePlanets.Add(GetPlanet(pf.Key));
                    }
                }
            }
            return enemyLinkedPlanets.Union(invasiblePlanets).ToArray();
        }

        public ICollection<string> GetAvailableMaps()
        {
            return MapNames.Except(GetUsedMaps()).ToArray();
        }

        ICollection<Planet> GetNeutralAdjacentPlanets(IFaction faction)
        {
            if (faction == null) {
                throw new ArgumentNullException("faction");
            }
            return GetNeutralAdjacentPlanets(faction.Name);
        }

        ICollection<Planet> GetNeutralAdjacentPlanets(string factionName)
        {
            if (factionName == null) {
                throw new ArgumentNullException("factionName");
            }
            var factionPlanets = GetPlanets(factionName);
            var neutralPlanets = GetNeutralPlanets();
            return GetLinkedPlanets(factionPlanets, neutralPlanets).Where(p => p.FactionName == null).ToArray();
        }

        public ICollection<Planet> GetClaimablePlanets(IFaction faction)
        {
            if (faction == null) {
                throw new ArgumentNullException("faction");
            }
            return GetClaimablePlanets(faction.Name);
        }

        public ICollection<Planet> GetClaimablePlanets()
        {
            return Factions.SelectMany(f => GetClaimablePlanets(f)).ToArray();
        }

        public ICollection<Link> GetColonyLinks(string factionName)
        {
            return GetLinks(GetClaimablePlanets(factionName), GetPlanets(factionName));
        }

        public ICollection<Planet> GetClaimablePlanets(string factionName)
        {
            if (!GetNeutralPlanets().Any()) {
                return new Planet[0];
            }
            var factionPlanets = GetPlanets(factionName);
            if (!factionPlanets.Any()) {
                return Planets.Where(p => p.IsStartingPlanet).Where(p => p.OwnerName == null).ToArray();
            }

            // dont grab the other faction's starting planet
            var neutralAdjacentPlanets =
                GetNeutralAdjacentPlanets(factionName).Where(p => !p.IsStartingPlanet).ToHashSet();

            // if the faction is surrounded but there are free planets allow claiming planets next to an enemy planet
            var claimablePlanets = neutralAdjacentPlanets.Any()
                                       ? neutralAdjacentPlanets
                                       : Factions.Select(f => GetNeutralAdjacentPlanets(f)).First(c => c.Any());

            return claimablePlanets;
        }

        public ICollection<Planet> GetEnemyPlanets(string factionName)
        {
            return Factions.Where(f => f.Name != factionName).SelectMany(f => GetPlanets(f)).ToArray();
        }

        public ICollection<Planet> GetEnemyPlanets(IFaction faction)
        {
            return GetEnemyPlanets(faction.Name);
        }

        public Faction GetFaction(int planetID)
        {
            return GetFaction(GetPlanet(planetID));
        }

        public Faction GetFaction(Planet planet)
        {
            if (planet == null) {
                throw new ArgumentNullException("planet");
            }
            return planet.FactionName == null ? null : GetFaction(planet.FactionName);
        }

        public Faction GetFaction(string factionName)
        {
            if (factionName == null) {
                throw new ArgumentNullException("factionName");
            }
            return Factions.SingleOrDefault(f => f.Name == factionName);
        }

        public Faction GetFaction(IPlayer player)
        {
            if (player == null) {
                throw new ArgumentNullException("player");
            }
            return Factions.SingleOrDefault(f => f.Name == player.FactionName);
        }

        /// <summary>
        /// Finds links that connect two planets both from a group.
        /// </summary>
        public ICollection<Link> GetLinks(ICollection<Planet> planets)
        {
            var notInGroupIDs = Planets.Select(p => p.ID).Except(planets.Select(p => p.ID)).ToList();
            var links = from l in Links
                        where !l.PlanetIDs.Any(notInGroupIDs.Contains)
                        select l;
            return links.ToHashSet();
        }

        /// <summary>
        /// Find links between two planets of a faction;
        /// </summary>
        public ICollection<Link> GetLinks(string factionName)
        {
            return GetLinks(GetPlanets(factionName));
        }

        /// <summary>
        /// Find links between two planets of a faction;
        /// </summary>
        public ICollection<Link> GetLinks(IFaction faction)
        {
            return GetLinks(faction.Name);
        }

        /// <summary>
        /// Find links between two neutral planets;
        /// </summary>
        public ICollection<Link> GetNeutralLinks()
        {
            return GetLinks(GetNeutralPlanets());
        }

        /// <summary>
        /// Finds links that contain a planet from two different groups.
        /// </summary>
        public ICollection<Link> GetLinks(ICollection<Planet> group1, ICollection<Planet> group2)
        {
            var group1IDs = group1.Select(p => p.ID).ToList();
            var group2IDs = group2.Select(p => p.ID).ToList();

            // get planets from links that lead from group1 to group2
            var links = from l in Links
                        where l.PlanetIDs.Any(group1IDs.Contains)
                        where l.PlanetIDs.Any(group2IDs.Contains)
                        select l;
            return links.ToHashSet();
        }

        /// <summary>
        /// Finds planets with a link that connects to a planet in a different group.
        /// </summary>
        public ICollection<Planet> GetLinkedPlanets(ICollection<Planet> group1, ICollection<Planet> group2)
        {
            var planets = from l in GetLinks(group1, group2)
                          from id in l.PlanetIDs
                          select GetPlanet(id);
            return planets.ToArray();
        }

        public ICollection<Link> GetLinks(IPlanet planet)
        {
            if (planet == null) {
                throw new ArgumentNullException("planet");
            }
            return Links.Where(l => l.PlanetIDs.Contains(planet.ID)).ToArray();
        }

        public ICollection<Planet> GetNeutralPlanets()
        {
            return Planets.Where(p => p.FactionName == null).ToArray();
        }

        public Player GetOwner(IPlanet planet)
        {
            if (planet == null) {
                throw new ArgumentNullException("planet");
            }
            return Players.SingleOrDefault(p => p.Name == planet.OwnerName);
        }

        public Player GetOwner(int planetID)
        {
            return GetOwner(GetPlanet(planetID));
        }

        public Planet GetPlanet(int planetID)
        {
            return Planets.SingleOrDefault(p => p.ID == planetID);
        }

        public Planet GetPlanet(IPlayer player)
        {
            if (player == null) {
                throw new ArgumentNullException("player");
            }
            return Planets.SingleOrDefault(p => p.OwnerName == player.Name);
        }

        public Planet GetPlanet(string playerName)
        {
            var player = GetPlayer(playerName);
            if (player == null) {
                return null;
            }
            return GetPlanet(player);
        }

        public ICollection<Planet> GetPlanets(IFaction faction)
        {
            if (faction == null) {
                throw new ArgumentNullException("faction");
            }
            return GetPlanets(faction.Name);
        }

        public ICollection<Planet> GetPlanets(string factionName)
        {
            if (factionName == null) {
                throw new ArgumentNullException("factionName");
            }
            var planets = Planets.Where(p => p.FactionName == factionName).ToArray();
            return planets;
        }

        public ICollection<Planet> GetPlanets(Link link)
        {
            return (from id in link.PlanetIDs
                    join p in Planets on id equals p.ID
                    select p).ToArray();
        }

        public Player GetPlayer(string playerName)
        {
            if (playerName == null) {
                throw new ArgumentNullException("playerName");
            }
            return Players.SingleOrDefault(p => p.Name == playerName);
        }

        public ICollection<Player> GetPlayers(IFaction faction)
        {
            if (faction == null) {
                throw new ArgumentNullException("faction");
            }
            return GetPlayers(faction.Name);
        }

        public ICollection<Player> GetPlayers(string factionName)
        {
            if (factionName == null) {
                throw new ArgumentNullException("factionName");
            }
            return Players.Where(p => factionName == p.FactionName).ToArray();
        }

        public ICollection<string> GetUsedMaps()
        {
            return (from p in Planets
                    where p.MapName != null
                    select p.MapName).ToArray();
        }

        /* an encircled planet is one that is surrounded by enemy planets */
        /* for now, its only for single planets that have more than 2 connections */

        public bool IsPlanetEncircled(IPlanet planet)
        {
            if (planet == null) {
                throw new ArgumentNullException("planet");
            }
            var links = GetLinks(planet);
            if (links == null || links.Count < 2) {
                return false;
            }

            foreach (var link in links) {
                string planet1fac = GetPlanet(link.PlanetIDs[0]).FactionName;
                string planet2fac = GetPlanet(link.PlanetIDs[1]).FactionName;
                // if factions differ and neither faction is neutral
                if ((planet1fac == planet2fac) || (planet1fac == null) || (planet2fac == null)) {
                    return false;
                }
            }

            return true;
        }

        public static Galaxy FromFile(string path)
        {
            return FromString(File.ReadAllText(path));
        }

        static readonly XmlSerializer serializer = new XmlSerializer(typeof(Galaxy));

        public static Galaxy FromString(string xmlString) // for galaxy designer
        {
            var state = (Galaxy)serializer.Deserialize(new StringReader(xmlString));
            foreach (var e in state.Events) {
                e.Galaxy = state;
            }
            return state;
        }

        public string SaveToString() // for galaxy designer
        {
            using (var stream = new StringWriter()) {
                serializer.Serialize(stream, this);
                return stream.ToString();
            }
        }
 
        public void SaveToFile(string path)
        {
            File.WriteAllText(path, SaveToString());
        }

        public List<string> GetWarnings()
        {
            var warnings = new List<string>();
            var planetIDs = Planets.Select(p => p.ID).ToHashSet();
            var playerNames = Players.Select(p => p.Name).ToHashSet();
            var factionNames = Factions.Select(f => f.Name).ToHashSet();
            var playerFactionNames = Factions.Select(p => p.Name).ToHashSet();
            var ownerNames = Planets.Select(p => p.OwnerName).Where(n => n != null).ToHashSet();

            if (!ownerNames.IsSubsetOf(playerNames)) {
                warnings.Add("Planet owner does not exist.");
            }
            if (!playerFactionNames.IsSubsetOf(factionNames)) {
                warnings.Add("Player faction does not exist.");
            }
            if (planetIDs.Count != Planets.Count) {
                warnings.Add("Duplicate planet ID.");
            }
            if (playerNames.Count != Players.Count) {
                warnings.Add("Duplicate player name.");
            }
            if (factionNames.Count != Factions.Count) {
                warnings.Add("Duplicate faction.");
            }
            {
                var link = Links.FirstOrDefault(l => l[0] == l[1]);
                if (link != null) {
                    warnings.Add(String.Format("Link has same source and destination: {0} {1}", link[0], link[1]));
                }
            }
            foreach (var link in Links) {
                if (!planetIDs.IsSupersetOf(link.PlanetIDs)) {
                    warnings.Add("Link points to inexistent planet");
                }
                Array.Sort(link.PlanetIDs);
            }
            foreach (var link1 in Links) {
                foreach (var link2 in Links) {
                    if (link1 != link2 && link1.PlanetIDs.SequenceEqual(link2.PlanetIDs)) {
                        warnings.Add("Duplicate Link");
                    }
                }
            }
            Func<string, Planet, string> format = (s, p) => String.Format("Planet {0} {1} missing.", p.Name ?? String.Empty, s);
            foreach (var p in Planets) {
                
                if (p.OwnerName != null || p.FactionName != null || p.MapName != null) {
                    if (p.OwnerName == null) {
                        warnings.Add(p.Name + format("owner", p));
                    }
                    if (p.FactionName == null) {
                        warnings.Add(format("faction", p));
                    }
                    if (p.MapName == null) {
                        warnings.Add(format("map", p));
                    }
                    if (p.Name == null) {
                        warnings.Add(format("name", p));
                    }
                }
            }

            if (Planets.Count(p => p.IsStartingPlanet) != Factions.Count) {
                warnings.Add("Wrong number of starting planets.");
            }

            if (Round == 0 && Planets.Any(p => p.OwnerName == null) && !playerNames.All(ownerNames.Contains)) {
                warnings.Add("Galaxy has free space and a player has no planet.");
            }
            if (Planets.Count > MapNames.Count) {
                warnings.Add("Not enough maps.");
            }

            if (Factions.Count < 2) {
                warnings.Add("Not enough factions.");
            }
            return warnings;
        }

        public void CheckIntegrity()
        {
            foreach (var w in GetWarnings()) throw new Exception(w);
        }

        class RankLimit
        {
            public int Count;
            public int MinRankPoints;
            public Rank Rank;
            public RankLimit() {}

            public RankLimit(Rank rank, int minRankPoints, int count)
            {
                Rank = rank;
                MinRankPoints = minRankPoints;
                Count = count;
            }
        }

        #endregion
    }
}