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
    // SpringBattle
    public partial class SpringBattle
    {
        public int SpringBattleID { get; set; } // SpringBattleID (Primary key)
        public string EngineGameID { get; set; } // EngineGameID
        public int HostAccountID { get; set; } // HostAccountID
        public string Title { get; set; } // Title
        public int MapResourceID { get; set; } // MapResourceID
        public int ModResourceID { get; set; } // ModResourceID
        public DateTime StartTime { get; set; } // StartTime
        public int Duration { get; set; } // Duration
        public int PlayerCount { get; set; } // PlayerCount
        public bool HasBots { get; set; } // HasBots
        public bool IsMission { get; set; } // IsMission
        public string ReplayFileName { get; set; } // ReplayFileName
        public string EngineVersion { get; set; } // EngineVersion
        public bool IsEloProcessed { get; set; } // IsEloProcessed
        public int? WinnerTeamXpChange { get; set; } // WinnerTeamXpChange
        public int? LoserTeamXpChange { get; set; } // LoserTeamXpChange
        public int? ForumThreadID { get; set; } // ForumThreadID
        public string TeamsTitle { get; set; } // TeamsTitle
        public bool IsFfa { get; set; } // IsFfa
        public int? RatingPollID { get; set; } // RatingPollID

        // Reverse navigation
        public virtual ICollection<AccountBattleAward> AccountBattleAwards { get; set; } // Many to many mapping
        public virtual ICollection<Event> Events { get; set; } // Many to many mapping
        public virtual ICollection<SpringBattlePlayer> SpringBattlePlayers { get; set; } // Many to many mapping

        // Foreign keys
        public virtual Account Account { get; set; } // FK_SpringBattle_Account
        public virtual ForumThread ForumThread { get; set; } // FK_SpringBattle_ForumThread
        public virtual RatingPoll RatingPoll { get; set; } // FK_SpringBattle_RatingPoll

        [ForeignKey("MapResourceID")]
        public virtual Resource ResourceByMapResourceID { get; set; }
        
        [ForeignKey("ModResourceID")]
        public virtual Resource ResourceByModResourceID { get; set; }

        public SpringBattle()
        {
            HasBots = false;
            IsMission = false;
            IsEloProcessed = false;
            IsFfa = false;
            AccountBattleAwards = new List<AccountBattleAward>();
            SpringBattlePlayers = new List<SpringBattlePlayer>();
            Events = new List<Event>();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
