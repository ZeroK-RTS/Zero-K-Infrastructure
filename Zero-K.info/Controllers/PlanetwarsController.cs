using System;
using System.Data.Linq.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Transactions;
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
		                           int page = 0,
		                           int pageSize = 10,
		                           bool partial = false)
		{
			var db = new ZkDataContext();
			if (Request.IsAjaxRequest()) partial = true;
			var res = db.Events.AsQueryable();
			if (planetID.HasValue) res = res.Where(x => x.EventPlanets.Any(y => y.PlanetID == planetID));
			if (accountID.HasValue) res = res.Where(x => x.EventAccounts.Any(y => y.AccountID == accountID));
			if (clanID.HasValue) res = res.Where(x => x.EventClans.Any(y => y.ClanID == clanID));
			if (springBattleID.HasValue) res = res.Where(x => x.EventSpringBattles.Any(y => y.SpringBattleID == clanID));
			if (!string.IsNullOrEmpty(filter)) res = res.Where(x => SqlMethods.Like(x.Text, string.Format("%{0}%", filter)));
			res = res.OrderByDescending(x => x.EventID);

			var ret = new EventsResult
			          {
			          	PageCount = (res.Count()/pageSize) + 1,
			          	Page = page,
			          	Events = res.Skip(page*pageSize).Take(pageSize),
			          	PageSize = pageSize,
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
					im.Save(cachePath);
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
		public ActionResult SendDropships(int planetID, int count)
		{
			var db = new ZkDataContext();
			var acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);

			var accessiblePlanets = ZkData.Galaxy.AccessiblePlanets(db, acc.ClanID, AllyStatus.Alliance).Select(x => x.PlanetID).ToList();
			var accessible = accessiblePlanets.Any(x => x == planetID);
			if (!accessible)
			{
				var jumpGateCapacity = acc.Planets.SelectMany(x => x.PlanetStructures).Sum(x => x.StructureType.EffectWarpGateCapacity) ?? 0;
				var usedJumpGates = acc.AccountPlanets.Where(x => !accessiblePlanets.Contains(x.PlanetID)).Sum(x => x.DropshipCount);
				if (usedJumpGates >= jumpGateCapacity)
					return
						Content(string.Format("Tha planet cannot be accessed via wormholes and your jumpgates are at capacity {0}/{1}", usedJumpGates, jumpGateCapacity));
			}
			var cnt = Math.Max(count, 0);
			cnt = Math.Min(cnt, acc.DropshipCount);
			acc.DropshipCount = (acc.DropshipCount) - cnt;
			var pac = acc.AccountPlanets.SingleOrDefault(x => x.PlanetID == planetID);
			if (pac == null)
			{
				pac = new AccountPlanet { AccountID = Global.AccountID, PlanetID = planetID };
				db.AccountPlanets.InsertOnSubmit(pac);
			}
			pac.DropshipCount += cnt;
			if (cnt > 0) db.Events.InsertOnSubmit(Global.CreateEvent("{0} sends {1} dropships to {2}", acc, cnt, pac.Planet));
			db.SubmitChanges();
			return RedirectToAction("Planet", new { id = planetID });
		}

		[Auth]
		public ActionResult SubmitBuyOrder(int planetID, int quantity, int price)
		{
			return SubmitMarketOrder(planetID, quantity, price, false);
		}


		[Auth]
		public ActionResult SubmitBuyStructure(int planetID, int structureTypeID)
		{
			var db = new ZkDataContext();
			var planet = db.Planets.Single(p => p.PlanetID == planetID);
			if (Global.Account.AccountID != planet.OwnerAccountID) return Content("Planet is not under control.");
			var structureType = db.StructureTypes.SingleOrDefault(s => s.StructureTypeID == structureTypeID);
			if (structureType == null) return Content("Structure type does not exist.");
			if (!structureType.IsBuildable) return Content("Structure is not buildable.");

			// assumes you can only build level 1 structures! if higher level structures can be built directly, we should check down the upgrade chain too
			if (HasStructureOrUpgrades(db, planet, structureType)) return Content("Structure or its upgrades already built");

			if (Global.Account.Credits < structureType.Cost) return Content("Insufficient credits.");
			Global.Account.Credits -= structureType.Cost;

			var newBuilding = new PlanetStructure { StructureTypeID = structureTypeID, PlanetID = planetID };
			db.PlanetStructures.InsertOnSubmit(newBuilding);
			db.SubmitChanges();

			db.Events.InsertOnSubmit(Global.CreateEvent("{0} has built a {1} on {2}.", Global.Account, newBuilding, planet));
			db.SubmitChanges();
			return RedirectToAction("Planet", new { id = planetID });
		}


		[Auth]
		public ActionResult SubmitCreateClan(Clan clan, HttpPostedFileBase image)
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
			db.SubmitChanges();

			if (created) // we created a new clan, set self as founder and rights
			{
				var acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);
				acc.ClanID = clan.ClanID;
				acc.IsClanFounder = true;
				acc.HasClanRights = true;
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
			db.SubmitChanges();
			ResolveMarketTransactions(db);
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
		public ActionResult SubmitUpgradeStructure(int planetID, int structureTypeID)
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
			Global.Account.Credits -= newStructureType.Cost;

			var newStructure = new PlanetStructure { PlanetID = planetID, StructureTypeID = oldStructure.StructureTypeID };

			db.PlanetStructures.InsertOnSubmit(newStructure);
			db.PlanetStructures.DeleteOnSubmit(oldStructure);

			db.SubmitChanges();
			db.Events.InsertOnSubmit(Global.CreateEvent("{0} has built a {1} on {2}.", Global.Account, newStructure, planet));

			db.SubmitChanges();
			return RedirectToAction("Planet", new { id = planetID });
		}

		bool HasStructureOrUpgrades(ZkDataContext db, Planet planet, StructureType structureType)
		{
			// has found stucture in tech tree
			if (planet.PlanetStructures.Any(s => structureType.UpgradesToStructureID == s.StructureTypeID)) return true;
			// has reached the end of the tech tree, no structure found
			if (structureType.UpgradesToStructureID == null) return false;
			// search the next step in the tech tree
			return HasStructureOrUpgrades(db, planet, db.StructureTypes.Single(s => s.StructureTypeID == structureType.UpgradesToStructureID));
		}

		public static void SetPlanetOwners(ZkDataContext db)
		{
			Galaxy.RecalculateShadowInfluence(db);
			var havePlanetsChangedHands = false;
			foreach (var planet in db.Planets)
			{
				var clansByInfluence = planet.AccountPlanets.GroupBy(ap => ap.Account.Clan).OrderByDescending(g => g.Sum(ap => ap.Influence + ap.ShadowInfluence));
				var mostInfluentialClan = clansByInfluence.FirstOrDefault();
				if (mostInfluentialClan != null)
				{
					var mostInfluentialPlayer = mostInfluentialClan.OrderByDescending(ap => ap.Influence + ap.ShadowInfluence).First();
					if (mostInfluentialPlayer.AccountID != planet.OwnerAccountID)
					{
						if (planet.OwnerAccountID == null) // no previous owner
						{
							planet.OwnerAccountID = mostInfluentialPlayer.AccountID;
							db.SubmitChanges();
							db.Events.InsertOnSubmit(Global.CreateEvent("{0} has claimed planet {1}.", mostInfluentialPlayer.Account, planet));
							db.SubmitChanges();
							havePlanetsChangedHands = true;
						}
						else
						{
							// todo: use factor instead of a fixed boost for defense?
							var defenseBoost = planet.PlanetStructures.Where(s => !s.IsDestroyed).Sum(s => s.StructureType.EffectInfluenceDefense) ?? 0;
							var ownerAccountPlanet = planet.AccountPlanets.Single(ap => ap.AccountID == planet.OwnerAccountID);
							var ownerIP = ownerAccountPlanet.Influence + ownerAccountPlanet.ShadowInfluence;
							var mostInfluentialPlayerIP = mostInfluentialPlayer.Influence + mostInfluentialPlayer.ShadowInfluence;
							if (ownerIP + defenseBoost < mostInfluentialPlayerIP)
							{
								planet.OwnerAccountID = mostInfluentialPlayer.AccountID;
								db.SubmitChanges();
								db.Events.InsertOnSubmit(Global.CreateEvent("{0} has captured planet {1} from {2}.",
								                                            mostInfluentialPlayer.Account,
								                                            planet,
								                                            ownerAccountPlanet.Planet));
								db.SubmitChanges();
								havePlanetsChangedHands = true;
							}
						}
					}
				}
			}
			if (havePlanetsChangedHands) SetPlanetOwners(db); // we need another cycle because of shadow influence chain reactions
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
			             where buyOffer.Price > sellOffer.Price
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
					db.SubmitChanges();

					buyOffer.Quantity -= quantity;
					if (buyOffer.Quantity == 0) db.MarketOffers.DeleteOnSubmit(buyOffer);

					sellOffer.Quantity -= quantity;
					if (sellOffer.Quantity == 0) db.MarketOffers.DeleteOnSubmit(sellOffer);

					db.SubmitChanges();
					if (seller.AccountID == buyer.AccountID) db.Events.InsertOnSubmit(Global.CreateEvent("{0} has purchased {1} influence from himself on {2}.", buyer, quantity, buyOffer.Planet));
					else db.Events.InsertOnSubmit(Global.CreateEvent("{0} has purchased {1} influence from {2} on {3}.", buyer, quantity, seller, buyOffer.Planet));
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
		public int PageSize;
		public bool Partial;
		public int? PlanetID;
		public int? SpringBattleID;
		public string Title;
	}
}