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
    // Poll
    public partial class Poll
    {
        public int PollID { get; set; } // PollID (Primary key)
        public string QuestionText { get; set; } // QuestionText
        public bool IsAnonymous { get; set; } // IsAnonymous
        public int? RoleTypeID { get; set; } // RoleTypeID
        public int? RoleTargetAccountID { get; set; } // RoleTargetAccountID
        public bool RoleIsRemoval { get; set; } // RoleIsRemoval
        public int? RestrictFactionID { get; set; } // RestrictFactionID
        public int? RestrictClanID { get; set; } // RestrictClanID
        public int? CreatedAccountID { get; set; } // CreatedAccountID
        public DateTime? ExpireBy { get; set; } // ExpireBy
        public bool IsHeadline { get; set; } // IsHeadline

        // Reverse navigation
        public virtual ICollection<PollOption> PollOptions { get; set; } // PollOption.FK_PollOption_Poll
        public virtual ICollection<PollVote> PollVotes { get; set; } // Many to many mapping

        // Foreign keys
        public virtual Account Account_CreatedAccountID { get; set; } // FK_Poll_Account1
        public virtual Account AccountByRoleTargetAccountID { get; set; } // FK_Poll_Account
        public virtual Faction Faction { get; set; } // FK_Poll_Faction
        public virtual RoleType RoleType { get; set; } // FK_Poll_RoleType

        public Poll()
        {
            IsAnonymous = false;
            RoleIsRemoval = false;
            IsHeadline = false;
            PollOptions = new List<PollOption>();
            PollVotes = new List<PollVote>();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
