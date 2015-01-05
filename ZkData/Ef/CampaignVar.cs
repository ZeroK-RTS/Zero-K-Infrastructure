using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZkData
{
    public class CampaignVar
    {
        
        public CampaignVar()
        {
            AccountCampaignVars = new HashSet<AccountCampaignVar>();
            CampaignJournalVars = new HashSet<CampaignJournalVar>();
            CampaignPlanetVars = new HashSet<CampaignPlanetVar>();
        }

        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int CampaignID { get; set; }
        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int VarID { get; set; }
        [Required]
        [StringLength(50)]
        public string KeyString { get; set; }
        [StringLength(4000)]
        public string Description { get; set; }

        public virtual ICollection<AccountCampaignVar> AccountCampaignVars { get; set; }
        public virtual Campaign Campaign { get; set; }
        public virtual ICollection<CampaignJournalVar> CampaignJournalVars { get; set; }
        public virtual ICollection<CampaignPlanetVar> CampaignPlanetVars { get; set; }
    }
}
