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
		                         out ResourceType resourceType,
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

		[WebMethod]
		public ReturnValue RegisterResource(int apiVersion,
		                                    string springVersion,
		                                    string md5,
		                                    int length,
		                                    ResourceType resourceType,
		                                    string archiveName,
		                                    string internalName,
		                                    int springHash,
		                                    byte[] serializedData,
		                                    List<string> dependencies,
		                                    byte[] minimap,
		                                    byte[] metalMap,
		                                    byte[] heightMap,
		                                    byte[] torrentData)
		{
			if (md5 == null) throw new ArgumentNullException("md5");
			if (archiveName == null) throw new ArgumentNullException("archiveName");
			if (internalName == null) throw new ArgumentNullException("internalName");
			if (serializedData == null) throw new ArgumentNullException("serializedData");
			if (torrentData == null) throw new ArgumentNullException("torrentData");
			var latestVersion = Int32.Parse(ConfigurationManager.AppSettings["ApiVersion"]);
			if (latestVersion > apiVersion) throw new Exception("Obsolete PlasmaServer Client");
			if (dependencies == null) dependencies = new List<string>();

			var db = new ZkDataContext();

			if (
				db.Resources.Any(
					x => x.InternalName == internalName && x.ResourceSpringHashes.Any(y => y.SpringVersion == springVersion && y.SpringHash != springHash))) return ReturnValue.InternalNameAlreadyExistsWithDifferentSpringHash;


			var contentFile = db.ResourceContentFiles.FirstOrDefault(x => x.Md5 == md5);
			if (contentFile != null) {
				// content file already stored
				if (contentFile.Resource.InternalName != internalName) return ReturnValue.Md5AlreadyExistsWithDifferentName;
				if (contentFile.Resource.ResourceSpringHashes.Any(x => x.SpringVersion == springVersion)) return ReturnValue.Md5AlreadyExists;

				// new spring version we add its hash
				contentFile.Resource.ResourceSpringHashes.Add(new ResourceSpringHash {SpringVersion = springVersion, SpringHash = springHash});
				db.SubmitChanges();
				return ReturnValue.Ok;
			}


			var resource = db.Resources.Where(x => x.InternalName == internalName).SingleOrDefault();


			if (resource == null) {
				resource = new Resource {InternalName = internalName, TypeID = resourceType};
				db.Resources.InsertOnSubmit(resource);

				var file = string.Format("{0}/{1}", Context.Server.MapPath("~/Resources"), resource.InternalName.EscapePath());

				if (minimap != null) File.WriteAllBytes(string.Format("{0}.minimap.jpg", file), minimap);
				if (metalMap != null) File.WriteAllBytes(string.Format("{0}.metalmap.jpg", file), metalMap);
				if (heightMap != null) File.WriteAllBytes(string.Format("{0}.heightmap.jpg", file), heightMap);
				File.WriteAllBytes(Extensions.GetTorrentPath(internalName, md5), torrentData);
				File.WriteAllBytes(string.Format("{0}.metadata.xml.gz", file), serializedData);
			}

			if (!resource.ResourceDependencies.Select(x => x.NeedsInternalName).Except(dependencies).Any()) {
				// new dependencies are superset
				foreach (var depend in dependencies) {
					// add missing dependencies
					var s = depend;
					if (!resource.ResourceDependencies.Any(x => x.NeedsInternalName == s)) resource.ResourceDependencies.Add(new ResourceDependency {NeedsInternalName = depend});
				}
			}

			if (resource.ResourceContentFiles.Any(x => x.Length == length && x.Md5 != md5)) {
				return ReturnValue.Md5AlreadyExistsWithDifferentName;
					// todo add proper message - file exists with different md5 and same size - cant register cant detect mirrors 
			}

			resource.ResourceContentFiles.Add(new ResourceContentFile {FileName = archiveName, Length = length, Md5 = md5});
			File.WriteAllBytes(Extensions.GetTorrentPath(internalName, md5), torrentData); // add new torrent file
			if (!resource.ResourceSpringHashes.Any(x => x.SpringVersion == springVersion)) resource.ResourceSpringHashes.Add(new ResourceSpringHash {SpringVersion = springVersion, SpringHash = springHash});

			db.SubmitChanges();

			return ReturnValue.Ok;
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