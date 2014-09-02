using System;
using System.Collections.Generic;
using System.Linq;
using LobbyClient;
using Newtonsoft.Json;
using PlasmaShared;
using ZkData;

namespace NightWatch
{
    public class PwMatchCommand
    {
        public enum ModeType
        {
            Clear = 0,
            Attack = 1,
            Defend = 2
        }

        public ModeType Mode;

        public List<VoteOption> Options = new List<VoteOption>();

        public class VoteOption
        {
            public int Count;
            public string Map;
            public int Needed = GlobalConst.PlanetWarsMatchSize;
            public int PlanetID;
            public string PlanetName;
        }
    }

    /// <summary>
    ///     Handles arranging and starting of PW games
    ///     Commands sent by nightwatch:
    ///     PW: CLEAR
    ///     PW: ATTACK jsondata
    /// </summary>
    public class PlanetWarsMatchMaker
    {
        DateTime? attackerNoBattleRunningTime;


        /// <summary>
        ///     Faction that should attack this turn
        /// </summary>
        Faction attackingFaction { get { return factions[attackerSideCounter%factions.Count]; } }
        AttackOption challenge;

        DateTime? challengeTime;
        readonly List<Faction> factions;
        readonly TasClient tas;
        /// <summary>
        ///     Possible attack options
        /// </summary>
        public readonly List<AttackOption> attackOptions = new List<AttackOption>();
        public readonly DateTime? attackerSideChangeTime;
        public readonly int attackerSideCounter;

        public PlanetWarsMatchMaker(TasClient tas)
        {
            this.tas = tas;
            tas.PreviewSaid += TasOnPreviewSaid;
            tas.LoginAccepted += TasOnLoginAccepted;

            var db = new ZkDataContext();
            Galaxy gal = db.Galaxies.First(x => x.IsDefault);
            attackerSideCounter = gal.AttackerSideCounter;
            attackerSideChangeTime = gal.AttackerSideChangeTime;

            factions = db.Factions.Where(x => !x.IsDeleted).ToList();
        }

        public void AddAttackOption(Planet planet)
        {
            if (!attackOptions.Any(x => x.PlanetID == planet.PlanetID))
            {
                attackOptions.Add(new AttackOption
                {
                    PlanetID = planet.PlanetID,
                    Map = planet.Resource.InternalName,
                    OwnerFactionID = planet.OwnerFactionID,
                    Name = planet.Name
                });
            }
        }

        public List<Faction> GetDefendingFactions(AttackOption target)
        {
            if (target.OwnerFactionID != null) return new List<Faction> { factions.Find(x => x.FactionID == target.OwnerFactionID) };
            return factions.Where(x => x != attackingFaction).ToList();
        }

        public void SaveStateToDb()
        {
            var db = new ZkDataContext();
            Galaxy gal = db.Galaxies.First(x => x.IsDefault);

            gal.AttackerSideCounter = attackerSideCounter;
            gal.AttackerSideChangeTime = attackerSideChangeTime;
            db.SubmitAndMergeChanges();
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
                    if (account != null && account.FactionID == attackingFaction.FactionID)
                    {
                        // remove existing user from other options
                        foreach (AttackOption aop in attackOptions) aop.Attackers.Remove(aop.Attackers.First(x => x.Name == userName));

                        // add user to this option
                        if (attackOption.Attackers.Count < GlobalConst.PlanetWarsMatchSize)
                        {
                            attackOption.Attackers.Add(user);

                            if (attackOption.Attackers.Count == GlobalConst.PlanetWarsMatchSize) StartChallenge(attackOption);
                            else
                            {
                                SendLobbyCommand(attackingFaction,
                                    new PwMatchCommand
                                    {
                                        Mode = PwMatchCommand.ModeType.Attack,
                                        Options = attackOptions.Select(x => x.ToVoteOption(PwMatchCommand.ModeType.Attack)).ToList()
                                    });
                            }
                        }
                    }
                }
            }
        }

        void JoinPlanetDefense(string userName)
        {
            User user;
            if (tas.ExistingUsers.TryGetValue(userName, out user))
            {
                var db = new ZkDataContext();
                Account account = Account.AccountByLobbyID(db, user.LobbyID);
            }
        }

        void SendLobbyCommand(Faction faction, PwMatchCommand command)
        {
            tas.Say(TasClient.SayPlace.Channel, faction.Shortcut, "PW: " + JsonConvert.SerializeObject(command), true);
        }

        void StartChallenge(AttackOption attackOption)
        {
            challenge = attackOption;
            challengeTime = DateTime.UtcNow;
            SendLobbyCommand(attackingFaction, new PwMatchCommand { Mode = PwMatchCommand.ModeType.Clear });
            foreach (Faction def in GetDefendingFactions(challenge))
            {
                SendLobbyCommand(def,
                    new PwMatchCommand
                    {
                        Mode = PwMatchCommand.ModeType.Defend,
                        Options = new List<PwMatchCommand.VoteOption> { attackOption.ToVoteOption(PwMatchCommand.ModeType.Defend) }
                    });
            }
        }

        void TasOnLoginAccepted(object sender, TasEventArgs tasEventArgs)
        {
            attackOptions.Clear();
            foreach (Faction f in factions)
            {
                if (f != attackingFaction) SendLobbyCommand(f, new PwMatchCommand { Mode = PwMatchCommand.ModeType.Clear });
                else SendLobbyCommand(attackingFaction, new PwMatchCommand { Mode = PwMatchCommand.ModeType.Attack });
            }
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
                Faction faction = factions.FirstOrDefault(x => x.Shortcut == args.Data.Channel);
                if (faction != null)
                {
                    if (faction == attackingFaction)
                    {
                        int targetPlanetID;
                        if (int.TryParse(args.Data.Text.Substring(1), out targetPlanetID)) JoinPlanetAttack(targetPlanetID, args.Data.UserName);
                    }
                    else if (GetDefendingFactions(challenge).Contains(faction))
                    {
                        int targetPlanetID;
                        if (int.TryParse(args.Data.Text.Substring(1), out targetPlanetID) && targetPlanetID == challenge.PlanetID) JoinPlanetDefense(args.Data.UserName);
                    }
                }
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