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
        
        public Planet()
        {
            AccountPlanets = new HashSet<AccountPlanet>();
            LinksByPlanetID1 = new HashSet<Link>();
            LinksByPlanetID2 = new HashSet<Link>();
            MarketOffers = new HashSet<MarketOffer>();
            PlanetFactions = new HashSet<PlanetFaction>();
            PlanetOwnerHistories = new HashSet<PlanetOwnerHistory>();
            PlanetStructures = new HashSet<PlanetStructure>();
            PlanetStructuresByTargetPlanetID = new HashSet<PlanetStructure>();
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

        
        public virtual ICollection<AccountPlanet> AccountPlanets { get; set; }

        public virtual Faction Faction { get; set; }

        public virtual ForumThread ForumThread { get; set; }

        public virtual Galaxy Galaxy { get; set; }

        
        public virtual ICollection<Link> LinksByPlanetID1 { get; set; }

        
        public virtual ICollection<Link> LinksByPlanetID2 { get; set; }

        
        public virtual ICollection<MarketOffer> MarketOffers { get; set; }

        public virtual Resource Resource { get; set; }

        
        public virtual ICollection<PlanetFaction> PlanetFactions { get; set; }

        
        public virtual ICollection<PlanetOwnerHistory> PlanetOwnerHistories { get; set; }

        
        public virtual ICollection<PlanetStructure> PlanetStructures { get; set; }

        
        public virtual ICollection<PlanetStructure> PlanetStructuresByTargetPlanetID { get; set; }

        
        public virtual ICollection<TreatyEffect> TreatyEffects { get; set; }

        
        public virtual ICollection<Event> Events { get; set; }
    }
}
