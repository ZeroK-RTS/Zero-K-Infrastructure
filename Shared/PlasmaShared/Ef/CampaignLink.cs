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
    // CampaignLink
    public partial class CampaignLink
    {
        public int PlanetToUnlockID { get; set; } // PlanetToUnlockID (Primary key)
        public int UnlockingPlanetID { get; set; } // UnlockingPlanetID (Primary key)
        public int CampaignID { get; set; } // CampaignID

        // Foreign keys
        public virtual Campaign Campaign { get; set; } // FK_CampaignLink_Campaign
        public virtual CampaignPlanet CampaignPlanet_CampaignID { get; set; } // FK_CampaignLink_CampaignPlanet
        public virtual CampaignPlanet CampaignPlanet1 { get; set; } // FK_CampaignLink_CampaignPlanet1
    }

}
