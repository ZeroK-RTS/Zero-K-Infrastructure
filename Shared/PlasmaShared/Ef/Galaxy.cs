namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Galaxy")]
    public partial class Galaxy
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Galaxy()
        {
            Links = new HashSet<Link>();
            Planets = new HashSet<Planet>();
        }

        public int GalaxyID { get; set; }

        public DateTime? Started { get; set; }

        public int Turn { get; set; }

        public DateTime? TurnStarted { get; set; }

        [StringLength(100)]
        public string ImageName { get; set; }

        public bool IsDirty { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public bool IsDefault { get; set; }

        public int AttackerSideCounter { get; set; }

        public DateTime? AttackerSideChangeTime { get; set; }

        [Column(TypeName = "text")]
        public string MatchMakerState { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Link> Links { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Planet> Planets { get; set; }
    }
}
