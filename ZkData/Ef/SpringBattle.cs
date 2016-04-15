using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ZkData
{
    public class SpringBattle
    {

        public SpringBattle()
        {
            AccountBattleAwards = new HashSet<AccountBattleAward>();
            SpringBattlePlayers = new HashSet<SpringBattlePlayer>();
            Events = new HashSet<Event>();
        }
        public int SpringBattleID { get; set; }
        [StringLength(64)]
        public string EngineGameID { get; set; }
        public int HostAccountID { get; set; }
        [StringLength(200)]
        public string Title { get; set; }
        public int MapResourceID { get; set; }
        public int ModResourceID { get; set; }
        public DateTime StartTime { get; set; }
        public int Duration { get; set; }
        public int PlayerCount { get; set; }
        public bool HasBots { get; set; }
        public bool IsMission { get; set; }
        [StringLength(500)]
        public string ReplayFileName { get; set; }
        [StringLength(100)]
        public string EngineVersion { get; set; }
        public bool IsEloProcessed { get; set; }
        public int? WinnerTeamXpChange { get; set; }
        public int? LoserTeamXpChange { get; set; }
        public int? ForumThreadID { get; set; }
        [StringLength(250)]
        public string TeamsTitle { get; set; }
        public bool IsFfa { get; set; }
        public int? RatingPollID { get; set; }

        public virtual Account Account { get; set; }
        public virtual ICollection<AccountBattleAward> AccountBattleAwards { get; set; }
        public virtual ForumThread ForumThread { get; set; }
        public virtual Resource ResourceByModResourceID { get; set; }
        public virtual Resource ResourceByMapResourceID { get; set; }
        public virtual ICollection<SpringBattlePlayer> SpringBattlePlayers { get; set; }
        public virtual ICollection<Event> Events { get; set; }


        public string BattleType
        {
            get
            {
                var type = "Multiplayer";
                if (PlayerCount <= 1) type = "Singleplayer";
                if (HasBots) type = "Bots";
                if (IsMission) type = "Mission";
                return type;
            }
        }
        public string FullTitle { get { return string.Format("B{0} {1} on {2} ({3})", SpringBattleID, PlayerCount, ResourceByMapResourceID.InternalName, BattleType); } }


        public override string ToString()
        {
            return "B" + SpringBattleID;
        }

        public void CalculateAllElo(bool noElo = false, bool planetwars = false)
        {
            if (IsEloProcessed || Duration < GlobalConst.MinDurationForElo)
            {
                IsEloProcessed = true;
                return;
            }

            if (IsMission || HasBots || PlayerCount < 2)
            {
                WinnerTeamXpChange = GlobalConst.XpForMissionOrBotsVictory;
                LoserTeamXpChange = GlobalConst.XpForMissionOrBots;
                if (Duration < GlobalConst.MinDurationForXP)
                {
                    WinnerTeamXpChange = 0;
                    LoserTeamXpChange = 0;
                }

                foreach (var a in SpringBattlePlayers.Where(x => !x.IsSpectator))
                {
                    if (a.IsInVictoryTeam)
                    {
                        a.Account.Xp += WinnerTeamXpChange.Value;
                        a.XpChange = WinnerTeamXpChange.Value;
                    }
                    else
                    {
                        a.Account.Xp += LoserTeamXpChange.Value;
                        a.XpChange = LoserTeamXpChange.Value;
                    }
                }

                IsEloProcessed = true;
                return;
            }

            double winnerW = 0;
            double loserW = 0;
            double winnerInvW = 0;
            double loserInvW = 0;

            double winnerElo = 0;
            double loserElo = 0;

            var losers = SpringBattlePlayers.Where(x => !x.IsSpectator && !x.IsInVictoryTeam).Select(x => new { Player = x, x.Account }).ToList();
            var winners = SpringBattlePlayers.Where(x => !x.IsSpectator && x.IsInVictoryTeam).Select(x => new { Player = x, x.Account }).ToList();

            if (losers.Count == 0 || winners.Count == 0)
            {
                IsEloProcessed = true;
                return;
            }

            foreach (var r in winners)
            {
                winnerW += r.Account.EloWeight;
                winnerInvW += r.Account.EloInvWeight;
                winnerElo += r.Account.EffectiveElo;
            }
            foreach (var r in losers)
            {
                loserW += r.Account.EloWeight;
                loserInvW += r.Account.EloInvWeight;
                loserElo += r.Account.EffectiveElo;
            }

            winnerElo = winnerElo / winners.Count;
            loserElo = loserElo / losers.Count;
            //winnerElo = winnerElo/winnerW;
            //loserElo = loserElo/loserW;

            var eWin = 1 / (1 + Math.Pow(10, (loserElo - winnerElo) / 400));
            var eLose = 1 / (1 + Math.Pow(10, (winnerElo - loserElo) / 400));

            var sumCount = losers.Count + winners.Count;
            var scoreWin = Math.Sqrt(sumCount / 2.0) * 32 * (1 - eWin) / winnerInvW;
            var scoreLose = Math.Sqrt(sumCount / 2.0) * 32 * (0 - eLose) / loserInvW;

            var sumW = winnerW + loserW;

            if (Duration >= GlobalConst.MinDurationForXP)
            {
                WinnerTeamXpChange = (int)(20 + (300 + 600 * (1 - eWin)) / (3.0 + winners.Count));
                LoserTeamXpChange = (int)(20 + (200 + 400 * (1 - eLose)) / (2.0 + losers.Count));
            }
            else
            {
                WinnerTeamXpChange = 0;
                LoserTeamXpChange = 0;
            }

            if (noElo || (ResourceByMapResourceID.MapIsSpecial == true) || (ResourceByMapResourceID.MapIsSupported != true))   // silly/unsupported map, just process XP
            {
                foreach (var r in winners)
                {
                    r.Account.Xp += WinnerTeamXpChange.Value;
                    r.Player.XpChange = WinnerTeamXpChange;
                }
                foreach (var r in losers)
                {
                    r.Account.Xp += LoserTeamXpChange.Value;
                    r.Player.XpChange = LoserTeamXpChange.Value;
                }
                IsEloProcessed = true;

                return;
            }

            if (losers.Count == 1 && winners.Count == 1) Calculate1v1Elo();
            else
            {
                foreach (var r in winners)
                {
                    var change = (float)(scoreWin * r.Account.EloInvWeight);
                    r.Player.EloChange = change;
                    if (planetwars) r.Account.EloPw += change;
                    else r.Account.Elo += change;

                    r.Account.Xp += WinnerTeamXpChange.Value;
                    r.Player.XpChange = WinnerTeamXpChange;

                    r.Account.EloWeight = Account.AdjustEloWeight(r.Account.EloWeight, sumW, sumCount);
                    r.Account.Elo1v1Weight = Account.AdjustEloWeight(r.Account.Elo1v1Weight, sumW, sumCount);
                }

                foreach (var r in losers)
                {
                    var change = (float)(scoreLose * r.Account.EloInvWeight);
                    r.Player.EloChange = change;
                    if (planetwars) r.Account.EloPw += change;
                    else r.Account.Elo += change;

                    r.Account.Xp += LoserTeamXpChange.Value;
                    r.Player.XpChange = LoserTeamXpChange.Value;

                    r.Account.EloWeight = Account.AdjustEloWeight(r.Account.EloWeight, sumW, sumCount);
                    r.Account.Elo1v1Weight = Account.AdjustEloWeight(r.Account.Elo1v1Weight, sumW, sumCount);
                }
            }


            IsEloProcessed = true;
        }

        public void Calculate1v1Elo()
        {
            if (!HasBots)
            {
                var losers = SpringBattlePlayers.Where(x => !x.IsSpectator && !x.IsInVictoryTeam).ToList();
                var winners = SpringBattlePlayers.Where(x => !x.IsSpectator && x.IsInVictoryTeam).ToList();
                if (losers.Count == 1 && winners.Count == 1)
                {
                    SpringBattlePlayer winner = winners.First();
                    SpringBattlePlayer loser = losers.First();
                    Account winnerAcc = winner.Account;
                    Account loserAcc = loser.Account;

                    var winnerElo = winnerAcc.Effective1v1Elo;
                    var loserElo = loserAcc.Effective1v1Elo;

                    var eWin = 1 / (1 + Math.Pow(10, (loserElo - winnerElo) / 400));
                    var eLose = 1 / (1 + Math.Pow(10, (winnerElo - loserElo) / 400));

                    var scoreWin = 32 * (1 - eWin);
                    var scoreLose = 32 * (0 - eLose);

                    winnerAcc.Elo1v1 += scoreWin;
                    loserAcc.Elo1v1 += scoreLose;
                    winner.EloChange = (float)scoreWin;
                    loser.EloChange = (float)scoreLose;

                    WinnerTeamXpChange = (int)(20 + (300 + 600 * (1 - eWin)) / 4.0);
                    LoserTeamXpChange = (int)(20 + (200 + 400 * (1 - eLose)) / 3.0);

                    winnerAcc.Xp += WinnerTeamXpChange.Value;
                    loserAcc.Xp += LoserTeamXpChange.Value;

                    var sumW = winnerAcc.Elo1v1Weight + loserAcc.Elo1v1Weight;
                    winnerAcc.Elo1v1Weight = Account.AdjustEloWeight(winnerAcc.Elo1v1Weight, sumW, 2);
                    winnerAcc.EloWeight = Account.AdjustEloWeight(winnerAcc.EloWeight, sumW, 2);
                    loserAcc.Elo1v1Weight = Account.AdjustEloWeight(loserAcc.Elo1v1Weight, sumW, 2);
                    loserAcc.EloWeight = Account.AdjustEloWeight(loserAcc.EloWeight, sumW, 2);
                }
            }
        }

    }
}
