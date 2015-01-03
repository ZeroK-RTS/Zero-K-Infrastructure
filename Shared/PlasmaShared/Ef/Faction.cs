namespace ZkData
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Faction")]
    public partial class Faction
    {
        
        public Faction()
        {
            Accounts = new HashSet<Account>();
            AccountRoles = new HashSet<AccountRole>();
            Clans = new HashSet<Clan>();
            FactionTreatiesByProposingFaction = new HashSet<FactionTreaty>();
            FactionTreatiesByAcceptingFaction = new HashSet<FactionTreaty>();
            Planets = new HashSet<Planet>();
            PlanetFactions = new HashSet<PlanetFaction>();
            PlanetOwnerHistories = new HashSet<PlanetOwnerHistory>();
            Polls = new HashSet<Poll>();
            RoleTypes = new HashSet<RoleType>();
            TreatyEffectsByGivingFactionID = new HashSet<TreatyEffect>();
            TreatyEffectsByReceivingFactionID = new HashSet<TreatyEffect>();
            Events = new HashSet<Event>();
        }

        public int FactionID { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [Required]
        [StringLength(50)]
        public string Shortcut { get; set; }

        [Required]
        [StringLength(20)]
        public string Color { get; set; }

        public bool IsDeleted { get; set; }

        public double Metal { get; set; }

        public double Dropships { get; set; }

        public double Bombers { get; set; }

        [StringLength(500)]
        public string SecretTopic { get; set; }

        public double EnergyProducedLastTurn { get; set; }

        public double EnergyDemandLastTurn { get; set; }

        public double Warps { get; set; }

        
        public virtual ICollection<Account> Accounts { get; set; }

        
        public virtual ICollection<AccountRole> AccountRoles { get; set; }

        
        public virtual ICollection<Clan> Clans { get; set; }

        
        public virtual ICollection<FactionTreaty> FactionTreatiesByProposingFaction { get; set; }

        
        public virtual ICollection<FactionTreaty> FactionTreatiesByAcceptingFaction { get; set; }

        
        public virtual ICollection<Planet> Planets { get; set; }

        
        public virtual ICollection<PlanetFaction> PlanetFactions { get; set; }

        
        public virtual ICollection<PlanetOwnerHistory> PlanetOwnerHistories { get; set; }

        
        public virtual ICollection<Poll> Polls { get; set; }

        
        public virtual ICollection<RoleType> RoleTypes { get; set; }

        
        public virtual ICollection<TreatyEffect> TreatyEffectsByGivingFactionID { get; set; }

        
        public virtual ICollection<TreatyEffect> TreatyEffectsByReceivingFactionID { get; set; }

        
        public virtual ICollection<Event> Events { get; set; }
    }
}
