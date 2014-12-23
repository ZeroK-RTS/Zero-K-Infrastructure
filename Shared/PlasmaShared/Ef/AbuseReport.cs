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
    // AbuseReport
    public partial class AbuseReport
    {
        public int AbuseReportID { get; set; } // AbuseReportID (Primary key)
        public int AccountID { get; set; } // AccountID
        public int ReporterAccountID { get; set; } // ReporterAccountID
        public DateTime Time { get; set; } // Time
        public string Text { get; set; } // Text

        // Foreign keys
        public virtual Account Account_AccountID { get; set; } // FK_AbuseReport_Account
        public virtual Account Account_ReporterAccountID { get; set; } // FK_AbuseReport_Account1
    }

}
