using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZkData
{
    public partial class AccountCampaignJournalProgress
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int AccountID { get; set; }

        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int CampaignID { get; set; }

        [Key]
        [Column(Order = 2)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int JournalID { get; set; }

        public bool IsUnlocked { get; set; }

        public virtual Account Account { get; set; }

        public virtual Campaign Campaign { get; set; }

        public virtual CampaignJournal CampaignJournal { get; set; }
    }
}
