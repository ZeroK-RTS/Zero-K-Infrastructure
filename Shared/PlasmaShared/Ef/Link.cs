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
    // Link
    public partial class Link
    {
        public int PlanetID1 { get; set; } // PlanetID1 (Primary key)
        public int PlanetID2 { get; set; } // PlanetID2 (Primary key)
        public int GalaxyID { get; set; } // GalaxyID

        // Foreign keys
        public virtual Galaxy Galaxy { get; set; } // FK_Link_Galaxy
        public virtual Planet Planet_PlanetID1 { get; set; } // FK_Link_Planet
        public virtual Planet Planet_PlanetID2 { get; set; } // FK_Link_Planet1
    }

}
