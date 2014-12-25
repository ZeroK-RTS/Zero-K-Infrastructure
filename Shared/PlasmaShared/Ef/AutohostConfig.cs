// ReSharper disable RedundantUsingDirective
// ReSharper disable DoNotCallOverridableMethodsInConstructor
// ReSharper disable InconsistentNaming
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable PartialMethodWithSinglePart
// ReSharper disable RedundantNameQualifier

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
//using DatabaseGeneratedOption = System.ComponentModel.DataAnnotations.DatabaseGeneratedOption;

namespace ZkData
{
    // AutohostConfig
    public partial class AutohostConfig
    {
        public int AutohostConfigID { get; set; } // AutohostConfigID (Primary key)
        public string ClusterNode { get; set; } // ClusterNode
        public string Login { get; set; } // Login
        public string Password { get; set; } // Password
        public int MaxPlayers { get; set; } // MaxPlayers
        public string Welcome { get; set; } // Welcome
        public bool AutoSpawn { get; set; } // AutoSpawn
        public string AutoUpdateRapidTag { get; set; } // AutoUpdateRapidTag
        public string SpringVersion { get; set; } // SpringVersion
        public string AutoUpdateSpringBranch { get; set; } // AutoUpdateSpringBranch
        public string CommandLevels { get; set; } // CommandLevels
        public string Map { get; set; } // Map
        public string Mod { get; set; } // Mod
        public string Title { get; set; } // Title
        public string JoinChannels { get; set; } // JoinChannels
        public string BattlePassword { get; set; } // BattlePassword
        public AutohostMode AutohostMode { get; set; } // AutohostMode
        public int? MinToStart { get; set; } // MinToStart
        public int? MaxToStart { get; set; } // MaxToStart
        public int? MinToJuggle { get; set; } // MinToJuggle
        public int? MaxToJuggle { get; set; } // MaxToJuggle
        public int? SplitBiggerThan { get; set; } // SplitBiggerThan
        public int? MergeSmallerThan { get; set; } // MergeSmallerThan
        public int? MaxEloDifference { get; set; } // MaxEloDifference
        public bool? DontMoveManuallyJoined { get; set; } // DontMoveManuallyJoined
        public int? MinLevel { get; set; } // MinLevel
        public int? MinElo { get; set; } // MinElo
        public int? MaxLevel { get; set; } // MaxLevel
        public int? MaxElo { get; set; } // MaxElo
        public bool? IsTrollHost { get; set; } // IsTrollHost

        public AutohostConfig()
        {
            Password = "N'*'";
            MaxPlayers = 32;
            Welcome = "N'Hi %1 (rights:%2), welcome to %3, automated host. For help say !help'";
            AutoSpawn = true;
            AutohostMode = 0;
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
