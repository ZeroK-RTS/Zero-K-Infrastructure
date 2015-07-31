using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using PlasmaShared;
using ZkData;
using ZkData.UnitSyncLib;

namespace LobbyClient
{
    public class Battle
    {
        public int BattleID { get; set; }
        public ConcurrentDictionary<string, BotBattleStatus> Bots { get; set; }

        public User Founder
        {
            get { return getUser(FounderName); }
        }
        public string FounderName { get; private set; }

        public int HostPort { get; set; }
        public string Ip { get; set; }

        public bool IsInGame { get { return Founder.IsInGame; } }
        public bool IsMission { get { return false; } }
        public bool IsPassworded { get { return !string.IsNullOrEmpty(Password); } }

        public string MapName { get; set; }

        public int MaxPlayers { get; set; }

        public string ModName { get; set; }
        public Dictionary<string, string> ModOptions { get; set; }

        public int NonSpectatorCount { get { return Users.Count - SpectatorCount; } }

        public string Password;

        public ConcurrentDictionary<int, BattleRect> Rectangles { get; set; }
        public string EngineName = "spring";
        public string EngineVersion { get; set; }
        public int SpectatorCount { get; set; }
        public string Title { get; set; }

        public ConcurrentDictionary<string, UserBattleStatus> Users { get; set; }


        public bool IsSpringieManaged
        {
            get { return Founder != null && Founder.ClientType == Login.ClientTypes.SpringieManaged; }
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

        public Battle()
        {
            Bots = new ConcurrentDictionary<string, BotBattleStatus>();
            ModOptions = new Dictionary<string, string>();
            Rectangles = new ConcurrentDictionary<int, BattleRect>();
            Users = new ConcurrentDictionary<string, UserBattleStatus>();
        }

        Func<string, User> getUser;
        
        
        public void UpdateWith(BattleHeader h, Func<string, User> getUser)
        {
            this.getUser = getUser;
            if (h.BattleID != null) BattleID = h.BattleID.Value;
            if (h.Founder != null) FounderName = h.Founder;
            if (h.Ip != null) Ip = h.Ip;
            if (h.Port != null) HostPort = h.Port.Value;
            if (h.MaxPlayers != null) MaxPlayers = h.MaxPlayers.Value;
            if (h.Password != null) Password = h.Password;
            if (h.Engine != null) EngineVersion = h.Engine;
            if (h.Map != null) MapName = h.Map;
            if (h.Title != null) Title = h.Title;
            if (h.Game != null) ModName = h.Game;
            if (h.SpectatorCount != null) SpectatorCount = h.SpectatorCount.Value;
        }


        public Battle(string engineVersion, string password, int port, int maxplayers, string mapName, string title, string modname)
            : this()
        {
            if (!String.IsNullOrEmpty(password)) Password = password;
            if (port == 0) HostPort = 8452; else HostPort = port;
            try
            {
                var ports = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners().OrderBy(x => x.Port).Select(x => x.Port).ToList();
                if (ports.Contains(HostPort))
                {
                    var blockedPort = HostPort;
                    while (ports.Contains(HostPort)) HostPort++;
                    Trace.TraceWarning("Host port {0} was used, using backup port {1}", blockedPort, HostPort);
                }
            }
            catch { }

            EngineVersion = engineVersion;
            MaxPlayers = maxplayers;
            MapName = mapName;
            Title = title;
            ModName = modname;
        }


     


        public int GetFirstEmptyRectangle()
        {
            for (var i = 0; i < Spring.MaxAllies; ++i) if (!Rectangles.ContainsKey(i)) return i;
            return -1;
        }

        public int GetFreeTeamID(string exceptUser)
        {
            return
                Enumerable.Range(0, TasClient.MaxTeams - 1).FirstOrDefault(
                    teamID =>
                    !Users.Values.Where(u => !u.IsSpectator).Any(user => user.Name != exceptUser && user.TeamNumber == teamID) &&
                    !Bots.Values.Any(x => x.TeamNumber == teamID));
        }



        public override string ToString()
        {
            return String.Format("{0} {1} ({2}+{3}/{4})", ModName, MapName, NonSpectatorCount, SpectatorCount, MaxPlayers);
        }



        public BattleContext GetContext()
        {
            var ret = new BattleContext();
            ret.AutohostName = Founder.Name;
            ret.Map = MapName;
            ret.Mod = ModName;
            ret.Title = Title;
            ret.EngineVersion = EngineVersion;
            ret.IsMission = IsMission;
            ret.Players = Users.Values.Where(x => x.SyncStatus != SyncStatuses.Unknown).Select(x => x.ToPlayerTeam()).ToList();
            ret.Bots = Bots.Values.Select(x => x.ToBotTeam()).ToList();
            return ret;
        }

        public Battle Clone()
        {
            var clone = (Battle)this.MemberwiseClone();
            clone.Users = new ConcurrentDictionary<string, UserBattleStatus>(this.Users);
            clone.Bots = new ConcurrentDictionary<string, BotBattleStatus>(this.Bots);
            clone.Rectangles = new ConcurrentDictionary<int, BattleRect>(this.Rectangles);
            clone.ModOptions = new Dictionary<string, string>(ModOptions);
            return clone;
        }
    }
}
