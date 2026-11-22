using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlasmaShared.Imaging;
using ZkData.UnitSyncLib;

namespace Tests.Portable
{
    /// <summary>
    /// Calls the real Spring engine.
    ///
    /// <see cref="UnitSyncResolutionTests"/> answered what the runtime LOOKS FOR when asked for
    /// `unitsync`, which needed no engine. This answers the next question, which does: whether the
    /// 135 P/Invoke declarations in NativeMethods.cs actually bind and marshal on .NET 9 on Linux.
    /// Until an engine was in the loop that was the oldest untested claim in the port -
    /// ZkLobbyServer/PORTABILITY.md carried it as "no entry point of the 135 has been called".
    ///
    /// **Skipped unless an engine is present**, so CI stays green without one:
    ///
    ///     ./tools/fetch-engine.sh                       # ~42MB, into the scratch dir it prints
    ///     ZK_DOTNET_MOUNT=&lt;that dir&gt; ./tools/dotnet.sh test Tests.Portable/Tests.Portable.csproj
    ///
    /// The engine is deliberately NOT downloaded into the repository: it is 42MB, it is not ours,
    /// and this checkout is synced by Synology Drive, which replicates anything in the tree and
    /// restores it after deletion.
    ///
    /// fetch-engine.sh also installs one small map, because the calls that still hold
    /// System.Drawing.Common - GetMinimap, GetHeightMap, GetMetalMap - do nothing without one.
    /// The minimap test below takes that path as far as it goes without GDI+, which turns out to
    /// be all but the last step. See Shared/PlasmaShared/IMAGING-MIGRATION.md.
    /// </summary>
    [TestClass]
    public class UnitSyncEngineTests
    {
        private static string EngineDir => Environment.GetEnvironmentVariable("ZK_UNITSYNC_DIR");

        /// <summary>
        ///     The runtime probes next to the assembly, so the engine's library is placed there
        ///     rather than the assembly being moved to the engine.
        /// </summary>
        [TestInitialize]
        public void PutTheEngineWhereTheRuntimeLooks()
        {
            if (string.IsNullOrEmpty(EngineDir)) return;

            // unitsync finds base/springcontent.sdz through SPRING_DATADIR, which tools/dotnet.sh
            // sets when it mounts an engine. It has to be in the environment before the native
            // library loads: setting it here with Environment.SetEnvironmentVariable is too late,
            // and Init still fails with "Required base file 'base/springcontent.sdz' does not
            // exist". Measured both ways rather than reasoned about.

            var source = Path.Combine(EngineDir, "libunitsync.so");
            var target = Path.Combine(AppContext.BaseDirectory, "libunitsync.so");
            if (File.Exists(source) && !File.Exists(target)) File.Copy(source, target);
        }

        /// <summary>
        /// Inconclusive is right on a machine with no engine - these are the only tests that need
        /// a 42MB download, and failing without one would make the suite unrunnable. It is wrong
        /// where an engine was supposed to be mounted: a mount that silently did not arrive turns
        /// every test here into a skip, and the step reports "Passed!" having called nothing.
        /// ZK_REQUIRE_FULL_RUN=1, which the workflow sets on the step that mounts the engine,
        /// says the engine is meant to be there and a skip is a failure.
        /// </summary>
        private static bool Skip()
        {
            if (!string.IsNullOrEmpty(EngineDir) && File.Exists(Path.Combine(EngineDir, "libunitsync.so"))) return false;

            const string why = "no engine: set ZK_UNITSYNC_DIR (see ./tools/fetch-engine.sh)";
            if (Environment.GetEnvironmentVariable("ZK_REQUIRE_FULL_RUN") == "1")
                Assert.Fail(why + " - and ZK_REQUIRE_FULL_RUN=1 says one was meant to be mounted");
            Assert.Inconclusive(why);
            return true;
        }

        [TestMethod]
        public void A_real_entry_point_binds_marshals_and_answers()
        {
            if (Skip()) return;

            var version = UnitSync.NativeMethods.GetSpringVersion();

            Assert.IsFalse(string.IsNullOrWhiteSpace(version),
                "GetSpringVersion returned nothing. The library loaded - otherwise this would have "
                + "thrown DllNotFoundException - so the declaration binds and the call returns, but "
                + "the string did not marshal back.");
            Console.WriteLine("        engine reports: " + version);
        }

