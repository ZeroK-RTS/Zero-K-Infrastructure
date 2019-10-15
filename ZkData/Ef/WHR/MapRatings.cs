using PlasmaShared;
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

        private static readonly IEnumerable<Category> categories = Enum.GetValues(typeof(Category)).Cast<Category>();
        private static ConcurrentDictionary<Category, ConcurrentDictionary<int, Player>> maps = new ConcurrentDictionary<Category, ConcurrentDictionary<int, Player>>();
        private static ConcurrentDictionary<Category, ConcurrentDictionary<int, Rating>> mapRatings = new ConcurrentDictionary<Category, ConcurrentDictionary<int, Rating>>();
        private static ConcurrentDictionary<Category,List<Rating>> mapRanking = new ConcurrentDictionary<Category, List<Rating>>();
        private static int lastPollId = -1;
        private static readonly Rating defaultRating = new Rating(WholeHistoryRating.RatingOffset, float.PositiveInfinity, null, int.MaxValue, 1);

        public static void Init()
        {

            foreach (Category cat in categories)
            {
                maps[cat] = new ConcurrentDictionary<int, Player>();
                mapRatings[cat] = new ConcurrentDictionary<int, Rating>();
                mapRanking[cat] = new List<Rating>();
            }
            Task.Factory.StartNew(() =>
            {
                UpdateRatings(true);
            });
            ladderRecalculationTimer = new Timer((t) => { UpdateRatings(); }, null, 15 * 60000, (int)(GlobalConst.LadderUpdatePeriod * 3600 * 1000 + 4242));
        }

        public static Rating GetMapRating(int ResourceId, Category cat)
        {
            Rating rating;
            if (mapRatings[cat].TryGetValue(ResourceId, out rating)) return rating;
            return defaultRating;
        }

        public static List<Rating> GetMapRanking(AutohostMode mode)
        {
            Category cat = Category.CasualTeams;
            if (mode == AutohostMode.GameChickens) cat = Category.Coop;
            if (mode == AutohostMode.GameFFA) cat = Category.FFA;
            return GetMapRanking(cat);
        }

        public static List<Rating> GetMapRanking(Category cat)
        {
            return mapRanking[cat];
        }

        private static Player GetPlayer(int id, Category cat)
        {
            lock (maps)
            {
                Player player;
                if (maps[cat].TryGetValue(id, out player)) return player;
                player = new Player(id);
                maps[cat].TryAdd(id, player);
                return player;
            }
        }

        private static void UpdateRatings(bool init = false)
        {
            List<MapPollOutcome> outcomes;
            DateTime start = DateTime.Now;
            try
            {
                using (var db = new ZkDataContext())
                {

                    db.Database.CommandTimeout = 300;
                    var endPollId = lastPollId + 10000;
                    outcomes = db.MapPollOutcomes.Where(x => x.MapPollID > lastPollId && x.MapPollID < endPollId).Include(x => x.MapPollOptions).OrderBy(x => x.MapPollID).AsNoTracking().ToList();
                }
                outcomes.ForEach(poll =>
                {
                    var opts = poll.MapPollOptions.DistinctBy(x => x.ResourceID).OrderByDescending(x => x.Votes).ToList();
                    var winners = opts.Where(x => x.Votes == opts[0].Votes);
                    var losers = opts.Where(x => x.Votes != opts[0].Votes);
                    if (losers.Count() > 0)
                    {
                        var game = new Game(winners.Select(x => GetPlayer(x.ResourceID, poll.Category)).ToList(), losers.Select(x => GetPlayer(x.ResourceID, poll.Category)).ToList(), true, 0, poll.MapPollID);
                        game.whitePlayers.ForEach(x => x.AddGame(game));
                        game.blackPlayers.ForEach(x => x.AddGame(game));
                    }
                    lastPollId = poll.MapPollID;
                });
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error reading map ratings from db: " + ex);
            }
            try {
                foreach (Category cat in maps.Keys)
                {
                    if (init) RunIterations(70, cat);
                    RunIterations(3, cat);

                    using (var db = new ZkDataContext())
                    {
                        var newRanking = maps[cat].Values.Select(x => x.days[0]).OrderByDescending(x => x.r).ToList();
                        var ranks = new List<Rating>();
                        for (int i = 0; i < newRanking.Count; i++)
                        {
                            int id = newRanking[i].player.id;
                            var rating = new Rating(newRanking[i].GetElo() + WholeHistoryRating.RatingOffset, newRanking[i].GetEloStdev(), db.Resources.FirstOrDefault(r => r.ResourceID == id), i, i / (float)newRanking.Count);
                            ranks.Add(rating);
                            mapRatings[cat][id] = rating;
                        }
                        mapRanking[cat] = ranks;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error calculating map ratings: " + ex);
            }
            Trace.TraceInformation("Most recent map poll processed for ratings: " + lastPollId + ". Processing took " + DateTime.Now.Subtract(start).TotalSeconds + " seconds.");
        }

        private static void RunIterations(int count, Category cat)
        {
            for (int i = 0; i < count - 1; i++)
            {
                maps[cat].Values.ForEach(x => x.RunOneNewtonIteration(false));
            }
            maps[cat].Values.ForEach(x => x.RunOneNewtonIteration(true));
        }

        public enum Category
        {
            CasualTeams,
            Coop,
            FFA
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
