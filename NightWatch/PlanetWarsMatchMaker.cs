using System;
using System.Collections.Generic;
using System.Linq;
using LobbyClient;
using PlasmaShared;
using ZkData;

namespace NightWatch
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
        AttackOption challenge;

        DateTime? attackerNoBattleRunningTime;
        readonly DateTime? attackerSideChangeTime;
        readonly int attackerSideCounter;


        /// <summary>
        ///     Faction that should attack this turn
        /// </summary>
        Faction attackingFaction { get { return factions[attackerSideCounter%factions.Count]; } }
        
        readonly DateTime? challengeTime;
        readonly List<Faction> factions;
        TasClient tas;

        public PlanetWarsMatchMaker(TasClient tas)
        {
            this.tas = tas;
            tas.PreviewSaid+= TasOnPreviewSaid;
            tas.LoginAccepted += TasOnLoginAccepted;

            var db = new ZkDataContext();
            Galaxy gal = db.Galaxies.First(x => x.IsDefault);
            attackerSideCounter = gal.AttackerSideCounter;
            attackerSideChangeTime = gal.AttackerSideChangeTime;
            factions = db.Factions.Where(x => !x.IsDeleted).ToList();
        }

        void TasOnLoginAccepted(object sender, TasEventArgs tasEventArgs)
        {
            attackOptions.Clear();
            // todo update polls
        }

        /// <summary>
        /// Intercept channel messages for attacking/defending 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void TasOnPreviewSaid(object sender, CancelEventArgs<TasSayEventArgs> args)
        {
            if (args.Data.Text.StartsWith("!") && args.Data.Place == TasSayEventArgs.Places.Channel && args.Data.Origin == TasSayEventArgs.Origins.Player && args.Data.UserName != GlobalConst.NightwatchName)
            {
                var faction = factions.FirstOrDefault(x => x.Shortcut == args.Data.Channel);
                if (faction != null)
                {
                    if (faction == attackingFaction)
                    {
                        int targetPlanetID;
                        if (int.TryParse(args.Data.Text.Substring(1), out targetPlanetID))
                        {
                            JoinPlanetAttack(targetPlanetID, args.Data.UserName);
                        }
                    }
                }

            }
        }

        void JoinPlanetAttack(int targetPlanetId, string userName)
        {
            var attackOption = attackOptions.Find(x => x.Planet.PlanetID == targetPlanetId);
            if (attackOption != null)
            {
                User user;
                if (tas.ExistingUsers.TryGetValue(userName, out user))
                {
                    // remove existing user from other options
                    foreach (var aop in attackOptions) aop.Attackers.Remove(aop.Attackers.First(x => x.Name == userName));

                    // add user to this option
                    attackOption.Attackers.Add(user);
                    
                }
            }
        }

        public void AddAttackOption(Planet planet)
        {
            if (!attackOptions.Any(x => x.Planet.PlanetID == planet.PlanetID)) attackOptions.Add(new AttackOption { Planet = planet, });
        }

        public void SaveStateToDb()
        {
            var db = new ZkDataContext();
            Galaxy gal = db.Galaxies.First(x => x.IsDefault);

            gal.AttackerSideCounter = attackerSideCounter;
            gal.AttackerSideChangeTime = attackerSideChangeTime;
            db.SubmitAndMergeChanges();
        }

        public class AttackOption
        {
            public Planet Planet;
            public List<User> Attackers = new List<User>();
            public List<User> Defenders = new List<User>();
        }
    }
}