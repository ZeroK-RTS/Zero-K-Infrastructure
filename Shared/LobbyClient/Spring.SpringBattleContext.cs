using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PlasmaShared;

namespace LobbyClient
{
    public class SpringBattleContext
    {
        public List<BattlePlayerResult> ActualPlayers = new List<BattlePlayerResult>();
        public List<string> PlayersUnreadyOnStart = new List<string>();

        public int Duration;
        public string EngineBattleID;

        public string EngineVersion;

        public bool GameEndedOk;
        public DateTime? IngameStartTime;

        public string IpAddress;

        public bool IsTimeoutForceStarted;
        public bool IsForceStarted;

        public bool IsCheating;

        public bool IsCrash;

        public bool IsHosting;
        public LobbyHostingContext LobbyStartContext = new LobbyHostingContext();

        public int MissionFrame;
        public int? MissionScore;
        public string MissionVars;
        public string MyPassword;
        public string MyUserName;

        public List<string> OutputExtras = new List<string>();
        public int Port;
        public string ReplayName;
        public DateTime StartTime;

        public bool WasKilled;


        public BattlePlayerResult GetOrAddPlayer(string name)
        {
            if (string.IsNullOrEmpty(name)) return null; // we don't want to add null players
            
            var ret = ActualPlayers.FirstOrDefault(y => y.Name == name);
            if (ret == null)
            {
                ret = new BattlePlayerResult(name) { IsSpectator = true, };
                ActualPlayers.Add(ret);
            }
            return ret;
        }


        public void SetForConnecting(string ip, int port, string myUser, string myPassword, string engineVersion)
        {
            IsHosting = false;
            IpAddress = ip;
            Port = port;
            MyUserName = myUser;
            MyPassword = myPassword;
            EngineVersion = engineVersion;
        }

        public void SetForHosting(LobbyHostingContext startContext,
            string ip,
            int? port,
            string myUser,
            string myPassword)
        {
            LobbyStartContext = startContext;
            EngineVersion = startContext.EngineVersion;
            IsHosting = true;
            IpAddress = ip ?? "127.0.0.1";
            Port = port ?? 8452;
            MyUserName = myUser;
            MyPassword = myPassword;
            ActualPlayers =
                LobbyStartContext.Players.Select(
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


        public void SetForSelfHosting(string engineVersion)
        {
            IsHosting = true;
            EngineVersion = engineVersion;
            IpAddress = "127.0.0.1";
            Port = 8452;
        }
    }
}