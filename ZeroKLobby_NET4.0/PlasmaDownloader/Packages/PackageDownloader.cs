﻿#region using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Cache;
//using System.Net.Http; //Net 4.5.1
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;
using ZkData;
using ZkData.UnitSyncLib;
using Timer = System.Timers.Timer;

#endregion

namespace PlasmaDownloader.Packages
{
    public class RepositoryCache
    {
        public List<PackageDownloader.Repository> Repositories = new List<PackageDownloader.Repository>();
    }

    public class PackageDownloader : IDisposable
    {
        bool isRefreshing;
        string masterContent;
        readonly string masterUrl;
        readonly PlasmaDownloader plasmaDownloader;
        readonly Timer refreshTimer;
        List<Repository> repositories = new List<Repository>();

        public List<Repository> Repositories { get { return repositories; } }

        public event EventHandler PackagesChanged = delegate { };
        public event EventHandler MasterManifestDownloaded = delegate { };

        public DateTime MasterLastModified;

        public PackageDownloader(PlasmaDownloader plasmaDownloader)
        {
            this.plasmaDownloader = plasmaDownloader;
            masterUrl = this.plasmaDownloader.Config.PackageMasterUrl;
            LoadRepositories();
            if (plasmaDownloader.Config.RepoMasterRefresh > 0)
            {
                refreshTimer = new Timer(this.plasmaDownloader.Config.RepoMasterRefresh * 1000);
                refreshTimer.AutoReset = true;
                refreshTimer.Elapsed += RefreshTimerElapsed;
                refreshTimer.Start();
            }
            LoadMasterAndVersions();

        }

        public void Dispose()
        {
            if (refreshTimer != null)
            {
                refreshTimer.Stop();
                refreshTimer.Elapsed -= RefreshTimerElapsed;
            }
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
            List<Repository> repositoriesCopy;
            lock (repositories) repositoriesCopy = Repositories.ToList();
            foreach (var repo in repositoriesCopy)
            {
                if (!string.IsNullOrEmpty(repo.BaseUrl))
                {
                    Version versionEntry;
                    if (repo.VersionsByTag.TryGetValue(name, out versionEntry))
                    {
                        // find by package name
                        return CreateDownload(repo, versionEntry);
                    }

                    if (repo.VersionsByInternalName.TryGetValue(name, out versionEntry))
                    {
                        // find by internal name
                        return CreateDownload(repo, versionEntry);
                    }
                }
            }

            return null;
        }

        public Task LoadMasterAndVersions()
        {
            return Task.Factory.StartNew(() =>
            {
                if (isRefreshing) return;
                isRefreshing = true;
                try
                {
                    if (refreshTimer != null) refreshTimer.Stop();
                    var hasChanged = false;

                    try
                    {
                        var repoList = Utils.DownloadFile(masterUrl + "/repos.gz", MasterLastModified);
                        try
                        {
                            if (repoList.WasModified)
                            {
                                if (ParseMaster(new GZipStream(new MemoryStream(repoList.Content), CompressionMode.Decompress))) hasChanged = true;
                                MasterLastModified = repoList.DateModified;
                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError("Error parsing package master {0}", ex);
                        }

                    }
                    catch (Exception ex)
                    {
                        Trace.TraceWarning("Error loading package master from " + masterUrl);
                    }

                    // update all repositories 
                    var waiting = new List<Task<Repository.RefreshResponse>>();
                    foreach (var entry in repositories)
                    {
                        try
                        {
                            var r = entry.Refresh();
                            waiting.Add(r);
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError("Could not refresh repository {0}: {1}", entry.BaseUrl, ex);
                        }
                    }

                    Task.WaitAll(waiting.ToArray()); //wait until all "repositories" element finish downloading.

                    
                    if (hasChanged)
                    {
                        SaveRepositories();
                        Utils.StartAsync(() => PackagesChanged(this, EventArgs.Empty));
                    }
                }
                finally
                {
                    isRefreshing = false;
                    Utils.StartAsync(() => MasterManifestDownloaded(this, EventArgs.Empty));
                    if (refreshTimer != null) refreshTimer.Start();
                }
            });
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
            var path = Utils.MakePath(plasmaDownloader.SpringPaths.Cache, "repositories.json");
            try
            {
                if (File.Exists(path))
                {
                    lock (repositories)
                    {
                        repositories = JsonConvert.DeserializeObject<RepositoryCache>(File.ReadAllText(path)).Repositories;
                    }
                }
                else
                    Trace.TraceWarning("PackageDownloader : File don't exist : {0}", path);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Could not load repository cache from {0}: {1}", path, ex);
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
            var path = Utils.MakePath(plasmaDownloader.SpringPaths.Cache, "repositories.json");
            lock (repositories)
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(new RepositoryCache() { Repositories = repositories }));
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

            public string BaseUrl { get; set; }
            public DateTime LastModified { get; set; }

            public Dictionary<string, Version> VersionsByInternalName { get { return versionsByInternalName; } }

            public Dictionary<string, Version> VersionsByTag { get { return versionsByTag; } }

            public Repository() { }

            public Repository(string baseUrl)
            {
                BaseUrl = baseUrl;
            }

            public Task<RefreshResponse> Refresh()
            {
                return Task.Factory.StartNew(() =>
                {
                    var res = new RefreshResponse();

                    try
                    {
                        var file = Utils.DownloadFile(BaseUrl + "/versions.gz", LastModified);
                        if (file.WasModified)
                        {
                            List<Version> changes;
                            ParseVersionList(file.Content, out changes);
                            res.ChangedVersions = changes;
                            res.HasChanged = true;
                            LastModified = file.DateModified;
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Error reading version list from {0}: {1}", BaseUrl, ex);
                    }
                    return res;
                });
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