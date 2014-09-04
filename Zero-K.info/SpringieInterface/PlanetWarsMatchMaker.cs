using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;
using LobbyClient;
using PlasmaShared;
using ServiceStack.Text;
using ZkData;
using Timer = System.Timers.Timer;

namespace ZeroKWeb
{
    /// <summary>
    ///     Handles arranging and starting of PW games
    /// </summary>
    public class PlanetWarsMatchMaker
    {
        /// <summary>
        ///     Possible attack options
        /// </summary>
        readonly List<AttackOption> attackOptions = new List<AttackOption>();
        DateTime attackerSideChangeTime;
        int attackerSideCounter;
        /// <summary>
        ///     Faction that should attack this turn
        /// </summary>
        public Faction AttackingFaction { get { return factions[attackerSideCounter % factions.Count]; } }
        AttackOption challenge;

        DateTime? challengeTime;
        readonly List<Faction> factions;
        readonly string pwHostName;
        readonly Dictionary<string, AttackOption> runningBattles = new Dictionary<string, AttackOption>();
        string targetHost;
        readonly TasClient tas;

        Timer timer;


        public AttackOption GetBattleInfo(string hostName)
        {
            AttackOption option;
            runningBattles.TryGetValue(hostName, out option);
            return option;
        }

        public PlanetWarsMatchMaker(TasClient tas)
        {
            this.tas = tas;
            tas.PreviewSaid += TasOnPreviewSaid;
            tas.LoginAccepted += TasOnLoginAccepted;
            tas.UserRemoved += TasOnUserRemoved;
            tas.ChannelUserAdded += TasOnChannelUserAdded;

            timer = new Timer(10000);
            timer.AutoReset = true;
            timer.Elapsed += TimerOnElapsed;
            timer.Start();

            var db = new ZkDataContext();
            pwHostName = db.AutohostConfigs.First(x => x.AutohostMode == AutohostMode.Planetwars).Login.TrimNumbers();

            Galaxy gal = db.Galaxies.First(x => x.IsDefault);
            factions = db.Factions.Where(x => !x.IsDeleted).ToList();

            attackerSideCounter = gal.AttackerSideCounter;
            attackerSideChangeTime = gal.AttackerSideChangeTime ?? DateTime.UtcNow;
            attackerSideChangeTime = DateTime.UtcNow;


        }

        /// <summary>
        ///     Invoked from web page
        /// </summary>
        /// <param name="planet"></param>
        public void AddAttackOption(Planet planet)
        {
            if (!attackOptions.Any(x => x.PlanetID == planet.PlanetID) && challenge == null && planet.OwnerFactionID != AttackingFaction.FactionID)
            {
                attackOptions.Add(new AttackOption
                {
                    PlanetID = planet.PlanetID,
                    Map = planet.Resource.InternalName,
                    OwnerFactionID = planet.OwnerFactionID,
                    Name = planet.Name
                });

                UpdateAttackerLobby();
            }
        }

        void AcceptChallenge()
        {
            Battle emptyHost =
                tas.ExistingBattles.Values.FirstOrDefault(
                    x => !x.IsInGame && x.Founder.Name.TrimNumbers() == pwHostName && x.Users.All(y => y.IsSpectator));

            if (emptyHost != null)
            {
                targetHost = emptyHost.Founder.Name;
                runningBattles[targetHost] = challenge;
                foreach (User x in challenge.Attackers) tas.ForceJoinBattle(x.Name, emptyHost.BattleID);
                foreach (User x in challenge.Defenders) tas.ForceJoinBattle(x.Name, emptyHost.BattleID);

                Utils.StartAsync(() =>
                {
                    Thread.Sleep(5000);
                    tas.Say(TasClient.SayPlace.User, targetHost, "!lock 180", false);
                    tas.Say(TasClient.SayPlace.User, targetHost, "!map " + challenge.Map, false);
                    Thread.Sleep(500);
                    tas.Say(TasClient.SayPlace.User, targetHost, "!balance", false);
                    tas.Say(TasClient.SayPlace.User, targetHost, "!forcestart", false);
                    tas.Say(TasClient.SayPlace.User, targetHost, "!endvote", false);
                    tas.Say(TasClient.SayPlace.User, targetHost, "!start", false);
                    tas.Say(TasClient.SayPlace.User, targetHost, "!forcestart", true);
                });
            }

            attackerSideCounter++;
            ResetAttackOptions();
        }

        List<Faction> GetDefendingFactions(AttackOption target)
        {
            if (target.OwnerFactionID != null) return new List<Faction> { factions.Find(x => x.FactionID == target.OwnerFactionID) };
            return factions.Where(x => x != AttackingFaction).ToList();
        }

