namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("PlanetStructure")]
    public partial class PlanetStructure
    {
        [Key]
        [Column(Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int PlanetID { get; set; }

        [Key]
        [Column(Order = 1)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int StructureTypeID { get; set; }

        public int? OwnerAccountID { get; set; }

        public int? ActivatedOnTurn { get; set; }

        public EnergyPriority EnergyPriority { get; set; }

        public bool IsActive { get; set; }

        public int? TargetPlanetID { get; set; }

        public virtual Account Account { get; set; }

        public virtual Planet Planet { get; set; }

        public virtual Planet PlanetByTargetPlanetID { get; set; }

        public virtual StructureType StructureType { get; set; }
    }
}
