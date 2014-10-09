#region using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Timers;
using PlasmaShared;
using PlasmaShared.UnitSyncLib;
using Timer = System.Timers.Timer;

#endregion

namespace PlasmaDownloader.Packages
{
	public class PackageDownloader: IDisposable
	{
		bool isRefreshing;
		string masterContent;
		readonly string masterUrl;
		readonly PlasmaDownloader plasmaDownloader;
		readonly Timer refreshTimer;
		List<Repository> repositories = new List<Repository>();
		List<string> selectedPackages = new List<string>();

		public List<Repository> Repositories { get { return repositories; } }

		public List<string> SelectedPackages { get { return selectedPackages; } }

		public event EventHandler PackagesChanged = delegate { };
		public event EventHandler SelectedPackagesChanged = delegate { };

		public PackageDownloader(PlasmaDownloader plasmaDownloader)
		{
			this.plasmaDownloader = plasmaDownloader;
			masterUrl = this.plasmaDownloader.Config.PackageMasterUrl;
			LoadRepositories();
			LoadSelectedPackages();
			refreshTimer = new Timer(this.plasmaDownloader.Config.RepoMasterRefresh*1000);
			refreshTimer.AutoReset = true;
			refreshTimer.Elapsed += RefreshTimerElapsed;
			//Utils.StartAsync(LoadMasterAndVersions);

            //Note: We moved away from asynchronous initialization because we need to prepare a 
            //complete repository list before letting go control (so that external code will work
            //properly). We are doing an on-demand initialization/lazy-initialization to speedup
            //startup time, so its important that stuff is finished when we Initialize them.
            //We used "Task" instead of simply do "LoadMasterAndVersion();" because: http://stackoverflow.com/questions/4192834/waitall-for-multiple-handles-on-a-sta-thread-is-not-supported
            var task1 = System.Threading.Tasks.Task.Factory.StartNew(() => LoadMasterAndVersions());
            task1.Wait();

			refreshTimer.Start();
		}

		public void Dispose()
		{
			refreshTimer.Stop();
			refreshTimer.Elapsed -= RefreshTimerElapsed;
		}

		public void DeselectPackage(string name)
		{
			lock (selectedPackages) selectedPackages.Remove(name);
			SaveSelectedPackages();
			SelectedPackagesChanged(this, EventArgs.Empty);
		}


		public Version GetByInternalName(string name)
		{
			foreach (var repo in repositories)
			{
				Version version;
				if (!string.IsNullOrEmpty(repo.BaseUrl) && repo.VersionsByInternalName.TryGetValue(name, out version)) return version;
			}
			return null;
		}

		public Version GetByTag(string tag)
		{
			foreach (var repo in repositories)
			{
				Version version;
				if (!string.IsNullOrEmpty(repo.BaseUrl) && repo.VersionsByTag.TryGetValue(tag, out version)) return version;
			}
			return null;
		}


		internal PackageDownload GetPackageDownload(string name)
		{
			lock (Repositories)
			{
				foreach (var repo in Repositories)
				{
					if (!string.IsNullOrEmpty(repo.BaseUrl))
					{
						Version versionEntry;
						if (repo.VersionsByTag.TryGetValue(name, out versionEntry))
						{
							// find by package name
							SelectPackage(versionEntry.Name); // select it if it was requested by direct package name
							return CreateDownload(repo, versionEntry);
						}

						if (repo.VersionsByInternalName.TryGetValue(name, out versionEntry))
						{
							// find by internal name
							return CreateDownload(repo, versionEntry);
						}
					}
				}
			}

			return null;
		}

		public void LoadMasterAndVersions()
		{
			if (isRefreshing) return;
			isRefreshing = true;
			try
			{
				refreshTimer.Stop();
				var hasChanged = false;

				var wd = new WebClient() { Proxy = null };
				byte[] repoList = null;
				try
				{
                    Trace.TraceInformation("PackageDownloader : Downloading master from :" + masterUrl);
					repoList = wd.DownloadData(masterUrl + "/repos.gz");
				}
				catch (Exception ex)
				{
					Trace.TraceWarning("Error loading package master from " + masterUrl);
				}
				if (repoList != null)
				{
					try
					{
						if (ParseMaster(new GZipStream(new MemoryStream(repoList), CompressionMode.Decompress))) hasChanged = true;
					}
					catch (Exception ex)
					{
						Trace.TraceError("Error parsing package master {0}", ex);
					}
				}

				// update all repositories 
				var waitHandles = new List<WaitHandle>();
				var results = new List<Repository.RefreshResponse>();
				foreach (var entry in repositories)
				{
					try
					{
						var r = entry.Refresh();
						results.Add(r);
						waitHandles.Add(r.WaitHandle);
					}
					catch (Exception ex)
					{
						Trace.TraceError("Could not refresh repository {0}: {1}", entry.BaseUrl, ex);
					}
				}
                WaitHandle.WaitAll(waitHandles.ToArray());//wait until all "repositories" element finish downloading.

				foreach (var result in results)
				{
					if (result.HasChanged) hasChanged = true;
					if (result.ChangedVersions != null) foreach (var ver in result.ChangedVersions) if (selectedPackages.Contains(ver.Name)) plasmaDownloader.GetResource(DownloadType.UNKNOWN, ver.Name);
				}

				if (hasChanged)
				{
					SaveRepositories();
					PackagesChanged(this, EventArgs.Empty);
				}
			}
			finally
			{
				isRefreshing = false;
				refreshTimer.Start();
			}
		}

