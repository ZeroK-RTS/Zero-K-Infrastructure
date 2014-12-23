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

namespace PlasmaShared.Ef
{
    // Faction
    public partial class Faction
    {
        public int FactionID { get; set; } // FactionID (Primary key)
        public string Name { get; set; } // Name
        public string Shortcut { get; set; } // Shortcut
        public string Color { get; set; } // Color
        public bool IsDeleted { get; set; } // IsDeleted
        public double Metal { get; set; } // Metal
        public double Dropships { get; set; } // Dropships
        public double Bombers { get; set; } // Bombers
        public string SecretTopic { get; set; } // SecretTopic
        public double EnergyProducedLastTurn { get; set; } // EnergyProducedLastTurn
        public double EnergyDemandLastTurn { get; set; } // EnergyDemandLastTurn
        public double Warps { get; set; } // Warps

        // Reverse navigation
        public virtual ICollection<Account> Accounts { get; set; } // Account.FK_Account_Faction
        public virtual ICollection<AccountRole> AccountRoles { get; set; } // AccountRole.FK_AccountRole_Faction
        public virtual ICollection<Clan> Clans { get; set; } // Clan.FK_Clan_Faction
        public virtual ICollection<Event> Events { get; set; } // Many to many mapping
        public virtual ICollection<FactionTreaty> FactionTreaties_AcceptingFactionID { get; set; } // FactionTreaty.FK_FactionTreaty_Faction1
        public virtual ICollection<FactionTreaty> FactionTreaties_ProposingFactionID { get; set; } // FactionTreaty.FK_FactionTreaty_Faction
        public virtual ICollection<Planet> Planets { get; set; } // Planet.FK_Planet_Faction
        public virtual ICollection<PlanetFaction> PlanetFactions { get; set; } // Many to many mapping
        public virtual ICollection<PlanetOwnerHistory> PlanetOwnerHistories { get; set; } // PlanetOwnerHistory.FK_PlanetOwnerHistory_Faction
        public virtual ICollection<Poll> Polls { get; set; } // Poll.FK_Poll_Faction
        public virtual ICollection<RoleType> RoleTypes { get; set; } // RoleType.FK_RoleType_Faction
        public virtual ICollection<TreatyEffect> TreatyEffects_GivingFactionID { get; set; } // TreatyEffect.FK_TreatyEffect_Faction
        public virtual ICollection<TreatyEffect> TreatyEffects_ReceivingFactionID { get; set; } // TreatyEffect.FK_TreatyEffect_Faction1

        public Faction()
        {
            IsDeleted = false;
            Metal = 0;
            Dropships = 1;
            Bombers = 0;
            EnergyProducedLastTurn = 0;
            EnergyDemandLastTurn = 0;
            Warps = 0;
            Accounts = new List<Account>();
            AccountRoles = new List<AccountRole>();
            Clans = new List<Clan>();
            FactionTreaties_AcceptingFactionID = new List<FactionTreaty>();
            FactionTreaties_ProposingFactionID = new List<FactionTreaty>();
            Planets = new List<Planet>();
            PlanetFactions = new List<PlanetFaction>();
            PlanetOwnerHistories = new List<PlanetOwnerHistory>();
            Polls = new List<Poll>();
            RoleTypes = new List<RoleType>();
            TreatyEffects_GivingFactionID = new List<TreatyEffect>();
            TreatyEffects_ReceivingFactionID = new List<TreatyEffect>();
            Events = new List<Event>();
            InitializePartial();
        }
        partial void InitializePartial();
    }

}
