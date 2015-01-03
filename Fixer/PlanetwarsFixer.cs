using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Serialization;
using LobbyClient;
using NightWatch;
using ZkData.UnitSyncLib;
using ZkData;
using Encoder = System.Drawing.Imaging.Encoder;

namespace Fixer
{
    public static class PlanetwarsFixer
    {
        public static void GenerateTechs()
        {
            var db = new ZkDataContext();
            db.StructureTypes.DeleteAllOnSubmit(db.StructureTypes.Where(x => x.Unlock != null));
            db.SubmitAndMergeChanges();

            foreach (var u in db.Unlocks.Where(x => x.UnlockType == UnlockTypes.Unit))
            {
                var s = new StructureType()
                {
                    BattleDeletesThis = false,
                    Cost = u.XpCost / 2,
                    MapIcon = "techlab.png",
                    DisabledMapIcon = "techlab_dead.png",
                    Name = u.Name,
                    Description = string.Format("Access to {0} and increases influence gains", u.Name),
                    TurnsToActivate = u.XpCost / 100,
                    IsBuildable = true,
                    IsIngameDestructible = true,
                    IsBomberDestructible = true,
                    Unlock = u,
                    UpkeepEnergy = u.XpCost / 5,
                    IngameUnitName = "pw_" + u.Code,
                };
                db.StructureTypes.InsertOnSubmit(s);
            }
            db.SubmitAndMergeChanges();

        }

        public static void PurgeGalaxy(int galaxyID, bool resetclans = false, bool resetroles = false)
        {
            System.Console.WriteLine("Purging galaxy " + galaxyID);
            using (var db = new ZkDataContext())
            {
                db.Database.CommandTimeout = 300;

                var gal = db.Galaxies.Single(x => x.GalaxyID == galaxyID);
                foreach (var p in gal.Planets)
                {
                    //p.ForumThread = null;
                    p.OwnerAccountID = null;
                    p.Faction = null;
                    p.Account = null;
                }
                foreach (var f in db.Factions)
                {
                    f.Metal = 0;
                    f.EnergyDemandLastTurn = 0;
                    f.EnergyProducedLastTurn = 0;
                    f.Bombers = 0;
                    f.Dropships = 0;
                    f.Warps = 0;
                }
                db.SubmitChanges();

                db.Database.ExecuteSqlCommand("update accounts set pwbombersproduced=0, pwbombersused=0, pwdropshipsproduced=0, pwdropshipsused=0, pwmetalproduced=0, pwmetalused=0, pwattackpoints=0, pwwarpproduced=0, pwwarpused=0, elopw=1500");
                if (resetclans) db.Database.ExecuteSqlCommand("update accounts set clanid=null");
                db.Database.ExecuteSqlCommand("delete from events");
                db.Database.ExecuteSqlCommand("delete from planetownerhistories");
                db.Database.ExecuteSqlCommand("delete from planetstructures");
                db.Database.ExecuteSqlCommand("delete from planetfactions");
                db.Database.ExecuteSqlCommand("delete from accountplanets");
                if (resetroles) db.Database.ExecuteSqlCommand("delete from accountroles where clanID is null");
                db.Database.ExecuteSqlCommand("delete from factiontreaties");
                db.Database.ExecuteSqlCommand("delete from treatyeffects");

                db.Database.ExecuteSqlCommand("delete from forumthreads where forumcategoryid={0}", db.ForumCategories.Single(x => x.IsPlanets).ForumCategoryID);

                if (resetclans)
                {
                    db.Database.ExecuteSqlCommand("delete from clans");
                    db.Database.ExecuteSqlCommand("delete from forumthreads where forumcategoryid={0}", db.ForumCategories.Single(x => x.IsClans).ForumCategoryID);
                }
            }
        }

