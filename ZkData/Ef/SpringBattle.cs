using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using PlasmaShared;

namespace ZkData
{
    public class SpringBattle
    {
        public virtual Account Account { get; set; }
        public virtual ICollection<AccountBattleAward> AccountBattleAwards { get; set; }


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
        public int Duration { get; set; }
        [StringLength(64)]
        public string EngineGameID { get; set; }
        [StringLength(100)]
        public string EngineVersion { get; set; }
        public virtual ICollection<Event> Events { get; set; }
        public virtual ForumThread ForumThread { get; set; }
        public int? ForumThreadID { get; set; }
        public string FullTitle
        {
            get { return string.Format("B{0} {1} on {2} ({3})", SpringBattleID, PlayerCount, ResourceByMapResourceID.InternalName, BattleType); }
        }


        [StringLength(250)]
        public string GlacierArchiveID { get; set; }
        public bool HasBots { get; set; }
        public int? HostAccountID { get; set; }
        public bool IsEloProcessed { get; set; }

        public bool IsMatchMaker { get; set; }
        public bool IsMission { get; set; }
        public int? LoserTeamXpChange { get; set; }
        public int MapResourceID { get; set; }

        public AutohostMode Mode { get; set; } = AutohostMode.None;
        public int ModResourceID { get; set; }
        public int PlayerCount { get; set; }
        [StringLength(500)]
        [Index]
        public string ReplayFileName { get; set; }
        public virtual Resource ResourceByMapResourceID { get; set; }
        public virtual Resource ResourceByModResourceID { get; set; }
        public int SpringBattleID { get; set; }
        public virtual ICollection<SpringBattlePlayer> SpringBattlePlayers { get; set; }
        public DateTime StartTime { get; set; }
        [StringLength(200)]
        public string Title { get; set; }
        public int? WinnerTeamXpChange { get; set; }

        public SpringBattle()
        {
            AccountBattleAwards = new HashSet<AccountBattleAward>();
            SpringBattlePlayers = new HashSet<SpringBattlePlayer>();
            Events = new HashSet<Event>();
        }


        public void CalculateAllElo(bool noElo = false)
        {
            if (IsEloProcessed) return;

            if (IsMission || HasBots || (PlayerCount < 2) || noElo || (ResourceByMapResourceID.MapIsSpecial == true) ||
                (ResourceByMapResourceID.MapSupportLevel < MapSupportLevel.Supported))
            {
                WinnerTeamXpChange = GlobalConst.XpForMissionOrBotsVictory;
                LoserTeamXpChange = GlobalConst.XpForMissionOrBots;
            }
            else
            {
                if (IsMatchMaker) CalculateEloGeneric(x => x.EloMm, x => x.EloMmWeight, (x, v) => x.EloMm = v, (x, v) => x.EloMmWeight = v);
                else if (Duration > GlobalConst.MinDurationForElo) CalculateEloGeneric(x => x.Elo, x => x.EloWeight, (x, v) => x.Elo = v, (x, v) => x.EloWeight = v);
            }

            if (Duration > GlobalConst.MinDurationForXP) ApplyXpChanges();

            IsEloProcessed = true;
        }


        public override string ToString()
        {
            return "B" + SpringBattleID;
        }

        private void ApplyXpChanges()
        {
            foreach (var a in SpringBattlePlayers.Where(x => !x.IsSpectator))
                if (a.IsInVictoryTeam)
                {
                    a.Account.Xp += WinnerTeamXpChange ?? 0;
                    a.XpChange = WinnerTeamXpChange;
                }
                else
                {
                    a.Account.Xp += LoserTeamXpChange ?? 0;
                    a.XpChange = LoserTeamXpChange;
                }
        }


        private void CalculateEloGeneric(Func<Account, double> getElo,
            Func<Account, double> getWeight,
            Action<Account, double> setElo,
            Action<Account, double> setWeight)
        {
            double winnerW = 0;
            double loserW = 0;
            double winnerInvW = 0;
            double loserInvW = 0;

            double winnerElo = 0;
            double loserElo = 0;

            var losers = SpringBattlePlayers.Where(x => !x.IsSpectator && !x.IsInVictoryTeam).Select(x => new { Player = x, x.Account }).ToList();
            var winners = SpringBattlePlayers.Where(x => !x.IsSpectator && x.IsInVictoryTeam).Select(x => new { Player = x, x.Account }).ToList();

            if ((losers.Count == 0) || (winners.Count == 0))
            {
                IsEloProcessed = true;
                return;
            }

            foreach (var r in winners)
            {
                winnerW += getWeight(r.Account);
                winnerInvW += GlobalConst.EloWeightMax + 1 - getWeight(r.Account);
                winnerElo += getElo(r.Account);
            }
            foreach (var r in losers)
            {
                loserW += getWeight(r.Account);
                loserInvW += GlobalConst.EloWeightMax + 1 - getWeight(r.Account);
                loserElo += getElo(r.Account);
            }

            winnerElo = winnerElo/winners.Count;
            loserElo = loserElo/losers.Count;
            //winnerElo = winnerElo/winnerW;
            //loserElo = loserElo/loserW;

            var eWin = 1/(1 + Math.Pow(10, (loserElo - winnerElo)/400));
            var eLose = 1/(1 + Math.Pow(10, (winnerElo - loserElo)/400));

            var sumCount = losers.Count + winners.Count;
            var scoreWin = Math.Sqrt(sumCount/2.0)*32*(1 - eWin)/winnerInvW;
            var scoreLose = Math.Sqrt(sumCount/2.0)*32*(0 - eLose)/loserInvW;

            WinnerTeamXpChange = (int)(20 + (300 + 600*(1 - eWin))/(3.0 + winners.Count)); // a bit ugly this sets to battle directly
            LoserTeamXpChange = (int)(20 + (200 + 400*(1 - eLose))/(2.0 + losers.Count));

            var sumW = winnerW + loserW;

            foreach (var r in winners)
            {
                var change = (float)(scoreWin*(GlobalConst.EloWeightMax + 1 - getWeight(r.Account)));
                r.Player.EloChange = change;
                setElo(r.Account, getElo(r.Account) + change);
                setWeight(r.Account, Account.AdjustEloWeight(getWeight(r.Account), sumW, sumCount));
            }

            foreach (var r in losers)
            {
                var change = (float)(scoreLose*(GlobalConst.EloWeightMax + 1 - getWeight(r.Account)));
                r.Player.EloChange = change;
                setElo(r.Account, getElo(r.Account) + change);
                setWeight(r.Account, Account.AdjustEloWeight(getWeight(r.Account), sumW, sumCount));
            }

            IsEloProcessed = true;
        }
    }
}
