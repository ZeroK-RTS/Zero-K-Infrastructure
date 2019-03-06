using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZkData;

namespace Ratings
{
    public static class MapRatings
    {
        private static Timer ladderRecalculationTimer;

        private static ConcurrentDictionary<int, Player> maps = new ConcurrentDictionary<int, Player>();
        private static ConcurrentDictionary<int, Rating> mapRatings = new ConcurrentDictionary<int, Rating>();
        private static List<Rating> mapRanking = new List<Rating>();
        private static int lastPollId = -1;
        private static readonly Rating defaultRating = new Rating(WholeHistoryRating.RatingOffset, float.PositiveInfinity, null, int.MaxValue, 1);

        public static void Init()
        {

            Task.Factory.StartNew(() =>
            {
                UpdateRatings(true);
            });
            ladderRecalculationTimer = new Timer((t) => { UpdateRatings(); }, null, 15 * 60000, (int)(GlobalConst.LadderUpdatePeriod * 3600 * 1000 + 4242));
        }

        public static Rating GetMapRating(int ResourceId)
        {
            Rating rating;
            if (mapRatings.TryGetValue(ResourceId, out rating)) return rating;
            return defaultRating;
        }

        public static List<Rating> GetMapRanking()
        {
            return mapRanking;
        }

        private static Player GetPlayer(int id)
        {
            lock (maps)
            {
                Player player;
                if (maps.TryGetValue(id, out player)) return player;
                player = new Player(id);
                maps.TryAdd(id, player);
                return player;
            }
        }

        private static void UpdateRatings(bool init = false)
        {
            try
            {
                using (var db = new ZkDataContext())
                {
                    db.MapPollOutcomes.Where(x => x.MapPollID > lastPollId).Include(x => x.MapPollOptions).OrderBy(x => x.MapPollID).AsNoTracking().AsEnumerable().ForEach(poll =>
                    {
                        var opts = poll.MapPollOptions.DistinctBy(x => x.ResourceID).OrderByDescending(x => x.Votes).ToList();
                        var winners = opts.Where(x => x.Votes == opts[0].Votes);
                        var losers = opts.Where(x => x.Votes != opts[0].Votes);
                        if (losers.Count() > 0)
                        {
                            var game = new Game(winners.Select(x => GetPlayer(x.ResourceID)).ToList(), losers.Select(x => GetPlayer(x.ResourceID)).ToList(), true, 0, poll.MapPollID);
                            game.whitePlayers.ForEach(x => x.AddGame(game));
                            game.blackPlayers.ForEach(x => x.AddGame(game));
                        }
                        lastPollId = poll.MapPollID;
                    });
                }
                if (init) RunIterations(70);
                RunIterations(3);
                using (var db = new ZkDataContext())
                {
                    var newRanking = maps.Values.Select(x => x.days[0]).OrderByDescending(x => x.r).ToList();
                    var ranks = new List<Rating>();
                    for (int i = 0; i < newRanking.Count; i++)
                    {
                        int id = newRanking[i].player.id;
                        var rating = new Rating(newRanking[i].GetElo() + WholeHistoryRating.RatingOffset, newRanking[i].GetEloStdev(), db.Resources.FirstOrDefault(r => r.ResourceID == id), i, i / (float)newRanking.Count);
                        ranks.Add(rating);
                        mapRatings[id] = rating;
                    }
                    mapRanking = ranks;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error calculating map ratings: " + ex);
            }
        }

        private static void RunIterations(int count)
        {
            for (int i = 0; i < count - 1; i++)
            {
                maps.Values.ForEach(x => x.RunOneNewtonIteration(false));
            }
            maps.Values.ForEach(x => x.RunOneNewtonIteration(true));
        }

        public class Rating
        {
            public readonly float Elo, EloStdev, Percentile;
            public readonly Resource Map;
            public readonly int Rank;

            public Rating(float elo, float stdev, Resource map, int rank, float percentile)
            {
                Elo = elo;
                EloStdev = stdev;
                Map = map;
                Rank = rank;
                Percentile = percentile;
            }
        }
    }
}
