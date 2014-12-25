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
    // PollVote
    public partial class PollVote
    {
        public int AccountID { get; set; } // AccountID (Primary key)
        public int PollID { get; set; } // PollID (Primary key)
        public int OptionID { get; set; } // OptionID

        // Foreign keys
        public virtual Account Account { get; set; } // FK_PollVote_Account
        public virtual Poll Poll { get; set; } // FK_PollVote_Poll
        public virtual PollOption PollOption { get; set; } // FK_PollVote_PollOption
    }

}
