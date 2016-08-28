using System;
using System.Collections.Generic;
using System.Linq;

namespace PlasmaShared
{
    public class SpringBattleContext
    {
        public List<BattlePlayerResult> ActualPlayers = new List<BattlePlayerResult>();

        public int Duration;
        public string EngineBattleID;

        public bool GameEndedOk;
        public DateTime? IngameStartTime;
        public Dictionary<string, string> ModOptions = new Dictionary<string, string>();

        public List<string> OutputExtras = new List<string>();
        public string ReplayName;
        public BattleContext StartContext;
        public DateTime StartTime;
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

        public bool IsCheating { get; set; }
        public int? MissionScore { get; set; }
        public int MissionFrame { get; set; }
        public string MissionVars { get; set; }

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