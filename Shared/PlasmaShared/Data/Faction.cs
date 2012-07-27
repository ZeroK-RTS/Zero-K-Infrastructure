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
            var effects = TreatyEffectsByReceivingFactionID.Where(x => x.FactionTreaty.TreatyState == TreatyState.Accepted && (x.Planet == null || x.Planet == planet));
            return effects.Any(x=>test(x.TreatyEffectType));
        }

        public void SpendDropships(double count) {
            var pcnt = count / Accounts.Count();
            Dropships -= count;
            foreach (var acc in Accounts) acc.PwDropshipsUsed += pcnt;
        }

        public void ProduceDropships(double count)
        {
            var pcnt = count / Accounts.Count();
            Dropships += count;
            foreach (var acc in Accounts) acc.PwDropshipsProduced += count;
        }

        public void SpendMetal(double count)
        {
            var pcnt = count / Accounts.Count();
            Metal -= count;
            foreach (var acc in Accounts) acc.PwMetalUsed += count;
        }

        public void ProduceMetal(double count)
        {
            var pcnt = count / Accounts.Count();
            Metal += count;
            foreach (var acc in Accounts) acc.PwMetalProduced += count;
        }

        public void SpendBombers(double count)
        {
            var pcnt = count / Accounts.Count();
            Bombers -= count;
            foreach (var acc in Accounts) acc.PwBombersUsed += count;
        }

        public void ProduceBombers(double count)
        {
            var pcnt = count / Accounts.Count();

            Bombers += count;
            foreach (var acc in Accounts) acc.PwBombersProduced += count;
        }

        public List<FactionUnlockEntry>  GetFactionUnlocks() {
            
            var ret = Planets.SelectMany(y => y.PlanetStructures).Where(x => x.IsActive && x.StructureType.EffectUnlockID != null).GroupBy(x=>x.StructureType.Unlock).Select(x =>
                        new FactionUnlockEntry() { Unlock = x.Key, Faction = this })
                    .ToList();
            foreach (var provider in TreatyEffectsByReceivingFactionID.Where(x=>x.FactionTreaty.TreatyState == TreatyState.Accepted && x.TreatyEffectType.EffectShareTechs == true).GroupBy(x=>x.FactionByGivingFactionID).Select(x=>x.Key)) {

                foreach (var tech in Planets.SelectMany(y => y.PlanetStructures).Where(x => x.IsActive && x.StructureType.EffectUnlockID != null).GroupBy(x=>x.StructureType.Unlock).Select(x=>x.Key)) {
                    if (!ret.Any(x=>x.Unlock == tech)) ret.Add(new FactionUnlockEntry(){Unlock = tech, Faction = provider});
                }
            }
            return ret;
        }

        public override string ToString() {
            return Name;
        }

        public class FactionUnlockEntry
        {
            public Faction Faction;
            public Unlock Unlock;
        }
    }
}
