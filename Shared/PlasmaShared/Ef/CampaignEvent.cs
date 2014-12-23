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
    // CampaignEvent
    public partial class CampaignEvent
    {
        public int AccountID { get; set; } // AccountID
        public int CampaignID { get; set; } // CampaignID
        public int EventID { get; set; } // EventID (Primary key)
        public int? PlanetID { get; set; } // PlanetID
        public string Text { get; set; } // Text
        public DateTime Time { get; set; } // Time
        public string PlainText { get; set; } // PlainText

        // Foreign keys
        public virtual Account Account { get; set; } // FK_CampaignEvent_Account
        public virtual CampaignPlanet CampaignPlanet { get; set; } // FK_CampaignEvent_CampaignPlanet
    }

}
