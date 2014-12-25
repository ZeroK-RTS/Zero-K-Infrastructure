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
    // Contribution
    public partial class Contribution
    {
        public int ContributionID { get; set; } // ContributionID (Primary key)
        public int? AccountID { get; set; } // AccountID
        public DateTime Time { get; set; } // Time
        public string PayPalTransactionID { get; set; } // PayPalTransactionID
        public string Name { get; set; } // Name
        public string OriginalCurrency { get; set; } // OriginalCurrency
        public double? OriginalAmount { get; set; } // OriginalAmount
        public double? Euros { get; set; } // Euros
        public double? EurosNet { get; set; } // EurosNet
        public int KudosValue { get; set; } // KudosValue
        public string ItemName { get; set; } // ItemName
        public string ItemCode { get; set; } // ItemCode
        public string Email { get; set; } // Email
        public string Comment { get; set; } // Comment
        public int? PackID { get; set; } // PackID
        public string RedeemCode { get; set; } // RedeemCode
        public bool IsSpringContribution { get; set; } // IsSpringContribution
        public int? ManuallyAddedAccountID { get; set; } // ManuallyAddedAccountID
        public int? ContributionJarID { get; set; } // ContributionJarID

        // Foreign keys
        public virtual Account AccountByAccountID { get; set; } // FK_Contribution_Account
        public virtual Account Account_ManuallyAddedAccountID { get; set; } // FK_Contribution_Account1
        public virtual ContributionJar ContributionJar { get; set; } // FK_Contribution_ContributionJar

        public Contribution()
        {
            IsSpringContribution = true;
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
