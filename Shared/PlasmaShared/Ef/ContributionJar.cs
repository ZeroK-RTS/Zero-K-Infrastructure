namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("ContributionJar")]
    public partial class ContributionJar
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public ContributionJar()
        {
            Contributions = new HashSet<Contribution>();
        }

        public int ContributionJarID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public int GuarantorAccountID { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public double TargetGrossEuros { get; set; }

        public bool IsDefault { get; set; }

        public virtual Account Account { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Contribution> Contributions { get; set; }
    }
}
