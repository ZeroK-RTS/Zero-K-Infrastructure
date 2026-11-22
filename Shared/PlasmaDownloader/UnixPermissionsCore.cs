using System.IO;

namespace PlasmaDownloader
{
    /// <summary>
    ///     Makes the downloaded engine binary executable. The .NET 9 half of a compat twin;
    ///     UnixPermissions.cs is the .NET Framework one, and the two have the same name so
    ///     <see cref="EngineDownload" /> calls it unchanged in both stacks.
    ///
    ///     It exists because Mono.Posix's Syscall.chmod throws on .NET 9 - "Unable to load shared
    ///     library 'MonoPosixHelper'", since that is a mono runtime component. EngineDownload
    ///     caught the exception, warned, and carried on, so the download reported success and left
    ///     the engine binary non-executable: the game would simply never start. Nobody would have
    ///     seen it from Windows either, because the whole path is guarded by
    ///     PlatformID.Unix.
    ///
    ///     File.SetUnixFileMode is the built-in replacement (.NET 7+). No package, no helper
    ///     library, same 0755.
    ///
    ///     With this in place ZkLobbyServer.Core no longer references Mono.Posix at all, and that
    ///     removal is the guard: a `using Mono.Unix` added to anything the .NET 9 server compiles
    ///     now fails the build rather than throwing at runtime into a catch.
    /// </summary>
    public static class UnixPermissions
    {
        public static void MakeExecutable(string path)
        {
            File.SetUnixFileMode(path,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }
    }
}
