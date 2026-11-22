using System;
using System.Drawing;
using Diagrams;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Portable
{
    /// <summary>
    /// Vector maths from the diagram renderer.
    ///
    /// This class is linked here for two reasons. It is untested polar-coordinate
    /// arithmetic with a normalisation step that is easy to get wrong, and it uses
    /// <see cref="Point"/> - which lives in System.Drawing.Primitives, part of the .NET 9
    /// shared framework. The imaging types in System.Drawing.Common are the Windows-only
    /// ones. That this file compiles and runs here is the evidence.
    /// </summary>
    [TestClass]
    public class VectorTests
    {
        [TestMethod]
        public void A_negative_magnitude_is_normalised_by_reversing_direction()
        {
            var v = new Vector(-5, 30);
            Assert.AreEqual(5, v.Magnitude, 1e-9);
            Assert.AreEqual(210, v.Direction, 1e-9);
        }

        [TestMethod]
        public void A_negative_direction_is_brought_into_the_positive_circle()
        {
            var v = new Vector(3, -90);
            Assert.AreEqual(3, v.Magnitude, 1e-9);
            Assert.AreEqual(270, v.Direction, 1e-9);
        }

        [TestMethod]
        public void Normalising_a_negative_magnitude_can_still_leave_a_negative_direction()
        {
            // Characterisation. The constructor reverses direction with (180 + d) % 360,
            // which for d < -180 stays negative, and only then applies the "make positive"
            // step - so this case is handled, but only by the second step. Pinned because
            // the ordering of those two steps is load-bearing.
            var v = new Vector(-2, -200);
            Assert.IsTrue(v.Direction >= 0 && v.Direction < 360,
                "direction should end up inside [0,360), was " + v.Direction);
            Assert.AreEqual(2, v.Magnitude, 1e-9);
        }

        [TestMethod]
        public void Scalar_multiplication_scales_magnitude_and_leaves_direction()
        {
            var v = new Vector(4, 45) * 2.5;
            Assert.AreEqual(10, v.Magnitude, 1e-9);
            Assert.AreEqual(45, v.Direction, 1e-9);
        }

        [TestMethod]
        public void Multiplying_by_a_negative_scalar_reverses_the_vector()
        {
            var v = new Vector(4, 45) * -1;
            Assert.AreEqual(4, v.Magnitude, 1e-9);
            Assert.AreEqual(225, v.Direction, 1e-9);
        }

        [TestMethod]
        public void Cardinal_directions_convert_to_the_expected_points()
        {
            Assert.AreEqual(new Point(10, 0), new Vector(10, 0).ToPoint());
            Assert.AreEqual(new Point(0, 10), new Vector(10, 90).ToPoint());
            Assert.AreEqual(new Point(-10, 0), new Vector(10, 180).ToPoint());
        }

        [TestMethod]
        public void Converting_to_a_point_truncates_toward_zero()
        {
            // (int) is a truncating cast, not a round. At 45 degrees a magnitude of 10 gives
            // 7.07 on each axis, so both components come back 7.
            var p = new Vector(10, 45).ToPoint();
            Assert.AreEqual(7, p.X);
            Assert.AreEqual(7, p.Y);
        }

        [TestMethod]
        public void A_zero_vector_is_the_origin()
        {
            var p = new Vector(0, 0).ToPoint();
            Assert.AreEqual(Point.Empty, p);
        }
    }
}
