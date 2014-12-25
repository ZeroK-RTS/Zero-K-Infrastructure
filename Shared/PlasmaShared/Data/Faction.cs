using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace ZkData
{
    partial class Faction
    {

        public string GetImageUrl() {
            return string.Format("/img/factions/{0}.png", Shortcut);
        }

        public string GetShipImageUrl() {
            return string.Format("/img/factions/{0}_ship.png", Shortcut);
        }

        public static string FactionColor(Faction fac, int myFactionID) {
            if (fac == null) return "";
            return fac.Color;
        }

        public bool HasTreatyRight(Faction giverFaction, Func<TreatyEffectType, bool> test, Planet planet = null) {
            var effects = TreatyEffects_ReceivingFactionID.Where(x => x.FactionTreaty.TreatyState == TreatyState.Accepted && x.GivingFactionID == giverFaction.FactionID && (x.Planet == null || x.Planet == planet));
            return effects.Any(x=>test(x.TreatyEffectType));
        }

        public bool GaveTreatyRight(Planet planet, Func<TreatyEffectType, bool> test)
        {
            if (planet == null) return false; // not targeted, must be either planet or faction or both
            var effects = TreatyEffects_GivingFactionID.Where(x => x.FactionTreaty.TreatyState == TreatyState.Accepted  && ((x.PlanetID == planet.PlanetID && (planet.OwnerFactionID == null|| planet.OwnerFactionID == x.ReceivingFactionID))||(x.PlanetID ==null&& x.ReceivingFactionID == planet.OwnerFactionID)));
            return effects.Any(x => test(x.TreatyEffectType));
        }


        public void SpendDropships(double count) {
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

        public List<FactionUnlockEntry>  GetFactionUnlocks() {
            
            var ret = Planets.SelectMany(y => y.PlanetStructures_PlanetID).Where(x => x.IsActive && x.StructureType.EffectUnlockID != null).GroupBy(x=>x.StructureType.Unlock).Select(x =>
                        new FactionUnlockEntry() { Unlock = x.Key, Faction = this })
                    .ToList();
            foreach (var provider in TreatyEffects_ReceivingFactionID.Where(x=>x.FactionTreaty.TreatyState == TreatyState.Accepted && x.TreatyEffectType.EffectShareTechs == true).GroupBy(x=>x.Faction_GivingFactionID).Select(x=>x.Key)) {

                foreach (var tech in Planets.SelectMany(y => y.PlanetStructures_PlanetID).Where(x => x.IsActive && x.StructureType.EffectUnlockID != null).GroupBy(x=>x.StructureType.Unlock).Select(x=>x.Key)) {
                    if (!ret.Any(x=>x.Unlock == tech)) ret.Add(new FactionUnlockEntry(){Unlock = tech, Faction = provider});
                }
            }
            return ret;
        }


        public void ProcessEnergy(int turn) {
            var energy = EnergyProducedLastTurn;
            var structs = Planets.SelectMany(x => x.PlanetStructures_PlanetID).ToList();
            var demand = structs.Sum(x => (double?)(x.StructureType.UpkeepEnergy??0)) ?? 0;
            
            if (energy >= demand) {
                foreach (var s in structs) {
                    s.PoweredTick(turn);
                }
            } else {

                // todo implement complex energy behavior with multilayered distribution
                foreach (var s in structs.OrderByDescending(x=>(int)x.EnergyPriority).ThenBy(x=>x.StructureType.UpkeepEnergy??0)) {
                    if (energy >= s.StructureType.UpkeepEnergy) {
                        s.PoweredTick(turn);
                        energy -= s.StructureType.UpkeepEnergy??0;
                    } else {
                        s.UnpoweredTick(turn);
                    }
                }
            }

            EnergyDemandLastTurn = demand;
            EnergyProducedLastTurn =
                Planets.SelectMany(x => x.PlanetStructures_PlanetID).Where(x => x.IsActive && x.StructureType.EffectEnergyPerTurn > 0).Sum(
                    x => x.StructureType.EffectEnergyPerTurn) ?? 0;

        }


        public override string ToString() {
            return Name;
        }

        public class FactionUnlockEntry
        {
            public Faction Faction;
            public Unlock Unlock;
        }

        public double GetMetalFromPlanets() {
            return Planets.Count()*GlobalConst.PlanetMetalPerTurn;
        }


        public double GetEnergyToMetalConversion() {
            var metal = (EnergyProducedLastTurn - EnergyDemandLastTurn)*GlobalConst.PlanetWarsEnergyToMetalRatio;
            if (metal > 0) return metal;
            else return 0;
        }

        public void ConvertExcessEnergyToMetal() {
            var metal = GetEnergyToMetalConversion();
            if (metal > 0) {
                var productions =
                    Planets.SelectMany(x => x.PlanetStructures_PlanetID).Where(x => x.IsActive && x.StructureType.EffectEnergyPerTurn > 0 && x.Account != null).GroupBy(
                        x => x.Account).Select(x=>new {Account = x.Key, Energy = x.Sum(y=>y.StructureType.EffectEnergyPerTurn) ?? 0}).ToDictionary(x=>x.Account, x=>x.Energy);
                var totalEnergy = productions.Sum(x => x.Value);

                if (totalEnergy > 0) {
                    foreach (var prod in productions) {
                        prod.Key.ProduceMetal(prod.Value / totalEnergy * metal);
                    }
                }
            }
        }
    }
}
