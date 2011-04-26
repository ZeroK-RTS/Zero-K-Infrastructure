using System;
using System.Data.Linq.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PlasmaShared;
using ZkData;

namespace ZeroKWeb.Controllers
{
	public class PlanetwarsController: Controller
	{
		//
		// GET: /Planetwars/

		[Auth]
		public ActionResult BuildStructure(int planetID, int structureTypeID)
		{
			using (var db = new ZkDataContext())
			{
				var planet = db.Planets.Single(p => p.PlanetID == planetID);
				if (Global.Account.AccountID != planet.OwnerAccountID) return Content("Planet is not under control.");
				var structureType = db.StructureTypes.SingleOrDefault(s => s.StructureTypeID == structureTypeID);
				if (structureType == null) return Content("Structure type does not exist.");
				if (!structureType.IsBuildable) return Content("Structure is not buildable.");

				// assumes you can only build level 1 structures! if higher level structures can be built directly, we should check down the upgrade chain too
				if (StructureType.HasStructureOrUpgrades(db, planet, structureType)) return Content("Structure or its upgrades already built");

				if (Global.Account.Credits < structureType.Cost) return Content("Insufficient credits.");
				planet.Account.Credits -= structureType.Cost;

				var newBuilding = new PlanetStructure { StructureTypeID = structureTypeID, PlanetID = planetID };
				db.PlanetStructures.InsertOnSubmit(newBuilding);
				db.SubmitChanges();

				db.Events.InsertOnSubmit(Global.CreateEvent("{0} has built a {1} on {2}.", Global.Account, newBuilding.StructureType.Name, planet));
				SetPlanetOwners(db);
			}
			
			return RedirectToAction("Planet", new { id = planetID });
		}

		[Auth]
		public ActionResult CancelMarketOrder(int planetID, int offerID)
		{
			var db = new ZkDataContext();
			var offer = db.MarketOffers.SingleOrDefault(o => o.OfferID == offerID);
			if (offer == null) return Content("Error: offer does not exist");
			if (offer.AccountID != Global.AccountID) return Content("Error: can't cancel other people's offers");
			db.MarketOffers.DeleteOnSubmit(offer);
			db.SubmitChanges();
			return RedirectToAction("Planet", new { id = planetID });
		}

		[Auth]
		public ActionResult ChangePlayerRights(int clanID, int accountID)
		{
			var db = new ZkDataContext();
			var clan = db.Clans.Single(c => clanID == c.ClanID);
			if (!(Global.Account.HasClanRights && clan.ClanID == Global.Account.ClanID || Global.Account.IsZeroKAdmin)) return Content("Unauthorized");
			var kickee = db.Accounts.Single(a => a.AccountID == accountID);
			if (kickee.IsClanFounder) return Content("Clan founders can't be modified.");
			kickee.HasClanRights = !kickee.HasClanRights;
			var ev = Global.CreateEvent("{0} {1} {2} rights to clan {3}", Global.Account, kickee.HasClanRights ? "gave" : "took", kickee, clan);
			db.Events.InsertOnSubmit(ev);
			db.SubmitChanges();
			return RedirectToAction("Clan", new { id = clanID });
		}


		/// <summary>
		/// Shows clan page
		/// </summary>
		/// <returns></returns>
		public ActionResult Clan(int id)
		{
			var db = new ZkDataContext();
			var clan = db.Clans.First(x => x.ClanID == id);
			if (Global.ClanID == clan.ClanID)
			{
				if (clan.ForumThread != null)
				{
					clan.ForumThread.UpdateLastRead(Global.AccountID, false);
					db.SubmitChanges();
				}
			}
			return View(clan);
		}

		public ActionResult ClanList()
		{
			var db = new ZkDataContext();

			return View(db.Clans.AsQueryable());
		}