        public static void RandomizeMaps(int galaxyID)
        {
            using (var db = new ZkDataContext())
            {
                var gal = db.Galaxies.Single(x => x.GalaxyID == galaxyID);

                var maps = db.Resources.Where(x => x.FeaturedOrder > 0 && x.MapPlanetWarsIcon != null).ToList().Shuffle();
                int cnt = 0;
                foreach (var p in gal.Planets)
                {
                    p.MapResourceID = maps[cnt++].ResourceID;
                }
                gal.Turn = 0;
                gal.Started = DateTime.UtcNow;
                gal.IsDirty = true;
                db.SubmitChanges();
            }
        }

        public static void SwapPlanetOwners(int planetID1, int planetID2)
        {
            using (var db = new ZkDataContext())
            {
                Planet planet1 = db.Planets.FirstOrDefault(x => x.PlanetID == planetID1);
                Planet planet2 = db.Planets.FirstOrDefault(x => x.PlanetID == planetID2);

                Account acc1 = planet1.Account;
                Account acc2 = planet2.Account;
                Faction fac1 = planet1.Faction;
                Faction fac2 = planet2.Faction;

                planet1.Account = acc2;
                planet2.Account = acc1;
                planet1.Faction = fac2;
                planet2.Faction = fac1;

                db.SubmitChanges();
            }
        }

        public static int IsPlanetNeighbour(int thisPlanetID, int thatPlanetID)
        {
            using (var db = new ZkDataContext())
            {
                if (thisPlanetID == thatPlanetID) return 2;
                Planet planet = db.Planets.FirstOrDefault(x => x.PlanetID == thatPlanetID);
                foreach (Link link in planet.LinksByPlanetID1.Union(planet.LinksByPlanetID2))
                {
                    Planet otherPlanet = link.PlanetByPlanetID1 == planet ? link.PlanetByPlanetID2 : link.PlanetByPlanetID1;
                    if (otherPlanet.PlanetID == thisPlanetID) return 1;
                }
                return 0;
            }
        }

        public static void RandomizePlanetOwners(int galaxyID, double proportionNeutral = 0.25)
        {
            using (var db = new ZkDataContext())
            {
                System.Console.WriteLine(String.Format("Randomly assigning planets in galaxy {0} to factions", galaxyID));
                var gal = db.Galaxies.Single(x => x.GalaxyID == galaxyID);
                var factions = db.Factions.Where(x => !x.IsDeleted).ToList();
                int numFactions = factions.ToList().Count;
                int index = 1;

                List<int> forceNeutral = new List<int>(new int[] { 3930, 3964 });

                List<List<Account>> accountsByFaction = new List<List<Account>>();
                foreach (Faction f in factions)
                {
                    accountsByFaction.Add(f.Accounts.Where(x => x.LastLogin > DateTime.UtcNow.AddDays(-15)).OrderByDescending(x => x.EloPw).ToList());
                }
                List<Account> alreadyAssigned = new List<Account>();

                Random rng = new Random();
                //List<Account> alreadyHavePlanets = new List<Account>;

                foreach (var p in gal.Planets)
                {
                    double rand = rng.NextDouble();
                    bool neutral = false;
                    foreach (int otherPlanet in forceNeutral)
                    {
                        if (IsPlanetNeighbour(p.PlanetID, otherPlanet) > 0)
                        {
                            neutral = true;
                        }
                    }
                    if (neutral || rand < proportionNeutral)
                    {
                        p.Faction = null;
                        p.Account = null;
                        System.Console.WriteLine(String.Format("\tPlanet {0} is neutral", p.Name));
                    }

                    else
                    {
                        Faction faction = factions.Where(x => x.FactionID == index).FirstOrDefault();
                        Account acc = accountsByFaction[index - 1].FirstOrDefault(x => !alreadyAssigned.Contains(x));
                        p.Faction = faction;
                        p.Account = acc;
                        index++;
                        if (index > numFactions) index = 1;
                        alreadyAssigned.Add(acc);
                        System.Console.WriteLine(String.Format("\tGiving planet {0} to {1} of {2}", p.Name, acc, p.Faction));
                    }
                }
                gal.IsDirty = true;
                db.SubmitChanges();
            }
        }

