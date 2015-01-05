using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ZkData
{
    public class ContributionJar
    {
       
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
        public virtual ICollection<Contribution> Contributions { get; set; }
    }
}
