using System.ComponentModel.DataAnnotations;
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
    // Clan
    public partial class Clan:IValidatableObject
    {
        public int ClanID { get; set; } // ClanID (Primary key)
        public string ClanName { get; set; } // ClanName
        public string Description { get; set; } // Description
        public string Password { get; set; } // Password
        public string SecretTopic { get; set; } // SecretTopic
        public string Shortcut { get; set; } // Shortcut
        public int? ForumThreadID { get; set; } // ForumThreadID
        public bool IsDeleted { get; set; } // IsDeleted
        public int? FactionID { get; set; } // FactionID

        // Reverse navigation
        public virtual ICollection<AccountRole> AccountRoles { get; set; } // AccountRole.FK_AccountRole_Clan
        public virtual ICollection<Event> Events { get; set; } // Many to many mapping
        public virtual ICollection<ForumThread> ForumThreads { get; set; } // ForumThread.FK_ForumThread_Clan
        public virtual ICollection<PlanetOwnerHistory> PlanetOwnerHistories { get; set; } // PlanetOwnerHistory.FK_PlanetOwnerHistory_Clan
        public virtual ICollection<Account> Accounts { get; set; } 

        // Foreign keys
        public virtual Faction Faction { get; set; } // FK_Clan_Faction
        public virtual ForumThread ForumThread { get; set; } // FK_Clan_ForumThread

        public Clan()
        {
            IsDeleted = false;
            FactionID = 6;
            AccountRoles = new List<AccountRole>();
            ForumThreads = new List<ForumThread>();
            PlanetOwnerHistories = new List<PlanetOwnerHistory>();
            Events = new List<Event>();
            Accounts = new List<Account>();
            InitializePartial();
        }
        partial void InitializePartial();
        
        
        
    }

}