        void JoinPlanetAttack(int targetPlanetId, string userName)
        {
            AttackOption attackOption = attackOptions.Find(x => x.PlanetID == targetPlanetId);
            if (attackOption != null)
            {
                User user;
                if (tas.ExistingUsers.TryGetValue(userName, out user))
                {
                    var db = new ZkDataContext();
                    Account account = Account.AccountByLobbyID(db, user.LobbyID);
                    if (account != null && account.FactionID == AttackingFaction.FactionID && account.CanPlayerPlanetWars())
                    {
                        // remove existing user from other options
                        foreach (AttackOption aop in attackOptions) aop.Attackers.RemoveAll(x => x.Name == userName);

                        // add user to this option
                        if (attackOption.Attackers.Count < GlobalConst.PlanetWarsMatchSize)
                        {
                            attackOption.Attackers.Add(user);

                            if (attackOption.Attackers.Count == GlobalConst.PlanetWarsMatchSize) StartChallenge(attackOption);
                            else UpdateAttackerLobby();
                        }
                    }
                }
            }
        }

        void JoinPlanetDefense(int targetPlanetID, string userName)
        {
            if (challenge != null && challenge.PlanetID == targetPlanetID && challenge.Defenders.Count < GlobalConst.PlanetWarsMatchSize )
            {
                User user;
                if (tas.ExistingUsers.TryGetValue(userName, out user))
                {
                    var db = new ZkDataContext();
                    Account account = Account.AccountByLobbyID(db, user.LobbyID);
                    if (account != null && GetDefendingFactions(challenge).Any(y => y.FactionID == account.FactionID)&& account.CanPlayerPlanetWars())
                    {
                        if (!challenge.Defenders.Any(y => y.LobbyID == user.LobbyID))
                        {
                            challenge.Defenders.Add(user);
                            if (challenge.Defenders.Count == GlobalConst.PlanetWarsMatchSize) AcceptChallenge();
                            else UpdateDefenderLobby();
                        }
                    }
                }
            }
        }

        void RecordPlanetwarsLoss(AttackOption option)
        {
            var db = new ZkDataContext();
            var text = new StringBuilder();
            List<int?> playerIds = option.Attackers.Select(x => (int?)x.LobbyID).Union(option.Defenders.Select(x => (int?)x.LobbyID)).ToList();
            text.AppendFormat("{0} won because nobody tried to defend", AttackingFaction.Name);
            PlanetWarsTurnHandler.EndTurn(option.Map, null, db, 0, db.Accounts.Where(x => playerIds.Contains(x.LobbyID)).ToList(), text, null);
        }

        void ResetAttackOptions()
        {
            attackOptions.Clear();
            attackerSideChangeTime = DateTime.UtcNow;
            challenge = null;
            challengeTime = null;
            SaveStateToDb();

            foreach (Faction f in factions)
            {
                if (f != AttackingFaction) SendLobbyCommand(f, new PwMatchCommand(PwMatchCommand.ModeType.Clear));
            }
            UpdateAttackerLobby();
        }

        void SaveStateToDb()
        {
            var db = new ZkDataContext();
            Galaxy gal = db.Galaxies.First(x => x.IsDefault);

            gal.AttackerSideCounter = attackerSideCounter;
            gal.AttackerSideChangeTime = attackerSideChangeTime;
            db.SubmitAndMergeChanges();
        }


        void SendLobbyCommand(Faction faction, PwMatchCommand command)
        {
            tas.Extensions.SendJsonDataToChannel(faction.Shortcut, command);
        }

        void SendLobbyCommand(string username, PwMatchCommand command)
        {
            tas.Extensions.SendJsonData(username, command);
        }


        void StartChallenge(AttackOption attackOption)
        {
            challenge = attackOption;
            challengeTime = DateTime.UtcNow;
            attackOptions.Clear();
            SendLobbyCommand(AttackingFaction, new PwMatchCommand(PwMatchCommand.ModeType.Clear));
            UpdateDefenderLobby();
        }

        
        
        void UpdateAttackerLobby()
        {
            SendLobbyCommand(AttackingFaction,
                new PwMatchCommand(PwMatchCommand.ModeType.Attack)
                {
                    Options = attackOptions.Select(x => x.ToVoteOption(PwMatchCommand.ModeType.Attack)).ToList(),
                    DeadlineSeconds = GlobalConst.PlanetWarsMinutesToAttack*60 - (int)DateTime.UtcNow.Subtract(attackerSideChangeTime).TotalSeconds
                });
        }

        void UpdateDefenderLobby()
        {
            foreach (Faction def in GetDefendingFactions(challenge))
            {
                SendLobbyCommand(def,
                    new PwMatchCommand(PwMatchCommand.ModeType.Defend)
                    {
                        Options = new List<PwMatchCommand.VoteOption> { challenge.ToVoteOption(PwMatchCommand.ModeType.Defend) },
                        DeadlineSeconds = GlobalConst.PlanetWarsMinutesToAccept*60 - (int)DateTime.UtcNow.Subtract(challengeTime??DateTime.UtcNow).TotalSeconds
                    });
            }
        }

