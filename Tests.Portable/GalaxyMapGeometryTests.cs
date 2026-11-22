using System;
using System.Globalization;
using System.Threading;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZeroKWeb;

namespace Tests.Portable
{
    /// <summary>
    /// Layout of the PlanetWars galaxy map links. This geometry used to live inline in three
    /// Razor views and be emitted as JavaScript, so nothing could check it.
    /// </summary>
    [TestClass]
    public class GalaxyMapGeometryTests
    {
        private const double Width = 2;

        [TestMethod]
        public void A_link_pointing_straight_down_is_not_rotated()
        {
            // The rectangle is laid out pointing down, so a link that already points down
            // needs no rotation.
            var r = GalaxyMapGeometry.Link(100, 100, 100, 200, Width);
            Assert.AreEqual(0, r.AngleDegrees, 1e-9);
            Assert.AreEqual(100, r.Length, 1e-9);
        }

        [TestMethod]
        public void Rotation_follows_the_compass_round()
        {
            // Rotation is clockwise from "down".
            Assert.AreEqual(-90, GalaxyMapGeometry.Link(100, 100, 200, 100, Width).AngleDegrees, 1e-9); // right
            Assert.AreEqual(90, GalaxyMapGeometry.Link(100, 100, 0, 100, Width).AngleDegrees, 1e-9);    // left
            Assert.AreEqual(180, Math.Abs(GalaxyMapGeometry.Link(100, 100, 100, 0, Width).AngleDegrees), 1e-9); // up
        }

        [TestMethod]
        public void Length_is_the_distance_between_the_planets()
        {
            var r = GalaxyMapGeometry.Link(0, 0, 30, 40, Width);
            Assert.AreEqual(50, r.Length, 1e-9);
        }

        [TestMethod]
        public void The_rectangle_is_centred_on_the_first_planet()
        {
            var r = GalaxyMapGeometry.Link(100, 100, 100, 200, Width);
            Assert.AreEqual(99, r.X, 1e-9, "half a width to the left of the planet centre");
            Assert.AreEqual(100, r.Y, 1e-9);
            Assert.AreEqual(Width, r.Width, 1e-9);
        }

        [TestMethod]
        public void Icon_clearances_shorten_the_bar_at_both_ends()
        {
            var r = GalaxyMapGeometry.Link(0, 0, 0, 100, Width, startOffset: 6, endTrim: 10);
            Assert.AreEqual(6, r.Y, 1e-9, "near end pushed clear of the first icon");
            Assert.AreEqual(90, r.Length, 1e-9, "far end trimmed clear of the second icon");
        }

        [TestMethod]
        public void A_link_shorter_than_its_icon_clearance_collapses_rather_than_going_negative()
        {
            // A negative height is invalid SVG. This is a deliberate change from the Raphael
            // version, which passed the negative value straight through.
            var r = GalaxyMapGeometry.Link(0, 0, 0, 5, Width, endTrim: 40);
            Assert.AreEqual(0, r.Length, 1e-9);
        }

        [TestMethod]
        public void Coincident_planets_do_not_produce_NaN()
        {
            var r = GalaxyMapGeometry.Link(50, 50, 50, 50, Width);
            Assert.AreEqual(0, r.Length, 1e-9);
            Assert.IsFalse(double.IsNaN(r.AngleDegrees));
        }

        [TestMethod]
        public void Numbers_are_formatted_invariantly_whatever_the_server_culture()
        {
            // The old views interpolated doubles straight into JavaScript. On a server with
            // a comma decimal separator that turned paper.rect(350.5, ...) into
            // paper.rect(350,5, ...) - a different argument list.
            var previous = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
                Assert.AreEqual("350.5", GalaxyMapGeometry.N(350.5));
                Assert.AreEqual("-0.25", GalaxyMapGeometry.N(-0.25));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }

        [TestMethod]
        public void A_rendered_link_is_valid_svg()
        {
            // The views interpolate these numbers straight into SVG attributes, and Razor
            // views are not compiler-checked. This at least proves the numbers a link
            // produces parse as XML and read back as the geometry we computed.
            var r = GalaxyMapGeometry.Link(120, 80, 300, 260, 2, startOffset: 3, endTrim: 12);

            var svg = string.Format(CultureInfo.InvariantCulture,
                "<svg xmlns=\"http://www.w3.org/2000/svg\">" +
                "<defs><linearGradient id=\"lg1_2\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\">" +
                "<stop offset=\"0\" stop-color=\"#ff0000\" /><stop offset=\"1\" stop-color=\"#0000ff\" />" +
                "</linearGradient></defs>" +
                "<rect x=\"{0}\" y=\"{1}\" width=\"{2}\" height=\"{3}\" fill=\"url(#lg1_2)\" " +
                "transform=\"rotate({4} {5} {6})\" /></svg>",
                GalaxyMapGeometry.N(r.X), GalaxyMapGeometry.N(r.Y),
                GalaxyMapGeometry.N(r.Width), GalaxyMapGeometry.N(r.Length),
                GalaxyMapGeometry.N(r.AngleDegrees), GalaxyMapGeometry.N(120), GalaxyMapGeometry.N(80));

            var doc = XDocument.Parse(svg);
            XNamespace ns = "http://www.w3.org/2000/svg";
            var rect = doc.Root.Element(ns + "rect");

            Assert.IsNotNull(rect, "the rect should survive parsing");
            Assert.AreEqual(r.X, double.Parse(rect.Attribute("x").Value, CultureInfo.InvariantCulture), 0.01);
            Assert.AreEqual(r.Length, double.Parse(rect.Attribute("height").Value, CultureInfo.InvariantCulture), 0.01);
            Assert.IsTrue(double.Parse(rect.Attribute("height").Value, CultureInfo.InvariantCulture) >= 0,
                "a negative height would be invalid SVG");
            Assert.AreEqual("url(#lg1_2)", rect.Attribute("fill").Value);
        }

        [TestMethod]
        public void Coordinates_are_rounded_to_two_places()
        {
            Assert.AreEqual("1.33", GalaxyMapGeometry.N(1.333333));
            Assert.AreEqual("100", GalaxyMapGeometry.N(100.0));
        }
    }
}
