using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;
using LobbyClient;
using Newtonsoft.Json;
using PlasmaShared;
using ZkData;
using JsonSerializer = ServiceStack.Text.JsonSerializer;
using Timer = System.Timers.Timer;

namespace ZeroKWeb
{
    public class MatchMakerState
    {
        /// <summary>
        ///     Possible attack options
        /// </summary>
        public List<PlanetWarsMatchMaker.AttackOption> AttackOptions { get; set; }
        public DateTime AttackerSideChangeTime { get; set; }
        public int AttackerSideCounter { get; set; }
        public PlanetWarsMatchMaker.AttackOption Challenge { get; set; }

        public DateTime? ChallengeTime { get; set; }

        public Dictionary<string, PlanetWarsMatchMaker.AttackOption> RunningBattles { get; set; }
        public MatchMakerState() { }
    }

    /// <summary>
    ///     Handles arranging and starting of PW games
    /// </summary>
    public class PlanetWarsMatchMaker : MatchMakerState
    {
        readonly List<Faction> factions;
        readonly string pwHostName;

        readonly TasClient tas;

        Timer timer;
        /// <summary>
        ///     Faction that should attack this turn
        /// </summary>
        [JsonIgnore]
        public Faction AttackingFaction { get { return factions[AttackerSideCounter % factions.Count]; } }


        int missedDefenseCount = 0;
        int missedDefenseFactionID = 0;
        private DateTime GetAttackDeadline()
        {
            int extra = 0;
            if (missedDefenseFactionID == AttackingFaction.FactionID) extra = Math.Min(missedDefenseCount*GlobalConst.PlanetWarsMinutesToAttack, 60);
            return AttackerSideChangeTime.AddMinutes(GlobalConst.PlanetWarsMinutesToAttack + extra);
        }

        private DateTime GetAcceptDeadline()
        {
            return ChallengeTime.Value.AddMinutes(GlobalConst.PlanetWarsMinutesToAccept);
        }

        public PlanetWarsMatchMaker(TasClient tas)
        {
            AttackOptions = new List<AttackOption>();
            RunningBattles = new Dictionary<string, AttackOption>();

            var db = new ZkDataContext();
            pwHostName = db.AutohostConfigs.First(x => x.AutohostMode == AutohostMode.Planetwars).Login.TrimNumbers();

            Galaxy gal = db.Galaxies.First(x => x.IsDefault);
            factions = db.Factions.Where(x => !x.IsDeleted).ToList();

            MatchMakerState dbState = null;
            if (gal.MatchMakerState != null)
            {
                try
                {
                    dbState = JsonConvert.DeserializeObject<MatchMakerState>(gal.MatchMakerState);
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }
            }
            if (dbState != null)
            {
                AttackerSideCounter = dbState.AttackerSideCounter;
                AttackOptions = dbState.AttackOptions;
                Challenge = dbState.Challenge;
                ChallengeTime = dbState.ChallengeTime;
                AttackerSideChangeTime = dbState.AttackerSideChangeTime;
                RunningBattles = dbState.RunningBattles;
            }
            else
            {
                AttackerSideCounter = gal.AttackerSideCounter;
                AttackerSideChangeTime = gal.AttackerSideChangeTime ?? DateTime.UtcNow;
            }

            this.tas = tas;
            tas.PreviewSaid += TasOnPreviewSaid;
            tas.UserRemoved += TasOnUserRemoved;
            tas.ChannelUserAdded += TasOnChannelUserAdded;
            tas.ChannelJoined += (sender, args) => { if (args.ServerParams[0] == "extension") tas.Extensions.SendJsonData(GenerateLobbyCommand()); };

            timer = new Timer(10000);
            timer.AutoReset = true;
            timer.Elapsed += TimerOnElapsed;
            timer.Start();
        }

