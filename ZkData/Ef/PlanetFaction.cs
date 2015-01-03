using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZkData
{
    public partial class PlanetFaction
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int PlanetID { get; set; }

        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int FactionID { get; set; }

        public double Influence { get; set; }

        public int Dropships { get; set; }

        public DateTime? DropshipsLastAdded { get; set; }

        public virtual Faction Faction { get; set; }

        public virtual Planet Planet { get; set; }
    }
}
