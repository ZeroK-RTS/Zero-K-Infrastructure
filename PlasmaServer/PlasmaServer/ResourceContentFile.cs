#region using

using System.Collections.Generic;
using System.IO;
using System.Web;

#endregion

namespace PlasmaServer
{
    public partial class ResourceContentFile
    {
        #region Public methods

        public List<string> GetLinkArray()
        {
            if (LinkCount == 0 || Links == null) return new List<string>();
            else return new List<string>(Links.Split('\n'));
        }

        public byte[] GetTorrentData()
        {
							return File.ReadAllBytes(GetTorrentPath());
        }

        public static string GetTorrentPath(string name, string md5)
        {
            return HttpContext.Current.Server.MapPath(string.Format("~/Resources/{0}", GetTorrentFileName(name, md5)));
        }

				public static string GetTorrentFileName(string name, string md5)
				{
					return string.Format("{0}_{1}.torrent", name.EscapePath(), md5);
				}

				public string GetTorrentFileName()
				{
					return GetTorrentFileName(Resource.InternalName, Md5);
				}
				

        public string GetTorrentPath()
        {
            return GetTorrentPath(Resource.InternalName, Md5);
        }



        #endregion
    }
}