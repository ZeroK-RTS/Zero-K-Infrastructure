using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PlasmaDownloader;

namespace Tests.Portable
{
    /// <summary>
    /// PlasmaDownloader's EngineDownload.FixPermissions makes the downloaded `spring` binary
    /// executable, and it does so with Mono.Posix's Syscall.chmod - inside a
    /// `catch (Exception) { Trace.TraceWarning(...) }`.
    ///
    /// Three things make that worth measuring rather than reading:
    ///
    ///   1. the method runs ONLY when Environment.OSVersion.Platform == PlatformID.Unix, so no
    ///      Windows build and no Windows developer ever executes it
    ///   2. Mono.Posix's syscalls P/Invoke MonoPosixHelper, a mono runtime component
    ///   3. if it throws, the catch turns it into a warning and the engine binary is left
    ///      non-executable - the download "succeeds" and the game never starts
    ///
    /// That is the kernel32 failure exactly: compiles everywhere, throws on the platform it was
    /// written for, and is swallowed into a broken state rather than an error.
    /// </summary>
    [TestClass]
    public class UnixPermissionsTests
    {
        /// <summary>
        ///     The same question against the thing it is actually for.
        ///
        ///     The test below uses a temp file, which proves the API works and nothing about the
        ///     engine. A Spring engine archive extracts its binaries **non-executable** - mode 0664 -
        ///     so <c>EngineDownload.FixPermissions</c> is the only reason a downloaded engine can be
        ///     run at all. When that silently failed on .NET 9, this is what it cost.
        ///
        ///     Skipped unless an engine is mounted; see UnitSyncEngineTests.
        /// </summary>
        [TestMethod]
        public void A_real_engine_binary_is_unrunnable_until_UnixPermissions_fixes_it()
        {
            var engineDir = Environment.GetEnvironmentVariable("ZK_UNITSYNC_DIR");
            if (string.IsNullOrEmpty(engineDir) || !File.Exists(Path.Combine(engineDir, "spring-dedicated")))
            {
                Assert.Inconclusive("no engine: set ZK_UNITSYNC_DIR (see ./tools/fetch-engine.sh)");
                return;
            }

            // Copied because the engine is mounted read-only, and set to what the archive gives:
            // readable, writable, not executable.
            var binary = Path.Combine(Path.GetTempPath(), "spring-dedicated-" + Guid.NewGuid().ToString("N"));
            File.Copy(Path.Combine(engineDir, "spring-dedicated"), binary);
            try
            {
                File.SetUnixFileMode(binary,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead);

                Assert.IsFalse(File.GetUnixFileMode(binary).HasFlag(UnixFileMode.UserExecute),
                    "the copy is already executable, so this test cannot show the difference");
                Assert.IsNull(RunVersion(binary), "a non-executable engine ran, which means this test proves nothing");

                // The fix from EngineDownload.FixPermissions, called directly.
                UnixPermissions.MakeExecutable(binary);

                var version = RunVersion(binary);
                Assert.IsNotNull(version,
                    "the engine still will not run after MakeExecutable - which is precisely the state a "
                    + "downloaded engine was left in while Syscall.chmod was throwing into a catch");
                StringAssert.Contains(version, "Spring Engine Version",
                    "something ran, but it did not identify itself as the engine");
                Console.WriteLine("        " + version.Trim());
            }
            finally
            {
                File.Delete(binary);
            }
        }

        /// <summary>Runs the binary and returns the line naming its version, or null if it will not start.</summary>
        private static string RunVersion(string binary)
        {
            try
            {
                using (var process = Process.Start(new ProcessStartInfo(binary, "--version")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                }))
                {
                    var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
                    process.WaitForExit(30000);
                    foreach (var line in output.Split('\n'))
                        if (line.Contains("Spring Engine Version")) return line;
                    return null;
                }
            }
            catch (Exception)
            {
                // Permission denied surfaces as Win32Exception here; anything that stops it starting
                // is the same answer for this test's purposes.
                return null;
            }
        }

        [TestMethod]
        public void The_runtime_can_make_a_file_executable_the_way_EngineDownload_needs()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Assert.Inconclusive("FixPermissions only runs on Unix");
                return;
            }

            var path = Path.Combine(Path.GetTempPath(), "zk-perm-probe-" + Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, "#!/bin/sh\nexit 0\n");
            try
            {
                // The permissions FixPermissions asks for: rwxr-xr-x.
                File.SetUnixFileMode(path,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                    UnixFileMode.OtherRead | UnixFileMode.OtherExecute);

                var mode = File.GetUnixFileMode(path);
                Assert.IsTrue(mode.HasFlag(UnixFileMode.UserExecute),
                    "the owner execute bit did not stick, so a downloaded engine would not be runnable");
                Assert.IsTrue(mode.HasFlag(UnixFileMode.GroupExecute) && mode.HasFlag(UnixFileMode.OtherExecute),
                    "group/other execute did not stick; FixPermissions asks for 0755");
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