        public static void GenerateArtefacts(int galaxyID, int[] planetIDs)
        {
            ZkDataContext db = new ZkDataContext();
            var planetList = planetIDs.ToList();
            var planets = db.Planets.Where(x => planetList.Contains(x.PlanetID));
            foreach (Planet p in planets)
            {
                p.AddStruct(9);
            }
            db.SubmitChanges();
        }

        public static void StartGalaxy(int galaxyID, params int[] startingPlanets)
        {
            using (var db = new ZkDataContext())
            {
                var facs = db.Factions.Where(x => !x.IsDeleted).ToList();
                for (int i = 0; i < facs.Count; i++)
                {
                    var planet = db.Planets.First(x => x.PlanetID == startingPlanets[i]);
                    var faction = facs[i];
                    planet.PlanetFactions.Add(new PlanetFaction()
                    {
                        Faction = faction,
                        Influence = 100
                    });
                    planet.Faction = faction;
                }

                db.SubmitAndMergeChanges();
                


                /*foreach (Account acc in db.Accounts)
                {
                    double elo = acc.Elo;
                    if (acc.Elo1v1 > elo) elo = acc.Elo1v1;
                    acc.EloPw = elo;
                }
                System.Console.WriteLine("PW Elo set");
                foreach (Faction fac in db.Factions)
                {
                    var accounts = fac.Accounts.Where(x=> x.Planets.Count > 0).ToList();
                    foreach (Account acc in accounts)
                    {
                        acc.ProduceDropships(1);
                    }
                    fac.ProduceDropships(5);
                }
                System.Console.WriteLine("Dropships ready");
                */
                var gal = db.Galaxies.Single(x => x.GalaxyID == galaxyID);
                foreach (Planet planet in gal.Planets)
                {
                    if (planet.Faction != null)
                    {
                        foreach (PlanetStructure s in planet.PlanetStructures)
                        {
                            //s.ActivatedOnTurn = 0;
                            s.IsActive = true;
                        }
                    }
                    else
                    {
                        foreach (PlanetStructure s in planet.PlanetStructures.Where(x => x.StructureType.EffectIsVictoryPlanet != true))
                        {
                            s.IsActive = false;
                            s.ActivatedOnTurn = null;
                        }
                    }
                }
                System.Console.WriteLine("Structure activation set");

                gal.Turn = 0;
                gal.Started = DateTime.UtcNow;
                db.SubmitChanges();
            }
        }

        public static void AddStruct(this Planet p, int structID)
        {
            p.PlanetStructures.Add(new PlanetStructure() { StructureTypeID = structID, IsActive = true });
        }

