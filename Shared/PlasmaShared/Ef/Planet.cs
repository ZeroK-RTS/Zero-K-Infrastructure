// ReSharper disable RedundantUsingDirective
// ReSharper disable DoNotCallOverridableMethodsInConstructor
// ReSharper disable InconsistentNaming
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable PartialMethodWithSinglePart
// ReSharper disable RedundantNameQualifier

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
//using DatabaseGeneratedOption = System.ComponentModel.DataAnnotations.DatabaseGeneratedOption;

namespace ZkData
{
    // Planet
    public partial class Planet
    {
        public int PlanetID { get; set; } // PlanetID (Primary key)
        public string Name { get; set; } // Name
        public double X { get; set; } // X
        public double Y { get; set; } // Y
        public int? MapResourceID { get; set; } // MapResourceID
        public int? OwnerAccountID { get; set; } // OwnerAccountID
        public int GalaxyID { get; set; } // GalaxyID
        public int? ForumThreadID { get; set; } // ForumThreadID
        public int? OwnerFactionID { get; set; } // OwnerFactionID
        public int TeamSize { get; set; } // TeamSize

        // Reverse navigation
        public virtual ICollection<AccountPlanet> AccountPlanets { get; set; } // Many to many mapping
        public virtual ICollection<Event> Events { get; set; } // Many to many mapping
        public virtual ICollection<Link> Links_PlanetID1 { get; set; } // Many to many mapping
        public virtual ICollection<Link> Links_PlanetID2 { get; set; } // Many to many mapping
        public virtual ICollection<MarketOffer> MarketOffers { get; set; } // MarketOffer.FK_MarketOffer_Planet
        public virtual ICollection<PlanetFaction> PlanetFactions { get; set; } // Many to many mapping
        public virtual ICollection<PlanetOwnerHistory> PlanetOwnerHistories { get; set; } // Many to many mapping
        public virtual ICollection<PlanetStructure> PlanetStructures_PlanetID { get; set; } // Many to many mapping
        public virtual ICollection<PlanetStructure> PlanetStructures_TargetPlanetID { get; set; } // PlanetStructure.FK_PlanetStructure_Planet1
        public virtual ICollection<TreatyEffect> TreatyEffects { get; set; } // TreatyEffect.FK_TreatyEffect_Planet

        // Foreign keys
        public virtual Account Account { get; set; } // FK_Planet_Account
        public virtual Faction Faction { get; set; } // FK_Planet_Faction
        public virtual ForumThread ForumThread { get; set; } // FK_Planet_ForumThread
        public virtual Galaxy Galaxy { get; set; } // FK_Planet_Galaxy
        public virtual Resource Resource { get; set; } // FK_Planet_Resource

        public Planet()
        {
            TeamSize = 2;
            AccountPlanets = new List<AccountPlanet>();
            Links_PlanetID1 = new List<Link>();
            Links_PlanetID2 = new List<Link>();
            MarketOffers = new List<MarketOffer>();
            PlanetFactions = new List<PlanetFaction>();
            PlanetOwnerHistories = new List<PlanetOwnerHistory>();
            PlanetStructures_PlanetID = new List<PlanetStructure>();
            PlanetStructures_TargetPlanetID = new List<PlanetStructure>();
            TreatyEffects = new List<TreatyEffect>();
            Events = new List<Event>();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
