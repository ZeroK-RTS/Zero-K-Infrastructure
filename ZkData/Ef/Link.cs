using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZkData
{
    public class Link
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int PlanetID1 { get; set; }
        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int PlanetID2 { get; set; }
        public int GalaxyID { get; set; }

        public virtual Galaxy Galaxy { get; set; }
        public virtual Planet PlanetByPlanetID1 { get; set; }
        public virtual Planet PlanetByPlanetID2 { get; set; }
    }
}
