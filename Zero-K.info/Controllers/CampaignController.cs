using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Transactions;
using System.Web.Mvc;
using PlasmaShared;
using ZkData;

namespace ZeroKWeb.Controllers
{
    public class CampaignController: Controller
    {
        //
        // GET: /Campaign/

        public Bitmap GenerateGalaxyImage(int campaignID, double zoom = 1, double antiAliasingFactor = 4) {
            zoom *= antiAliasingFactor;
            using (var db = new ZkDataContext()) {
                Campaign camp = db.Campaigns.Single(x => x.CampaignID == campaignID);

                using (Image background = Image.FromFile(Server.MapPath("/img/galaxies/" + camp.MapImageName))) {
                    var im = new Bitmap((int)(background.Width*zoom), (int)(background.Height*zoom));
                    using (Graphics gr = Graphics.FromImage(im)) {
                        gr.DrawImage(background, 0, 0, im.Width, im.Height);

                        foreach (CampaignPlanet p in camp.CampaignPlanets) {
                            string planetIconPath = null;
                            Resource map = db.Resources.FirstOrDefault(m => m.InternalName == p.Mission.Map);
                            try {
                                planetIconPath = "/img/planets/" + (map.MapPlanetWarsIcon ?? "1.png"); // backup image is 1.png
                                using (Image pi = Image.FromFile(Server.MapPath(planetIconPath))) {
                                    double aspect = pi.Height/(double)pi.Width;
                                    var width = (int)(map.PlanetWarsIconSize * zoom);
                                    var height = (int)(width*aspect);
                                    gr.DrawImage(pi, (int)(p.X*im.Width) - width/2, (int)(p.Y*im.Height) - height/2, width, height);
                                }
                            } catch (Exception ex) {
                                throw new ApplicationException(
                                    string.Format("Cannot process planet image {0} for planet {1} map {2}",
                                                  planetIconPath,
                                                  p.PlanetID,
                                                  map.ResourceID),
                                    ex);
                            }
                        }
                        if (antiAliasingFactor == 1) return im;
                        else {
                            zoom /= antiAliasingFactor;
                            return im.GetResized((int)(background.Width*zoom), (int)(background.Height*zoom), InterpolationMode.HighQualityBicubic);
                        }
                    }
                }
            }
        }

        public ActionResult Index(int? campaignID = null)
        {
            if (Global.Account == null) return Content("You must be logged in to view campaign info");

            var db = new ZkDataContext();

            Campaign camp;
            if (campaignID != null) camp = db.Campaigns.Single(x => x.CampaignID == campaignID);
            else camp = db.Campaigns.Single(x => x.CampaignID == 1);
            string cachePath = Server.MapPath(string.Format("/img/galaxies/campaign/render_{0}.jpg", camp.CampaignID));
            // /*
            if (camp.IsDirty || !System.IO.File.Exists(cachePath)) {
                using (Bitmap im = GenerateGalaxyImage(camp.CampaignID)) {
                    im.SaveJpeg(cachePath, 85);
                    camp.IsDirty = false;
                    camp.MapWidth = im.Width;
                    camp.MapHeight = im.Height;
                    db.SubmitChanges();
                }
            }
            // */
            return View("CampaignMap", camp);
        }

        /*
        public ActionResult Minimap() {
            var db = new ZkDataContext();

            return View(db.Galaxies.Single(g => g.IsDefault));
        }
         */


        public ActionResult Planet(int id) {
            var db = new ZkDataContext();
            CampaignPlanet planet = db.CampaignPlanets.Single(x => x.PlanetID == id);
            return View(planet);
        }

    }
}
