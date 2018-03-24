using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using EntityFramework.Extensions;
using ZkData;
using Ratings;
using PlasmaShared;

namespace ZeroKWeb.Controllers
{
    [Auth(Role = AdminLevel.Moderator)]
    public class PlanetwarsAdminController : Controller
    {
        public class PlanetwarsAdminModel
        {
            public PlanetWarsModes PlanetWarsMode { get; set; }
            public PlanetWarsModes? PlanetWarsNextMode { get; set; }
            public DateTime? PlanetWarsNextModeDate { get; set; }
            public int LastSelectedGalaxyID { get; set; }
            public bool ResetRoles { get; set; } = true;
            public bool DeleteClans { get; set; }
            public IQueryable<Galaxy> Galaxies;
        }

        // GET: PlanetwarsAdmin
        public ActionResult Index(PlanetwarsAdminModel model, string set, string purge, string futureset)
        {
            var db = new ZkDataContext();

            if (model != null)
            {
                if (!string.IsNullOrEmpty(set))
                {
                    MiscVar.PlanetWarsMode = model.PlanetWarsMode;
                    db.Events.Add(PlanetwarsEventCreator.CreateEvent("{0} changed PlanetWars status to {1}",
                        db.Accounts.Find(Global.AccountID),
                        model.PlanetWarsMode.Description()));

                    db.SaveChanges();
                }

                if (!string.IsNullOrEmpty(purge))
                {
                    PurgeGalaxy(model.LastSelectedGalaxyID, model.ResetRoles, model.DeleteClans);
                }

                if (!string.IsNullOrEmpty(futureset))
                {
                    if (model.PlanetWarsNextMode == MiscVar.PlanetWarsMode || model.PlanetWarsNextModeDate == null ||
                        model.PlanetWarsNextModeDate < DateTime.UtcNow || model.PlanetWarsNextMode == null)
                    {
                        model.PlanetWarsNextMode = null;
                        model.PlanetWarsNextModeDate = null;
                    }

                    MiscVar.PlanetWarsNextMode = model.PlanetWarsNextMode;
                    MiscVar.PlanetWarsNextModeTime = model.PlanetWarsNextModeDate;
                }
            }

            model = model ?? new PlanetwarsAdminModel();

            model.PlanetWarsMode = MiscVar.PlanetWarsMode;
            model.PlanetWarsNextMode = MiscVar.PlanetWarsNextMode;
            model.PlanetWarsNextModeDate = MiscVar.PlanetWarsNextModeTime;

            model.Galaxies = db.Galaxies.OrderByDescending(x=>x.IsDefault).ThenByDescending(x => x.GalaxyID);
            model.LastSelectedGalaxyID = model.Galaxies.FirstOrDefault(x => x.IsDefault)?.GalaxyID ?? 0;


            return View("PlanetwarsAdminIndex", model);
        }