        public void AcceptChallenge()
        {
            if (missedDefenseFactionID == Challenge.OwnerFactionID)
            {
                missedDefenseCount = 0;
                missedDefenseFactionID = 0;
            }

            Battle emptyHost =
                tas.ExistingBattles.Values.FirstOrDefault(
                    x => !x.IsInGame && x.Founder.Name.TrimNumbers() == pwHostName && x.Users.All(y => y.IsSpectator || y.Name == x.Founder.Name));

            if (emptyHost != null)
            {
                var targetHost = emptyHost.Founder.Name;
                RunningBattles[targetHost] = Challenge;

                tas.Say(TasClient.SayPlace.User, targetHost, "!map " + Challenge.Map, false);
                Thread.Sleep(500);
                foreach (string x in Challenge.Attackers) tas.ForceJoinBattle(x, emptyHost.BattleID);
                foreach (string x in Challenge.Defenders) tas.ForceJoinBattle(x, emptyHost.BattleID);

                // move spectators to battle
                var pwSpec = FindPwSpecHost();
                var bat = tas.ExistingBattles.Values.FirstOrDefault(x => x.Founder.Name == pwSpec);
                if (bat != null)
                {
                    foreach (var b in bat.Users.Where(x => x.Name != pwSpec)) tas.ForceJoinBattle(b.Name, targetHost);
                }

                var text = string.Format("Battle for planet {0} starts on zk://@join_player:{1}  Roster: {2} vs {3}",
                    Challenge.Name,
                    targetHost,
                    string.Join(",", Challenge.Attackers),
                    string.Join(",", Challenge.Defenders));

                foreach (var fac in factions)
                {
                    tas.Say(TasClient.SayPlace.Channel, fac.Shortcut, text, true);
                }

                Utils.StartAsync(() =>
                {
                    Thread.Sleep(6000);
                    tas.Say(TasClient.SayPlace.User, targetHost, "!balance", false);
                    Thread.Sleep(1000);
                    tas.Say(TasClient.SayPlace.User, targetHost, "!endvote", false);
                    tas.Say(TasClient.SayPlace.User, targetHost, "!forcestart", false);
                });
            }
            else
            {
                foreach (var c in factions)
                {
                    tas.Say(TasClient.SayPlace.Channel, c.Shortcut, "Battle could not start - no autohost found", true);
                }
            }

            AttackerSideCounter++;
            ResetAttackOptions();
        }

        /// <summary>
        ///     Invoked from web page
        /// </summary>
        /// <param name="planet"></param>
        public void AddAttackOption(Planet planet)
        {
            if (!AttackOptions.Any(x => x.PlanetID == planet.PlanetID) && Challenge == null && planet.OwnerFactionID != AttackingFaction.FactionID)
            {
                InternalAddOption(planet);
                UpdateLobby();
            }
        }

        public PwMatchCommand GenerateLobbyCommand()
        {
            PwMatchCommand command;
            if (Challenge == null)
            {
                command = new PwMatchCommand(PwMatchCommand.ModeType.Attack)
                {
                    Options = AttackOptions.Select(x => x.ToVoteOption(PwMatchCommand.ModeType.Attack)).ToList(),
                    DeadlineSeconds = (int)GetAttackDeadline().Subtract(DateTime.UtcNow).TotalSeconds,
                    AttackerFaction = AttackingFaction.Shortcut
                };
            }
            else
            {
                command = new PwMatchCommand(PwMatchCommand.ModeType.Defend)
                {
                    Options = new List<PwMatchCommand.VoteOption> { Challenge.ToVoteOption(PwMatchCommand.ModeType.Defend) },
                    DeadlineSeconds = (int)GetAcceptDeadline().Subtract(DateTime.UtcNow).TotalSeconds,
                    AttackerFaction = AttackingFaction.Shortcut,
                    DefenderFactions = GetDefendingFactions(Challenge).Select(x => x.Shortcut).ToList()
                };
            }
            return command;
        }

        public AttackOption GetBattleInfo(string hostName)
        {
            AttackOption option;
            RunningBattles.TryGetValue(hostName, out option);
            return option;
        }

        public void UpdateLobby()
        {
            tas.Extensions.SendJsonData(GenerateLobbyCommand());
            SaveStateToDb();
        }

        public void UpdateLobby(string player)
        {
            tas.Extensions.SendJsonData(player, GenerateLobbyCommand());
        }

        List<Faction> GetDefendingFactions(AttackOption target)
        {
            if (target.OwnerFactionID != null) return new List<Faction> { factions.Find(x => x.FactionID == target.OwnerFactionID) };
            return factions.Where(x => x != AttackingFaction).ToList();
        }

        void JoinPlanetAttack(int targetPlanetId, string userName)
        {
            AttackOption attackOption = AttackOptions.Find(x => x.PlanetID == targetPlanetId);
            if (attackOption != null)
            {
                User user;
                if (tas.ExistingUsers.TryGetValue(userName, out user))
                {
                    var db = new ZkDataContext();
                    Account account =  db.Accounts.Find(user.LobbyID);
                    if (account != null && account.FactionID == AttackingFaction.FactionID && account.CanPlayerPlanetWars())
                    {
                        // remove existing user from other options
                        foreach (AttackOption aop in AttackOptions) aop.Attackers.RemoveAll(x => x == userName);

                        // add user to this option
                        if (attackOption.Attackers.Count < attackOption.TeamSize)
                        {
                            attackOption.Attackers.Add(user.Name);
                            tas.Say(TasClient.SayPlace.Channel, user.Faction, string.Format("{0} joins attack on {1}", userName, attackOption.Name), true);

                            if (attackOption.Attackers.Count == attackOption.TeamSize) StartChallenge(attackOption);
                            else UpdateLobby();
                        }
                    }
                }
            }
        }

