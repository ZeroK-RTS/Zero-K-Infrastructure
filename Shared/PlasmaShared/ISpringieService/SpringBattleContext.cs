using System;
using System.Collections.Generic;
using System.Linq;

namespace PlasmaShared
{
    public class SpringBattleContext
    {
        public readonly BattleContext StartContext;
        public List<BattlePlayerResult> ActualPlayers = new List<BattlePlayerResult>();

        public int Duration;
        public string EngineBattleID;

        public bool GameEndedOk;
        public DateTime? IngameStartTime;
        public string IpAddress;

        public bool IsCheating;

        public bool IsHosting;
        public int MissionFrame;
        public int? MissionScore;
        public string MissionVars;
        public Dictionary<string, string> ModOptions = new Dictionary<string, string>();
        public string MyUserName;

        public List<string> OutputExtras = new List<string>();
        public int Port;
        public string ReplayName;
        public DateTime StartTime;

        public bool IsCrash;
        public bool WasKilled;

        public bool UseDedicatedServer;
        public Dictionary<string, Dictionary<string, string>> UserParameters = new Dictionary<string, Dictionary<string, string>>();

        public SpringBattleContext(BattleContext startContext)
        {
            StartContext = startContext;
            ActualPlayers =
                StartContext.Players.Select(
                    x =>
                        new BattlePlayerResult(x.Name)
                        {
                            AllyNumber = x.AllyID,
                            IsSpectator = x.IsSpectator,
                            IsVictoryTeam = false,
                            IsIngameReady = false,
                            IsIngame = false,
                        }).ToList();
        }

        public BattlePlayerResult GetOrAddPlayer(string name)
        {
            var ret = ActualPlayers.FirstOrDefault(y => y.Name == name);
            if (ret == null)
            {
                ret = new BattlePlayerResult(name);
                ActualPlayers.Add(ret);
            }
            return ret;
        }
    }
}