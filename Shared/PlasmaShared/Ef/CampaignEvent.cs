namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("CampaignEvent")]
    public partial class CampaignEvent
    {
        public int AccountID { get; set; }

        public int CampaignID { get; set; }

        [Key]
        public int EventID { get; set; }

        public int? PlanetID { get; set; }

        [Required]
        [StringLength(4000)]
        public string Text { get; set; }

        public DateTime Time { get; set; }

        [StringLength(4000)]
        public string PlainText { get; set; }

        public virtual Account Account { get; set; }

        public virtual Campaign Campaign { get; set; }

        public virtual CampaignPlanet CampaignPlanet { get; set; }
    }
}
