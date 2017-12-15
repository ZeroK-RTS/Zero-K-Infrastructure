using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using EntityFramework.Extensions;
using Microsoft.Linq.Translations;
using ZkData;
using ZkLobbyServer;

namespace ZeroKWeb
{
    public class LadderCalculator: ITopPlayerProvider
    {
        private const int LadderRefreshMinutes = 30;

        private LadderModel ladderModel = new LadderModel();

        private Timer timer;

        public LadderCalculator()
        {
            timer = new Timer((t) => { ladderModel = ComputeLadder(); }, this, LadderRefreshMinutes * 60 * 1000, LadderRefreshMinutes * 60 * 1000);
        }

        public void RecomputeNow()
        {
            ladderModel = ComputeLadder();
            TopPlayersUpdated?.Invoke(this, this);
        }

        public LadderModel GetLadder()
        {
            return ladderModel;
        }

        public List<Account> GetTop() => ladderModel?.TopAccounts;
        public List<Account> GetTopCasual() => ladderModel?.TopCasual;
        public event EventHandler<ITopPlayerProvider> TopPlayersUpdated;


        private static List<AwardItem> CalculateAwards(ZkDataContext db)
        {
            var monthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var validAwards =
                db.SpringBattles.Where(
                        x =>
                            (x.StartTime >= monthStart) && (x.HasBots == false) &&
                            (x.ResourceByMapResourceID.MapSupportLevel >= MapSupportLevel.Supported) && (x.ResourceByMapResourceID.MapIsSpecial == false))
                    .SelectMany(x => x.AccountBattleAwards)
                    .GroupBy(x => x.AwardKey);

            var awardItems = new List<AwardItem>();

            foreach (var awardsByType in validAwards)
            {
                var awardType = awardsByType.Key;

                var awardCounts =
                    awardsByType.GroupBy(x => x.Account).Select(x => new { Account = x.Key, Count = x.Count() }).OrderByDescending(x => x.Count);

                var topCountM = awardCounts.First().Count;
                var topCollectorsM = new List<Account>();
                foreach (var award in awardCounts)
                    if (award.Count == topCountM) topCollectorsM.Add(award.Account);
                    else break;

                var topScore = 0;
                string titleName = null;

                topScore = 0;
                var fullTitleM = "";
                var topActID = 0;
                var topBattleID = 0;
                foreach (var award in awardsByType)
                {
                    if (titleName == null) titleName = award.AwardDescription.Split(',').First();
                    int score;
                    if (int.TryParse(Regex.Replace(award.AwardDescription, @"\D", string.Empty), out score))
                        if (score > topScore)
                        {
                            topActID = award.AccountID;
                            topBattleID = award.SpringBattleID;
                            topScore = score;
                            fullTitleM = string.Join(" ", award.AwardDescription.Split(',').Skip(1));
                        }
                }

                var awardItem = new AwardItem
                {
                    AwardType = awardType,
                    AwardTitle = titleName,
                    TopScoreHolderM = db.Accounts.SingleOrDefault(x => x.AccountID == topActID),
                    TopScoreDescM = fullTitleM,
                    TopScoreBattlePlayerM =
                        db.SpringBattlePlayers.Include(x => x.SpringBattle)
                            .Include(x => x.SpringBattle.ResourceByMapResourceID)
                            .SingleOrDefault(x => (x.AccountID == topActID) && (x.SpringBattleID == topBattleID)),
                    TopCollectorsM = topCollectorsM,
                    TopCollectorCountM = topCountM
                };
                awardItems.Add(awardItem);
            }
            return awardItems;
        }

