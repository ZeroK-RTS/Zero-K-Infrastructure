namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("RoleType")]
    public partial class RoleType
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public RoleType()
        {
            AccountRoles = new HashSet<AccountRole>();
            Polls = new HashSet<Poll>();
            RoleTypeHierarchies = new HashSet<RoleTypeHierarchy>();
            RoleTypeHierarchies1 = new HashSet<RoleTypeHierarchy>();
        }

        public int RoleTypeID { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; }

        public bool IsClanOnly { get; set; }

        public bool IsOnePersonOnly { get; set; }

        public int? RestrictFactionID { get; set; }

        public bool IsVoteable { get; set; }

        public int PollDurationDays { get; set; }

        public double RightDropshipQuota { get; set; }

        public double RightBomberQuota { get; set; }

        public double RightMetalQuota { get; set; }

        public double RightWarpQuota { get; set; }

        public bool RightDiplomacy { get; set; }

        public bool RightEditTexts { get; set; }

        public bool RightSetEnergyPriority { get; set; }

        public bool RightKickPeople { get; set; }

        public int DisplayOrder { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<AccountRole> AccountRoles { get; set; }

        public virtual Faction Faction { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Poll> Polls { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<RoleTypeHierarchy> RoleTypeHierarchies { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<RoleTypeHierarchy> RoleTypeHierarchies1 { get; set; }
    }
}
