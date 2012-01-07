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
        public bool AutoSpawnClones;
        public string AutoUpdateRapidTag;
        public string SpringVersion;
        public string AutoUpdateSpringBranch;
        public AutohostMode Mode;
        public CommandLevel[] CommandLevels;
        public AhConfig() {}


        public AhConfig(AutohostConfig db) {
            Login = db.Login;
            Password = db.Password;
            JoinChannels = (db.JoinChannels + "").Split('\n').Where(x=>!string.IsNullOrEmpty(x)).ToArray();
            Title = db.Title;
            Welcome = db.Welcome;
            Map = db.Map;
            Mod = db.Mod;
            MaxPlayers = db.MaxPlayers;
            AutoSpawnClones = db.AutoSpawn;
            AutoUpdateRapidTag = db.AutoUpdateRapidTag;
            SpringVersion = db.SpringVersion;
            AutoUpdateSpringBranch = db.AutoUpdateSpringBranch;
            Mode = db.AutohostMode;
            CommandLevels = (db.CommandLevels + "").Split('\n').Where(x => !string.IsNullOrEmpty(x)).Select(x =>
                { 
                    var parts = x.Split('=');
                    return new CommandLevel() { Command = parts[0], Level = int.Parse(parts[1]) };
                }).ToArray();
        }
    }

    public class CommandLevel {
        public string Command;
        public int Level;
    }

}