        private static void PurgeGalaxy(int galaxyID, bool resetRoles, bool deleteClans)
        {
            using (var db = new ZkDataContext())
            {
                db.Database.CommandTimeout = 300;

                var gal = db.Galaxies.Find(galaxyID);
                gal.IsDirty = true;
                gal.Started = null;
                gal.Ended = null;
                gal.EndMessage = null;

                foreach (var p in gal.Planets)
                {
                    p.Faction = null;
                    p.Account = null;
                    p.OwnerFactionID = null;
                    p.OwnerAccountID = null;
                }
                foreach (var f in db.Factions)
                {
                    f.Metal = 0;
                    f.EnergyDemandLastTurn = 0;
                    f.EnergyProducedLastTurn = 0;
                    f.Bombers = 0;
                    f.Dropships = 0;
                    f.Warps = 0;
                    f.VictoryPoints = 0;
                }
                db.SaveChanges();


                db.Accounts.Update(x => new Account()
                {
                    PwBombersProduced = 0,
                    PwBombersUsed = 0,
                    PwDropshipsProduced = 0,
                    PwDropshipsUsed = 0,
                    PwMetalProduced = 0,
                    PwMetalUsed = 0,
                    PwAttackPoints = 0,
                    PwWarpProduced = 0,
                    PwWarpUsed = 0,
                    FactionID = null,
                });

                db.Events.Delete();
                db.PlanetOwnerHistories.Delete();

                db.PlanetStructures.Where(x => x.Planet.GalaxyID == gal.GalaxyID && x.StructureType.OwnerChangeWinsGame == false && x.StructureType.EffectIsVictoryPlanet != true && x.StructureType.EffectVictoryPointProduction == null).Delete();
                db.PlanetStructures.Where(x => x.Planet.GalaxyID == gal.GalaxyID).Update(x => new PlanetStructure { OwnerAccountID = null });
                db.PlanetFactions.Where(x => x.Planet.GalaxyID == gal.GalaxyID).Delete();
                db.AccountPlanets.Where(x => x.Planet.GalaxyID == gal.GalaxyID).Delete();

                db.FactionTreaties.Delete();
                db.TreatyEffects.Delete();

                db.Clans.Update(x => new Clan { FactionID = null });

                if (resetRoles) db.AccountRoles.Where(x=>x.ClanID == null).Delete();

                if (deleteClans)
                {
                    db.Accounts.Update(x => new Account() { ClanID = null });
                    db.Clans.Delete();
                    var clanCategory = db.ForumCategories.Single(x => x.ForumMode == ForumMode.Clans).ForumCategoryID;
                    db.ForumThreads.Where(x => x.ForumCategoryID == clanCategory).Delete();

                }
            }

        }