		[Auth]
		public ActionResult OfferTreaty(int targetClanID, AllyStatus ourStatus, string ourMessage, bool ourResearch)
		{
			if (!Global.Account.HasClanRights || Global.Clan == null) return Content("You don't have rights to do this");
			var db = new ZkDataContext();
			var clan = db.Clans.Single(x => x.ClanID == Global.ClanID);
			var targetClan = db.Clans.Single(x => x.ClanID == targetClanID);
			var oldEffect = clan.GetEffectiveTreaty(targetClan);
			var entry = clan.TreatyOffersByOfferingClanID.SingleOrDefault(x => x.TargetClanID == targetClanID);
			if (entry == null)
			{
				entry = new TreatyOffer() { OfferingClanID = clan.ClanID, TargetClanID = targetClanID };
				db.TreatyOffers.InsertOnSubmit(entry);
			}
			entry.OfferingClanMessage = ourMessage;
			entry.AllyStatus = ourStatus;
			entry.IsResearchAgreement = ourResearch;
			db.SubmitChanges();
			db.Events.InsertOnSubmit(Global.CreateEvent("{0} offers {1}, research: {2} to {3}", clan, ourStatus, ourResearch, targetClan));

			var newEffect = clan.GetEffectiveTreaty(targetClan);

			if (newEffect.AllyStatus != oldEffect.AllyStatus || newEffect.IsResearchAgreement != oldEffect.IsResearchAgreement)
			{
				db.Events.InsertOnSubmit(Global.CreateEvent("New effective treaty between {0} and {1}: {2}->{3}, research {4}->{5}",
				                                            clan,
				                                            targetClan,
				                                            oldEffect.AllyStatus,
				                                            newEffect.AllyStatus,
				                                            oldEffect.IsResearchAgreement,
				                                            newEffect.IsResearchAgreement));
			}
			db.SubmitChanges();

			return RedirectToAction("ClanDiplomacy", new { id = clan.ClanID });
		}

		[Auth]
		public ActionResult CreateClan()
		{
			if (Global.Account.Clan == null || (Global.Account.HasClanRights)) return View(Global.Clan ?? new Clan());
			else return Content("You already have clan and you dont have rights to it");
		}

		public ActionResult Events(int? planetID,
		                           int? accountID,
		                           int? springBattleID,
		                           int? clanID,
		                           string filter,
			int pageSize = 0,
			int page = 0,
															bool partial = false)
		{
			var db = new ZkDataContext();
			if (Request.IsAjaxRequest()) partial = true;
			if (pageSize == 0) if (!partial) pageSize = 40; else pageSize = 10;
			var res = db.Events.AsQueryable();
			if (planetID.HasValue) res = res.Where(x => x.EventPlanets.Any(y => y.PlanetID == planetID));
			if (accountID.HasValue) res = res.Where(x => x.EventAccounts.Any(y => y.AccountID == accountID));
			if (clanID.HasValue) res = res.Where(x => x.EventClans.Any(y => y.ClanID == clanID));
			if (springBattleID.HasValue) res = res.Where(x => x.EventSpringBattles.Any(y => y.SpringBattleID == springBattleID));
			if (!string.IsNullOrEmpty(filter)) res = res.Where(x => SqlMethods.Like(x.Text, string.Format("%{0}%", filter)));
			res = res.OrderByDescending(x => x.EventID);

			var ret = new EventsResult
			          {
			          	PageCount = (res.Count()/pageSize) + 1,
			          	Page = page,
			          	Events = res.Skip(page*pageSize).Take(pageSize),
			          	PlanetID = planetID,
			          	AccountID = accountID,
			          	SpringBattleID = springBattleID,
			          	Filter = filter,
			          	ClanID = clanID,
			          	Partial = partial
			          };

			return View(ret);
		}


		public Bitmap GenerateGalaxyImage(int galaxyID, double zoom = 1, double antiAliasingFactor = 4)
		{
			zoom *= antiAliasingFactor;
			using (var db = new ZkDataContext())
			{
				var gal = db.Galaxies.Single(x => x.GalaxyID == galaxyID);

				using (var background = Image.FromFile(Server.MapPath("/img/galaxies/" + gal.ImageName)))
				{
					var im = new Bitmap((int)(background.Width*zoom), (int)(background.Height*zoom));
					using (var gr = Graphics.FromImage(im))
					{
						gr.DrawImage(background, 0, 0, im.Width, im.Height);

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
						}

						foreach (var p in gal.Planets)
						{
							using (var pi = Image.FromFile(Server.MapPath("/img/planets/" + p.Resource.MapPlanetWarsIcon)))
							{
								var aspect = pi.Height/(double)pi.Width;
								var width = (int)(p.Resource.PlanetWarsIconSize*zoom);
								var height = (int)(width*aspect);
								gr.DrawImage(pi, (int)(p.X*im.Width) - width/2, (int)(p.Y*im.Height) - height/2, width, height);
							}
						}
						if (antiAliasingFactor == 1) return im;
						else
						{
							zoom /= antiAliasingFactor;
							return im.GetResized((int)(background.Width*zoom), (int)(background.Height*zoom), InterpolationMode.HighQualityBicubic);
						}
					}
				}
			}
		}

