using System;
using System.Collections.Concurrent;
using System.Reflection;
using LobbyClient;
using MaxMind.Db;
using MaxMind.GeoIP2;
using ZkData;

namespace ZkLobbyServer
{
    public class SharedServerState
    {
        public string Version { get; private set; }
        public string Engine { get; set; }
        public string Game { get; set; }
        public int ClientCounter;
        public int BattleCounter;
        public CommandJsonSerializer Serializer = new CommandJsonSerializer();

        public ConcurrentDictionary<string, ConnectedUser> ConnectedUsers = new ConcurrentDictionary<string, ConnectedUser>();
        public ConcurrentDictionary<string, Channel> Rooms = new ConcurrentDictionary<string, Channel>();
        public ConcurrentDictionary<int, Battle> Battles = new ConcurrentDictionary<int, Battle>();

        public event EventHandler<Say> Said = (s, e) => { };

        public LoginChecker LoginChecker;

        public void OnSaid(object sender, Say say)
        {
            Said(sender, say);
        }

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