        void JoinPlanetDefense(int targetPlanetID, string userName)
        {
            if (Challenge != null && Challenge.PlanetID == targetPlanetID && Challenge.Defenders.Count < Challenge.TeamSize)
            {
                User user;
                if (tas.ExistingUsers.TryGetValue(userName, out user))
                {
                    var db = new ZkDataContext();
                    Account account = db.Accounts.Find(user.LobbyID);
                    if (account != null && GetDefendingFactions(Challenge).Any(y => y.FactionID == account.FactionID) && account.CanPlayerPlanetWars())
                    {
                        if (!Challenge.Defenders.Any(y => y == user.Name))
                        {
                            Challenge.Defenders.Add(user.Name);
                            tas.Say(TasClient.SayPlace.Channel, user.Faction, string.Format("{0} joins defense of {1}", userName, Challenge.Name), true);

                            if (Challenge.Defenders.Count == Challenge.TeamSize) AcceptChallenge();
                            else UpdateLobby();
                        }
                    }
                }
            }
        }

        void RecordPlanetwarsLoss(AttackOption option)
        {
            if (option.OwnerFactionID != null)
            {
                if (option.OwnerFactionID == missedDefenseFactionID)
                {
                    missedDefenseCount++;
                }
                else
                {
                    missedDefenseCount = 0;
                    missedDefenseFactionID = option.OwnerFactionID.Value;
                }
                
            }


            var message = string.Format("{0} won because nobody tried to defend", AttackingFaction.Name);
            foreach (var fac in factions)
            {
                tas.Say(TasClient.SayPlace.Channel, fac.Shortcut, message, true);
            }


            var text = new StringBuilder();
            try
            {
                var db = new ZkDataContext();
                List<string> playerIds = option.Attackers.Select(x => x).Union(option.Defenders.Select(x => x)).ToList();

                
                PlanetWarsTurnHandler.EndTurn(option.Map, null, db, 0, db.Accounts.Where(x => playerIds.Contains(x.Name) && x.Faction != null).ToList(), text, null, db.Accounts.Where(x => option.Attackers.Contains(x.Name) && x.Faction != null).ToList());
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                text.Append(ex);
            }


        }

        void ResetAttackOptions()
        {
            AttackOptions.Clear();
            AttackerSideChangeTime = DateTime.UtcNow;
            Challenge = null;
            ChallengeTime = null;

            using (var db = new ZkDataContext())
            {
                var gal = db.Galaxies.First(x => x.IsDefault);
                int cnt = 2;
                var attacker = db.Factions.Single(x => x.FactionID == AttackingFaction.FactionID);
                var planets = gal.Planets.Where(x => x.OwnerFactionID != AttackingFaction.FactionID).OrderByDescending(x => x.PlanetFactions.Where(y => y.FactionID == AttackingFaction.FactionID).Sum(y => y.Dropships)).ThenByDescending(x => x.PlanetFactions.Where(y => y.FactionID == AttackingFaction.FactionID).Sum(y => y.Influence)).ToList();
                // list of planets by attacker's influence

                foreach (var planet in planets)
                {
                    if (planet.CanMatchMakerPlay(attacker))
                    {
                        // pick only those where you can actually attack atm
                        InternalAddOption(planet);
                        cnt--;
                    }
                    if (cnt == 0) break;
                }

                if (!AttackOptions.Any(y => y.TeamSize == 2))
                {
                    var planet = planets.FirstOrDefault(x => x.TeamSize == 2 && x.CanMatchMakerPlay(attacker));
                    if (planet != null) InternalAddOption(planet);
                }
            }

            UpdateLobby();

            tas.Say(TasClient.SayPlace.Channel, AttackingFaction.Shortcut, "It's your turn! Select a planet to attack", true);
        }

        void InternalAddOption(Planet planet)
        {
            AttackOptions.Add(new AttackOption
            {
                PlanetID = planet.PlanetID,
                Map = planet.Resource.InternalName,
                OwnerFactionID = planet.OwnerFactionID,
                Name = planet.Name,
                TeamSize = planet.TeamSize,
            });
        }

        void SaveStateToDb()
        {
            var db = new ZkDataContext();
            Galaxy gal = db.Galaxies.First(x => x.IsDefault);

            gal.MatchMakerState = JsonConvert.SerializeObject((MatchMakerState)this);

            gal.AttackerSideCounter = AttackerSideCounter;
            gal.AttackerSideChangeTime = AttackerSideChangeTime;
            db.SubmitAndMergeChanges();
        }


        void StartChallenge(AttackOption attackOption)
        {
            Challenge = attackOption;
            ChallengeTime = DateTime.UtcNow;
            AttackOptions.Clear();
            UpdateLobby();
        }


