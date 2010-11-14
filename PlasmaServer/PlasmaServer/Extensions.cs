using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using ZkData;

namespace PlasmaServer
{
	static class Extensions
	{
		public static void RemoveResourceFiles(this Resource resource)
		{
			var file = string.Format("{0}/{1}", HttpContext.Current.Server.MapPath("~/Resources"), resource.InternalName.EscapePath());
			Utils.SafeDelete(string.Format("{0}.minimap.jpg", file));
			Utils.SafeDelete(string.Format("{0}.heightmap.jpg", file));
			Utils.SafeDelete(string.Format("{0}.metalmap.jpg", file));
			Utils.SafeDelete(string.Format("{0}.metadata.xml.gz", file));
			foreach (var content in resource.ResourceContentFiles) Utils.SafeDelete(content.GetTorrentPath());
		}

		public static List<string> GetLinkArray(this ResourceContentFile cf)
		{
			if (cf.LinkCount == 0 || cf.Links == null) return new List<string>();
			else return new List<string>(cf.Links.Split('\n'));
		}

		public static byte[] GetTorrentData(this ResourceContentFile cf)
		{
			return File.ReadAllBytes(cf.GetTorrentPath());
		}

		public static string GetTorrentPath(string name, string md5)
		{
			return HttpContext.Current.Server.MapPath(string.Format("~/Resources/{0}", GetTorrentFileName(name, md5)));
		}

		public static string GetTorrentFileName(string name, string md5)
		{
			return string.Format("{0}_{1}.torrent", name.EscapePath(), md5);
		}

		public static string GetTorrentFileName(this ResourceContentFile cf)
		{
			return GetTorrentFileName(cf.Resource.InternalName, cf.Md5);
		}


		public static string GetTorrentPath(this ResourceContentFile cf)
		{
			return GetTorrentPath(cf.Resource.InternalName,cf.Md5);
		}



	}
}