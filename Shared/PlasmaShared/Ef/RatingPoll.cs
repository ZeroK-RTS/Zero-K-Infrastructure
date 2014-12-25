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
    // RatingPoll
    public partial class RatingPoll
    {
        public int RatingPollID { get; set; } // RatingPollID (Primary key)
        public double? Average { get; set; } // Average
        public int Count { get; set; } // Count

        // Reverse navigation
        public virtual ICollection<AccountRatingVote> AccountRatingVotes { get; set; } // Many to many mapping
        public virtual ICollection<Mission> Missions_DifficultyRatingPollID { get; set; } // Mission.FK_Mission_RatingPoll1
        public virtual ICollection<Mission> Missions_RatingPollID { get; set; } // Mission.FK_Mission_RatingPoll
        public virtual ICollection<Resource> Resources { get; set; } // Resource.FK_Resource_RatingPoll
        public virtual ICollection<SpringBattle> SpringBattles { get; set; } // SpringBattle.FK_SpringBattle_RatingPoll

        public RatingPoll()
        {
            Count = 0;
            AccountRatingVotes = new List<AccountRatingVote>();
            Missions_DifficultyRatingPollID = new List<Mission>();
            Missions_RatingPollID = new List<Mission>();
            Resources = new List<Resource>();
            SpringBattles = new List<SpringBattle>();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
