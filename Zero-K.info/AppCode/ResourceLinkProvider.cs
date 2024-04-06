﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ZkData;

namespace ZeroKWeb
{
    public static class ResourceLinkProvider
    {
        const double CheckPeriodForMissingLinks = 1; // check invalid every minute
        const double CheckPeriodForValidLinks = 60 * 12; // check links every 12 hours
        static readonly Dictionary<int, RequestData> Requests = new Dictionary<int, RequestData>();

        public static string[] Mirrors = GlobalConst.DefaultDownloadMirrors;

        static ResourceLinkProvider()
        {
        }

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
                torrent = null;
                links = null;
                dependencies = null;
                resourceType = ResourceType.Map;
                torrentFileName = null;
                return false;
            }

            dependencies = resource.ResourceDependencies.Select(x => x.NeedsInternalName).ToList();
            resourceType = resource.TypeID;

            var bestOld = resource.ResourceContentFiles.FirstOrDefault(x => x.LinkCount == resource.ResourceContentFiles.Max(y => y.LinkCount));
            if (bestOld != null && bestOld.LinkCount > 0 && (resource.MissionID != null ||
                (resource.LastLinkCheck != null && DateTime.UtcNow.Subtract(resource.LastLinkCheck.Value).TotalHours < 2)))
            {
                // use cached values for missions or resources checked less than 1 day ago
                links = PlasmaServer.GetLinkArray(bestOld);
                torrent = PlasmaServer.GetTorrentData(bestOld);
                torrentFileName = PlasmaServer.GetTorrentFileName(bestOld);
                if (links.Count > 0) db.Database.ExecuteSqlCommand("UPDATE Resources SET DownloadCount = DownloadCount+1 WHERE ResourceID={0}", resource.ResourceID);
                else db.Database.ExecuteSqlCommand("UPDATE Resources SET NoLinkDownloadCount = NoLinkDownloadCount+1 WHERE ResourceID={0}", resource.ResourceID);

                return true;
            }

            RequestData data;
            var isNew = false;
            lock (Requests)
            {
                if (!Requests.TryGetValue(resource.ResourceID, out data))
                {
                    data = new RequestData(resource.ResourceID);
                    isNew = true;
                    Requests.Add(resource.ResourceID, data);
                }
            }

            if (!isNew)
            {
                // request is ongoing, wait for completion
                data.WaitHandle.WaitOne();
                torrentFileName = PlasmaServer.GetTorrentFileName(data.ContentFile);
                links = PlasmaServer.GetLinkArray(data.ContentFile);
                torrent = PlasmaServer.GetTorrentData(data.ContentFile);
                if (links.Count > 0) db.Database.ExecuteSqlCommand("UPDATE Resources SET DownloadCount = DownloadCount+1 WHERE ResourceID={0}", resource.ResourceID);
                else db.Database.ExecuteSqlCommand("UPDATE Resources SET NoLinkDownloadCount = NoLinkDownloadCount+1 WHERE ResourceID={0}", resource.ResourceID);
                return true;
            }
            else
            {
                // new request - actually perform it
                try
                {
                    var toCheck = from x in resource.ResourceContentFiles
                                  group x by new { x.FileName, x.Length }
                                  into g
                                  where !g.Key.FileName.EndsWith(".sdp")
                                  select g.First();

                    Task.WaitAll(toCheck.Select(x => Task.Factory.StartNew(() => UpdateLinks(x))).ToArray());

                    db.SaveChanges();

                    // find best content file - the one with most links
                    var best = resource.ResourceContentFiles.FirstOrDefault(x => x.LinkCount == resource.ResourceContentFiles.Max(y => y.LinkCount));

                    if (best != null) data.ContentFile = best;
                    else data.ContentFile = resource.ResourceContentFiles.First(); // all content files sux, reurn any

                    links = PlasmaServer.GetLinkArray(data.ContentFile);
                    torrent = PlasmaServer.GetTorrentData(data.ContentFile);
                    torrentFileName = PlasmaServer.GetTorrentFileName(data.ContentFile);
                    if (links.Count > 0) resource.DownloadCount++;
                    else resource.NoLinkDownloadCount++;
                    db.SaveChanges();
                    return true;
                }
                finally
                {
                    lock (Requests) Requests.Remove(data.ResourceID);
                    data.WaitHandle.Set(); // notify other waiting Requests that its done
                }
            }
        }

        public static void ValidateLink(string link, int length, List<string> valids)
        {
            string realLink;
            if (GetLinkLength(link, out realLink) != length) lock (valids) valids.Remove(link); // invalid length, remove
            else if (link != realLink)
            {
                lock (valids) // redirect, update url
                {
                    valids.Remove(link);
                    valids.Add(realLink);
                }
            }
        }

        static long GetLinkLength(string url, out string redirectUrl)
        {
            redirectUrl = url;
            try
            {
                var wr = (HttpWebRequest)WebRequest.Create(url);
                wr.Timeout = 2000;
                wr.Method = "HEAD";
                var res = wr.GetResponse();
                redirectUrl = res.ResponseUri.ToString();
                var cl = res.ContentLength;
                wr.Abort();
                return cl;
            }
            catch
            {
                return 0;
            }
        }

        public static void UpdateLinks(ResourceContentFile content)
        {
            var valids = new List<string>();
            if (content.LinkCount > 0 || content.Links != null) valids = new List<string>(content.Links.Split('\n')); // get previous links

            if (content.FileName.EndsWith(".sdp")) return;

            if (!Debugger.IsAttached)
            {
                // should we use cached entries or run full check?
                if (content.Resource.LastLinkCheck != null)
                {
                    if (content.LinkCount > 0 &&
                        DateTime.UtcNow.Subtract(content.Resource.LastLinkCheck.Value).TotalMinutes < CheckPeriodForValidLinks) return;
                    if (content.LinkCount == 0 &&
                        DateTime.UtcNow.Subtract(content.Resource.LastLinkCheck.Value).TotalMinutes < CheckPeriodForMissingLinks) return;
                }
            }

            // combine with hardcoded mirrors
            foreach (var url in Mirrors)
            {
                var replaced = url.Replace("%t", content.Resource.TypeID == ResourceType.Map ? "maps" : "games").Replace("%f", content.FileName);
                if (!valids.Contains(replaced)) valids.Add(replaced);
            }

            // check validity of all links at once

            Task.WaitAll(new List<string>(valids).Select(link => Task.Factory.StartNew(() => ValidateLink(link, content.Length, valids))).ToArray());

            valids = valids.Distinct().ToList();

            lock (content)
            {
                content.LinkCount = valids.Count;
                content.Resource.LastLinkCheck = DateTime.UtcNow;
                content.Links = string.Join("\n", valids.ToArray());
            }
        }

        class RequestData
        {
            public ResourceContentFile ContentFile;
            public readonly int ResourceID;
            public readonly EventWaitHandle WaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

            public RequestData(int resourceID)
            {
                ResourceID = resourceID;
            }
        }
    }
}