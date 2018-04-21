﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Transactions;
using System.Web.Mvc;
using System.Data.Entity;
using PlasmaShared;
using ZkData;
using Ratings;

namespace ZeroKWeb.Controllers
{
    public class PlanetwarsController : Controller
    {
        //
        // GET: /Planetwars/

        [Auth]
        public ActionResult BombPlanet(int planetID, int count, bool? useWarp)
        {
            var db = new ZkDataContext();
            Account acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);
            if (acc.Faction == null) return Content("Join some faction first");
            Planet planet = db.Planets.Single(x => x.PlanetID == planetID);
            bool accessible =(useWarp == true) ? planet.CanBombersWarp(acc.Faction) : planet.CanBombersAttack(acc.Faction);
            if (!accessible) return Content("You cannot attack here");
            if (Global.Server.GetPlanetBattles(planet).Any(x => x.IsInGame)) return Content("Battle in progress on the planet, cannot bomb planet");

            bool selfbomb = acc.FactionID == planet.OwnerFactionID;
            if (count < 0) count = 0;
            double avail = Math.Min(count, acc.GetBombersAvailable());
            if (useWarp == true) avail = Math.Min(acc.GetWarpAvailable(), avail);

            var capa = acc.GetBomberCapacity();

            if (avail > capa) return Content("Too many bombers - the fleet limit is " + capa);

            if (avail > 0)
            {
                double defense = planet.PlanetStructures.Where(x => x.IsActive).Sum(x => x.StructureType.EffectBomberDefense) ?? 0;
                double effective = avail;
                if (!selfbomb) effective = effective - defense;

                if (effective <= 0) return Content("Enemy defenses completely block your ships");

                acc.SpendBombers(avail);
                if (useWarp == true) acc.SpendWarps(avail);

                var r = new Random();

                double strucKillChance = !selfbomb ? effective * GlobalConst.BomberKillStructureChance : 0;
                int strucKillCount = (int)Math.Floor(strucKillChance + r.NextDouble());

                double ipKillChance = effective * GlobalConst.BomberKillIpChance;
                int ipKillCount = (int)Math.Floor(ipKillChance + r.NextDouble());

                List<PlanetStructure> structs = planet.PlanetStructures.Where(x => x.StructureType.IsBomberDestructible).ToList();
                var bombed = new List<StructureType>();
                while (structs.Count > 0 && strucKillCount > 0)
                {
                    strucKillCount--;
                    PlanetStructure s = structs[r.Next(structs.Count)];
                    bombed.Add(s.StructureType);
                    structs.Remove(s);
                    db.PlanetStructures.DeleteOnSubmit(s);
                }

                double ipKillAmmount = ipKillCount * GlobalConst.BomberKillIpAmount;
                if (ipKillAmmount > 0)
                {
                    var influenceDecayMin = planet.PlanetStructures.Where(x => x.IsActive && x.StructureType.EffectPreventInfluenceDecayBelow != null).Select(x => x.StructureType.EffectPreventInfluenceDecayBelow).OrderByDescending(x => x).FirstOrDefault() ?? 0;


                    foreach (PlanetFaction pf in planet.PlanetFactions.Where(x => x.FactionID != acc.FactionID))
                    {
                        pf.Influence -= ipKillAmmount;
                        if (pf.Influence < 0) pf.Influence = 0;

                        // prevent bombing below influence decaymin for owner - set by active structures
                        if (pf.FactionID == planet.OwnerFactionID && pf.Influence < influenceDecayMin) pf.Influence = influenceDecayMin;
                    }


                }

                var args = new List<object>
                           {
                               acc,
                               acc.Faction,
                               !selfbomb ? planet.Faction : null,
                               planet,
                               avail,
                               defense,
                               useWarp == true ? "They attacked by warp. " : "",
                               ipKillAmmount
                           };
                args.AddRange(bombed);

                string str;
                if (selfbomb) str = "{0} of {1} bombed own planet {3} using {4} bombers against {5} defenses. {6}Ground armies lost {7} influence";
                else str = "{0} of {1} bombed {2} planet {3} using {4} bombers against {5} defenses. {6}Ground armies lost {7} influence";
                if (bombed.Count > 1)
                {
                    str += " and ";
                    int counter = 8;
                    foreach (var b in bombed)
                    {
                        str += "{" + counter + "}" + ", ";
                        counter++;
                    }
                    str += " were destroyed.";
                }
                else if (bombed.Count == 1)
                {
                    str += " and {8} was destroyed.";
                }
                else str += ".";

                db.Events.InsertOnSubmit(PlanetwarsEventCreator.CreateEvent(str, args.ToArray()));
            }

