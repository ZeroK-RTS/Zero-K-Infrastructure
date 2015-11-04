using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using PlasmaShared;
using ZkData;
using ZkData.UnitSyncLib;

namespace LobbyClient.Legacy
{
    [Obsolete]
    public class Battle: ICloneable
    {
        public enum NatMode
        {
            None = 0,
            HolePunching = 1,
            FixedPorts = 2
        };

        /// <summary>
        /// Full map metadata - loaded only for joined battle
        /// </summary>
        readonly Map map;
        /// <summary>
        /// Full mod metadata - loaded only for joined battle
        /// </summary>
        readonly Mod mod;

        public int BattleID { get; set; }
        public List<BotBattleStatus> Bots { get; set; }
        public BattleDetails Details { get; set; }
        public List<string> DisabledUnits { get; set; }

        public User Founder { get; set; }

        public int HostPort { get; set; }
        public string Ip { get; set; }
        public bool IsFull { get { return NonSpectatorCount == MaxPlayers; } }
        public bool IsInGame { get { return Founder.IsInGame; } }
        public bool IsLocked { get; set; }
        public bool IsMission { get { return mod != null && mod.IsMission; } }
        public bool IsPassworded { get { return Password != "*"; } }
        public bool IsReplay { get; set; }

        public int? MapHash { get; set; }
        public string MapName { get; set; }

        public int MaxPlayers { get; set; }

        public int? ModHash { get; set; }
        public string ModName { get; set; }
        public Dictionary<string, string> ModOptions { get; private set; }
        public NatMode Nat { get; set; }

        public int NonSpectatorCount { get { return Users.Count - SpectatorCount; } }

        public string Password = "*";

        public int Rank { get; set; }
        public Dictionary<int, BattleRect> Rectangles { get; set; }
        public List<string> ScriptTags = new List<string>();
        public string EngineName = "spring";
        public string EngineVersion { get; set; }
        public int SpectatorCount { get; set; }
        public string Title { get; set; }

        public List<UserBattleStatus> Users { get; set; }


        public bool IsSpringieManaged
        {
            get { return Founder != null && Founder.IsSpringieManaged;}
        }

        public bool IsQueue
        {
            get { return IsSpringieManaged && Title.StartsWith("Queue"); }
        }

        public string QueueName
        {
            get
            {
                if (IsQueue)
                {
                    return Title.Substring(6);
                }
                else return null;
            }
        }

        internal Battle()
        {
            Bots = new List<BotBattleStatus>();
            Details = new BattleDetails();
            ModOptions = new Dictionary<string, string>();
            Rectangles = new Dictionary<int, BattleRect>();
            DisabledUnits = new List<string>();
            Password = "*";
            Nat = NatMode.None;
            Users = new List<UserBattleStatus>();
        }


        public Battle(string engineVersion, string password, int port, int maxplayers, int rank, Map map, string title, Mod mod, BattleDetails details): this()
        {
            if (!String.IsNullOrEmpty(password)) Password = password;
            if (port == 0) HostPort = 8452; else HostPort = port;
            try {
                var ports = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners().OrderBy(x => x.Port).Select(x=>x.Port).ToList();
                if (ports.Contains(HostPort)) {
                    var blockedPort = HostPort;
                    while (ports.Contains(HostPort)) HostPort++;
                    Trace.TraceWarning("Host port {0} was used, using backup port {1}", blockedPort, HostPort);
                }
            } catch {}




            EngineVersion = engineVersion;
            MaxPlayers = maxplayers;
            Rank = rank;
            this.map = map;
            MapName = map.Name;
            MapHash = 0;
            Title = title;
            this.mod = mod;
            ModName = mod.Name;
            ModHash = 0;
            if (details != null) Details = details;
        }


        public bool CanBeJoined(int playerRank)
        {
            return NonSpectatorCount > 0 && !IsLocked && MaxPlayers > NonSpectatorCount && Password == "*" && Rank >= playerRank;
        }

        public bool ContainsUser(string name, out UserBattleStatus status)
        {
            status = Users.SingleOrDefault(x => x.Name == name);
            return status != null;
        }


        public int GetUserIndex(string name)
        {
            for (var i = 0; i < Users.Count; ++i) if (Users[i].Name == name) return i;
            return -1;
        }


        public void RemoveUser(string name)
        {
            var ret = GetUserIndex(name);
            if (ret != -1) Users.RemoveAt(ret);
        }

        public override string ToString()
        {
            return String.Format("{0} {1} ({2}+{3}/{4})", ModName, MapName, NonSpectatorCount, SpectatorCount, MaxPlayers);
        }


        public object Clone()
        {
            var b = (Battle)MemberwiseClone();
            if (Details != null) b.Details = (BattleDetails)Details.Clone();
            if (Users != null) b.Users = new List<UserBattleStatus>(Users);
            if (Rectangles != null)
            {
                // copy the dictionary
                b.Rectangles = new Dictionary<int, BattleRect>();
                foreach (var kvp in Rectangles) b.Rectangles.Add(kvp.Key, kvp.Value);
            }

            if (DisabledUnits != null) b.DisabledUnits = new List<string>(DisabledUnits);
            return b;
        }

        public  BattleContext GetContext()
        {
            var ret = new BattleContext();
            ret.AutohostName = Founder.Name;
            ret.Map = MapName;
            ret.Mod = ModName;
            ret.Players = Users.Where(x=>x.SyncStatus != SyncStatuses.Unknown).Select(x => new PlayerTeam() { AllyID = x.AllyNumber, Name = x.Name, LobbyID = x.LobbyUser.LobbyID, TeamID = x.TeamNumber, IsSpectator = x.IsSpectator }).ToList();

            ret.Bots = Bots.Select(x => new BotTeam() { BotName = x.Name, AllyID = x.AllyNumber, TeamID = x.TeamNumber, Owner = x.owner, BotAI = x.aiLib }).ToList();
            return ret;
        }
    }
}