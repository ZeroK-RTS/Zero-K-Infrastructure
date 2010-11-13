#region using

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ZkData;

#endregion

namespace ZeroKWeb
{
  public static class ResourceLinkProvider
  {
    const double CheckPeriodForMissingLinks = 1; // check invalid every minute
    const double CheckPeriodForValidLinks = 60*12; // check links every 12 hours
    static readonly Dictionary<int, RequestData> Requests = new Dictionary<int, RequestData>();

    public static string[] Mirrors = new string[] { };

    static ResourceLinkProvider()
    {
      var data = ConfigurationManager.AppSettings["Mirrors"];
      var lines = data.Split('\n');
      var newMirrors = new List<string>();
      foreach (var l in lines) newMirrors.Add(l.Trim());
      Mirrors = newMirrors.ToArray();
    }

    public static bool GetLinksAndTorrent(string internalName,
                                          out List<string> links,
                                          out byte[] torrent,
                                          out List<string> dependencies,
                                          out ZkData.ResourceType resourceType,
                                          out string torrentFileName)
    {
      var db = new ZkDataContext();

      var resource = db.Resources.SingleOrDefault(x => x.InternalName == internalName);
      if (resource == null)
      {
        torrent = null;
        links = null;
        dependencies = null;
        resourceType = ZkData.ResourceType.Map;
        torrentFileName = null;
        return false;
      }

      dependencies = resource.ResourceDependencies.Select(x => x.NeedsInternalName).ToList();
      resourceType = resource.TypeID;

      var bestOld = resource.ResourceContentFiles.FirstOrDefault(x => x.LinkCount == resource.ResourceContentFiles.Max(y => y.LinkCount));
      if (bestOld != null && bestOld.LinkCount > 0 && resource.MissionID != null ||
          (resource.LastLinkCheck != null && DateTime.Now.Subtract(resource.LastLinkCheck.Value).Days < 1))
      {
        // use cached values for missions or resources checked less than 1 day ago
        links = bestOld.GetLinkArray();
        torrent = bestOld.GetTorrentData();
        torrentFileName = bestOld.GetTorrentFileName();
        if (links.Count > 0) db.ExecuteCommand("UPDATE Resource SET DownloadCount = DownloadCount+1 WHERE ResourceID={0}", resource.ResourceID);
        else db.ExecuteCommand("UPDATE Resource SET NoLinkDownloadCount = NoLinkDownloadCount+1 WHERE ResourceID={0}", resource.ResourceID);

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
        torrentFileName = data.ContentFile.GetTorrentFileName();
        links = data.ContentFile.GetLinkArray();
        torrent = data.ContentFile.GetTorrentData();
        if (links.Count > 0) db.ExecuteCommand("UPDATE Resource SET DownloadCount = DownloadCount+1 WHERE ResourceID={0}", resource.ResourceID);
        else db.ExecuteCommand("UPDATE Resource SET NoLinkDownloadCount = NoLinkDownloadCount+1 WHERE ResourceID={0}", resource.ResourceID);
        return true;
      }
      else
      {
        // new request - actually perform it
        try
        {
          if (resource.ResourceContentFiles.Count > 1) // multiple content fiels - perform updates in paralell
          {
            var toCheck = from x in resource.ResourceContentFiles
                          group x by new { x.FileName, x.Length }
                          into g where !g.Key.FileName.EndsWith(".sdp") select g.First();

            Task.WaitAll(toCheck.Select(x => Task.Factory.StartNew(() => UpdateLinks(x))).ToArray());
          }
          else foreach (var content in resource.ResourceContentFiles) UpdateLinks(content);

          db.SubmitChanges();

          // find best content file - the one with most links
          var best = resource.ResourceContentFiles.FirstOrDefault(x => x.LinkCount == resource.ResourceContentFiles.Max(y => y.LinkCount));

          if (best != null) data.ContentFile = best;
          else data.ContentFile = resource.ResourceContentFiles.First(); // all content files sux, reurn any

          links = data.ContentFile.GetLinkArray();
          torrent = data.ContentFile.GetTorrentData();
          torrentFileName = data.ContentFile.GetTorrentFileName();
          if (links.Count > 0) resource.DownloadCount++;
          else resource.NoLinkDownloadCount++;
          db.SubmitChanges();
          return true;
        }
        finally
        {
          lock (Requests) Requests.Remove(data.ResourceID);
          data.WaitHandle.Set(); // notify other waiting Requests that its done
        }
      }
    }