            db.SaveChanges();
            PlanetWarsTurnHandler.SetPlanetOwners(new PlanetwarsEventCreator());
            return RedirectToAction("Planet", new { id = planetID });
        }

        [Auth]
        public ActionResult BuildStructure(int planetID, int structureTypeID)
        {
            using (var db = new ZkDataContext())
            {
                Planet planet = db.Planets.Single(p => p.PlanetID == planetID);
                if (Global.Server.GetPlanetBattles(planet).Any(x => x.IsInGame)) return Content("Battle in progress on the planet, cannot build structures");
                Account acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);
                if (acc.FactionID != planet.OwnerFactionID) return Content("Planet is not under your control.");

                StructureType structureType = db.StructureTypes.SingleOrDefault(s => s.StructureTypeID == structureTypeID);
                if (structureType == null) return Content("Structure type does not exist.");
                if (!structureType.IsBuildable) return Content("Structure is not buildable.");

                if (acc.GetMetalAvailable() < structureType.Cost) return Content("Insufficient metal");
                acc.SpendMetal(structureType.Cost);

                var newBuilding = new PlanetStructure
                                  {
                                      StructureTypeID = structureTypeID,
                                      StructureType = structureType,
                                      PlanetID = planetID,
                                      OwnerAccountID = acc.AccountID,
                                  };
                newBuilding.ReactivateAfterBuild();

                db.PlanetStructures.InsertOnSubmit(newBuilding);
                db.SaveChanges();

                db.Events.InsertOnSubmit(PlanetwarsEventCreator.CreateEvent("{0} has built a {1} on {2} planet {3}.",
                                                            acc,
                                                            newBuilding.StructureType,
                                                            planet.Faction,
                                                            planet));
                PlanetWarsTurnHandler.SetPlanetOwners(new PlanetwarsEventCreator(), db);
            }
            return RedirectToAction("Planet", new { id = planetID });
        }

        /// <summary>
        /// Demolish an existing structure (not destroyed from bombing or such)
        /// </summary>
        [Auth]
        public ActionResult DestroyStructure(int planetID, int structureTypeID)
        {
            using (var db = new ZkDataContext())
            {
                Planet planet = db.Planets.Single(p => p.PlanetID == planetID);
                if (Global.Server.GetPlanetBattles(planet).Any(x => x.IsInGame)) return Content("Battle in progress on the planet, cannot destroy structures");
                Account acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);
                StructureType structureType = db.StructureTypes.SingleOrDefault(s => s.StructureTypeID == structureTypeID);
                Faction faction = planet.Faction;
                if (structureType == null) return Content("Structure type does not exist.");
                if (!structureType.IsBuildable) return Content("Structure is not buildable.");

                // assumes you can only build level 1 structures! if higher level structures can be built directly, we should check down the upgrade chain too

                if (!planet.PlanetStructures.Any(x => x.StructureTypeID == structureTypeID)) return Content("Structure or its upgrades not present");

                List<PlanetStructure> list = planet.PlanetStructures.Where(x => x.StructureTypeID == structureTypeID).ToList();
                PlanetStructure toDestroy = list[0];
                if (!toDestroy.IsActive) return Content("Structure is currently disabled");
                var canDestroy = toDestroy.OwnerAccountID == acc.AccountID || planet.OwnerAccountID == acc.AccountID;
                if (!canDestroy) return Content("Structure is not under your control.");
                var refund = toDestroy.StructureType.Cost * GlobalConst.SelfDestructRefund;
                if (toDestroy.Account != null) toDestroy.Account.ProduceMetal(refund);
                else faction?.ProduceMetal(refund);
                db.Events.InsertOnSubmit(PlanetwarsEventCreator.CreateEvent("{0} has demolished a {1} on {2}.", acc, toDestroy.StructureType, planet));
                db.PlanetStructures.DeleteOnSubmit(toDestroy);
                db.SaveChanges();
                PlanetWarsTurnHandler.SetPlanetOwners(new PlanetwarsEventCreator(), db);
            }

            return RedirectToAction("Planet", new { id = planetID });
        }


        public ActionResult Events(int? planetID,
                                   int? accountID,
                                   int? springBattleID,
                                   int? clanID,
                                   int? factionID,
                                   string filter,
                                   int pageSize = 0,
                                   int page = 0,
                                   bool partial = false)
        {
            var db = new ZkDataContext();
            if (Request.IsAjaxRequest()) partial = true;
            if (pageSize == 0)
            {
                if (!partial) pageSize = 40;
                else pageSize = 10;
            }
            IQueryable<Event> res = db.Events.AsQueryable();
            if (planetID.HasValue) res = res.Where(x => x.Planets.Any(y => y.PlanetID == planetID));
            if (accountID.HasValue) res = res.Where(x => x.Accounts.Any(y => y.AccountID == accountID));
            if (clanID.HasValue) res = res.Where(x => x.Clans.Any(y => y.ClanID == clanID));
            if (springBattleID.HasValue) res = res.Where(x => x.SpringBattles.Any(y => y.SpringBattleID == springBattleID));
            if (factionID.HasValue) res = res.Where(x => x.Factions.Any(y => y.FactionID == factionID));
            if (!string.IsNullOrEmpty(filter)) res = res.Where(x => x.Text.Contains(filter));
            res = res.OrderByDescending(x => x.EventID);

            var ret = new EventsResult
                      {
                          PageCount = (res.Count() / pageSize) + 1,
                          Page = page,
                          Events = res.Skip(page * pageSize).Take(pageSize),
                          PlanetID = planetID,
                          AccountID = accountID,
                          SpringBattleID = springBattleID,
                          Filter = filter,
                          ClanID = clanID,
                          Partial = partial,
                          PageSize = pageSize
                      };

            return View(ret);
        }

        /// <summary>
        /// Makes an image: galaxy background with planet images drawn on it (cheaper than rendering each planet individually)
        /// </summary>
        // FIXME: having issues with bitmap parameters; setting AA factor to 1 as fallback (was 4)
        public Bitmap GenerateGalaxyImage(int galaxyID, double zoom = 1, double antiAliasingFactor = 1)
        {
            zoom *= antiAliasingFactor;
            using (var db = new ZkDataContext())
            {
                Galaxy gal = db.Galaxies.Single(x => x.GalaxyID == galaxyID);

                using (Image background = Image.FromFile(Server.MapPath("/img/galaxies/" + gal.ImageName)))
                {
                    //var im = new Bitmap((int)(background.Width*zoom), (int)(background.Height*zoom));
                    var im = new Bitmap(background.Width, background.Height);
                    using (Graphics gr = Graphics.FromImage(im))
                    {
                        gr.DrawImage(background, 0, 0, im.Width, im.Height);

                        /*
						using (var pen = new Pen(Color.FromArgb(255, 180, 180, 180), (int)(1*zoom)))
						{
							foreach (var l in gal.Links)
							{
								gr.DrawLine(pen,
								            (int)(l.PlanetByPlanetID1.X*im.Width),
								            (int)(l.PlanetByPlanetID1.Y*im.Height),
								            (int)(l.PlanetByPlanetID2.X*im.Width),
								            (int)(l.PlanetByPlanetID2.Y*im.Height));
							}
						}*/

                        foreach (Planet p in gal.Planets)
                        {
                            string planetIconPath = null;
                            try
                            {
                                planetIconPath = "/img/planets/" + (p.Resource.MapPlanetWarsIcon ?? "1.png"); // backup image is 1.png
                                using (Image pi = Image.FromFile(Server.MapPath(planetIconPath)))
                                {
                                    double aspect = pi.Height / (double)pi.Width;
                                    var width = (int)(p.Resource.PlanetWarsIconSize * zoom);
                                    var height = (int)(width * aspect);
                                    gr.DrawImage(pi, (int)(p.X * im.Width) - width / 2, (int)(p.Y * im.Height) - height / 2, width, height);
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new ApplicationException(
                                    string.Format("Cannot process planet image {0} for planet {1} map {2}",
                                                  planetIconPath,
                                                  p.PlanetID,
                                                  p.MapResourceID),
                                    ex);
                            }
                        }
                        if (antiAliasingFactor == 1) return im;
                        else
                        {
                            zoom /= antiAliasingFactor;
                            return im.GetResized((int)(background.Width * zoom), (int)(background.Height * zoom), InterpolationMode.HighQualityBicubic);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Go to main Planetwars page
        /// </summary>
        public ActionResult Index(int? galaxyID = null)
        {
            var db = new ZkDataContext();
            
            Galaxy gal;
            if (galaxyID != null) gal = db.Galaxies.Single(x => x.GalaxyID == galaxyID);
            else gal = db.Galaxies.Single(x => x.IsDefault);

            string cachePath = Server.MapPath(string.Format("/img/galaxies/render_{0}.jpg", gal.GalaxyID));
            if (gal.IsDirty || !System.IO.File.Exists(cachePath))
            {
                using (Bitmap im = GenerateGalaxyImage(gal.GalaxyID))
                {
                    im.SaveJpeg(cachePath, 85);
                    gal.IsDirty = false;
                    gal.Width = im.Width;
                    gal.Height = im.Height;
                    db.SaveChanges();
                }
            }
            
            return View("Galaxy", gal);
        }


        public ActionResult Minimap()
        {
            var db = new ZkDataContext();

            return View(db.Galaxies.Single(g => g.IsDefault));
        }


        public ActionResult Planet(int id)
        {
            var db = new ZkDataContext();
            ViewBag.Db = db;
            Planet planet = db.Planets.Single(x => x.PlanetID == id);
            if (planet.ForumThread != null)
            {
                planet.ForumThread.UpdateLastRead(Global.AccountID, false);
                db.SaveChanges();
            }
            return View(planet);
        }

        [Auth]
        public ActionResult SendDropships(int planetID, int count, bool? useWarp)
        {
            var db = new ZkDataContext();
            Account acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);
            if (acc.Faction == null) return Content("Join a faction first");
            Planet planet = db.Planets.SingleOrDefault(x => x.PlanetID == planetID);
            int there = planet.PlanetFactions.Where(x => x.FactionID == acc.FactionID).Sum(x => (int?)x.Dropships) ?? 0;
            bool accessible = useWarp == true ? planet.CanDropshipsWarp(acc.Faction) : planet.CanDropshipsAttack(acc.Faction);
            if (!accessible) return Content(string.Format("That planet cannot be attacked"));
            if (Global.Server.GetPlanetBattles(planet).Any(x => x.IsInGame)) return Content("Battle in progress on the planet, cannot send ships");
            
            int cnt = Math.Max(count, 0);

            int capa = acc.GetDropshipCapacity();

            if (cnt + there > capa) return Content("Too many dropships on planet - the fleet limit is " + capa);
            cnt = Math.Min(cnt, (int)acc.GetDropshipsAvailable());
            if (useWarp == true) cnt = Math.Min(cnt, (int)acc.GetWarpAvailable());
            if (cnt > 0)
            {
                acc.SpendDropships(cnt);
                if (useWarp == true)
                {
                    acc.SpendWarps(cnt);
                    if (cnt < GlobalConst.DropshipsForFullWarpIPGain) return Content($"You must send at least {GlobalConst.DropshipsForFullWarpIPGain} dropships when warping");
                }

                if (planet.Account != null) {
                    Global.Server.GhostPm(planet.Account.Name, string.Format(
                        "Warning: long range scanners detected fleet of {0} ships inbound to your planet {1} {3}/Planetwars/Planet/{2}",
                        cnt,
                        planet.Name,
                        planet.PlanetID,
                        GlobalConst.BaseSiteUrl));
                }
                PlanetFaction pac = planet.PlanetFactions.SingleOrDefault(x => x.FactionID == acc.FactionID);
                if (pac == null)
                {
                    pac = new PlanetFaction { FactionID = Global.FactionID, PlanetID = planetID };
                    db.PlanetFactions.InsertOnSubmit(pac);
                }
                pac.Dropships += cnt;
                pac.DropshipsLastAdded = DateTime.UtcNow;

                if (cnt > 0)
                {
                    db.Events.InsertOnSubmit(PlanetwarsEventCreator.CreateEvent("{0} sends {1} {2} dropships to {3} {4} {5}",
                                                                acc,
                                                                cnt,
                                                                acc.Faction,
                                                                planet.Faction,
                                                                planet,
                                                                useWarp == true ? "using warp drives" : ""));
                }
                db.SaveChanges();
            }
            return RedirectToAction("Planet", new { id = planetID });
        }

        public ActionResult RunSetPlanetOwners()
        {
            using (var db = new ZkDataContext()) PlanetWarsTurnHandler.SetPlanetOwners(new PlanetwarsEventCreator(), db);
            return Content("Done.");
        }








        [Auth(Role = AdminLevel.Moderator)]
        public ActionResult SubmitRenamePlanet(int planetID, string newName, int teamSize, string map)
        {
            using (var scope = new TransactionScope())
            {
                if (String.IsNullOrWhiteSpace(newName)) return Content("Error: the planet must have a name.");
                var db = new ZkDataContext();
                var acc = db.Accounts.Find(Global.AccountID);
                Planet planet = db.Planets.Single(p => p.PlanetID == planetID);
                if (planet.Name != newName) db.Events.InsertOnSubmit(PlanetwarsEventCreator.CreateEvent("{0} renamed planet {1} to {2}", acc, planet, newName));
                db.SaveChanges();
                planet.Name = newName;
                planet.TeamSize = teamSize;
                planet.Resource = db.Resources.Where(x => x.TypeID == ResourceType.Map && x.InternalName == map).First();
                planet.Galaxy.IsDirty = true;
                db.SaveChanges();
                scope.Complete();
                return RedirectToAction("Planet", new { id = planet.PlanetID });
            }
        }


        public ActionResult RecallRole(int accountID, int roletypeID)
        {
            var db = new ZkDataContext();
            Account targetAccount = db.Accounts.Single(x => x.AccountID == accountID);
            Account myAccount = db.Accounts.Single(x => x.AccountID == Global.AccountID);
            RoleType role = db.RoleTypes.Single(x => x.RoleTypeID == roletypeID);
            if (myAccount.CanRecall(targetAccount, role))
            {
                db.AccountRoles.DeleteAllOnSubmit(db.AccountRoles.Where(x => x.AccountID == accountID && x.RoleTypeID == roletypeID));
                db.Events.InsertOnSubmit(PlanetwarsEventCreator.CreateEvent("{0} was recalled from the {1} role of {2} by {3}",
                                                            targetAccount,
                                                            role.IsClanOnly ? (object)myAccount.Clan : myAccount.Faction,
                                                            role,
                                                            myAccount));
                Global.Server.GhostPm(targetAccount.Name, string.Format("You were recalled from the function of {0} by {1}", role.Name, myAccount.Name));
                db.SaveChanges();
                return RedirectToAction("Detail", "Users", new { id = accountID });
            }
            else return Content("Cannot recall");
        }

        public ActionResult AppointRole(int accountID, int roletypeID)
        {
            var db = new ZkDataContext();
            Account targetAccount = db.Accounts.Single(x => x.AccountID == accountID);
            Account myAccount = db.Accounts.Single(x => x.AccountID == Global.AccountID);
            RoleType role = db.RoleTypes.Single(x => x.RoleTypeID == roletypeID);
            if (myAccount.CanAppoint(targetAccount, role))
            {
                Account previous = null;
                if (role.IsOnePersonOnly)
                {
                    List<AccountRole> entries =
                        db.AccountRoles.Where(
                        x => x.RoleTypeID == role.RoleTypeID && (role.IsClanOnly ? x.ClanID == myAccount.ClanID : x.FactionID == myAccount.FactionID)).ToList();
                    if (entries.Any())
                    {
                        previous = entries.First().Account;
                        db.AccountRoles.DeleteAllOnSubmit(entries);
                    }
                }
                var entry = new AccountRole
                            {
                                AccountID = accountID,
                                Inauguration = DateTime.UtcNow,
                                Clan = role.IsClanOnly ? myAccount.Clan : null,
                                Faction = !role.IsClanOnly ? myAccount.Faction : null,
                                RoleTypeID = roletypeID,
                            };
                db.AccountRoles.InsertOnSubmit(entry);
                if (previous != null)
                {
                    db.Events.InsertOnSubmit(PlanetwarsEventCreator.CreateEvent("{0} was appointed to the {1} role of {2} by {3} - replacing {4}",
                                                                targetAccount,
                                                                role.IsClanOnly ? (object)myAccount.Clan : myAccount.Faction,
                                                                role,
                                                                myAccount,
                                                                previous));
                }
                else
                {
                    db.Events.InsertOnSubmit(PlanetwarsEventCreator.CreateEvent("{0} was appointed to the {1} role of {2} by {3}",
                                                                targetAccount,
                                                                role.IsClanOnly ? (object)myAccount.Clan : myAccount.Faction,
                                                                role,
                                                                myAccount));
                }
                Global.Server.GhostPm(targetAccount.Name, string.Format("You were appointed for the function of {0} by {1}", role.Name, myAccount.Name));
                db.SaveChanges();
                return RedirectToAction("Detail", "Users", new { id = accountID });
            }
            else return Content("Cannot recall");
        }

        [Auth]
        public ActionResult ConfiscateStructure(int planetID, int structureTypeID)
        {
            using (var db = new ZkDataContext())
            {
                Planet planet = db.Planets.Single(p => p.PlanetID == planetID);
                //if (Global.Nightwatch.GetPlanetBattles(planet).Any(x => x.IsInGame)) return Content("Battle in progress on the planet, cannot destroy structures");
                Account acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);
                bool factionLeader = acc.HasFactionRight(x => x.RightMetalQuota > 0) && (acc.Faction == planet.Faction);
                if ((planet.OwnerAccountID != acc.AccountID) && !factionLeader) return Content("Planet not yours");
                StructureType structureType = db.StructureTypes.SingleOrDefault(s => s.StructureTypeID == structureTypeID);
                if (structureType == null) return Content("Structure type does not exist.");
                //if (!structureType.IsBuildable) return Content("Structure is not buildable.");

                if (!planet.PlanetStructures.Any(x => x.StructureTypeID == structureTypeID)) return Content("Structure or its upgrades not present");
                List<PlanetStructure> list = planet.PlanetStructures.Where(x => x.StructureTypeID == structureTypeID).ToList();
                PlanetStructure structure = list[0];
                Account orgAc = structure.Account;
                double cost = (!factionLeader) ? structure.StructureType.Cost : 0;
                if (orgAc != null)
                {
                    structure.Account.SpendMetal(-cost);
                    acc.SpendMetal(cost);
                }
                structure.Account = acc;
                db.Events.InsertOnSubmit(PlanetwarsEventCreator.CreateEvent("{0} has confiscated {1} structure {2} on {3}.",
                                                            acc,
                                                            orgAc,
                                                            structure.StructureType,
                                                            planet));
                db.SaveChanges();

                return RedirectToAction("Planet", new { id = planetID });
            }
        }

        [Auth]
        public ActionResult SetEnergyPriority(int planetID, int structuretypeID, EnergyPriority priority)
        {
            var db = new ZkDataContext();
            var acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);
            var planet = db.Planets.Single(x => x.PlanetID == planetID);
            var structure = planet.PlanetStructures.Single(x => x.StructureTypeID == structuretypeID);
            if (!acc.CanSetPriority(structure)) return Content("Cannot set priority");
            structure.EnergyPriority = priority;
            db.Events.InsertOnSubmit(PlanetwarsEventCreator.CreateEvent("{0} changed energy priority of {1} on {2} to {3}", acc, structure.StructureType, planet, priority));
            db.SaveChanges();
            return RedirectToAction("Planet", new { id = planet.PlanetID });
        }

        [Auth]
        public ActionResult SetStructureTarget(int planetID, int structureTypeID, int targetPlanetID)
        {
            var db = new ZkDataContext();
            var acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);
            var planet = db.Planets.Single(x => x.PlanetID == planetID);
            var structure = planet.PlanetStructures.Single(x => x.StructureTypeID == structureTypeID);
            if (!acc.CanSetStructureTarget(structure)) return Content("Cannot set target");
            var target = db.Planets.Single(x => x.PlanetID == targetPlanetID);
            if (target != structure.PlanetByTargetPlanetID)
            {
                structure.ReactivateAfterBuild();
            }
          

            structure.PlanetByTargetPlanetID = target;
            db.Events.InsertOnSubmit(PlanetwarsEventCreator.CreateEvent("{0} of {1} aimed {2} located at {3} to {4} planet {5}", acc, acc.Faction, structure.StructureType, planet, target.Faction, target));

            if (structure.IsActive && !structure.StructureType.IsSingleUse) return ActivateTargetedStructure(planetID, structureTypeID);

            db.SaveChanges();
            return RedirectToAction("Planet", new { id = planet.PlanetID });
        }

        [Auth]
        public ActionResult ActivateTargetedStructure(int planetID, int structureTypeID)
        {
            //This call is never to be called before/outside of SetStructureTarget
            var db = new ZkDataContext();
            //Get the pre-set target planet ID
            PlanetStructure structure = db.PlanetStructures.FirstOrDefault(x => x.PlanetID == planetID && x.StructureTypeID == structureTypeID);
            int targetID = structure.TargetPlanetID ?? -1;
            if (targetID == -1) return Content("Structure has no target");

            if (!structure.IsActive) return Content(String.Format("Structure {0} is inactive", structure.StructureType.Name));

            ActionResult ret = null;
            if (structure.StructureType.EffectCreateLink == true)
            {
                ret = CreateLink(planetID, structureTypeID, targetID);
            }
            if (ret != null) return ret;    // exit with message if error occurs
            if (structure.StructureType.EffectChangePlanetMap == true)
            {
                ret = ChangePlanetMap(planetID, structureTypeID, targetID, null);
            }
            if (ret != null) return ret;    // exit with message if error occurs

            if (structure.StructureType.EffectPlanetBuster == true)
            {
                ret = FirePlanetBuster(planetID, structureTypeID, targetID);
            }
            if (ret != null) return ret;    // exit with message if error occurs
            if (structure.StructureType.IsSingleUse)    // single-use structure, remove
            {
                db.PlanetStructures.DeleteOnSubmit(structure);
            }

            db.SaveChanges();
            PlanetWarsTurnHandler.SetPlanetOwners(new PlanetwarsEventCreator(), db);     //this is needed for the buster to update ownership after planet destruction

            if (ret != null) return ret;
            return RedirectToAction("Planet", new { id = planetID });
        }

        [Auth]
        public ActionResult CreateLink(int planetID, int structureTypeID, int targetID)
        {
            var db = new ZkDataContext();

            PlanetStructure structure = db.PlanetStructures.FirstOrDefault(x => x.PlanetID == planetID && x.StructureTypeID == structureTypeID);
            Planet source = db.Planets.FirstOrDefault(x => x.PlanetID == planetID);
            Planet target = db.Planets.FirstOrDefault(x => x.PlanetID == targetID);

            // warp jammers protect against link creation
            //var warpDefense = target.PlanetStructures.Where(x => x.StructureType.EffectBlocksJumpgate == true).ToList();
            //if (warpDefense.Count > 0) return Content("Warp jamming prevents link creation");

            if (source.GalaxyID != target.GalaxyID) return Content("Cannot form exo-galaxy link");

            db.Links.InsertOnSubmit(new Link { PlanetID1 = source.PlanetID, PlanetID2 = target.PlanetID, GalaxyID = source.GalaxyID });
            db.Events.InsertOnSubmit(PlanetwarsEventCreator.CreateEvent("A new link was created between {0} planet {1} and {2} planet {3} by the {4}", source.Faction, source, target.Faction, target, structure.StructureType));
            db.SaveChanges();
            return null;
        }

        [Auth]
        private ActionResult FirePlanetBuster(int planetID, int structureTypeID, int targetID)
        {
            var db = new ZkDataContext();
            PlanetStructure structure = db.PlanetStructures.FirstOrDefault(x => x.PlanetID == planetID && x.StructureTypeID == structureTypeID);
            Planet source = db.Planets.FirstOrDefault(x => x.PlanetID == planetID);
            Planet target = db.Planets.FirstOrDefault(x => x.PlanetID == targetID);

            Account acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);
            if (acc.Faction == null) return Content("Join some faction first");
            if (!target.CanFirePlanetBuster(acc.Faction)) return Content("You cannot attack here");

            //Get rid of all strutures
            var structures = target.PlanetStructures.Where(x => x.StructureType.EffectIsVictoryPlanet != true && x.StructureType.OwnerChangeWinsGame != true).ToList();


            //kill all IP
            foreach (var pf in target.PlanetFactions.Where(x => x.Influence > 0))
            {
                pf.Influence = 0;
            }

            db.Events.InsertOnSubmit(PlanetwarsEventCreator.CreateEvent("A {4} fired from {0} {1} has destroyed {2} {3}!", source.Faction, source, target.Faction, target, structure.StructureType));
            db.SaveChanges();

            db.PlanetStructures.DeleteAllOnSubmit(structures);
            var residue = db.StructureTypes.First(x => x.Name == "Residue"); // todo not nice use constant instead
            target.PlanetStructures.Add(new PlanetStructure() { StructureType = residue, IsActive = true});
            db.SaveChanges();

            return null;
        }

        private ActionResult ChangePlanetMap(int planetID, int structureTypeID, int targetID, int? newMapID)
        {
            var db = new ZkDataContext();
            PlanetStructure structure = db.PlanetStructures.FirstOrDefault(x => x.PlanetID == planetID && x.StructureTypeID == structureTypeID);
            Planet source = db.Planets.FirstOrDefault(x => x.PlanetID == planetID);
            Planet target = db.Planets.FirstOrDefault(x => x.PlanetID == targetID);
            Galaxy gal = db.Galaxies.FirstOrDefault(x => x.GalaxyID == source.GalaxyID);

            if (newMapID == null)
            {
                List<Resource> mapList =
                    db.Resources.Where(
                        x =>
                            x.MapPlanetWarsIcon != null && x.Planets.Where(p => p.GalaxyID == gal.GalaxyID).Count() == 0 && x.MapSupportLevel >= MapSupportLevel.Featured &&
                            x.ResourceID != source.MapResourceID).ToList();
                if (mapList.Count > 0)
                {
                    int r = new Random().Next(mapList.Count);
                    newMapID = mapList[r].ResourceID;
                }
            }
            if (newMapID != null)
            {
                Resource newMap = db.Resources.Single(x => x.ResourceID == newMapID);
                target.Resource = newMap;
                gal.IsDirty = true;
                string word = "";
                if (target.Faction == source.Faction)
                {
                    if (target.TeamSize < GlobalConst.PlanetWarsMaxTeamsize) target.TeamSize++;
                    word = "terraformed";
                }
                else
                {
                    word = "nanodegraded";
                    if (target.TeamSize > 1) target.TeamSize--;
                }

                db.Events.InsertOnSubmit(PlanetwarsEventCreator.CreateEvent("{0} {1} has been {6} by {2} from {3} {4}. New team size is {5} vs {5}",
                    target.Faction,
                    target,
                    structure.StructureType,
                    source.Faction,
                    source,
                    target.TeamSize,
                    word));
            }
            else
            {
                return
                    Content(string.Format("Terraform attempt on {0} {1} using {2} from {3} {4} has failed - no valid maps",
                        target.Faction,
                        target,
                        structure.StructureType,
                        source.Faction,
                        source));
            }
            db.SaveChanges();
            return null;
        }

        public ActionResult Ladder()
        {
            var ret = MemCache.GetCached("pwLadder",
                () =>
                {
                    ZkDataContext db = new ZkDataContext();
                    var gal = db.Galaxies.First(x => x.IsDefault);
                    DateTime minDate = gal.Started ?? DateTime.UtcNow;
                    List<PwLadder> items = db.Accounts.Where(x => x.FactionID != null && x.LastLogin > minDate && x.SpringBattlePlayers.Any(y => y.SpringBattle.StartTime > minDate && !y.IsSpectator && y.SpringBattle.Mode == AutohostMode.Planetwars)).ToList().GroupBy(x => x.Faction)
                            .Select(
                                x =>
                                    new PwLadder
                                    {
                                        Faction = x.Key,
                                        Top10 =
                                            x.OrderByDescending(y => y.PwAttackPoints)
                                                .ThenByDescending(y => y.AccountRatings.Where(r => r.RatingCategory == RatingCategory.Planetwars).Select(r => r.Elo).DefaultIfEmpty(WholeHistoryRating.DefaultRating.RealElo).FirstOrDefault())
                                                .Take(10)
                                                .ToList()
                                    })
                            .ToList();
                    return items;
                },
                60 * 2);
            return View("Ladder", ret);
        }

        [Auth]
        public ActionResult MatchMakerAttack(int planetID)
        {
            var db = new ZkDataContext();
            var planet = db.Planets.Single(x => x.PlanetID == planetID);
            if (Global.IsAccountAuthorized && Global.Account.CanPlayerPlanetWars() && planet.CanMatchMakerPlay(db.CurrentAccount().Faction))
            {
                Global.Server.PlanetWarsMatchMaker.AddAttackOption(planet);
                Global.Server.RequestJoinPlanet(Global.Account.Name, planet.PlanetID);
            }
            return RedirectToAction("Planet", new { id = planetID });
        }


        [Auth]
        public ActionResult MatchMaker()
        {
            var pwm = Global.Server.PlanetWarsMatchMaker;
            if (pwm != null)
            {
                var state = Global.Server.PlanetWarsMatchMaker.GenerateLobbyCommand();
                if (state != null) return View("PwMatchMaker", state);
            }
            return Content("Match maker offline");
        }

        [Auth]
        public ActionResult MatchMakerJoin(int planetID)
        {
            Global.Server.RequestJoinPlanet(Global.Account.Name,  planetID);
            return MatchMaker();
        }


        [Auth]
        public ActionResult RushActivation(int planetID, int structureTypeID)
        {
            using (var db = new ZkDataContext())
            {
                var planet = db.Planets.Single(p => p.PlanetID == planetID);
                var acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);
                var structure = planet.PlanetStructures.Single(x => x.StructureTypeID == structureTypeID);
                if (structure.RushStructure(acc))
                    db.Events.InsertOnSubmit(PlanetwarsEventCreator.CreateEvent("{0} has rushed activation of {1} on {2} planet {3}.",
                        acc,
                        structure.StructureType,
                        planet.Faction,
                        planet));
                else return Content("You cannot rush this");

                db.SaveChanges();

                return RedirectToAction("Planet", new { id = planetID });
            }
        }
    }

    #region Nested type: ClanEntry

    public class ClanEntry
    {
        readonly Clan clan;
        readonly int clanInfluence;
        public int ShadowInfluence;

        public ClanEntry(Clan clan, int clanInfluence)
        {
            this.clan = clan;
            this.clanInfluence = clanInfluence;
        }

        public Clan Clan { get { return clan; } }
        public int ClanInfluence { get { return clanInfluence; } }
    }

    #endregion

    #region Nested type: EventsResult

    public class EventsResult
    {
        public int? AccountID;
        public int? ClanID;
        public IQueryable<Event> Events;
        public string Filter;
        public int Page;
        public int PageCount;
        public int PageSize;
        public bool Partial;
        public int? PlanetID;
        public int? SpringBattleID;
    }

    #endregion

    #region Nested type: PwLadder

    public class PwLadder
    {
        public Faction Faction;
        public List<Account> Top10;
    }

    #endregion
}
