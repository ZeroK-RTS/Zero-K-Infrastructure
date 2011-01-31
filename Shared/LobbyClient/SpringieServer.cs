using System;
using System.Collections.Generic;
using PlasmaShared;
using PlasmaShared.ContentService;

namespace LobbyClient
{
  public class SpringieServer
  {
    const int CachedEntrySeconds = 3600; // entries valid for 1 hour

    public delegate void EloRecieved(string name, double elo, double w, object token);

    readonly Dictionary<string, EloEntry> eloCache = new Dictionary<string, EloEntry>();
    readonly Dictionary<string, int> top10 = new Dictionary<string, int>();

    public SpringieServer()
    {
      var wc = new ContentService { Proxy = null };
      wc.GetEloTop10Completed +=
        (s, e) => { if (!e.Cancelled && e.Error == null && e.Result != null) for (var i = 0; i < e.Result.Length; i++) top10[e.Result[i]] = i + 1; };
      wc.GetEloTop10Async();
    }

    public double GetElo(string name)
    {
      double elo;
      double w;
      GetElo(name, out elo, out w);
      return elo;
    }


    /// <summary>
    /// Gets elo synchronously (can get from cache)
    /// </summary>
    /// <param name="name"></param>
    /// <param name="elo"></param>
    /// <param name="w"></param>
    /// <returns></returns>
    public bool GetElo(string name, out double elo, out double w)
    {
      EloEntry cachedEntry;

      lock (eloCache)
        if (eloCache.TryGetValue(name, out cachedEntry))
        {
          if (DateTime.Now.Subtract(cachedEntry.Time).TotalSeconds < CachedEntrySeconds)
          {
            elo = cachedEntry.Elo;
            w = cachedEntry.W;
            return true;
          }
        }

      var serv = new ContentService() { Proxy = null };
      EloInfo ret;
      try
      {
        ret = serv.GetEloByName(name);
        elo = ret.Elo;
        w = ret.Weight;
      }
      catch
      {
        elo = 1500;
        w = 1;
        return false;
      }

      EloEntry entry;
      lock (eloCache)
      {
        if (!eloCache.TryGetValue(name, out entry))
        {
          entry = new EloEntry();
          eloCache[name] = entry;
        }
      }

      entry.Elo = ret.Elo;
      entry.W = ret.Weight;
      entry.Time = DateTime.Now;

      return true;
    }


    /// <summary>
    /// Loads elo asynchronously
    /// </summary>
    /// <param name="name">name of player</param>
    /// <param name="callBack">delegate to be invoked when elo is fetched</param>
    /// <param name="token">token to be passed back to delegate</param>
    public void GetEloAsync(string name, EloRecieved callBack, object token)
    {
      Utils.StartAsync(() =>
        {
          double elo;
          double w;
          GetElo(name, out elo, out w);
          callBack(name, elo, w, token);
        });
    }

    public EloEntry GetEloEntry(string name)
    {
      double elo;
      double w;
      GetElo(name, out elo, out w);
      return new EloEntry() { Elo = elo, W = w };
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