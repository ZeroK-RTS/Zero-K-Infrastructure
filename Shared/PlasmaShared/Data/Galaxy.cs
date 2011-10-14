using System;
using System.Collections.Generic;
using System.Linq;

namespace ZkData
{
    partial class Galaxy
    {
        public static List<ClanUnlockEntry> ClanUnlocks(ZkDataContext db, int? clanID)
        {
            if (clanID == null) return new List<ClanUnlockEntry>();
            var techAlly = new List<int>();
            var clan = db.Clans.Single(x => x.ClanID == clanID);
            techAlly = (from treaty in db.TreatyOffers.Where(x => x.OfferingClanID == clanID)
                        join tr2 in db.TreatyOffers on treaty.TargetClanID equals tr2.OfferingClanID
                        where tr2.TargetClanID == clanID && tr2.IsResearchAgreement && treaty.IsResearchAgreement
                        select treaty.TargetClanID).ToList();

            var gal = db.Galaxies.Single(x => x.IsDefault);
            var planets =
                gal.Planets.Where(
                    x =>
                    x.Account != null &&
                    (x.Account.ClanID == clanID || x.Account.FactionID == clan.FactionID || techAlly.Contains(x.Account.ClanID ?? 0)));

            var result = new Dictionary<int, ClanUnlockEntry>();

            return
                planets.SelectMany(y => y.PlanetStructures).Where(x => !x.IsDestroyed && x.StructureType.EffectUnlockID != null).Select(
                    x => new { unlock = x.StructureType.Unlock, clan = x.Planet.Account.Clan }).GroupBy(x => x.unlock).Select(
                        x =>
                        new ClanUnlockEntry() { Unlock = x.Key, Clan = x.OrderByDescending(y => y.clan.ClanID == clanID).Select(y => y.clan).First() })
                    .ToList();
        }


        public static List<Planet> DropshipAttackablePlanets(ZkDataContext db, int? clanID)
        {
            var milAlly = new List<int>();

            milAlly = (from treaty in db.TreatyOffers.Where(x => x.OfferingClanID == clanID)
                       join tr2 in db.TreatyOffers on treaty.TargetClanID equals tr2.OfferingClanID
                       where tr2.TargetClanID == clanID && tr2.AllyStatus == AllyStatus.Alliance && treaty.AllyStatus == AllyStatus.Alliance
                       select treaty.TargetClanID).ToList();

            var facId = db.Clans.Where(x => x.ClanID == clanID).Select(x => x.FactionID).FirstOrDefault();

            var gal = db.Galaxies.Single(x => x.IsDefault);
            var planets = gal.Planets.Where(x => x.Account != null && (x.Account.FactionID == facId || milAlly.Contains(x.Account.ClanID ?? 0)));
            if (!planets.Any()) return gal.Planets.ToList(); // if cannot attack any planet (not own/allied to any) -> allow attack anywhere
            var accesiblePlanets = new List<Planet>();

            foreach (var thisPlanet in planets)
            {
                accesiblePlanets.Add(thisPlanet);
                var thisPlanetID = thisPlanet.PlanetID;

                // iterate links to this planet
                foreach (var link in gal.Links.Where(l => (l.PlanetID1 == thisPlanetID || l.PlanetID2 == thisPlanetID)))
                {
                    var otherPlanet = thisPlanetID == link.PlanetID1 ? link.PlanetByPlanetID2 : link.PlanetByPlanetID1;

                    if (thisPlanet.PlanetStructures.Where(x => !x.IsDestroyed).Max(x => x.StructureType.EffectLinkStrength) > 0) accesiblePlanets.Add(otherPlanet);
                }
            }

            return accesiblePlanets;
        }


        /// <summary>
        /// Removes ships which cannot be supported by current jumpgates/links
        /// </summary>
        public static void RemoveOrphanedShips(ZkDataContext db) {

            foreach (var clanShips in db.AccountPlanets.Where(x => x.DropshipCount > 0 && x.Account.ClanID != null).GroupBy(x => x.Account.Clan)) {
                var accessible = DropshipAttackablePlanets(db, clanShips.Key.ClanID);

                foreach (var accountShips in clanShips.GroupBy(x => x.Account)) {
                    var jumpgates = accountShips.Key.GetJumpGateCapacity();
                    var capacity = accountShips.Key.GetDropshipCapacity();

                    foreach (var planetEntry in accountShips) {
                        var isAccessible = accessible.Contains(planetEntry.Planet);
                        var maxCount = isAccessible ? capacity : jumpgates;
                        if (planetEntry.DropshipCount > maxCount) {
                            accountShips.Key.DropshipCount += planetEntry.DropshipCount - maxCount;
                            planetEntry.DropshipCount = maxCount;
                        }
                    }
                }
            }
        }


        public static void RecalculateShadowInfluence(ZkDataContext db)
        {
            var gal = db.Galaxies.Single(x => x.IsDefault);
            foreach (var thisPlanet in gal.Planets)
            {
                var thisPlanetID = thisPlanet.PlanetID;
                var thisLinkStrenght = thisPlanet.PlanetStructures.Where(s => !s.IsDestroyed).Sum(s => s.StructureType.EffectLinkStrength) ?? 0;

                // clear shadow influence
                foreach (var thisAccountPlanet in thisPlanet.AccountPlanets) thisAccountPlanet.ShadowInfluence = 0;

                // set shadow influence

                // iterate links to this planet
                foreach (var link in gal.Links.Where(l => l.PlanetID1 == thisPlanetID || l.PlanetID2 == thisPlanetID))
                {
                    var otherPlanet = thisPlanetID == link.PlanetID1 ? link.PlanetByPlanetID2 : link.PlanetByPlanetID1;
                    var otherLinkStrenght = otherPlanet.PlanetStructures.Where(s => !s.IsDestroyed).Sum(s => s.StructureType.EffectLinkStrength) ?? 0;

                    // increment shadow influence of player on the other side of the link
                    var influenceFactor = (thisLinkStrenght + otherLinkStrenght)/2.0;
                    if (thisLinkStrenght == 0 || otherLinkStrenght == 0) influenceFactor = 0;

                    link.LinktStrength = influenceFactor;

                    if (otherPlanet.Account == null || otherPlanet.Account.ClanID == null) continue; // no owner: planet can't project shadow influence
                    if (thisPlanet.Account == null || thisPlanet.Account.Clan == null) continue; // no owner: planet cant recieve shadow influence
                    if (thisPlanet.Account.FactionID != otherPlanet.Account.FactionID) continue; // planet factions dont match - influence cant be transferred
                    var factionID = thisPlanet.Account.FactionID;

                    // iterate accountPlanets on other side of the link
                    foreach (var otherAccountPlanet in
                        otherPlanet.AccountPlanets.Where(ap => ap.Account != null && ap.Account.FactionID == factionID && ap.Influence > 0))
                    {
                        var otherAccountID = otherAccountPlanet.AccountID;
                        // get corresponding accountPlanet on this side of the link
                        var thisAccountPlanet = thisPlanet.AccountPlanets.SingleOrDefault(ap => ap.AccountID == otherAccountID);
                        if (thisAccountPlanet == null)
                        {
                            thisAccountPlanet = new AccountPlanet { AccountID = otherAccountID, PlanetID = thisPlanetID };
                            thisPlanet.AccountPlanets.Add(thisAccountPlanet);
                        }

                        thisAccountPlanet.ShadowInfluence += (int)(otherAccountPlanet.Influence*influenceFactor);
                    }
                }
            }
            db.SubmitChanges();
        }

        public class ClanUnlockEntry
        {
            public Clan Clan;
            public Unlock Unlock;
        }
    }
}