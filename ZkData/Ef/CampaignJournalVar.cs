using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZkData
{
    public partial class CampaignJournalVar
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int CampaignID { get; set; }

        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int JournalID { get; set; }

        [Key]
        [Column(Order = 2)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int RequiredVarID { get; set; }

        [Required]
        [StringLength(500)]
        public string RequiredValue { get; set; }

        public virtual Campaign Campaign { get; set; }

        public virtual CampaignJournal CampaignJournal { get; set; }

        public virtual CampaignVar CampaignVar { get; set; }
    }
}