		public void SelectPackage(string key)
		{
			var isNew = false;
			lock (selectedPackages)
			{
				if (!selectedPackages.Contains(key))
				{
					isNew = true;
					selectedPackages.Add(key);
				}
			}
			if (isNew)
			{
				SaveSelectedPackages();
				SelectedPackagesChanged(this, EventArgs.Empty);
			}
		}

		PackageDownload CreateDownload(Repository repo, Version versionEntry)
		{
			var down = new PackageDownload(repo.BaseUrl, versionEntry.InternalName, versionEntry.Hash, plasmaDownloader.SpringPaths);

			if (versionEntry.Dependencies != null)
			{
				foreach (var dept in versionEntry.Dependencies)
				{
					if (!string.IsNullOrEmpty(dept))
					{
						var dd = plasmaDownloader.GetResource(DownloadType.UNKNOWN, dept);
						if (dd != null) down.AddNeededDownload(dd);
					}
				}
			}
			down.Start();
			return down;
		}


		void LoadRepositories()
		{
			var path = Utils.MakePath(plasmaDownloader.SpringPaths.Cache, "repositories.dat");
			var bf = new BinaryFormatter();
            try
            {
                if (File.Exists(path))
                {
                    lock (repositories)
                    {
                        using (var fs = File.OpenRead(path)) repositories = (List<Repository>)bf.Deserialize(fs);
                    }
                }
                else
                    Trace.TraceWarning("PackageDownloader : There's no {0}", path);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Could not load repository cache from {0}: {1}", path, ex);
            }
		}

		void LoadSelectedPackages()
		{
			try
			{
                var path = Utils.MakePath(plasmaDownloader.SpringPaths.WritableDirectory, "packages", "selected.list");
			    if (File.Exists(path)) {
			        var text = File.ReadAllText(path);
			        var newPackages = new List<string>();
			        foreach (var s in text.Split('\n')) if (!string.IsNullOrEmpty(s)) newPackages.Add(s);
			        lock (selectedPackages) selectedPackages = newPackages;
                } else
                    Trace.TraceWarning("PackageDownloader : There's no {0}", path);
			}
			catch (Exception ex)
			{
				Trace.TraceWarning("Unable to load selected packages list: {0}", ex);
			}
		}

		bool ParseMaster(Stream stream)
		{
			var reader = new StreamReader(stream);
			var newContent = reader.ReadToEnd();

			lock (repositories)
			{
				var toDel = new List<Repository>(repositories);

				foreach (var line in newContent.Split('\n'))
				{
					var args = line.Split(',');
					if (args.Length < 2) continue;
					var baseUrl = args[1];

					var repo = repositories.SingleOrDefault(x => x.BaseUrl == baseUrl);
					if (repo != null) toDel.Remove(repo);
					else
					{
						repo = new Repository(baseUrl);
						repositories.Add(repo);
					}
				}

				if (toDel.Count > 0) foreach (var del in toDel) repositories.Remove(del);
			}

			if (newContent != masterContent)
			{
				masterContent = newContent;
				return true;
			}
			else return false;
		}

		void SaveRepositories()
		{
			var path = Utils.MakePath(plasmaDownloader.SpringPaths.Cache, "repositories.dat");
			var bf = new BinaryFormatter();
			lock (repositories)
			{
				using (var fs = File.OpenWrite(path)) bf.Serialize(fs, repositories);
			}
		}

		void SaveSelectedPackages()
		{
			try
			{
				var path = Utils.MakePath(plasmaDownloader.SpringPaths.WritableDirectory, "packages", "selected.list");
				var sb = new StringBuilder();

				lock (selectedPackages)
				{
					foreach (var entry in selectedPackages)
					{
						sb.Append(entry);
						sb.Append('\n');
					}
				}
				File.WriteAllText(path, sb.ToString());
			}
			catch (Exception ex)
			{
				Trace.TraceWarning("Unable to load selected packages list: {0}", ex);
			}
		}

