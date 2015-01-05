using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ZkData
{
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



        public string GetImageUrl()
        {
            return string.Format((string)"/img/factions/{0}.png", (object)Shortcut);
        }

        public string GetShipImageUrl()
        {
            return string.Format((string)"/img/factions/{0}_ship.png", (object)Shortcut);
        }

        public static string FactionColor(Faction fac, int myFactionID)
        {
            if (fac == null) return "";
            return fac.Color;
        }

        public bool HasTreatyRight(Faction giverFaction, Func<TreatyEffectType, bool> test, Planet planet = null)
        {
            var effects = TreatyEffectsByReceivingFactionID.Where(x => x.FactionTreaty.TreatyState == TreatyState.Accepted && x.GivingFactionID == giverFaction.FactionID && (x.Planet == null || x.Planet == planet));
            return effects.Any(x => test(x.TreatyEffectType));
        }

        public bool GaveTreatyRight(Planet planet, Func<TreatyEffectType, bool> test)
        {
            if (planet == null) return false; // not targeted, must be either planet or faction or both
            var effects = TreatyEffectsByGivingFactionID.Where(x => x.FactionTreaty.TreatyState == TreatyState.Accepted && ((x.PlanetID == planet.PlanetID && (planet.OwnerFactionID == null || planet.OwnerFactionID == x.ReceivingFactionID)) || (x.PlanetID == null && x.ReceivingFactionID == planet.OwnerFactionID)));
            return effects.Any(x => test(x.TreatyEffectType));
        }


        public void SpendDropships(double count)
        {
            var pcnt = count / Accounts.Count();
            Dropships -= count;
            //foreach (var acc in Accounts) acc.PwDropshipsUsed += pcnt;
        }

        public void ProduceDropships(double count)
        {
            var pcnt = count / Accounts.Count();
            Dropships += count;
            //foreach (var acc in Accounts) acc.PwDropshipsProduced += pcnt;
        }

        public void SpendMetal(double count)
        {
            var pcnt = count / Accounts.Count();
            Metal -= count;
            //foreach (var acc in Accounts) acc.PwMetalUsed += pcnt;
        }

        public void ProduceMetal(double count)
        {
            var pcnt = count / Accounts.Count();
            Metal += count;
            //foreach (var acc in Accounts) acc.PwMetalProduced += pcnt;
        }

        public void ProduceWarps(double count)
        {
            var pcnt = count / Accounts.Count();
            Warps += count;
            //foreach (var acc in Accounts) acc.PwWarpProduced += pcnt;
        }

        public void SpendWarps(double count)
        {
            var pcnt = count / Accounts.Count();
            Warps -= count;
            //foreach (var acc in Accounts) acc.PwWarpUsed += pcnt;
        }

        public void SpendBombers(double count)
        {
            var pcnt = count / Accounts.Count();
            Bombers -= count;
            //foreach (var acc in Accounts) acc.PwBombersUsed += pcnt;
        }

        public void ProduceBombers(double count)
        {
            var pcnt = count / Accounts.Count();

            Bombers += count;
            //foreach (var acc in Accounts) acc.PwBombersProduced += pcnt;
        }

        public List<FactionUnlockEntry> GetFactionUnlocks()
        {

            var ret = Planets.SelectMany(y => y.PlanetStructures).Where(x => x.IsActive && x.StructureType.EffectUnlockID != null).GroupBy(x => x.StructureType.Unlock).Select(x =>
                        new FactionUnlockEntry() { Unlock = x.Key, Faction = this })
                    .ToList();
            foreach (var provider in TreatyEffectsByReceivingFactionID.Where(x => x.FactionTreaty.TreatyState == TreatyState.Accepted && x.TreatyEffectType.EffectShareTechs == true).GroupBy(x => x.FactionByGivingFactionID).Select(x => x.Key))
            {

                foreach (var tech in Planets.SelectMany(y => y.PlanetStructures).Where(x => x.IsActive && x.StructureType.EffectUnlockID != null).GroupBy(x => x.StructureType.Unlock).Select(x => x.Key))
                {
                    if (!ret.Any(x => x.Unlock == tech)) ret.Add(new FactionUnlockEntry() { Unlock = tech, Faction = provider });
                }
            }
            return ret;
        }


        public void ProcessEnergy(int turn)
        {
            var energy = EnergyProducedLastTurn;
            var structs = Planets.SelectMany(x => x.PlanetStructures).ToList();
            var demand = structs.Sum(x => (double?)(x.StructureType.UpkeepEnergy ?? 0)) ?? 0;

            if (energy >= demand)
            {
                foreach (var s in structs)
                {
                    s.PoweredTick(turn);
                }
            }
            else
            {

                // todo implement complex energy behavior with multilayered distribution
                foreach (var s in structs.OrderByDescending(x => (int)x.EnergyPriority).ThenBy(x => x.StructureType.UpkeepEnergy ?? 0))
                {
                    if (energy >= s.StructureType.UpkeepEnergy)
                    {
                        s.PoweredTick(turn);
                        energy -= s.StructureType.UpkeepEnergy ?? 0;
                    }
                    else
                    {
                        s.UnpoweredTick(turn);
                    }
                }
            }

            EnergyDemandLastTurn = demand;
            EnergyProducedLastTurn =
                Planets.SelectMany(x => x.PlanetStructures).Where(x => x.IsActive && x.StructureType.EffectEnergyPerTurn > 0).Sum(
                    x => x.StructureType.EffectEnergyPerTurn) ?? 0;

        }


        public override string ToString()
        {
            return Name;
        }

        public class FactionUnlockEntry
        {
            public Faction Faction;
            public Unlock Unlock;
        }

        public double GetMetalFromPlanets()
        {
            return Planets.Count() * GlobalConst.PlanetMetalPerTurn;
        }


        public double GetEnergyToMetalConversion()
        {
            var metal = (EnergyProducedLastTurn - EnergyDemandLastTurn) * GlobalConst.PlanetWarsEnergyToMetalRatio;
            if (metal > 0) return metal;
            else return 0;
        }

        public void ConvertExcessEnergyToMetal()
        {
            var metal = GetEnergyToMetalConversion();
            if (metal > 0)
            {
                var productions =
                    Planets.SelectMany(x => x.PlanetStructures).Where(x => x.IsActive && x.StructureType.EffectEnergyPerTurn > 0 && x.Account != null).GroupBy(
                        x => x.Account).Select(x => new { Account = x.Key, Energy = x.Sum(y => y.StructureType.EffectEnergyPerTurn) ?? 0 }).ToDictionary(x => x.Account, x => x.Energy);
                var totalEnergy = productions.Sum(x => x.Value);

                if (totalEnergy > 0)
                {
                    foreach (var prod in productions)
                    {
                        prod.Key.ProduceMetal(prod.Value / totalEnergy * metal);
                    }
                }
            }
        }
    }
}
