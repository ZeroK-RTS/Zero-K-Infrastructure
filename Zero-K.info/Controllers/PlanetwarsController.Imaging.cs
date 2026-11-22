using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Web.Mvc;
using PlasmaShared;
using PlasmaShared.Imaging;
using ZkData;

namespace ZeroKWeb.Controllers
{
    /// <summary>
    /// The half of <see cref="PlanetwarsController"/> that draws the galaxy map.
    ///
    /// It used to be split off because it was System.Drawing - Bitmap, Graphics,
    /// Image.FromFile - which throws PlatformNotSupportedException off Windows since .NET 7.
    /// It now goes through <see cref="Images.Processor"/> like every other image the server
    /// writes, so the split is no longer load-bearing; the file stays because the drawing is a
    /// self-contained job and reads better on its own.
    ///
    /// See Shared/PlasmaShared/IMAGING-MIGRATION.md.
    /// </summary>
    public partial class PlanetwarsController
    {
        /// <summary>
        /// Makes an image: galaxy background with planet images drawn on it (cheaper than rendering each planet individually)
        ///
        /// Returns encoded JPEG bytes rather than a Bitmap, which is what let this cross: the
        /// caller needs the bytes and the dimensions, and both are had without naming an imaging
        /// type. Quality 85 is what the call site used and is passed explicitly, because an
        /// imaging library's default is not 85.
        /// </summary>
        /// <remarks>
        /// [NonAction] because public on a controller means routable, not just visible: under
        /// the default route this was reachable as /Planetwars/GenerateGalaxyImage?galaxyID=N,
        /// with no login, composing a full galaxy JPEG per request and skipping the render_N.jpg
        /// cache that Index exists to maintain. Its only caller is Index, one method below.
        /// </remarks>
        [NonAction]
        public byte[] GenerateGalaxyImage(int galaxyID, double zoom = 1, double antiAliasingFactor = 1)
        {
            // The old code multiplied zoom by this and then, if it was not 1, resized the result
            // down again. That branch was unreachable: the only caller takes the defaults, and
            // the parameter was pinned to 1 by a FIXME saying the Bitmap path had issues. Rather
            // than port a branch nobody has run, it says so.
            if (antiAliasingFactor != 1)
                throw new NotSupportedException(
                    "antiAliasingFactor is not supported: the System.Drawing version pinned it to 1 " +
                    "with a FIXME and nothing ever passed anything else. Supersampling here would " +
                    "mean composing at a multiple and resizing down, which is a change to what the " +
                    "galaxy looks like, not a port.");

            using (var db = new ZkDataContext())
            {
                Galaxy gal = db.Galaxies.Single(x => x.GalaxyID == galaxyID);

                var background = System.IO.File.ReadAllBytes(this.MapPath("/img/galaxies/" + gal.ImageName));
                var canvas = Images.Processor.Measure(background);

                var overlays = new List<ImageOverlay>();
                foreach (Planet p in gal.Planets)
                {
                    string planetIconPath = null;
                    try
                    {
                        planetIconPath = "/img/planets/" + (p.Resource.MapPlanetWarsIcon ?? "1.png"); // backup image is 1.png
                        var icon = System.IO.File.ReadAllBytes(this.MapPath(planetIconPath));

                        overlays.Add(new ImageOverlay(icon,
                            ImageSizing.PlanetIconPlacement(p.X, p.Y, canvas, Images.Processor.Measure(icon),
                                                            p.Resource.PlanetWarsIconSize, zoom)));
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

                return Images.Processor.ComposeJpeg(background, overlays, 85);
            }
        }

        /// <summary>
        /// Go to main Planetwars page
        /// </summary>
        [WritesOnGetByDesign("writes the galaxy render cache, which is what the page is for")]
        public ActionResult Index(int? galaxyID = null)
        {
            var db = new ZkDataContext();
            
            Galaxy gal;
            if (galaxyID != null) gal = db.Galaxies.Single(x => x.GalaxyID == galaxyID);
            else gal = db.Galaxies.Single(x => x.IsDefault);

            string cachePath = this.MapPath(string.Format("/img/galaxies/render_{0}.jpg", gal.GalaxyID));
            if (gal.IsDirty || !System.IO.File.Exists(cachePath))
            {
                var rendered = GenerateGalaxyImage(gal.GalaxyID);
                System.IO.File.WriteAllBytes(cachePath, rendered);

                var size = Images.Processor.Measure(rendered);
                gal.IsDirty = false;
                gal.Width = size.Width;
                gal.Height = size.Height;
                db.SaveChanges();
            }
            
            return View("Galaxy", gal);
        }
    }
}