		[Auth]
		public ActionResult Give(int? planetID, int? giveInfluence, int? giveCredits, int targetAccountID)
		{
			var db = new ZkDataContext();
			var me = db.Accounts.Single(x => x.AccountID == Global.AccountID);
			var target = db.Accounts.Single(x => x.AccountID == targetAccountID);
			if (giveCredits > 0)
			{
				var creds = Math.Min(giveCredits ?? 0, me.Credits);
				me.Credits -= creds;
				target.Credits += creds;
				db.Events.InsertOnSubmit(Global.CreateEvent("{0} sends {1} credits to {2}", me, creds, target));
			}
			if (planetID > 0 && giveInfluence > 0)
			{
				var mePlanet = me.AccountPlanets.Single(x => x.PlanetID == planetID);
				var infl = Math.Min(giveInfluence ?? 0, mePlanet.Influence);
				var targetPlanet = target.AccountPlanets.SingleOrDefault(x => x.PlanetID == planetID);
				if (targetPlanet == null)
				{
					targetPlanet = new AccountPlanet() { AccountID = target.AccountID, PlanetID = planetID.Value };
					db.AccountPlanets.InsertOnSubmit(targetPlanet);
				}
				mePlanet.Influence -= infl;
				targetPlanet.Influence += infl;
				db.Events.InsertOnSubmit(Global.CreateEvent("{0} gives {1} influence on {2} to {3}", me, infl, mePlanet.Planet, target));
			}
			db.SubmitChanges();
			SetPlanetOwners(db);
			db.SubmitChanges();
			return RedirectToAction("Index", "Users", new { name = targetAccountID });
		}

		public ActionResult Index(int? galaxyID = null)
		{
			var db = new ZkDataContext();

			Galaxy gal;
			if (galaxyID != null) gal = db.Galaxies.Single(x => x.GalaxyID == galaxyID);
			else gal = db.Galaxies.Single(x => x.IsDefault);

			var cachePath = Server.MapPath(string.Format("/img/galaxies/render_{0}.jpg", gal.GalaxyID));
			if (gal.IsDirty || !System.IO.File.Exists(cachePath))
			{
				using (var im = GenerateGalaxyImage(gal.GalaxyID))
				{
					im.SaveJpeg(cachePath, 85);
					gal.IsDirty = false;
					gal.Width = im.Width;
					gal.Height = im.Height;
					db.SubmitChanges();
				}
			}
			return View("Galaxy", gal);
		}