        static void GenerateStructures(int galaxyID)
        {
            var rand = new Random();
            var db = new ZkDataContext();
            var gal = db.Galaxies.Single(x => x.GalaxyID == galaxyID);
            var names = Resources.names.Lines().ToList();

            var wormhole = 16;
            var wormhole2 = 19;


            var mine = 1;
            var mine2 = 3;
            var mine3 = 4;
            var warp = 10;
            var chicken = 20;
            var dfac = 6;
            var ddepot = 7;
            int artefact = 9;
            int militia = 594;

            /*
            567	Jumpjet/Specialist Plant
            568	Screamer
            569	Athena
            570	Heavy Tank Factory
            571	Airplane Plant
            572	Krow
            573	Bantha
            574	Jugglenaut
            575	Detriment
            576	Singularity Reactor
            577	Annihilator
            578	Doomsday Machine
            579	Behemoth
            580	Starlight
            581	Big Bertha
            582	Goliath
            583	Licho
            584	Reef
            585	Scythe
            586	Panther
            587	Black Dawn
            588	Dominatrix
            589	Newton
            590	Shield Bot Factory
            591	Silencer
            592	Disco Rave Party*/


            List<int> bannedStructures = new List<int>() { };// { 568, 577, 578, 584, 585, 586, 588, 589, 571, 590 };

            var structs = db.StructureTypes.Where(x => x.Unlock != null && !bannedStructures.Contains(x.StructureTypeID));
            List<Tuple<int, int>> costs = new List<Tuple<int, int>>();
            foreach (var s in structs)
            {
                costs.Add(Tuple.Create((int)(5000 / s.Cost), s.StructureTypeID)); // probabality is relative to 1200-cost
            }
            var sumCosts = costs.Sum(x => x.Item1);

            foreach (var p in gal.Planets)
            {
                p.PlanetStructures.Clear();
                //p.Name = names[rand.Next(names.Count)];
                //names.Remove(p.Name);
                //if (rand.Next(50) == 0 ) p.AddStruct(wormhole2);
                //else 
                //if (rand.Next(10)<8) 
                p.AddStruct(wormhole);
                //p.AddStruct(militia);

                //if (rand.Next(30) ==0) p.AddStruct(mine3);
                //else if (rand.Next(20)==0) p.AddStruct(mine2);
                //else 
                //if (rand.Next(20) ==0) p.AddStruct(mine);

                //if (rand.Next(20) == 0) p.AddStruct(dfac);
                //if (rand.Next(20) == 0) p.AddStruct(ddepot);
                //if (rand.Next(20) == 0) p.AddStruct(warp);

                if (p.Resource.MapIsChickens == true) p.AddStruct(chicken);

                // tech structures
                /*if (rand.Next(8) ==0)
                {

                    var probe = rand.Next(sumCosts);
                    foreach (var s in costs)
                    {
                        probe -= s.Item1;
                        if (probe <= 0)
                        {
                            p.AddStruct(s.Item2);
                            break;
                        }
                    }
                }*/
            }

            // artefacts
            //foreach (var p in gal.Planets.Where(x => x.Resource.MapIsChickens!=true && !x.Resource.MapIsFfa != true && x.Resource.MapIs1v1 != true).Shuffle().Take(5)) p.AddStruct(artefact);

            // jump gates
            //foreach (var p in gal.Planets.Shuffle().Take(6)) p.AddStruct(warp);

            db.SubmitChanges();
            db.SubmitChanges();
        }

        public static void AddWormholes()
        {
            var db = new ZkDataContext();
            var wormhole = db.StructureTypes.Where(x => x.EffectInfluenceSpread > 0).OrderBy(x => x.EffectInfluenceSpread).First();
            foreach (var p in db.Planets.Where(x => !x.PlanetStructures.Any(y => y.StructureType.EffectInfluenceSpread > 0)))
            {
                p.PlanetStructures.Add(new PlanetStructure() { StructureTypeID = wormhole.StructureTypeID });
            }
            db.SubmitChanges();
        }

        public static void RemoveTechStructures(bool bRefund, bool removeDefs)
        {
            ZkDataContext db = new ZkDataContext();
            foreach (PlanetStructure structure in db.PlanetStructures.Where(x => x.StructureType.Unlock != null && x.StructureType.Unlock.UnlockType == ZkData.UnlockTypes.Unit))
            {
                db.PlanetStructures.DeleteOnSubmit(structure);
                if (bRefund)
                {
                    var refund = structure.StructureType.Cost;
                    if (structure.Account != null) structure.Account.ProduceMetal(refund);
                    else structure.Planet.Faction.ProduceMetal(refund);
                }
            }
            if (removeDefs)
            {
                foreach (StructureType structType in db.StructureTypes.Where(x => x.Unlock != null && x.Unlock.UnlockType == ZkData.UnlockTypes.Unit))
                {
                    db.StructureTypes.DeleteOnSubmit(structType);
                }
            }
            db.SubmitChanges();
        }
    }
}