        object computeLadderLock = new object();
        private LadderModel ComputeLadder()
        {
            lock (computeLadderLock)
            {
                try
                {
                    var db = new ZkDataContext();
                    db.Database.CommandTimeout = 600;

                    var awardItems = CalculateAwards(db);

                    var ladderTimeout = DateTime.UtcNow.AddDays(-GlobalConst.LadderActivityDays);

                    // set unused accounts weight to 1
                    db.Accounts.Where(x => !x.SpringBattlePlayers.Any(
                                    y => (y.SpringBattle.StartTime > ladderTimeout) && !y.SpringBattle.IsMatchMaker && !y.IsSpectator))
                        .Update(acc => new Account() { EloWeight = 1 });

                    db.Accounts.Where(x => !x.SpringBattlePlayers.Any(y => (y.SpringBattle.StartTime > ladderTimeout) && y.SpringBattle.IsMatchMaker && !y.IsSpectator)).Update(acc => new Account() { EloMmWeight = 1 });

                    db.SaveChanges();


                    foreach (
                        var entry in
                        db.Accounts.Where(x => x.EloWeight > 1)
                            .Select(
                                acc =>
                                    new
                                    {
                                        Account = acc,
                                        LastGame =
                                        acc.SpringBattlePlayers.Where(x => !x.IsSpectator && !x.SpringBattle.IsMatchMaker)
                                            .OrderByDescending(x => x.SpringBattleID)
                                            .Select(x =>x.SpringBattle.StartTime).FirstOrDefault()
                                    }))
                    {
                        var days = DateTime.UtcNow.Subtract(entry.LastGame).TotalDays;
                        var decayRatio = ((days-7)/(GlobalConst.LadderActivityDays-7)).Clamp(0,1);
                        entry.Account.EloWeight = Math.Min(entry.Account.EloWeight,
                            Math.Max(1, GlobalConst.EloWeightMax - (GlobalConst.EloWeightMax - 1)*decayRatio));
                    }

                    db.SaveChanges();


                    foreach (
                        var entry in
                        db.Accounts.Where(x => x.EloMmWeight > 1)
                            .Select(
                                acc =>
                                    new
                                    {
                                        Account = acc,
                                        LastGame =
                                        acc.SpringBattlePlayers.Where(x => !x.IsSpectator && x.SpringBattle.IsMatchMaker)
                                            .OrderByDescending(x => x.SpringBattleID)
                                            .Select(x => x.SpringBattle.StartTime).FirstOrDefault()
                                    }))
                    {
                        var days = DateTime.UtcNow.Subtract(entry.LastGame).TotalDays;
                        var decayRatio = ((days - 7) / (GlobalConst.LadderActivityDays - 7)).Clamp(0, 1);
                        entry.Account.EloMmWeight = Math.Min(entry.Account.EloMmWeight,
                            Math.Max(1, GlobalConst.EloWeightMax - (GlobalConst.EloWeightMax - 1) * decayRatio));
                    }
                    db.SaveChanges();


                    // recalc competitive ranking
                    var cnt = 0;
                    foreach (var a in
                        db.Accounts.Where(
                                x =>
                                    x.SpringBattlePlayers.Any(
                                        y => (y.SpringBattle.StartTime > ladderTimeout) && y.SpringBattle.IsMatchMaker && !y.IsSpectator))
                            .OrderByDescending(x => x.EffectiveMmElo)
                            .WithTranslations())
                    {
                        cnt++;
                        a.CompetitiveRank = cnt;
                    }
                    db.SaveChanges();

                    cnt = 0;
                    foreach (var a in
                        db.Accounts.Where(
                                x =>
                                    x.SpringBattlePlayers.Any(
                                        y => (y.SpringBattle.StartTime > ladderTimeout) && !y.SpringBattle.IsMatchMaker && !y.IsSpectator))
                            .OrderByDescending(x => x.EffectiveElo)
                            .WithTranslations())
                    {
                        cnt++;
                        a.CasualRank = cnt;
                    }
                    db.SaveChanges();

                    var topAccounts =
                        db.Accounts.Where(
                                x =>
                                    x.SpringBattlePlayers.Any(
                                        y => (y.SpringBattle.StartTime > ladderTimeout) && y.SpringBattle.IsMatchMaker && !y.IsSpectator))
                            .Include(x => x.Clan)
                            .Include(x => x.Faction)
                            .OrderByDescending(x => x.EffectiveMmElo)
                            .WithTranslations()
                            .Take(GlobalConst.LadderSize)
                            .ToList();

                    var topCasual =
                        db.Accounts.Where(
                                x =>
                                    x.SpringBattlePlayers.Any(
                                        y => (y.SpringBattle.StartTime > ladderTimeout) && !y.SpringBattle.IsMatchMaker && !y.IsSpectator))
                            .Include(x => x.Clan)
                            .Include(x => x.Faction)
                            .OrderByDescending(x => x.EffectiveElo)
                            .WithTranslations()
                            .Take(GlobalConst.LadderSize)
                            .ToList();

                    return new LadderModel { AwardItems = awardItems, TopAccounts = topAccounts, TopCasual = topCasual };
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error computing ladder: {0}", ex);
                    return new LadderModel();
                }
            }
        }
        public class AwardItem
        {
            public string AwardTitle;
            public string AwardType;

            //for this month
            public int TopCollectorCountM;
            public List<Account> TopCollectorsM;
            public SpringBattlePlayer TopScoreBattlePlayerM;
            public string TopScoreDescM;
            public Account TopScoreHolderM;
        }

        public class LadderModel
        {
            public List<AwardItem> AwardItems = new List<AwardItem>();
            public List<Account> TopAccounts = new List<Account>();
            public List<Account> TopCasual = new List<Account>();
        }

    }
}