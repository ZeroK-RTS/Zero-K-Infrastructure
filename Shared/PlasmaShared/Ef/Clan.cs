namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Clan")]
    public partial class Clan
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Clan()
        {
            AccountRoles = new HashSet<AccountRole>();
            ForumThreads = new HashSet<ForumThread>();
            PlanetOwnerHistories = new HashSet<PlanetOwnerHistory>();
            Events = new HashSet<Event>();
        }

        public int ClanID { get; set; }

        [Required]
        [StringLength(50)]
        public string ClanName { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [StringLength(20)]
        public string Password { get; set; }

        [StringLength(500)]
        public string SecretTopic { get; set; }

        [Required]
        [StringLength(6)]
        public string Shortcut { get; set; }

        public int? ForumThreadID { get; set; }

        public bool IsDeleted { get; set; }

        public int? FactionID { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<AccountRole> AccountRoles { get; set; }

        public virtual Faction Faction { get; set; }

        public virtual ForumThread ForumThread { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ForumThread> ForumThreads { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PlanetOwnerHistory> PlanetOwnerHistories { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Event> Events { get; set; }
    }
}
