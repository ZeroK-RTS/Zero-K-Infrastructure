using System;
using Mono.Unix.Native;

namespace PlasmaDownloader
{
    /// <summary>
    ///     Makes the downloaded engine binary executable. The .NET Framework half of a compat twin;
    ///     <see cref="UnixPermissions" /> in UnixPermissionsCore.cs is the .NET 9 one, and the two
    ///     have the same name so <see cref="EngineDownload" /> calls it unchanged in both stacks.
    ///
    ///     Mono.Posix's syscalls P/Invoke MonoPosixHelper, which the mono runtime provides and
    ///     .NET 9 does not - measured, not assumed: Tests.Portable/UnixPermissionsTests. Under
    ///     mono, which is what serves players today, this is the call that has always worked.
    /// </summary>
    public static class UnixPermissions
    {
        public static void MakeExecutable(string path)
        {
            Syscall.chmod(path,
                FilePermissions.S_IRWXU | FilePermissions.S_IRGRP | FilePermissions.S_IXGRP |
                FilePermissions.S_IROTH | FilePermissions.S_IXOTH);
        }
    }
}