		[Auth]
		public ActionResult JoinClan(int id, string password)
		{
			var db = new ZkDataContext();
			var clan = db.Clans.Single(x => x.ClanID == id);
			if (clan.CanJoin(Global.Account))
			{
				if (!string.IsNullOrEmpty(clan.Password) && clan.Password != password) return View(clan.ClanID);
				else
				{
					var acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);
					acc.ClanID = clan.ClanID;
					db.SubmitChanges();
					return RedirectToAction("Clan", new { id = clan.ClanID });
				}
			}
			else return Content("You cannot join this clan");
		}

		[Auth]
		public ActionResult KickPlayerFromClan(int clanID, int accountID)
		{
			var db = new ZkDataContext();
			var clan = db.Clans.Single(c => clanID == c.ClanID);
			// todo: disallow kicking after the round starts
			if (!(Global.Account.HasClanRights && clan.ClanID == Global.Account.ClanID)) return Content("Unauthorized");
			var kickee = db.Accounts.Single(a => a.AccountID == accountID);
			if (kickee.IsClanFounder) return Content("Clan founders can't be kicked.");
			foreach (var p in kickee.Planets.ToList()) p.OwnerAccountID = null; // disown his planets
			kickee.ClanID = null;
			db.SubmitChanges();
			return RedirectToAction("Clan", new { id = clanID });
		}

		public ActionResult Planet(int id)
		{
			var db = new ZkDataContext();
			var planet = db.Planets.Single(x => x.PlanetID == id);
			if (planet.ForumThread != null)
			{
				planet.ForumThread.UpdateLastRead(Global.AccountID, false);
				db.SubmitChanges();
			}
			return View(planet);
		}

		[Auth]
		public ActionResult RepairStructure(int planetID, int structureTypeID)
		{
			var db = new ZkDataContext();
			var planet = db.Planets.Single(p => p.PlanetID == planetID);
			if (Global.Account.AccountID != planet.OwnerAccountID) return Content("Planet is not under control.");
			var structure = db.PlanetStructures.SingleOrDefault(s => s.PlanetID == planetID && s.StructureTypeID == structureTypeID);
			if (!structure.IsDestroyed) return Content("Can't repair a working structure.");
			if (Global.Account.Credits < structure.StructureType.Cost) return Content("Insufficient credits.");
			planet.Account.Credits -= structure.StructureType.Cost;
			structure.IsDestroyed = false;
			db.Events.InsertOnSubmit(Global.CreateEvent("{0} has repaired a {1} on {2}.", Global.Account, structure.StructureType.Name, planet));
			db.SubmitChanges();
			SetPlanetOwners(db);
			return RedirectToAction("Planet", new { id = planetID });
		}


		[Auth]
		public ActionResult SendDropships(int planetID, int count)
		{
			var db = new ZkDataContext();
			var acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);

			var accessiblePlanets = Galaxy.AccessiblePlanets(db, acc.ClanID, AllyStatus.Alliance).Select(x => x.PlanetID).ToList();
			var accessible = accessiblePlanets.Any(x => x == planetID);
			if (!accessible)
			{
				var jumpGateCapacity = acc.Planets.SelectMany(x => x.PlanetStructures).Sum(x => x.StructureType.EffectWarpGateCapacity) ?? 0;
				var usedJumpGates = acc.AccountPlanets.Where(x => !accessiblePlanets.Contains(x.PlanetID)).Sum(x => x.DropshipCount);
				if (usedJumpGates >= jumpGateCapacity)
				{
					return
						Content(string.Format("Tha planet cannot be accessed via wormholes and your jumpgates are at capacity {0}/{1}", usedJumpGates, jumpGateCapacity));
				}
			}
			var cnt = Math.Max(count, 0);
			cnt = Math.Min(cnt, acc.DropshipCount);
			acc.DropshipCount = (acc.DropshipCount) - cnt;
			var planet = db.Planets.SingleOrDefault(x => x.PlanetID == planetID);
			var pac = acc.AccountPlanets.SingleOrDefault(x => x.PlanetID == planetID);
			if (pac == null)
			{
				pac = new AccountPlanet { AccountID = Global.AccountID, PlanetID = planetID };
				db.AccountPlanets.InsertOnSubmit(pac);
			}
			pac.DropshipCount += cnt;
			if (cnt > 0) db.Events.InsertOnSubmit(Global.CreateEvent("{0} sends {1} dropships to {2}", acc, cnt, planet));
			db.SubmitChanges();
			return RedirectToAction("Planet", new { id = planetID });
		}

		public ActionResult SetPlanetOwners()
		{
			using (var db = new ZkDataContext()) SetPlanetOwners(db);
			return Content("Done.");
		}


		public ActionResult ClanDiplomacy(int id)
		{
			return View("ClanDiplomacy", new ZkDataContext().Clans.Single(x => x.ClanID == id));
		}

		/// <summary>
		/// Updates shadow influence and new owners
		/// </summary>
		/// <param name="db"></param>
		/// <param name="sb">optional spring batle that caused this change (for event logging)</param>
		public static void SetPlanetOwners(ZkDataContext db, SpringBattle sb = null)
		{
			Galaxy.RecalculateShadowInfluence(db);
			var havePlanetsChangedHands = false;

			var gal = db.Galaxies.Single(x => x.IsDefault);
			foreach (var planet in gal.Planets)
			{
				var currentOwnerClanID = planet.Account != null ? planet.Account.ClanID : null;

				// in case of a tie when deciding which CLAN to get a planet - give to one with less planets
				var mostInfluentialClanEntry =
					planet.AccountPlanets.GroupBy(ap => ap.Account.Clan).Where(x => x.Key != null).Select(
							x => new { Clan = x.Key, ClanInfluence = (int?)x.Sum(y => y.Influence + y.ShadowInfluence) ?? 0 }).OrderByDescending(
						x => x.ClanInfluence).ThenBy(y => y.Clan.Accounts.Sum(z => z.Planets.Count())).FirstOrDefault();


				if ((mostInfluentialClanEntry == null || mostInfluentialClanEntry.Clan == null || mostInfluentialClanEntry.ClanInfluence == 0) && planet.Account != null) 
				{
					// disown the planet, nobody has right to own it atm
					db.Events.InsertOnSubmit(Global.CreateEvent("{0} of {2} has abandoned planet {1}. {3}", planet.Account, planet, planet.Account.Clan, sb));
					planet.Account = null;
					havePlanetsChangedHands = true;
				} else if (mostInfluentialClanEntry != null && mostInfluentialClanEntry.Clan != null && mostInfluentialClanEntry.Clan.ClanID != currentOwnerClanID &&
				    mostInfluentialClanEntry.ClanInfluence > planet.GetIPToCapture())
				{
					// planet changes owner, most influential clan is not current owner and has more ip to capture than needed

					havePlanetsChangedHands = true;

					foreach (var structure in planet.PlanetStructures.Where(structure => structure.StructureType.OwnerChangeDeletesThis).ToList()) planet.PlanetStructures.Remove(structure); //  delete structure

					// find who will own it
					// in case of a tie when deciding which PLAYER to get a planet - give it to one with least planets
					var mostInfluentialPlayer =
						planet.AccountPlanets.Where(x => x.Account.ClanID == mostInfluentialClanEntry.Clan.ClanID).OrderByDescending(x => x.Influence + x.ShadowInfluence).ThenBy(x => x.Account.Planets.Count()).First().Account;


					if (planet.OwnerAccountID == null) // no previous owner
					{
						planet.Account = mostInfluentialPlayer;
						db.Events.InsertOnSubmit(Global.CreateEvent("{0} has claimed planet {1} for {2}. {3}",
						                                            mostInfluentialPlayer,
						                                            planet,
						                                            mostInfluentialClanEntry.Clan,
						                                            sb));
					}
					else
					{
						db.Events.InsertOnSubmit(Global.CreateEvent("{0} of {3} has captured planet {1} from {2} of {4}. {5}",
						                                            mostInfluentialPlayer,
						                                            planet,
						                                            planet.Account,
						                                            mostInfluentialClanEntry.Clan,
																												planet.Account.Clan,
						                                            sb));
						planet.Account = mostInfluentialPlayer;
					}
				}
			}

			db.SubmitChanges();
			if (havePlanetsChangedHands)
			{
				SetPlanetOwners(db,sb); // we need another cycle because of shadow influence chain reactions
			}
		}

		[Auth]
		public ActionResult SubmitBuyOrder(int planetID, int quantity, int price)
		{
			return SubmitMarketOrder(planetID, quantity, price, false);
		}


		[Auth]
        public ActionResult SubmitCreateClan(Clan clan, HttpPostedFileBase image, HttpPostedFileBase bgimage)
		{
			var db = new ZkDataContext();
			var created = clan.ClanID == 0; // existing clan vs creation
			if (!created)
			{
				if (!Global.Account.HasClanRights || clan.ClanID != Global.Account.ClanID) return Content("Unauthorized");
				var orgClan = db.Clans.Single(x => x.ClanID == clan.ClanID);
				orgClan.ClanName = clan.ClanName;
				orgClan.LeaderTitle = clan.LeaderTitle;
				orgClan.Shortcut = clan.Shortcut;
				orgClan.Description = clan.Description;
				orgClan.SecretTopic = clan.SecretTopic;
				orgClan.Password = clan.Password;
				//orgClan.DbCopyProperties(clan); 
			}
			else
			{
				if (Global.Clan != null) return Content("You already have a clan");
				db.Clans.InsertOnSubmit(clan);
			}
			if (string.IsNullOrEmpty(clan.ClanName) || string.IsNullOrEmpty(clan.Shortcut)) return Content("Name and shortcut cannot be empty!");

			if (created && (image == null || image.ContentLength == 0)) return Content("Upload image");
			if (image != null && image.ContentLength > 0)
			{
				var im = Image.FromStream(image.InputStream);
				if (im.Width != 64 || im.Height != 64) im = im.GetResized(64, 64, InterpolationMode.HighQualityBicubic);
				db.SubmitChanges(); // needed to get clan id for image url - stupid way really
				im.Save(Server.MapPath(clan.GetImageUrl()));
			}
            if (bgimage != null && bgimage.ContentLength > 0)
            {
                var im = Image.FromStream(bgimage.InputStream);
                db.SubmitChanges(); // needed to get clan id for image url - stupid way really
                                    // DW - Actually its not stupid, its required to enforce locking.
                                    // It would be possbile to enforce a pre-save id
                im.Save(Server.MapPath(clan.GetBGImageUrl()));
            }
			db.SubmitChanges();

			if (created) // we created a new clan, set self as founder and rights
			{
				var acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);
				acc.ClanID = clan.ClanID;
				acc.IsClanFounder = true;
				acc.HasClanRights = true;
				db.SubmitChanges();
				db.Events.InsertOnSubmit(Global.CreateEvent("New clan {0} formed by {1}", clan, acc));
				db.SubmitChanges();
			}

			return RedirectToAction("Clan", new { id = clan.ClanID });
		}


		[Auth]
		public ActionResult SubmitMarketOrder(int planetID, int quantity, int price, bool isSell)
		{
			var db = new ZkDataContext();
			db.MarketOffers.InsertOnSubmit(new MarketOffer
			                               {
			                               	AccountID = Global.AccountID,
			                               	DatePlaced = DateTime.Now,
			                               	IsSell = isSell,
			                               	PlanetID = planetID,
			                               	Quantity = quantity,
			                               	Price = price,
			                               });
			db.Events.InsertOnSubmit(Global.CreateEvent("{0} {1} {2} influence for {3}/unit at {4}",Global.Account, isSell?"offers":"asks for", quantity, price, db.Planets.Single(x=>x.PlanetID==planetID)));
			db.SubmitChanges();
			ResolveMarketTransactions(db);
			return RedirectToAction("Planet", new { id = planetID });
		}


		[Auth]
		public ActionResult SubmitRenamePlanet(int planetID, string newName)
		{
			if (String.IsNullOrWhiteSpace(newName)) return Content("Error: the planet must have a name.");
			var db = new ZkDataContext();
			var planet = db.Planets.Single(p => p.PlanetID == planetID);
			if (Global.Account.AccountID != planet.OwnerAccountID) return Content("Unauthorized");
			db.SubmitChanges();
			db.Events.InsertOnSubmit(Global.CreateEvent("{0} renamed planet {1} from {2} to {3}", Global.Account, planet, planet.Name, newName));
			planet.Name = newName;
			db.SubmitChanges();
			return RedirectToAction("Planet", new { id = planet.PlanetID });
		}

		[Auth]
		public ActionResult SubmitSellOrder(int planetID, int quantity, int price)
		{
			return SubmitMarketOrder(planetID, quantity, price, true);
		}

		[Auth]
		public ActionResult UpgradeStructure(int planetID, int structureTypeID)
		{
			var db = new ZkDataContext();
			var planet = db.Planets.Single(p => p.PlanetID == planetID);
			if (Global.Account.AccountID != planet.OwnerAccountID) return Content("Planet is not under control.");
			var oldStructure = db.PlanetStructures.SingleOrDefault(s => s.PlanetID == planetID && s.StructureTypeID == structureTypeID);
			if (oldStructure == null) return Content("Structure does not exist");
			if (oldStructure.StructureType.UpgradesToStructureID == null) return Content("Structure can't be upgraded.");
			if (oldStructure.IsDestroyed) return Content("Can't upgrade a destroyed structure");

			var newStructureType = db.StructureTypes.Single(s => s.StructureTypeID == oldStructure.StructureType.UpgradesToStructureID);
			if (Global.Account.Credits < newStructureType.Cost) return Content("Insufficient credits.");
			planet.Account.Credits -= newStructureType.Cost;

			var newStructure = new PlanetStructure { PlanetID = planetID, StructureTypeID = newStructureType.StructureTypeID };

			db.PlanetStructures.InsertOnSubmit(newStructure);
			db.PlanetStructures.DeleteOnSubmit(oldStructure);

			db.SubmitChanges();
			db.Events.InsertOnSubmit(Global.CreateEvent("{0} has built a {1} on {2}.", Global.Account, newStructure.StructureType.Name, planet));

			db.SubmitChanges();
			SetPlanetOwners(db);
			return RedirectToAction("Planet", new { id = planetID });
		}

		// TODO: run at any change of influence, credits or market offers
		void ResolveMarketTransactions(ZkDataContext db)
		{
			var now = DateTime.Now;

			var currentOffers = db.MarketOffers.Where(o => o.DateAccepted == null);
			var buyOffers = currentOffers.Where(o => !o.IsSell);
			var sellOffers = currentOffers.Where(o => o.IsSell);

			var offers = from buyOffer in buyOffers
			             join sellOffer in sellOffers on buyOffer.PlanetID equals sellOffer.PlanetID
			             where buyOffer.Price >= sellOffer.Price
			             orderby sellOffer.Price
			             select new { Buy = buyOffer, Sell = sellOffer };

			var influenceChanged = false;

			foreach (var offerPair in offers)
			{
				var buyOffer = offerPair.Buy;
				var sellOffer = offerPair.Sell;

				var buyer = buyOffer.AccountByAccountID;
				var seller = sellOffer.AccountByAccountID;
				var sellerAccountPlanet = db.AccountPlanets.SingleOrDefault(ap => ap.AccountID == seller.AccountID && ap.PlanetID == buyOffer.PlanetID);

				if (sellerAccountPlanet == null) continue; // seller has nothing to sell

				var maxWillBuy = Math.Min(buyer.Credits/sellOffer.Price, buyOffer.Quantity);
				var maxWillSell = Math.Min(sellerAccountPlanet.Influence, sellOffer.Quantity);

				var quantity = Math.Min(maxWillBuy, maxWillSell);

				if (quantity > 0)
				{
					buyer.Credits -= quantity*sellOffer.Price;
					seller.Credits += quantity*sellOffer.Price;

					var buyerAccountPlanet = db.AccountPlanets.SingleOrDefault(ap => ap.AccountID == buyer.AccountID && ap.PlanetID == buyOffer.PlanetID);
					if (buyerAccountPlanet == null)
					{
						buyerAccountPlanet = new AccountPlanet { AccountID = buyer.AccountID, PlanetID = buyOffer.PlanetID };
						db.AccountPlanets.InsertOnSubmit(buyerAccountPlanet);
						db.SubmitChanges();
					}

					if (buyer.AccountID != seller.AccountID)
					{
						buyerAccountPlanet.Influence += quantity;
						sellerAccountPlanet.Influence -= quantity;
						influenceChanged = true;

						// record transactions, for history
						db.MarketOffers.InsertOnSubmit(new MarketOffer
						                               {
						                               	AcceptedAccountID = seller.AccountID,
						                               	AccountID = buyer.AccountID,
						                               	DateAccepted = now,
						                               	IsSell = false,
						                               	Price = sellOffer.Price,
						                               	DatePlaced = buyOffer.DatePlaced,
						                               	Quantity = quantity,
						                               	PlanetID = sellOffer.PlanetID,
						                               });

						db.MarketOffers.InsertOnSubmit(new MarketOffer
						                               {
						                               	AcceptedAccountID = buyer.AccountID,
						                               	AccountID = seller.AccountID,
						                               	DateAccepted = now,
						                               	IsSell = true,
						                               	Price = sellOffer.Price,
						                               	DatePlaced = sellOffer.DatePlaced,
						                               	Quantity = quantity,
						                               	PlanetID = sellOffer.PlanetID,
						                               });
						db.Events.InsertOnSubmit(Global.CreateEvent("{0} has purchased {1} influence from {2} on {3} for {4} each.",
						                                            buyer,
						                                            quantity,
						                                            seller,
						                                            buyOffer.Planet,
						                                            sellOffer.Price));
					}

					buyOffer.Quantity -= quantity;
					if (buyOffer.Quantity == 0) db.MarketOffers.DeleteOnSubmit(buyOffer);

					sellOffer.Quantity -= quantity;
					if (sellOffer.Quantity == 0) db.MarketOffers.DeleteOnSubmit(sellOffer);

					db.SubmitChanges();
				}
			}
			if (influenceChanged) SetPlanetOwners(db);
		}
	}

	public class EventsResult
	{
		public int? AccountID;
		public int? ClanID;
		public IQueryable<Event> Events;
		public string Filter;
		public int Page;
		public int PageCount;
		public bool Partial;
		public int? PlanetID;
		public int? SpringBattleID;
	}
}