    static List<string> GetJobjolMirrorLinks(string fileName, ZkData.ResourceType type)
    {
      var result = new List<string>();

      result.Add(string.Format("http://www.springfiles.com/download.php?maincategory=1&subcategory={0}&file={1}",
                               type == ZkData.ResourceType.Map ? 2 : 5,
                               fileName));
      try
      {
        using (var wc = new WebClient())
        {
          var pom = string.Format("http://www.springfiles.com/checkmirror.php?q={0}&c={1}",
                                  Uri.EscapeDataString(fileName),
                                  type == ZkData.ResourceType.Mod ? "mods" : "maps");

          var ret = wc.DownloadString(pom);

          var matches = Regex.Matches(ret, "\\&mirror=([^\\&]+)");
          foreach (Match match in matches)
          {
            if (match.Success && match.Groups.Count > 1)
            {
              var mirror = Uri.UnescapeDataString(match.Groups[1].Value);
              result.Add(mirror);
            }
          }
        }
      }
      catch (Exception ex)
      {
        Console.Error.WriteLine("Error getting jobjol mirrors " + ex);
      }
      return result;
    }

    static long GetLinkLength(string url)
    {
      try
      {
        var wr = (HttpWebRequest)WebRequest.Create(url);
        wr.Timeout = 3000;
        wr.Method = "GET";
        var res = wr.GetResponse();
        var cl = res.ContentLength;
        wr.Abort();
        return cl;
      }
      catch
      {
        return 0;
      }
    }

    static void UpdateLinks(ResourceContentFile content)
    {
      if (content.FileName.EndsWith(".sdp")) return; // ignore sdp files - those have special downloader

      var valids = new List<string>();
      if (content.LinkCount > 0 || content.Links != null) valids = new List<string>(content.Links.Split('\n')); // get previous links

      if (!Debugger.IsAttached)
      {
        // should we use cached entries or run full check?
        if (content.Resource.LastLinkCheck != null)
        {
          if (content.LinkCount > 0 && DateTime.UtcNow.Subtract(content.Resource.LastLinkCheck.Value).TotalMinutes < CheckPeriodForValidLinks) return;
          if (content.LinkCount == 0 && DateTime.UtcNow.Subtract(content.Resource.LastLinkCheck.Value).TotalMinutes < CheckPeriodForMissingLinks) return;
        }
      }

      // get mirror list from jobjol
      foreach (var link in GetJobjolMirrorLinks(content.FileName, content.Resource.TypeID)) if (!valids.Contains(link)) valids.Add(link);

      // combine with hardcoded mirrors
      foreach (var url in Mirrors)
      {
        var replaced = url.Replace("%t", content.Resource.TypeID == ZkData.ResourceType.Mod ? "mods" : "maps").Replace("%f", content.FileName);
        if (!valids.Contains(replaced)) valids.Add(replaced);
      }

      // check validity of all links at once

      Task.WaitAll(new List<string>(valids).Select(link => Task.Factory.StartNew(() => ValidateLink(link, content.Length, valids))).ToArray());

      content.LinkCount = valids.Count;
      content.Resource.LastLinkCheck = DateTime.UtcNow;
      content.Links = string.Join("\n", valids.ToArray());
    }

    static void ValidateLink(string link, int length, List<string> valids)
    {
      if (GetLinkLength(link) != length) lock (valids) valids.Remove(link);
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