        void TasOnChannelUserAdded(object sender, TasEventArgs args)
        {
            string chan = args.ServerParams[0];
            string userName = args.ServerParams[1];
            Faction faction = factions.FirstOrDefault(x => x.Shortcut == chan);
            if (faction != null)
            {
                var db = new ZkDataContext();
                var acc = Account.AccountByName(db, userName);
                if (acc != null && acc.CanPlayerPlanetWars()) UpdateLobby(userName);
            }
        }

        /// <summary>
        ///     Intercept channel messages for attacking/defending
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void TasOnPreviewSaid(object sender, CancelEventArgs<TasSayEventArgs> args)
        {
            if (args.Data.Text.StartsWith("!") && (args.Data.Place == TasSayEventArgs.Places.Channel || args.Data.Place == TasSayEventArgs.Places.Normal) &&
                args.Data.Origin == TasSayEventArgs.Origins.Player && args.Data.UserName != GlobalConst.NightwatchName)
            {
                int targetPlanetId;
                if (int.TryParse(args.Data.Text.Substring(1), out targetPlanetId)) JoinPlanet(args.Data.UserName, targetPlanetId);
            }
        }

        /// <summary>
        ///     Remove/reduce poll count due to lobby quits
        /// </summary>
        void TasOnUserRemoved(object sender, TasEventArgs args)
        {
            if (Challenge == null)
            {
                if (AttackOptions.Count > 0)
                {
                    string userName = args.ServerParams[0];
                    int sumRemoved = 0;
                    foreach (AttackOption aop in AttackOptions) sumRemoved += aop.Attackers.RemoveAll(x => x == userName);
                    if (sumRemoved > 0) UpdateLobby();
                }
            }
            else
            {
                string userName = args.ServerParams[0];
                if (Challenge.Defenders.RemoveAll(x => x == userName) > 0) UpdateLobby();
            }
        }

        void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            try
            {
                if (Challenge == null)
                {
                    // attack timer
                    if (DateTime.UtcNow > GetAttackDeadline())
                    {
                        AttackerSideCounter++;
                        ResetAttackOptions();
                    }
                }
                else
                {
                    // accept timer
                    if (DateTime.UtcNow > GetAcceptDeadline())
                    {
                        if (Challenge.Defenders.Count >= Challenge.Attackers.Count - 1 && Challenge.Defenders.Count > 0) AcceptChallenge();
                        else
                        {
                            RecordPlanetwarsLoss(Challenge);
                            AttackerSideCounter++;
                            ResetAttackOptions();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }

        public class AttackOption
        {
            public List<string> Attackers { get; set; }

            public List<string> Defenders { get; set; }
            public string Map { get; set; }
            public string Name { get; set; }
            public int? OwnerFactionID { get; set; }
            public int PlanetID { get; set; }
            public int TeamSize { get; set; }

            public AttackOption()
            {
                Attackers = new List<string>();
                Defenders = new List<string>();
            }

            public PwMatchCommand.VoteOption ToVoteOption(PwMatchCommand.ModeType mode)
            {
                var opt = new PwMatchCommand.VoteOption
                {
                    PlanetID = PlanetID,
                    PlanetName = Name,
                    Map = Map,
                    Count = mode == PwMatchCommand.ModeType.Attack ? Attackers.Count : Defenders.Count,
                    Needed = TeamSize
                };

                return opt;
            }
        }

        public void JoinPlanet(string name, int planetId)
        {
            if (tas.ExistingUsers.ContainsKey(name))
            {
                Faction faction = factions.FirstOrDefault(x => x.Shortcut == tas.ExistingUsers[name].Faction);
                if (faction == null)
                {
                    var db = new ZkDataContext(); // this is a fallback, should not be needed
                    var acc = Account.AccountByName(db, name);
                    faction = factions.FirstOrDefault(x => x.FactionID == acc.FactionID);
                }
                if (faction == AttackingFaction)
                {
                    JoinPlanetAttack(planetId, name);
                }
                else if (Challenge != null && GetDefendingFactions(Challenge).Contains(faction))
                {
                    JoinPlanetDefense(planetId, name);
                }
            }
        }

        public string FindPwSpecHost()
        {
            return tas.ExistingBattles.Values.Select(x => x.Founder.Name).FirstOrDefault(x => x.StartsWith("PlanetWarsSpec"));
        }

        public void RemoveFromRunningBattles(string autohostName)
        {
            RunningBattles.Remove(autohostName);

            // move spectators out from battle
            var pwSpec = FindPwSpecHost();

            var bat = tas.ExistingBattles.Values.FirstOrDefault(x => x.Founder.Name == autohostName);
            if (bat != null && tas.ExistingBattles.Values.Any(x => x.Founder.Name == pwSpec))
            {
                foreach (var b in bat.Users.Where(x => x.Name != autohostName)) tas.ForceJoinBattle(b.Name, pwSpec);
            }
        }
    }
}