        public ActionResult SetDefault(int galaxyid)
        {
            var db = new ZkDataContext();
            db.Galaxies.Where(x=>x.GalaxyID != galaxyid).Update(x => new Galaxy() { IsDefault = false });
            db.Galaxies.Where(x => x.GalaxyID == galaxyid).Update(x => new Galaxy() { IsDefault = true });
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [Auth(Role = AdminLevel.SuperAdmin)]
        public ActionResult Delete(int galaxyid)
        {
            var db = new ZkDataContext();
            var gal = db.Galaxies.Find(galaxyid);
            db.Galaxies.Remove(gal);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult SetPlanetTeamSizes(int galaxyID)
        {
            var db = new ZkDataContext();
            var gal = db.Galaxies.First(x => x.GalaxyID == galaxyID);
            var planets = gal.Planets.ToList().OrderBy(x => x.Resource.MapDiagonal).ToList();
            var cnt = planets.Count;
            int num = 0;
            foreach (var p in planets)
            {
                //if (num < cnt*0.15) p.TeamSize = 1;else 
                if (num < cnt * 0.80) p.TeamSize = 2;
                //else if (num < cnt*0.85) p.TeamSize = 3;
                else p.TeamSize = 3;
                num++;
            }

            gal.IsDirty = true;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult RandomizeMaps(int galaxyID)
        {
            using (var db = new ZkDataContext())
            {
                var gal = db.Galaxies.Single(x => x.GalaxyID == galaxyID);

                var maps = db.Resources.Where(x => x.MapSupportLevel >= MapSupportLevel.Featured && x.MapPlanetWarsIcon != null).ToList().Shuffle();
                int cnt = 0;
                foreach (var p in gal.Planets)
                {
                    p.MapResourceID = maps[cnt++].ResourceID;
                }
                gal.Turn = 0;
                gal.Started = DateTime.UtcNow;
                gal.IsDirty = true;
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        public ActionResult ResetRatings()
        {
            using (var db = new ZkDataContext())
            {
                db.SpringBattles.Where(x => x.ApplicableRatings == RatingCategoryFlags.Planetwars || x.ApplicableRatings == (RatingCategoryFlags.Planetwars | RatingCategoryFlags.Casual)).Update(x => new SpringBattle()
                {
                    ApplicableRatings = RatingCategoryFlags.Casual
                });
            }
            (RatingSystems.GetRatingSystem(RatingCategory.Planetwars) as WholeHistoryRating).ResetAll();

            return RedirectToAction("Index");
        }

        public ActionResult AddWormholes(int galaxyID)
        {
            var db = new ZkDataContext();
            var wormhole = db.StructureTypes.Where(x => x.EffectInfluenceSpread > 0).OrderBy(x => x.EffectInfluenceSpread).First();
            foreach (var p in db.Planets.Where(x => x.GalaxyID == galaxyID && !x.PlanetStructures.Any(y => y.StructureType.EffectInfluenceSpread > 0)))
            {
                p.PlanetStructures.Add(new PlanetStructure() { StructureTypeID = wormhole.StructureTypeID });
            }

            db.Galaxies.Find(galaxyID).IsDirty = true;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult OwnPlanets(int galaxyID)
        {
            var db = new ZkDataContext();
            var gal = db.Galaxies.FirstOrDefault(x => x.GalaxyID == galaxyID);
            
            var hqs = gal.Planets.Where(x => x.PlanetStructures.Any(y => y.StructureType.OwnerChangeWinsGame)).ToList();
            foreach (var p in gal.Planets)
            {
                var distances = hqs.ToDictionary(x => x, x => x.GetLinkDistanceTo(p));
                var min = distances.OrderBy(x => x.Value).First();
                if (distances.Any(x => x.Key != min.Key && x.Value == min.Value))
                {
                    p.OwnerFactionID = null;
                }
                else
                {
                    p.OwnerFactionID = min.Key.OwnerFactionID;
                }
            }

            db.SaveChanges();

            var neutrals = gal.Planets.Where(x => x.OwnerFactionID == null).Select(x => x.PlanetID).ToList();

            foreach (var p in gal.Planets)
            {
                Planet m;
                if (p.GetLinkDistanceTo(x => neutrals.Contains(x.PlanetID), null, out m) == 1) p.OwnerFactionID = null;
            }

            db.SaveChanges();

            foreach (var p in gal.Planets)
            {
                p.PlanetFactions.Clear();
                if (p.OwnerFactionID != null) p.PlanetFactions.Add(new PlanetFaction() { FactionID = p.OwnerFactionID.Value, Influence = 100 });
            }

            gal.IsDirty = true;
            db.SaveChanges();

            return RedirectToAction("Index");
        }

        public ActionResult StartGalaxy(int galaxyID)
        {
            using (var db = new ZkDataContext())
            {
                var facs = db.Factions.Where(x => !x.IsDeleted).ToList();
                var startingPlanets =
                        db.Planets.Where(x => x.GalaxyID == galaxyID && x.PlanetStructures.Any(y => y.StructureType.OwnerChangeWinsGame == true))
                            .OrderBy(x => x.Y)
                            .ThenBy(x => x.Y)
                            .Select(x => x.PlanetID)
                            .ToArray();

                for (int i = 0; i < facs.Count; i++)
                {
                    var pid = startingPlanets[i];
                    var planet = db.Planets.First(x => x.PlanetID == pid);
                    var faction = facs[i];
                    planet.PlanetFactions.Add(new PlanetFaction()
                    {
                        Faction = faction,
                        Influence = 100
                    });
                    planet.Faction = faction;
                }

                db.SaveChanges();

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
                        foreach (PlanetStructure s in planet.PlanetStructures.Where(x => x.StructureType.OwnerChangeWinsGame != true))
                        {
                            s.ReactivateAfterBuild();
                        }
                    }
                }
                System.Console.WriteLine("Structure activation set");

                gal.Turn = 0;
                gal.Started = DateTime.UtcNow;
                gal.Ended = null;
                gal.EndMessage = null;
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }
    }
}