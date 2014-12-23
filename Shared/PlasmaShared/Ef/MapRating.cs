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
    // MapRating
    public partial class MapRating
    {
        public int ResourceID { get; set; } // ResourceID (Primary key)
        public int AccountID { get; set; } // AccountID (Primary key)
        public int Rating { get; set; } // Rating

        // Foreign keys
        public virtual Account Account { get; set; } // FK_ResourceRating_Account
        public virtual Resource Resource { get; set; } // FK_ResourceRating_Resource
    }

}
