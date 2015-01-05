using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ZkData;

namespace LobbyClient
{
  public class SpringieServer
  {
    const int CachedEntrySeconds = 3600; // entries valid for 1 hour

    readonly Dictionary<string, int> top10 = new Dictionary<string, int>();

    public SpringieServer()
    {
      var wc = GlobalConst.GetContentService();
        Task.Factory.StartNew(() => {
            try {
                var res = wc.GetEloTop10();
                for (var i = 0; i < res.Count; i++) top10[res[i]] = i + 1; 
            } catch (Exception ex) {
             Trace.TraceError(ex.ToString());
            }
        });
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