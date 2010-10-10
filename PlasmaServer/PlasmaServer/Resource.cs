using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Web;

namespace PlasmaServer
{
	partial class Resource
	{
		public void RemoveResourceFiles()
		{
			var file = string.Format("{0}/{1}", HttpContext.Current.Server.MapPath("~/Resources"), InternalName.EscapePath());
			Utils.SafeDelete(string.Format("{0}.minimap.jpg", file));
			Utils.SafeDelete(string.Format("{0}.heightmap.jpg", file));
			Utils.SafeDelete(string.Format("{0}.metalmap.jpg", file));
			Utils.SafeDelete(string.Format("{0}.metadata.xml.gz", file));
			foreach (var content in ResourceContentFiles) Utils.SafeDelete(content.GetTorrentPath());
		}
	}
}
