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
    // CampaignPlanetVar
    public partial class CampaignPlanetVar
    {
        public int CampaignID { get; set; } // CampaignID (Primary key)
        public int PlanetID { get; set; } // PlanetID (Primary key)
        public int RequiredVarID { get; set; } // RequiredVarID (Primary key)
        public string RequiredValue { get; set; } // RequiredValue

        // Foreign keys
        public virtual CampaignVar CampaignVar { get; set; } // FK_CampaignPlanetVar_CampaignVar
    }

}
