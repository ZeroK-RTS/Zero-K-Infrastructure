using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ZkData;

namespace ZeroKWeb
{
    public static class ResourceLinkProvider
    {
        const double SpringfilesRecheckHours = 24;

        // dedup concurrent springfiles probes per Md5 — Lazy guarantees a single Task even under contention
        static readonly ConcurrentDictionary<string, Lazy<Task<bool>>> InflightProbes
            = new ConcurrentDictionary<string, Lazy<Task<bool>>>();

        public static bool GetLinksAndTorrent(string internalName,
                                              out List<string> links,
                                              out byte[] torrent,
                                              out List<string> dependencies,
                                              out ResourceType resourceType,
                                              out string torrentFileName)
        {
            var db = new ZkDataContext();
            var resource = db.Resources.SingleOrDefault(x => x.InternalName == internalName);
            if (resource == null)
            {
                torrent = null; links = null; dependencies = null;
                resourceType = ResourceType.Map; torrentFileName = null;
                return false;
            }

            dependencies = resource.ResourceDependencies.Select(x => x.NeedsInternalName).ToList();
            resourceType = resource.TypeID;

            var content = resource.ResourceContentFiles.Where(x => !x.FileName.EndsWith(".sdp"))
                              .OrderByDescending(x => x.LinkCount).FirstOrDefault()
                          ?? resource.ResourceContentFiles.FirstOrDefault();

            if (content == null)
            {
                links = new List<string>();
                torrent = null; torrentFileName = null;
                return true;
            }

            torrentFileName = PlasmaServer.GetTorrentFileName(content);
            torrent = PlasmaServer.GetTorrentData(content);
            links = BuildLinks(content);

            if (links.Count > 0)
                db.Database.ExecuteSqlCommand("UPDATE Resources SET DownloadCount = DownloadCount+1 WHERE ResourceID={0}", resource.ResourceID);
            else
                db.Database.ExecuteSqlCommand("UPDATE Resources SET NoLinkDownloadCount = NoLinkDownloadCount+1 WHERE ResourceID={0}", resource.ResourceID);
            return true;
        }

        public static List<string> BuildLinks(ResourceContentFile content)
        {
            // missions: URLs are baked in by MissionUpdater
            if (content.Resource.MissionID != null)
                return string.IsNullOrEmpty(content.Links)
                    ? new List<string>()
                    : content.Links.Split('\n').Where(s => s.Length > 0).ToList();

            if (content.FileName.EndsWith(".sdp")) return new List<string>();

            var subfolder = content.Resource.TypeID == ResourceType.Map ? "maps" : "games";
            var localUrl = $"{GlobalConst.BaseSiteUrl}/content/{subfolder}/{content.FileName}";
            var springfilesUrl = $"{GlobalConst.SpringfilesBaseUrl}files/{subfolder}/{content.FileName}";

            var links = new List<string>();
            if (File.Exists(Global.MapPath($"~/content/{subfolder}/{content.FileName}"))) links.Add(localUrl);
            if (IsSpringfilesAvailable(content, springfilesUrl)) links.Add(springfilesUrl);
            return links;
        }

        // 24 h cache + dedup: at most one HEAD per Md5 in flight regardless of caller count.
        // LinkCount in DB encodes the cached probe result (1 = springfiles had it, 0 = it didn't).
        static bool IsSpringfilesAvailable(ResourceContentFile content, string url)
        {
            if (content.Resource.LastLinkCheck.HasValue
                && DateTime.UtcNow.Subtract(content.Resource.LastLinkCheck.Value).TotalHours < SpringfilesRecheckHours)
                return content.LinkCount > 0;

            var md5 = content.Md5;
            var length = content.Length;
            return InflightProbes.GetOrAdd(md5, key => new Lazy<Task<bool>>(
                () => Task.Run(() => RunProbe(key, url, length)),
                LazyThreadSafetyMode.ExecutionAndPublication)).Value.Result;
        }

        // The cache re-check inside the using block covers the gap between Task completion
        // and InflightProbes eviction — a caller arriving in that window creates a new Lazy
        // but the new probe sees the just-persisted LastLinkCheck and skips the HEAD.
        static bool RunProbe(string md5, string url, int length)
        {
            try
            {
                using (var db = new ZkDataContext())
                {
                    var cf = db.ResourceContentFiles.SingleOrDefault(x => x.Md5 == md5);
                    if (cf == null) return false;
                    if (cf.Resource.LastLinkCheck.HasValue
                        && DateTime.UtcNow.Subtract(cf.Resource.LastLinkCheck.Value).TotalHours < SpringfilesRecheckHours)
                        return cf.LinkCount > 0;

                    var ok = ProbeAndAssign(cf);
                    db.SaveChanges();
                    return ok;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("ResourceLinkProvider: probe failed for {0}: {1}", md5, ex.Message);
                return false;
            }
            finally
            {
                Lazy<Task<bool>> _;
                InflightProbes.TryRemove(md5, out _);
            }
        }

        // called once at registration time from PlasmaServer.RegisterResource;
        // caller's db.SaveChanges() persists. Runs synchronously so newly-registered
        // maps appear immediately on Detail pages without waiting for a first request.
        internal static void UpdateLinks(ResourceContentFile content)
        {
            if (content.FileName.EndsWith(".sdp") || content.Resource.MissionID != null) return;
            ProbeAndAssign(content);
        }

        // HEAD-probes springfiles, then writes Links/LinkCount/LastLinkCheck on the entity.
        // Caller is responsible for SaveChanges (or accepting unsaved entity state).
        // Returns true iff springfiles has the file at the expected length.
        static bool ProbeAndAssign(ResourceContentFile cf)
        {
            var subfolder = cf.Resource.TypeID == ResourceType.Map ? "maps" : "games";
            var localUrl = $"{GlobalConst.BaseSiteUrl}/content/{subfolder}/{cf.FileName}";
            var springfilesUrl = $"{GlobalConst.SpringfilesBaseUrl}files/{subfolder}/{cf.FileName}";

            var ok = HeadCheck(springfilesUrl, cf.Length);

            var links = new List<string>();
            if (File.Exists(Global.MapPath($"~/content/{subfolder}/{cf.FileName}"))) links.Add(localUrl);
            if (ok) links.Add(springfilesUrl);

            cf.Links = string.Join("\n", links);
            cf.LinkCount = ok ? 1 : 0;
            cf.Resource.LastLinkCheck = DateTime.UtcNow;
            return ok;
        }

        static bool HeadCheck(string url, int length)
        {
            try
            {
                var wr = (HttpWebRequest)WebRequest.Create(url);
                wr.Timeout = 5000;
                wr.Method = "HEAD";
                using (var res = wr.GetResponse()) return res.ContentLength == length;
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Springfiles HEAD failed for {0}: {1}", url, ex.Message);
                return false;
            }
        }
    }
}
