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
    // ForumThread
    public partial class ForumThread
    {
        public int ForumThreadID { get; set; } // ForumThreadID (Primary key)
        public string Title { get; set; } // Title
        public DateTime Created { get; set; } // Created
        public int? CreatedAccountID { get; set; } // CreatedAccountID
        public DateTime? LastPost { get; set; } // LastPost
        public int? LastPostAccountID { get; set; } // LastPostAccountID
        public int PostCount { get; set; } // PostCount
        public int ViewCount { get; set; } // ViewCount
        public bool IsLocked { get; set; } // IsLocked
        public int? ForumCategoryID { get; set; } // ForumCategoryID
        public bool IsPinned { get; set; } // IsPinned
        public int? RestrictedClanID { get; set; } // RestrictedClanID

        // Reverse navigation
        public virtual ICollection<Clan> Clans { get; set; } // Clan.FK_Clan_ForumThread
        public virtual ICollection<ForumThreadLastRead> ForumThreadLastReads { get; set; } // Many to many mapping
        public virtual ICollection<Mission> Missions { get; set; } // Mission.FK_Mission_ForumThread
        public virtual ICollection<News> News { get; set; } // News.FK_News_ForumThread
        public virtual ICollection<Planet> Planets { get; set; } // Planet.FK_Planet_ForumThread
        public virtual ICollection<Resource> Resources { get; set; } // Resource.FK_Resource_ForumThread
        public virtual ICollection<SpringBattle> SpringBattles { get; set; } // SpringBattle.FK_SpringBattle_ForumThread
        public virtual ICollection<ForumPost> ForumPosts { get; set; }

        // Foreign keys
        public virtual Account Account_CreatedAccountID { get; set; } // FK_ForumThread_Account
        public virtual Account Account_LastPostAccountID { get; set; } // FK_ForumThread_Account1
        public virtual Clan Clan { get; set; } // FK_ForumThread_Clan
        public virtual ForumCategory ForumCategory { get; set; } // FK_ForumThread_ForumCategory

        public ForumThread()
        {
            PostCount = 0;
            ViewCount = 0;
            IsLocked = false;
            IsPinned = false;
            Clans = new List<Clan>();
            ForumThreadLastReads = new List<ForumThreadLastRead>();
            Missions = new List<Mission>();
            News = new List<News>();
            Planets = new List<Planet>();
            Resources = new List<Resource>();
            SpringBattles = new List<SpringBattle>();
            ForumPosts = new List<ForumPost>();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
