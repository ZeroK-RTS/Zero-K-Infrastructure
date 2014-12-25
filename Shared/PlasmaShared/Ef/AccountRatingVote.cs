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
    // AccountRatingVote
    public partial class AccountRatingVote
    {
        public int RatingPollID { get; set; } // RatingPollID (Primary key)
        public int AccountID { get; set; } // AccountID (Primary key)
        public int Vote { get; set; } // Vote

        // Foreign keys
        public virtual Account Account { get; set; } // FK_AccountRatingVote_Account
        public virtual RatingPoll RatingPoll { get; set; } // FK_AccountRatingVote_RatingPoll
    }

}