        /// <summary>
        ///     The call that still holds System.Drawing.Common in the port, taken as far as it can
        ///     go without it - which turns out to be all but the last step.
        ///
        ///     Deliberately does NOT use GdiPixelBridge, and not only because it would throw:
        ///     ImagesProcessorChoiceTests asserts this assembly has no System.Drawing.Common
        ///     reference at all, and linking that file would add one. Marshal.Copy does the same
        ///     job as GdiPixelBridge.CopyFrom, which is all that is needed to get the bytes out.
        /// </summary>
        [TestMethod]
        public void A_real_minimap_comes_back_from_the_engine_and_unpacks_without_any_GDI()
        {
            if (Skip()) return;

            Assert.IsTrue(UnitSync.NativeMethods.Init(true, 0),
                "unitsync refused to initialise: " + (UnitSync.NativeMethods.GetNextError() ?? "and reported no error"));
            try
            {
                Assert.AreNotEqual(0, UnitSync.NativeMethods.GetMapCount(),
                    "no map in the data directory - tools/fetch-engine.sh puts one in maps/");
                var name = UnitSync.NativeMethods.GetMapName(0);

                const int size = 1024;          // mip level 0
                const int bytesPerPixel = 2;    // RGB565
                const int stride = size * bytesPerPixel;

                var pointer = UnitSync.NativeMethods.GetMinimap(name, 0);
                Assert.AreNotEqual(IntPtr.Zero, pointer, "GetMinimap returned nothing for " + name);

                var raw = new byte[stride * size];
                Marshal.Copy(pointer, raw, 0, raw.Length);

                var rgb = PixelBuffers.Rgb565ToRgb24(raw, new System.Drawing.Size(size, size), stride);
                Assert.AreEqual(size * size * 3, rgb.Length, "the unpacked buffer is not 24-bit RGB of the right size");

                // Not all one colour: a failed read or an unmapped buffer gives a flat block.
                var first = (rgb[0], rgb[1], rgb[2]);
                var differs = false;
                for (var i = 3; i < rgb.Length && !differs; i += 3)
                    if ((rgb[i], rgb[i + 1], rgb[i + 2]) != first) differs = true;
                Assert.IsTrue(differs, "every pixel is identical, so this is probably not an image");

                Console.WriteLine("        {0}: {1}x{1}, first pixel rgb({2},{3},{4})", name, size, rgb[0], rgb[1], rgb[2]);

                // The step that used to throw. MapImage is the compat twin: this assembly gets the
                // .NET 9 half, which keeps the pixels and reaches for ImageSharp only to resize or
                // encode. Before it existed, GdiPixelBridge.FromRgb24 threw
                // TypeInitializationException here and the minimap path stopped one call short.
                using (var image = MapImage.FromRgb24(rgb, new System.Drawing.Size(size, size)))
                {
                    Assert.AreEqual(size, image.Size.Width);

                    // What AutoRegistrator does with it, and the reason the type needs to survive a
                    // resize and an encode rather than just hold bytes.
                    var jpeg = image.ToBytes(256);
                    Assert.IsTrue(jpeg.Length > 0, "the minimap encoded to nothing");
                    Assert.AreEqual(0xFF, jpeg[0], "not a JPEG: the first byte should be 0xFF");
                    Assert.AreEqual(0xD8, jpeg[1], "not a JPEG: the second byte should be 0xD8");
                    Console.WriteLine("        encoded to {0} bytes of JPEG, bounded to 256", jpeg.Length);
                }
            }
            finally
            {
                UnitSync.NativeMethods.UnInit();
            }
        }

        [TestMethod]
        public void The_engine_initialises_and_reports_an_empty_archive_set()
        {
            if (Skip()) return;

            // isServer: true is what AutoRegistrator's Unitsyncer passes; id is a client id.
            Assert.IsTrue(UnitSync.NativeMethods.Init(true, 0),
                "unitsync refused to initialise: " + (UnitSync.NativeMethods.GetNextError() ?? "and reported no error"));
            try
            {
                // No archives have been added, so both counts must be 0. The point is not the
                // number - it is that an int comes back across the boundary at all, from a call
                // that does real work inside the engine rather than returning a constant.
                // fetch-engine.sh puts one map in the data directory, and no game.
                Assert.AreEqual(1, UnitSync.NativeMethods.GetMapCount(), "expected exactly the map fetch-engine.sh installs");
                Assert.AreEqual(0, UnitSync.NativeMethods.GetPrimaryModCount(), "a game appeared, and fetch-engine.sh installs none");
            }
            finally
            {
                UnitSync.NativeMethods.UnInit();
            }
        }
    }
}
