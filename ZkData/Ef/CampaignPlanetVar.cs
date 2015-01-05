using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZkData
{
    public class CampaignPlanetVar
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int CampaignID { get; set; }
        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int PlanetID { get; set; }
        [Key]
        [Column(Order = 2)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int RequiredVarID { get; set; }
        [Required]
        [StringLength(500)]
        public string RequiredValue { get; set; }

        public virtual Campaign Campaign { get; set; }
        public virtual CampaignPlanet CampaignPlanet { get; set; }
        public virtual CampaignVar CampaignVar { get; set; }
    }
}
