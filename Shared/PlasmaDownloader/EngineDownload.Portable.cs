using System;
using System.Collections.Generic;

namespace PlasmaDownloader
{
    /// <summary>
    /// The half of <see cref="EngineDownload"/> with no framework dependency, split out for the
    /// same reason as Utils.Enumerable.cs and GlobalConst.Portable.cs: the rest of that file
    /// downloads engines - HTTP, torrents, the file system - and cannot be linked into the .NET 9
    /// build, while this is seventeen lines of string comparison that ContentServiceImplementation
    /// needs in order to sort engine versions.
    /// </summary>
    public partial class EngineDownload
    {
        public class VersionNumberComparer : IComparer<string>
        {
            public int Compare(string a, string b)
            {
                var pa = a.Split(new char[] { '.', '-' });
                var pb = b.Split(new char[] { '.', '-' });

                for (var i = 0; i < Math.Min(pa.Length, pb.Length); i++)
                {
                    int va;
                    int vb;
                    if (int.TryParse(pa[i], out va) && int.TryParse(pb[i], out vb) && va != vb) return va.CompareTo(vb);
                    else if (pa[i] != pb[i]) return String.Compare(pa[i], pb[i], StringComparison.Ordinal);
                }
                return pa.Length.CompareTo(pb.Length);
            }
        }
    }
}
