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
    // Rating
    public partial class Rating
    {
        public int RatingID { get; set; } // RatingID (Primary key)
        public int AccountID { get; set; } // AccountID
        public int? MissionID { get; set; } // MissionID
        public int? Rating_ { get; set; } // Rating
        public int? Difficulty { get; set; } // Difficulty

        // Foreign keys
        public virtual Account Account { get; set; } // FK_Rating_Account
        public virtual Mission Mission { get; set; } // FK_Rating_Mission
    }

}
