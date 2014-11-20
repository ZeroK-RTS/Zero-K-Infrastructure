using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ZkData;

namespace ZeroKWeb.SpringieInterface
{
    public class AhConfig {
        public string Login;
        public string Password;
        public string[] JoinChannels;
        public string Title;
        public string Welcome;
        public string Map;
        public string Mod;
        public int MaxPlayers;
        public int? SplitBiggerThan;
        public bool AutoSpawnClones;
        public string AutoUpdateRapidTag;
        public string SpringVersion;
        public string AutoUpdateSpringBranch;
        public string BattlePassword;
        public AutohostMode Mode;
        public CommandLevel[] CommandLevels;
        int? MaxEloDifference;
        int? MinToJuggle;
        int? MaxToJuggle;
        public AhConfig() {}


        public AhConfig(AutohostConfig db)
        {
            Login = db.Login;
            Password = db.Password;
            JoinChannels = (db.JoinChannels + "").Split('\n').Where(x => !string.IsNullOrEmpty(x)).ToArray();
            Title = db.Title;
            Welcome = db.Welcome;
            Map = db.Map;
            Mod = db.Mod;
            MaxPlayers = db.MaxPlayers;
            AutoSpawnClones = db.AutoSpawn;
            AutoUpdateRapidTag = db.AutoUpdateRapidTag;
            SpringVersion = db.SpringVersion;
            SplitBiggerThan = db.SplitBiggerThan;
            AutoUpdateSpringBranch = db.AutoUpdateSpringBranch;
            Mode = db.AutohostMode;
            BattlePassword = db.BattlePassword;
            CommandLevels = (db.CommandLevels + "").Split('\n').Where(x => !string.IsNullOrEmpty(x)).Select(x =>
            {
                var parts = x.Split('=');
                return new CommandLevel() { Command = parts[0], Level = int.Parse(parts[1]) };
            }).ToArray();
            MaxEloDifference = db.MaxEloDifference;
            MinToJuggle = db.MinToJuggle;
            MaxToJuggle = db.MaxToJuggle;
        }
    }

    public class CommandLevel {
        public string Command;
        public int Level;
    }

}