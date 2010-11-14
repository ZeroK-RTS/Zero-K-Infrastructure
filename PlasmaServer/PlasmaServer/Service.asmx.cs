#region using

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web.Services;
using ZkData;

#endregion

namespace PlasmaServer
{
	[WebService(Namespace = "http://planet-wars.eu/PlasmaServer/")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[ToolboxItem(false)]
	public class PlasmaService : WebService
	{
		#region Public methods

		[WebMethod]
		public ReturnValue DeleteResource(string login, string password, string internalName)
		{
			if (!IsAdmin(login, password)) return ReturnValue.InvalidLogin;

			var db = new ZkDataContext();
			var todel = db.Resources.SingleOrDefault(x => x.InternalName == internalName);
			if (todel == null) return ReturnValue.ResourceNotFound;
			todel.RemoveResourceFiles();
			
			db.Resources.DeleteOnSubmit(todel);
			db.SubmitChanges();
			return ReturnValue.Ok;
		}

		[WebMethod]
		public bool DownloadFile(string internalName,
		                         out List<string> links,
		                         out byte[] torrent,
		                         out List<string> dependencies,
		                         out ZkData.ResourceType resourceType,
		                         out string torrentFileName)
		{
			return LinkProvider.GetLinksAndTorrent(internalName, out links, out torrent, out dependencies, out resourceType, out torrentFileName);
		}

		/// <summary>
		/// Finds resource by either md5 or internal name
		/// </summary>
		/// <param name="md5"></param>
		/// <param name="internalName"></param>
		/// <returns></returns>
		[WebMethod]
		public ResourceData GetResourceData(string md5, string internalName)
		{
			var ret = FindResource(md5, internalName);
			if (ret == null) return null;
			return new ResourceData(ret);
		}


		[WebMethod]
		public List<ResourceData> GetResourceList()
		{
			var db = new ZkDataContext();
			return db.Resources.Select(r => new ResourceData(r)).ToList();
		}

		public static bool IsAdmin(string login, string password)
		{
			var db = new ZkDataContext();
			return login == "Admin" && password == "Sux";
		}


		#endregion

		#region Other methods

		private static Resource FindResource(string md5, string internalName)
		{
			var db = new ZkDataContext();
			Resource ret = null;
			if (!string.IsNullOrEmpty(md5)) {
				var r = db.ResourceContentFiles.SingleOrDefault(x => x.Md5 == md5);
				if (r != null) ret = r.Resource;
			} else if (!string.IsNullOrEmpty(internalName)) ret = db.Resources.SingleOrDefault(x => x.InternalName == internalName);
			return ret;
		}

		#endregion
	}
}