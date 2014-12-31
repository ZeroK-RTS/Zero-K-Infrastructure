namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Planet")]
    public partial class Planet
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Planet()
        {
            AccountPlanets = new HashSet<AccountPlanet>();
            Links = new HashSet<Link>();
            Links1 = new HashSet<Link>();
            MarketOffers = new HashSet<MarketOffer>();
            PlanetFactions = new HashSet<PlanetFaction>();
            PlanetOwnerHistories = new HashSet<PlanetOwnerHistory>();
            PlanetStructures = new HashSet<PlanetStructure>();
            PlanetStructures1 = new HashSet<PlanetStructure>();
            TreatyEffects = new HashSet<TreatyEffect>();
            Events = new HashSet<Event>();
        }

        public int PlanetID { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        public double X { get; set; }

        public double Y { get; set; }

        public int? MapResourceID { get; set; }

        public int? OwnerAccountID { get; set; }

        public int GalaxyID { get; set; }

        public int? ForumThreadID { get; set; }

        public int? OwnerFactionID { get; set; }

        public int TeamSize { get; set; }

        public virtual Account Account { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<AccountPlanet> AccountPlanets { get; set; }

        public virtual Faction Faction { get; set; }

        public virtual ForumThread ForumThread { get; set; }

        public virtual Galaxy Galaxy { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Link> Links { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Link> Links1 { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<MarketOffer> MarketOffers { get; set; }

        public virtual Resource Resource { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PlanetFaction> PlanetFactions { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PlanetOwnerHistory> PlanetOwnerHistories { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PlanetStructure> PlanetStructures { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PlanetStructure> PlanetStructures1 { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<TreatyEffect> TreatyEffects { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Event> Events { get; set; }
    }
}
