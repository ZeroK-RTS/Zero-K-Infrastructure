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

namespace PlasmaShared.Ef
{
    // RoleType
    public partial class RoleType
    {
        public int RoleTypeID { get; set; } // RoleTypeID (Primary key)
        public string Name { get; set; } // Name
        public string Description { get; set; } // Description
        public bool IsClanOnly { get; set; } // IsClanOnly
        public bool IsOnePersonOnly { get; set; } // IsOnePersonOnly
        public int? RestrictFactionID { get; set; } // RestrictFactionID
        public bool IsVoteable { get; set; } // IsVoteable
        public int PollDurationDays { get; set; } // PollDurationDays
        public double RightDropshipQuota { get; set; } // RightDropshipQuota
        public double RightBomberQuota { get; set; } // RightBomberQuota
        public double RightMetalQuota { get; set; } // RightMetalQuota
        public double RightWarpQuota { get; set; } // RightWarpQuota
        public bool RightDiplomacy { get; set; } // RightDiplomacy
        public bool RightEditTexts { get; set; } // RightEditTexts
        public bool RightSetEnergyPriority { get; set; } // RightSetEnergyPriority
        public bool RightKickPeople { get; set; } // RightKickPeople
        public int DisplayOrder { get; set; } // DisplayOrder

        // Reverse navigation
        public virtual ICollection<AccountRole> AccountRoles { get; set; } // Many to many mapping
        public virtual ICollection<Poll> Polls { get; set; } // Poll.FK_Poll_RoleType
        public virtual ICollection<RoleTypeHierarchy> RoleTypeHierarchies_MasterRoleTypeID { get; set; } // Many to many mapping
        public virtual ICollection<RoleTypeHierarchy> RoleTypeHierarchies_SlaveRoleTypeID { get; set; } // Many to many mapping

        // Foreign keys
        public virtual Faction Faction { get; set; } // FK_RoleType_Faction

        public RoleType()
        {
            IsClanOnly = false;
            IsOnePersonOnly = false;
            IsVoteable = true;
            PollDurationDays = 1;
            RightDropshipQuota = 0;
            RightBomberQuota = 0;
            RightMetalQuota = 0;
            RightWarpQuota = 0;
            RightDiplomacy = false;
            RightEditTexts = false;
            RightSetEnergyPriority = false;
            RightKickPeople = false;
            DisplayOrder = 0;
            AccountRoles = new List<AccountRole>();
            Polls = new List<Poll>();
            RoleTypeHierarchies_MasterRoleTypeID = new List<RoleTypeHierarchy>();
            RoleTypeHierarchies_SlaveRoleTypeID = new List<RoleTypeHierarchy>();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
