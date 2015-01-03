using System;
using System.Collections.Generic;
using ZkData;
using ZkData.ContentService;

namespace LobbyClient
{
  public class SpringieServer
  {
    const int CachedEntrySeconds = 3600; // entries valid for 1 hour

    readonly Dictionary<string, int> top10 = new Dictionary<string, int>();

    public SpringieServer()
    {
      var wc = new ContentService { Proxy = null };
      wc.GetEloTop10Completed +=
        (s, e) => { if (!e.Cancelled && e.Error == null && e.Result != null) for (var i = 0; i < e.Result.Length; i++) top10[e.Result[i]] = i + 1; };
      wc.GetEloTop10Async();
    }




    /// <summary>
    /// Returns position in top10 (0 if not in top 10)
    /// </summary>
    public int GetTop10Rank(string name)
    {
      int num;
      top10.TryGetValue(name, out num);
      return num;
    }

    public class EloEntry
    {
      public Double Elo;
      public DateTime Time;
      public Double W;
    }
  }
}