		void RefreshTimerElapsed(object sender, ElapsedEventArgs e)
		{
			LoadMasterAndVersions();
		}

		[Serializable]
		public class Repository
		{
			Dictionary<string, Version> versionsByInternalName = new Dictionary<string, Version>();
			Dictionary<string, Version> versionsByTag = new Dictionary<string, Version>();

			public string BaseUrl { get; internal set; }
			public byte[] LastDigest { get; protected set; }

			public Dictionary<string, Version> VersionsByInternalName { get { return versionsByInternalName; } }

			public Dictionary<string, Version> VersionsByTag { get { return versionsByTag; } }

			public Repository(string baseUrl)
			{
				BaseUrl = baseUrl;
			}

			public RefreshResponse Refresh()
			{
				var res = new RefreshResponse();
				res.WaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
				var wc = new WebClient() { Proxy = null};
				wc.DownloadDataCompleted += (s, digestResult) =>
					{
						// digest downloaded
						if (digestResult.Error != null || digestResult.Cancelled)
						{
							// digest download failed
							Trace.TraceError(string.Format("Failed to download digest for {0}: {1}", BaseUrl, digestResult.Error));
							res.WaitHandle.Set();
						}
						else if (Hash.ByteArrayEquals(LastDigest, digestResult.Result)) res.WaitHandle.Set(); // digest same as before
						else
						{
							// digest new

							var wcRevs = new WebClient() { Proxy = null};

							wcRevs.DownloadDataCompleted += (s2, versionsResult) =>
								{
									// version list downloaded
									try
									{
										if (versionsResult.Error != null || versionsResult.Cancelled) Trace.TraceError(string.Format("Failed to download versions from {0}: {1}", BaseUrl, versionsResult.Error));
										else
										{
											List<Version> changes;
											ParseVersionList(versionsResult.Result, out changes);
											res.ChangedVersions = changes;
											res.HasChanged = true;
											LastDigest = digestResult.Result;
										}
									}
									catch (Exception ex)
									{
										Trace.TraceError("Error reading version list from {0}: {1}", BaseUrl, ex);
									}
									finally
									{
										res.WaitHandle.Set();
									}
								};

							wcRevs.DownloadDataAsync(new Uri(BaseUrl + "/versions.gz"));
							// start version list download
						}
					};
				wc.DownloadDataAsync(new Uri(BaseUrl + "/versions.digest"));
				return res;
			}

			void ParseVersionList(byte[] input, out List<Version> changedVersions)
			{
				var newVersionsByTag = new Dictionary<string, Version>();
				var newVersionsByName = new Dictionary<string, Version>();
				changedVersions = new List<Version>();
				var stream = new StreamReader(new GZipStream(new MemoryStream(input), CompressionMode.Decompress));

				while (!stream.EndOfStream)
				{
					var line = stream.ReadLine();
					var ar = line.Split(',');
					var versionName = ar[0];
					var versionHash = ar[1];
					var deps = ar[2];
					var internalName = ar[3];
					var dependencies = deps != null ? deps.Split('|') : null;
					if (dependencies != null) dependencies = dependencies.Where(x => !UnitSync.DependencyExceptions.Contains(x) && !string.IsNullOrEmpty(x)).ToArray();
					var versionEntry = new Version(versionName, new Hash(versionHash), dependencies, internalName);
					Version oldRev;

					if (versionsByTag.TryGetValue(versionName, out oldRev)) if (oldRev.Hash != versionEntry.Hash) changedVersions.Add(versionEntry);
					newVersionsByTag[versionName] = versionEntry;
					newVersionsByName[internalName] = versionEntry;
				}
				versionsByTag = newVersionsByTag;
				versionsByInternalName = newVersionsByName;
			}

			public class RefreshResponse
			{
				public List<Version> ChangedVersions { get; internal set; }
				public bool HasChanged { get; internal set; }
				public EventWaitHandle WaitHandle { get; internal set; }
			}
		}

		[Serializable]
		public class Version
		{
			public string[] Dependencies { get; private set; }
			public Hash Hash { get; private set; }
			public string InternalName { get; private set; }
			public string Name { get; private set; }

			public Version(string name, Hash hash, IEnumerable<string> dependencies, string internalName)
			{
				Hash = hash;
				Name = name;
				Dependencies = dependencies.ToArray();
				InternalName = internalName;
			}
		}
	}
}