namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Contribution")]
    public partial class Contribution
    {
        public int ContributionID { get; set; }

        public int? AccountID { get; set; }

        public DateTime Time { get; set; }

        [StringLength(50)]
        public string PayPalTransactionID { get; set; }

        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(5)]
        public string OriginalCurrency { get; set; }

        public double? OriginalAmount { get; set; }

        public double? Euros { get; set; }

        public double? EurosNet { get; set; }

        public int KudosValue { get; set; }

        [StringLength(50)]
        public string ItemName { get; set; }

        [StringLength(50)]
        public string ItemCode { get; set; }

        [StringLength(50)]
        public string Email { get; set; }

        [StringLength(200)]
        public string Comment { get; set; }

        public int? PackID { get; set; }

        [StringLength(100)]
        public string RedeemCode { get; set; }

        public bool IsSpringContribution { get; set; }

        public int? ManuallyAddedAccountID { get; set; }

        public int? ContributionJarID { get; set; }

        public virtual Account AccountByAccountID { get; set; }

        public virtual Account AccountByManuallyAddedID { get; set; }

        public virtual ContributionJar ContributionJar { get; set; }
    }
}