        void TasOnChannelUserAdded(object sender, TasEventArgs args)
        {
            string chan = args.ServerParams[0];
            string userName = args.ServerParams[1];
            Faction faction = factions.First(x => x.Shortcut == chan);
            if (faction != null)
            {
                if (challenge == null)
                {
                    if (faction == AttackingFaction)
                    {
                        SendLobbyCommand(userName,
                            new PwMatchCommand(PwMatchCommand.ModeType.Attack)
                            {
                                Options = attackOptions.Select(x => x.ToVoteOption(PwMatchCommand.ModeType.Attack)).ToList(),
                                DeadlineSeconds =
                                    GlobalConst.PlanetWarsMinutesToAttack*60 - (int)DateTime.UtcNow.Subtract(attackerSideChangeTime).TotalSeconds
                            });
                    }
                    else
                    {
                        SendLobbyCommand(userName, new PwMatchCommand(PwMatchCommand.ModeType.Clear));
                    }
                }
                else if (GetDefendingFactions(challenge).Contains(faction))
                {
                    SendLobbyCommand(userName,
                        new PwMatchCommand(PwMatchCommand.ModeType.Defend)
                        {
                            Options = attackOptions.Select(x => x.ToVoteOption(PwMatchCommand.ModeType.Defend)).ToList(),
                            DeadlineSeconds =
                                GlobalConst.PlanetWarsMinutesToAccept*60 -
                                (int)DateTime.UtcNow.Subtract(challengeTime ?? DateTime.UtcNow).TotalSeconds
                        });
                }
                else
                {
                    SendLobbyCommand(userName, new PwMatchCommand(PwMatchCommand.ModeType.Clear));
                }
            }
        }

        void TasOnLoginAccepted(object sender, TasEventArgs tasEventArgs)
        {
            ResetAttackOptions();
        }

        /// <summary>
        ///     Intercept channel messages for attacking/defending
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void TasOnPreviewSaid(object sender, CancelEventArgs<TasSayEventArgs> args)
        {
            if (args.Data.Text.StartsWith("!") && args.Data.Place == TasSayEventArgs.Places.Channel &&
                args.Data.Origin == TasSayEventArgs.Origins.Player && args.Data.UserName != GlobalConst.NightwatchName)
            {
                Faction faction = factions.FirstOrDefault(x =>  x.Shortcut == args.Data.Channel);
                if (faction != null)
                {
                    if (faction == AttackingFaction)
                    {
                        int targetPlanetID;
                        if (int.TryParse(args.Data.Text.Substring(1), out targetPlanetID)) JoinPlanetAttack(targetPlanetID, args.Data.UserName);
                    }
                    else if (challenge != null && GetDefendingFactions(challenge).Contains(faction))
                    {
                        int targetPlanetID;
                        if (int.TryParse(args.Data.Text.Substring(1), out targetPlanetID)) JoinPlanetDefense(targetPlanetID, args.Data.UserName);
                    }
                }
            }
        }

        /// <summary>
        ///     Remove/reduce poll count due to lobby quits
        /// </summary>
        void TasOnUserRemoved(object sender, TasEventArgs args)
        {
            if (challenge == null)
            {
                if (attackOptions.Count > 0)
                {
                    string userName = args.ServerParams[0];
                    var sumRemoved = 0;
                    foreach (AttackOption aop in attackOptions) sumRemoved += aop.Attackers.RemoveAll(x => x.Name == userName);
                    if (sumRemoved > 0) UpdateAttackerLobby();
                }
            }
            else
            {
                string userName = args.ServerParams[0];
                if (challenge.Defenders.RemoveAll(x => x.Name == userName) > 0) UpdateDefenderLobby();
            }
        }

        void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            try
            {
                if (challenge == null)
                {
                    // attack timer
                    if (DateTime.UtcNow.Subtract(attackerSideChangeTime).TotalMinutes > GlobalConst.PlanetWarsMinutesToAttack)
                    {
                        attackerSideCounter++;
                        ResetAttackOptions();
                    }
                }
                else
                {
                    // accept timer
                    if (DateTime.UtcNow.Subtract(challengeTime.Value).TotalMinutes > GlobalConst.PlanetWarsMinutesToAccept)
                    {
                        RecordPlanetwarsLoss(challenge);

                        attackerSideCounter++;
                        ResetAttackOptions();
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
            public List<User> Attackers = new List<User>();
            public List<User> Defenders = new List<User>();
            public string Map;
            public string Name;
            public int? OwnerFactionID;
            public int PlanetID;

            public PwMatchCommand.VoteOption ToVoteOption(PwMatchCommand.ModeType mode)
            {
                var opt = new PwMatchCommand.VoteOption
                {
                    PlanetID = PlanetID,
                    PlanetName = Name,
                    Map = Map,
                    Count = mode == PwMatchCommand.ModeType.Attack ? Attackers.Count : Defenders.Count
                };

                return opt;
            }
        }
    }
}