using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
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
            timer = new Timer((t) => { ladderModel = ComputeLadder(); }, this, 5000, LadderRefreshMinutes * 60 * 1000);
        }

        public LadderModel GetLadder()
        {
            return ladderModel;
        }

        public List<Account> GetTop50() => ladderModel?.Top50Accounts;
        public List<Account> GetTop50Casual() => ladderModel?.Top50Casual;


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

        private static LadderModel ComputeLadder()
        {
            try
            {
                var db = new ZkDataContext();
                db.Database.CommandTimeout = 600;

                var awardItems = CalculateAwards(db);

                var ladderTimeout = DateTime.UtcNow.AddDays(-GlobalConst.LadderActivityDays);

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

                var top50Accounts =
                    db.Accounts.Where(
                            x =>
                                x.SpringBattlePlayers.Any(
                                    y => (y.SpringBattle.StartTime > ladderTimeout) && y.SpringBattle.IsMatchMaker && !y.IsSpectator))
                        .Include(x => x.Clan)
                        .Include(x => x.Faction)
                        .OrderByDescending(x => x.EffectiveMmElo)
                        .WithTranslations()
                        .Take(50)
                        .ToList();

                var top50Casual =
                    db.Accounts.Where(
                            x =>
                                x.SpringBattlePlayers.Any(
                                    y => (y.SpringBattle.StartTime > ladderTimeout) && !y.SpringBattle.IsMatchMaker && !y.IsSpectator))
                        .Include(x => x.Clan)
                        .Include(x => x.Faction)
                        .OrderByDescending(x => x.EffectiveElo)
                        .WithTranslations()
                        .Take(50)
                        .ToList();

                return new LadderModel { AwardItems = awardItems, Top50Accounts = top50Accounts, Top50Casual = top50Casual };
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error computing ladder: {0}",ex);
                return new LadderModel();
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
            public List<AwardItem> AwardItems;
            public List<Account> Top50Accounts;
            public List<Account> Top50Casual;
        }

    }
}