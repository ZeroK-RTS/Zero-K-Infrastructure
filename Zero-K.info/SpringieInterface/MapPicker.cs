 using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ZkData;

namespace ZeroKWeb.SpringieInterface
{
    public class RecommendedMapResult
    {
        public string MapName;
        public string Message;
    }

    public class MapPicker
    {
        public class PlanetPickEntry
        {
            readonly Planet planet;
            readonly int weight;
            public Planet Planet { get { return planet; } }
            public int Weight { get { return weight; } }

            public PlanetPickEntry(Planet planet, int weight)
            {
                this.planet = planet;
                this.weight = weight;
            }
        }

        public static RecommendedMapResult GetRecommendedMap(BattleContext context, bool pickNew)
        {
            var mode = context.GetMode();
            var res = new RecommendedMapResult();
            using (var db = new ZkDataContext())
            {
                if (mode == AutohostMode.Planetwars)
                {
                    var playerAccounts = context.Players.Where(x => !x.IsSpectator).Select(x => db.Accounts.First(z => z.LobbyID == x.LobbyID)).ToList();
                    var playerAccountIDs = playerAccounts.Select(x => x.AccountID).ToList();

                    var facGroups =
                        playerAccounts.Where(x => x.ClanID != null).GroupBy(x => x.FactionID).Select(x => new { FactionID = x.Key, Count = x.Count() })
                            .ToList();
                    var playerFactionIDs = facGroups.Select(x => x.FactionID).ToList();
                    var biggestFactionIDs = new List<int?>();
                    if (facGroups.Any())
                    {
                        var biggestGroup = facGroups.OrderByDescending(x => x.Count).Select(x => x.Count).FirstOrDefault();
                        biggestFactionIDs = facGroups.Where(x => x.Count == biggestGroup).Select(x => x.FactionID).ToList();
                    }

                    var gal = db.Galaxies.Single(x => x.IsDefault);

                    var valids =
                        gal.Planets.Select(
                            x =>
                            new
                            {
                                Planet = x,
                                Ships = (x.AccountPlanets.Where(y => playerAccountIDs.Contains(y.AccountID)).Sum(y => (int?)y.DropshipCount) ?? 0),
                                Defenses = (x.PlanetStructures.Where(y => !y.IsDestroyed).Sum(y => y.StructureType.EffectDropshipDefense) ?? 0)
                            }).
                            Where(x => (x.Planet.Account == null || playerFactionIDs.Contains(x.Planet.Account.FactionID)) && x.Ships >= x.Defenses).
                            ToList();
                    var maxc = valids.Max(x => (int?)x.Ships) ?? 0;

                    List<MapPicker.PlanetPickEntry> targets = null;
                    // if there are no dropships target unclaimed and biggest clan planets - INSURGENTS
                    if (maxc == 0)
                    {
                        targets =
                            gal.Planets.Where(x => x.Account != null && biggestFactionIDs.Contains(x.Account.FactionID)).Select(
                                x =>
                                new MapPicker.PlanetPickEntry(x,
                                                    Math.Max(1, (2000 - x.AccountPlanets.Sum(y => (int?)y.Influence + y.ShadowInfluence) ?? 0) / 200) -
                                                    (x.PlanetStructures.Where(y => !y.IsDestroyed).Sum(y => y.StructureType.EffectDropshipDefense) ??
                                                     0))).ToList();

                        targets.AddRange(
                            gal.Planets.Where(
                                x =>
                                x.OwnerAccountID == null &&
                                db.Links.Any(
                                    y =>
                                    (y.PlanetID1 == x.PlanetID && y.PlanetByPlanetID2.Account != null &&
                                     biggestFactionIDs.Contains(y.PlanetByPlanetID2.Account.FactionID) ||
                                     (y.PlanetID2 == x.PlanetID && y.PlanetByPlanetID1.Account != null &&
                                      biggestFactionIDs.Contains(y.PlanetByPlanetID1.Account.FactionID))))).Select(
                                          x => new MapPicker.PlanetPickEntry(x, 16 + (x.AccountPlanets.Sum(y => (int?)y.Influence) ?? 0) / 50)));

                        if (!targets.Any()) targets = gal.Planets.Select(x => new MapPicker.PlanetPickEntry(x, 1)).ToList();
                    }
                    else targets = valids.Where(x => x.Ships == maxc).Select(x => new MapPicker.PlanetPickEntry(x.Planet, 1)).ToList();
                    // target valid planets with most dropships

                    var r = new Random(context.AutohostName.GetHashCode() + gal.Turn); // randomizer based on autohost name + turn to always return same

                    Planet planet = null;
                    var sumw = targets.Sum(x => x.Weight);
                    if (sumw > 0)
                    {
                        var random = r.Next(sumw);
                        sumw = 0;
                        foreach (var target in targets)
                        {
                            sumw += target.Weight;
                            if (sumw >= random)
                            {
                                planet = target.Planet;
                                break;
                            }
                        }
                    }
                    if (planet == null) planet = targets[r.Next(targets.Count)].Planet; // this should not be needed;

                    res.MapName = planet.Resource.InternalName;
                    var owner = "";
                    if (planet.Account != null) owner = planet.Account.Name;

                    var shipInfo = String.Join(",",
                                               planet.AccountPlanets.Where(x => x.DropshipCount > 0 && playerAccountIDs.Contains(x.AccountID)).Select(
                                                   x => String.Format("{0} ships from {1}", x.DropshipCount, x.Account.Name)));

                    res.Message = String.Format("Welcome to {0} planet {1} http://zero-k.info/PlanetWars/Planet/{2} attacked by {3}",
                                                owner,
                                                planet.Name,
                                                planet.PlanetID,
                                                String.IsNullOrEmpty(shipInfo) ? "insurgents" : shipInfo);

                    if (planet.OwnerAccountID != null && planet.Account.Clan != null)
                    {
                        var be = Global.Nightwatch.Tas.ExistingBattles.Values.FirstOrDefault(x => x.Founder.Name == context.AutohostName);
                        if (be != null && !be.Founder.IsInGame && be.MapName != res.MapName && be.NonSpectatorCount > 0)
                        {
                            foreach (var a in planet.Account.Clan.Accounts)
                            {
                                AuthServiceClient.SendLobbyMessage(a,
                                                                   String.Format(
                                                                       "Your clan's planet {0} is about to be attacked, defend it! Come to PlanetWars spring://@join_player:{1} ",
                                                                       planet.Name,
                                                                       context.AutohostName));
                            }
                        }
                    }

                    db.SubmitChanges();
                }
                else
                {
                    if (!pickNew) res.MapName = context.Map;
                    else {
                        List<Resource> list= null;
                        var players = context.Players.Count(x => !x.IsSpectator);
                        switch (mode)
                        {
                           case AutohostMode.GameTeams:
                           case AutohostMode.SmallTeams:
                               var ret =  db.Resources.Where(x => x.TypeID == ResourceType.Map && x.FeaturedOrder != null && x.MapIsFfa != true && x.MapIsChickens!=true);
                               if (players > 16) ret = ret.Where(x => x.MapDiagonal > 24);
                               else if (players > 6) ret = ret.Where(x => x.MapDiagonal <= 24 && x.MapDiagonal > 16);
                               else ret = ret.Where(x => x.MapDiagonal <= 16);
                                list = ret.ToList();


                                break;
                            case AutohostMode.Game1v1:
                                list = db.Resources.Where(x => x.TypeID == ResourceType.Map && x.FeaturedOrder != null && x.MapIs1v1==true&& x.MapIsFfa != true && x.MapIsChickens!=true).ToList();
                                break;
                            case AutohostMode.GameChickens:
                                ret = db.Resources.Where(x => x.TypeID == ResourceType.Map && x.FeaturedOrder != null && (x.MapIsChickens == true || x.MapWaterLevel == 1));
                                if (players > 16) ret = ret.Where(x => x.MapDiagonal > 24);
                                else if (players > 6) ret = ret.Where(x => x.MapDiagonal <= 24 && x.MapDiagonal > 16);
                                else ret = ret.Where(x => x.MapDiagonal <= 16);
                                list= ret.ToList();

                                break;
                            case AutohostMode.GameFFA:
                                list = db.Resources.Where(x => x.TypeID == ResourceType.Map && x.FeaturedOrder != null && x.MapIsFfa == true && x.MapFFAMaxTeams == players).ToList();
                                if (!list.Any()) list = db.Resources.Where(x => x.TypeID == ResourceType.Map && x.FeaturedOrder != null && x.MapIsFfa == true && (players%x.MapFFAMaxTeams==0)).ToList();
                                if (!list.Any()) list = db.Resources.Where(x => x.TypeID == ResourceType.Map && x.FeaturedOrder != null && x.MapIsFfa == true).ToList();

                                break;
                        }
                        if (list != null)
                        {
                            var r = new Random();
                            res.MapName = list[r.Next(list.Count)].InternalName;
                        }
                    }
                    
                    
                }
            }
            return res;
        }
    }
}