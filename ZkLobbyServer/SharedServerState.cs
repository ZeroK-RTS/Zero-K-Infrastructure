using System;
using System.Collections.Concurrent;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LobbyClient;
using MaxMind.Db;
using MaxMind.GeoIP2;
using ZkData;

namespace ZkLobbyServer
{
    public class SharedServerState
    {
        public int BattleCounter;
        public ConcurrentDictionary<int, Battle> Battles = new ConcurrentDictionary<int, Battle>();
        public int ClientCounter;
        public ConcurrentDictionary<string, ConnectedUser> ConnectedUsers = new ConcurrentDictionary<string, ConnectedUser>();
        public string Engine { get; set; }
        public string Game { get; set; }

        public LoginChecker LoginChecker;
        public ConcurrentDictionary<string, Channel> Rooms = new ConcurrentDictionary<string, Channel>();
        public CommandJsonSerializer Serializer = new CommandJsonSerializer();
        public OfflineMessageHandler OfflineMessageHandler = new OfflineMessageHandler();
        public string Version { get; private set; }




        public SharedServerState(string geoIPpath)
        {
            var entry = Assembly.GetExecutingAssembly();
            Version = entry.GetName().Version.ToString();
            Engine = GlobalConst.DefaultEngineOverride;
            Game = "zk:stable";
            LoginChecker = new LoginChecker(this, geoIPpath);
        }

    }
}