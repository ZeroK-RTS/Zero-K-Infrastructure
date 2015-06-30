using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        private ConcurrentDictionary<string, ConcurrentBag<Client>> clientsByName = new ConcurrentDictionary<string, ConcurrentBag<Client>>();
        public ConcurrentDictionary<string, Channel> Rooms = new ConcurrentDictionary<string, Channel>();
        public ConcurrentDictionary<int, Battle> Battles = new ConcurrentDictionary<int, Battle>();

        private ConcurrentDictionary<Client, bool> allClients = new ConcurrentDictionary<Client, bool>();


        public IEnumerable<Client> GetAllClients()
        {
            return allClients.Keys;
        }

        public LoginChecker LoginChecker;


        public ConcurrentBag<Client> GetClients(string name)
        {
            return clientsByName.GetOrAdd(name, (n) => new ConcurrentBag<Client>());
        }

        public void AddClient(Client client)
        {
            allClients.TryAdd(client, true);
            GetClients(client.Name).Add(client);
        }


        public void RemoveClient(Client client)
        {
            bool dummy;
            allClients.TryRemove(client, out dummy);
            GetClients(client.Name).Add(client);
        }


        public SharedServerState()
        {
            var entry = Assembly.GetEntryAssembly();
            Version = entry.GetName().Version.ToString();
            Engine = GlobalConst.DefaultEngineOverride;
            Game = "zk:stable";
            LoginChecker = new LoginChecker(this);
        }
    }
}