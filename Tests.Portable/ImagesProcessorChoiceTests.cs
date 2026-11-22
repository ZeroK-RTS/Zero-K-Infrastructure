using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlasmaShared.Imaging;

namespace Tests.Portable
{
    /// <summary>
    /// Which implementation the server actually uses.
    ///
    /// Images.Processor is one line, and everything about the imaging port turns on it: the
    /// five migrated call sites all go through it, and it decides whether uploads are encoded
    /// by System.Drawing or ImageSharp. It was flipped to ImageSharp on purpose, knowing new
    /// images would differ slightly from stored ones.
    ///
    /// Nothing tested that. A revert - deliberate or by a bad merge - would change what the
    /// site writes to disk and no check would say so. This is that check. If the line is
    /// meant to change back, this test changes with it, which is the point: the decision
    /// should be visible in a diff rather than inferred from a field initialiser.
    /// </summary>
    [TestClass]
    public class ImagesProcessorChoiceTests
    {
        [TestMethod]
        public void The_server_processes_images_with_ImageSharp()
        {
            Assert.IsInstanceOfType(Images.Processor, typeof(ImageSharpImageProcessor),
                "Images.Processor decides how every upload is encoded. See the note on the field "
                + "and Shared/PlasmaShared/IMAGING-MIGRATION.md before changing it.");
        }

        /// <summary>
        /// The other half of why the flip mattered: this file can only be linked into a .NET 9
        /// project while it names an implementation that does not need System.Drawing.
        /// </summary>
        [TestMethod]
        public void Choosing_the_implementation_costs_no_System_Drawing_dependency()
        {
            var assembly = typeof(Images).Assembly;
            foreach (var reference in assembly.GetReferencedAssemblies())
                Assert.AreNotEqual("System.Drawing.Common", reference.Name,
                    "this test project links Images.cs; a System.Drawing.Common reference here means "
                    + "the imaging seam has picked up a Windows-only dependency again");
        }
    }
}
