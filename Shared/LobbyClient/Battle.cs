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

        public string FounderName { get; set; }


        public bool IsInGame { get; set; }
        public bool IsMission { get { return false; } }
        public bool IsPassworded { get { return !string.IsNullOrEmpty(Password); } }

        public string MapName { get; set; }

        public int MaxPlayers { get; set; }

        public string ModName { get; set; }
        public Dictionary<string, string> ModOptions { get; set; }

        public int NonSpectatorCount { get { return Users.Count - SpectatorCount; } }

        public string Password;

        public string EngineVersion { get; set; }
        public int SpectatorCount { get; set; }
        public string Title { get; set; }
        public AutohostMode Mode { get; set; }

        public DateTime? RunningSince { get; set; }

        public bool IsMatchMakerBattle { get; protected set; }


        public ConcurrentDictionary<string, UserBattleStatus> Users { get; set; }


        public Battle()
        {
            Bots = new ConcurrentDictionary<string, BotBattleStatus>();
            ModOptions = new Dictionary<string, string>();
            Users = new ConcurrentDictionary<string, UserBattleStatus>();
        }

       
        
        public virtual void UpdateWith(BattleHeader h)
        {
            if (h.BattleID != null) BattleID = h.BattleID.Value;
            if (h.Founder != null) FounderName = h.Founder;
            if (h.MaxPlayers != null) MaxPlayers = h.MaxPlayers.Value;
            if (!string.IsNullOrEmpty(h.Password)) Password = h.Password;
            if (h.Engine != null) EngineVersion = h.Engine;
            if (h.Map != null) MapName = h.Map;
            if (h.Title != null) Title = h.Title;
            if (h.Game != null) ModName = h.Game;
            if (h.SpectatorCount != null) SpectatorCount = h.SpectatorCount.Value;
            if (h.Mode != null) Mode = h.Mode.Value;
            if (h.RunningSince != null) RunningSince = h.RunningSince;
            if (h.IsRunning != null) IsInGame = h.IsRunning.Value;
            if (h.IsMatchMaker != null) IsMatchMakerBattle = h.IsMatchMaker.Value;
        }

        public virtual BattleHeader GetHeader()
        {
            var b = this;
            return new BattleHeader()
            {
                BattleID = b.BattleID,
                Engine = b.EngineVersion,
                Game = b.ModName,
                Founder = b.FounderName,
                Map = b.MapName,
                Title = b.Title,
                SpectatorCount = b.SpectatorCount,
                MaxPlayers = b.MaxPlayers,
                Password = b.Password != null ? "?" : null,
                Mode = b.Mode,
                IsRunning = b.IsInGame,
                RunningSince = b.IsInGame ? b.RunningSince : null,
                IsMatchMaker = b.IsMatchMakerBattle
            };
        }


        public Battle(string engineVersion, string password, int port, int maxplayers, string mapName, string title, string modname)
            : this()
        {
            if (!String.IsNullOrEmpty(password)) Password = password;

            EngineVersion = engineVersion;
            MaxPlayers = maxplayers;
            MapName = mapName;
            Title = title;
            ModName = modname;
        }


        public override string ToString()
        {
            return $"{ModName} {MapName} ({NonSpectatorCount}+{SpectatorCount}/{MaxPlayers})";
        }



        public LobbyHostingContext GetContext()
        {
            var ret = new LobbyHostingContext();
            ret.FounderName = FounderName;
            ret.Map = MapName;
            ret.Mod = ModName;
            ret.Title = Title;
            ret.EngineVersion = EngineVersion;
            ret.IsMission = IsMission;
            ret.Players = Users.Values.Select(x => x.ToPlayerTeam()).ToList();
            ret.Bots = Bots.Values.Select(x => x.ToBotTeam()).ToList();
            ret.ModOptions = new Dictionary<string, string>(ModOptions);
            ret.Mode = Mode;
            ret.IsMatchMakerGame = IsMatchMakerBattle;
            ret.BattleID = BattleID;
            return ret;
        }

        public Battle Clone()
        {
            var clone = (Battle)this.MemberwiseClone();
            clone.Users = new ConcurrentDictionary<string, UserBattleStatus>(this.Users);
            clone.Bots = new ConcurrentDictionary<string, BotBattleStatus>(this.Bots);
            clone.ModOptions = new Dictionary<string, string>(ModOptions);
            return clone;
        }
    }
}
