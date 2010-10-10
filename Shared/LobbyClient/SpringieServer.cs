using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading;
using PlasmaShared;

namespace LobbyClient
{
    public class SpringieServer
    {
        const string BaseUrl = "http://springie.licho.eu/";
        const int CachedEntrySeconds = 3600; // entries valid for 1 hour

        public delegate void EloRecieved(string name, double elo, double w, object token);

        readonly Dictionary<string, EloEntry> eloCache = new Dictionary<string, EloEntry>();
        Dictionary<string, int> top10 = new Dictionary<string, int>();

        public SpringieServer()
        {
            var wc = new WebClient { Proxy = null };
            wc.DownloadStringCompleted += (s, e) =>
                {
                    if (!e.Cancelled && e.Error == null && e.Result != null)
                    {
                        var items = e.Result.Trim().Split('\n');
                        for (var i = 0; i < items.Length; i++) top10[items[i]] = i + 1;
                    }
                };

            wc.DownloadStringAsync(new Uri(BaseUrl + "top10.php"));
        }

				public double GetElo(string name)
				{
					double elo;
					double w;
					GetElo(name, out elo, out w);
					return elo;
				}


				public EloEntry GetEloEntry(string name)
				{
					double elo;
					double w;
					GetElo(name, out elo, out w);
					return new EloEntry() { Elo = elo, W = w };
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
            var previousCulture = Thread.CurrentThread.CurrentCulture;
            var webClient = new WebClient();
            try
            {
                EloEntry cachedEntry;

                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

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

                try
                {
                    var ret = webClient.DownloadString(string.Format("{0}stats.php?welo={1}", BaseUrl, name));
                    var args = ret.Trim().Split('|');
										if (args.Length < 2)
										{
											elo = 1500;
											w = 1;
											return false;
										}
										else
										{
											double.TryParse(args[0], out elo);
											double.TryParse(args[1], out w);

											EloEntry entry;
											lock (eloCache)
											{
												if (!eloCache.TryGetValue(name, out entry))
												{
													entry = new EloEntry();
													eloCache[name] = entry;
												}
											}

											entry.Elo = elo;
											entry.W = w;
											entry.Time = DateTime.Now;

											return true;
										}
                }
                catch (WebException)
                {
                    elo = 1500;
                    w = 1;
                    return false;
                }
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previousCulture;
                webClient.Dispose();
            }
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