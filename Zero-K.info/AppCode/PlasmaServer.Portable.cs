using System;
using System.IO;
using PlasmaShared;
using ZkData;

namespace ZeroKWeb
{
    /// <summary>
    /// The torrent-path half of PlasmaServer, split out so the .NET 9 build can link it.
    ///
    /// PlasmaServer.cs itself is 290 lines built around System.Drawing.Drawing2D and
    /// System.Drawing.Imaging - it resizes minimaps - and that does not port by moving it.
    /// These five methods touch none of it: they format a file name and read a file.
    ///
    /// ResourceLinkProvider calls two of them, and MapsController.Detail calls
    /// ResourceLinkProvider on every page view, so this is on the path of an ordinary
    /// request rather than an admin one.
    ///
    /// Same split, and the same reason, as GlobalConst.Portable.cs and Utils.Enumerable.cs:
    /// one partial class, one definition of each member, both stacks compile it.
    /// </summary>
    public partial class PlasmaServer
    {
        public static byte[] GetTorrentData(ResourceContentFile cf)
        {
            return File.ReadAllBytes(GetTorrentPath(cf));
        }

        public static string GetTorrentFileName(string name, string md5)
        {
            return String.Format("{0}_{1}.torrent", name.EscapePath(), md5);
        }

        public static string GetTorrentFileName(ResourceContentFile cf)
        {
            return GetTorrentFileName(cf.Resource.InternalName, cf.Md5);
        }

        public static string GetTorrentPath(string name, string md5)
        {
            return Global.MapPath($"~/{GlobalConst.ResourceFolder}/{GetTorrentFileName(name, md5)}");
        }

        public static string GetTorrentPath(ResourceContentFile cf)
        {
            return GetTorrentPath(cf.Resource.InternalName, cf.Md5);
        }
    }
}
