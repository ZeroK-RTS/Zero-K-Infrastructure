using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
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
		public ActionResult GalaxyImage(int galaxyID = 1)
		{
			var db = new ZkDataContext();
			var gal = db.Galaxies.Single(x => x.GalaxyID == galaxyID);
			var cachePath = Server.MapPath(string.Format("/img/galaxies/render_{0}.jpg", gal.GalaxyID));
			Stream res;
			if (gal.IsDirty || !System.IO.File.Exists(cachePath))
			{
				using (var im = GenerateGalaxyImage(galaxyID))
				{
					im.Save(cachePath);
				
					gal.IsDirty = false;
					gal.Width = im.Width;
					gal.Height = im.Height;

					var ms = new MemoryStream();
					im.Save(ms, ImageFormat.Jpeg);
					ms.Seek(0, SeekOrigin.Begin);
					res = ms;
				}
				db.SubmitChanges();
			}
			else res = System.IO.File.OpenRead(cachePath);

			return File(res, "image/jpeg");
		}

		public ActionResult Galaxy(int galaxyID = 1)
		{
			var db = new ZkDataContext();
			var gal = db.Galaxies.Single(x => x.GalaxyID == galaxyID);
			return View(gal);
		}

		[Auth]
		public ActionResult Planet(int id)
		{
			var db = new ZkDataContext();
			var planet = db.Planets.Single(x => x.PlanetID == id);
			return View(planet);

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
							return im.GetResized((int)(background.Width * zoom), (int)(background.Height * zoom), InterpolationMode.HighQualityBicubic);
						}
					}
				}
			}
		}

		[Auth]
		public ActionResult CreateClan()
		{
			if (Global.Account.Clan == null || (Global.Account.HasClanRights))
			{
				return View(Global.Clan ?? new Clan());	
			} else  return Content("You already have clan and you dont have rights to it");
		}

		[Auth]
		public ActionResult SubmitRenamePlanet(int planetID, string newName)
		{
			if (String.IsNullOrWhiteSpace(newName)) return Content("Error: the planet must have a name.");
			// if (Global.Account.AccountID != planet.OwnerAccountID) return Content("Unauthorized");
			var db = new ZkDataContext();
			var planet = db.Planets.Single(p => p.PlanetID == planetID);
			planet.Name = newName;
			db.SubmitChanges();
			return RedirectToAction("Planet", new { id = planet.PlanetID });
		}

		[Auth]
		public ActionResult SubmitCreateClan(Clan clan, HttpPostedFileBase image)
		{
			var db = new ZkDataContext();
			bool created = clan.ClanID == 0; // existing clan vs creation
			if (!created) {
				if (!Global.Account.HasClanRights || clan.ClanID != Global.Account.ClanID) return Content("Unauthorized");
				var orgClan = db.Clans.Single(x => x.ClanID == clan.ClanID);
				orgClan.ClanName = clan.ClanName;
				orgClan.LeaderTitle = clan.LeaderTitle;
				orgClan.Shortcut = clan.Shortcut;
				orgClan.Description = clan.Description;
				orgClan.SecretTopic = clan.SecretTopic;
				orgClan.Password = clan.Password;
				//orgClan.DbCopyProperties(clan); 
			} else
			{
				if (Global.Clan != null) return Content("You already have a clan");
				db.Clans.InsertOnSubmit(clan);
			}
			if (string.IsNullOrEmpty(clan.ClanName) || string.IsNullOrEmpty(clan.Shortcut)) return Content("Name and shortcut cannot be empty!");

			if (created && (image == null || image.ContentLength ==0)) return Content("Upload image");
			if (image != null && image.ContentLength >0)
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
		public ActionResult JoinClan(int id, string password)
		{
			var db = new ZkDataContext();
			var clan = db.Clans.Single(x => x.ClanID == id);
			if (clan.CanJoin(Global.Account)) {
				if (!string.IsNullOrEmpty(clan.Password) && clan.Password != password) return View(clan.ClanID);
				else {
					var acc = db.Accounts.Single(x => x.AccountID == Global.AccountID);
					acc.ClanID = clan.ClanID;
					db.SubmitChanges();
					return RedirectToAction("Clan", new { id = clan.ClanID });
				}
			} else return Content("You cannot join this clan");

		}

		public ActionResult Index()
		{
			return View("Galaxy", new ZkDataContext().Galaxies.Single(x => x.IsDefault));
			return    View();
		}

		/// <summary>
		/// Shows clan page
		/// </summary>
		/// <returns></returns>
		public ActionResult Clan(int id)
		{
			var db = new ZkDataContext();
			return View(db.Clans.First(x => x.ClanID == id));
		}

		public ActionResult ClanList()
		{
			var db = new ZkDataContext();

			return View(db.Clans.AsQueryable());
		}
	}
}