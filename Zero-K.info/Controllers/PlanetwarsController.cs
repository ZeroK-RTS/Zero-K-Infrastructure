using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web.Mvc;
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

		public Bitmap GenerateGalaxyImage(int galaxyID, double zoom = 1)
		{
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
						return im;
					}
				}
			}
		}

		public ActionResult Index()
		{
			return View();
		}
	}
}