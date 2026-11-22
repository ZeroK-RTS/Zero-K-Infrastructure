using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZkData.UnitSyncLib;

namespace Tests.Portable
{
    /// <summary>
    /// `unitsync` is the Spring engine's own native library, and ZkLobbyServer.Core compiles 135
    /// P/Invokes against it. tools/native-calls.txt records a verdict for it:
    ///
    ///     Deliberately spelled without an extension, so .NET resolves libunitsync.so on Linux
    ///     and unitsync.dll on Windows.
    ///
    /// **That was an assertion, not a measurement**, and the whole reason this check exists is that
    /// the last unmeasured assumption about a P/Invoke - that kernel32 was fine because it compiled
    /// - cost the server its player port. A DllImport name without an extension is *supposed* to
    /// get the platform's prefix and suffix applied, but nothing here had ever confirmed it for
    /// this name, on this runtime.
    ///
    /// These two tests confirm it without the engine, which is not installed anywhere in CI:
    /// the failure names what was looked for, and a stand-in file with the Linux spelling is
    /// actually found.
    ///
    /// What they do NOT show is that a real libunitsync.so works - that the entry points match,
    /// that the calls marshal, that any of the 135 do what they did on .NET Framework. Only the
    /// engine can answer that. See ZkLobbyServer/PORTABILITY.md.
    /// </summary>
    [TestClass]
    public class UnitSyncResolutionTests
    {
        private static string StandIn => Path.Combine(AppContext.BaseDirectory, "libunitsync.so");

        /// <summary>
        ///     These two ask what happens when NO engine can be found, so they are meaningless - and
        ///     actively wrong - once one is mounted. UnitSyncEngineTests installs the real
        ///     libunitsync.so in this same directory, which both satisfies the load these tests
        ///     expect to fail and puts a file here that the cleanup below would delete out from under
        ///     them. Running the whole suite with an engine is what surfaced that; CI has none.
        /// </summary>
        private static bool AnEngineIsMounted =>
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ZK_UNITSYNC_DIR"));

        [TestInitialize]
        [TestCleanup]
        public void RemoveTheStandIn()
        {
            if (AnEngineIsMounted) return;
            if (File.Exists(StandIn)) File.Delete(StandIn);
        }

        [TestMethod]
        public void With_no_engine_present_the_runtime_says_it_looked_for_the_linux_spelling()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Assert.Inconclusive("this asks what Linux probes for; it says nothing on other platforms");
                return;
            }
            if (AnEngineIsMounted)
            {
                Assert.Inconclusive("an engine is mounted, so 'not found' is not the question here - see UnitSyncEngineTests");
                return;
            }

            // The real P/Invoke, not a stand-in for it: whatever this does is what the lobby
            // server does when it asks the engine a question.
            var thrown = Assert.ThrowsException<DllNotFoundException>(
                () => UnitSync.NativeMethods.GetSpringVersion(),
                "unitsync loaded, which means an engine is installed here - then this test is "
                + "measuring the wrong thing and should be read again");

            StringAssert.Contains(thrown.Message, "libunitsync.so",
                "the runtime never looked for the Linux spelling. The DllImport name would then "
                + "need the platform handling that GetSpringVersion's declaration does not have, "
                + "and every one of the 135 unitsync calls would fail on Linux no matter what "
                + "engine is installed. What it did look for is in the message above.");
        }

        [TestMethod]
        public void A_file_with_the_linux_spelling_is_actually_found()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Assert.Inconclusive("this asks what Linux probes for; it says nothing on other platforms");
                return;
            }
            if (AnEngineIsMounted)
            {
                Assert.Inconclusive("an engine is mounted, so 'not found' is not the question here - see UnitSyncEngineTests");
                return;
            }

            // Probing for a name and finding a file under it are different claims, so this puts a
            // real shared object where the engine's would go. Any valid ELF will do - the question
            // is resolution, not what the library contains. libSystem.Native.so ships with the
            // runtime, so it is always here.
            var donor = Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "libSystem.Native.so");
            if (!File.Exists(donor))
            {
                Assert.Inconclusive("no libSystem.Native.so to copy; nothing to stand in for the engine");
                return;
            }
            File.Copy(donor, StandIn, true);

            Assert.IsTrue(NativeLibrary.TryLoad("unitsync", typeof(UnitSync).Assembly, null, out var handle),
                "a valid shared object named libunitsync.so sits next to the assembly and the "
                + "runtime still did not find it under the name 'unitsync'");
            NativeLibrary.Free(handle);
        }
    }
}
