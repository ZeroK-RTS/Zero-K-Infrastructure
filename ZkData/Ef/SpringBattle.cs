using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using PlasmaShared;
using Ratings;
using System.Diagnostics;

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

        public RatingCategoryFlags ApplicableRatings { get; set; }

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
        public virtual ICollection<SpringBattleBot> SpringBattleBots { get; set; } = new List<SpringBattleBot>();

        [Index]
        public DateTime StartTime { get; set; }
        [StringLength(200)]
        public string Title { get; set; }
        public int? WinnerTeamXpChange { get; set; }
        public int? Rank { get; set; }

        public SpringBattle()
        {
            AccountBattleAwards = new HashSet<AccountBattleAward>();
            SpringBattlePlayers = new HashSet<SpringBattlePlayer>();
            Events = new HashSet<Event>();
        }

        public bool IsRatedMatch()
        {
            return ApplicableRatings != 0;
        }


        public RatingCategory GetRatingCategory()
        { 
            if (ApplicableRatings.HasFlag(RatingCategoryFlags.MatchMaking)) return RatingCategory.MatchMaking;
            if (ApplicableRatings.HasFlag(RatingCategoryFlags.Planetwars)) return RatingCategory.Planetwars;
            if (ApplicableRatings.HasFlag(RatingCategoryFlags.Casual)) return RatingCategory.Casual;
            Trace.TraceError("Tried to retrieve rating category for battle without rating category: B" + SpringBattleID);
            return RatingCategory.Casual;
        }

        public void ResetApplicableRatings()
        {
            ApplicableRatings = (IsMatchMaker ? RatingCategoryFlags.MatchMaking | RatingCategoryFlags.Casual : 0)
                                | (!(IsMission || HasBots || (PlayerCount < 2) || (ResourceByMapResourceID?.MapIsSpecial == true) || Duration < GlobalConst.MinDurationForElo) ? RatingCategoryFlags.Casual : 0)
                                | (Mode == AutohostMode.Planetwars ? RatingCategoryFlags.Planetwars : 0);
        }

        public void CalculateAllElo(bool noElo = false)
        {
            if (IsEloProcessed) return;

            if (!noElo) ResetApplicableRatings();

            if (IsRatedMatch())
            {
                Rank = SpringBattlePlayers.Where(x => !x.IsSpectator).Select(x => x.Account.Rank).Max();
            }

            if (Duration > GlobalConst.MinDurationForXP)
            {

                if (!IsRatedMatch())
                {
                    WinnerTeamXpChange = GlobalConst.XpForMissionOrBotsVictory;
                    LoserTeamXpChange = GlobalConst.XpForMissionOrBots;
                }
                else
                {
                    var losers = SpringBattlePlayers.Where(x => !x.IsSpectator && !x.IsInVictoryTeam).Select(x => x.Account).ToList();
                    var winners = SpringBattlePlayers.Where(x => !x.IsSpectator && x.IsInVictoryTeam).Select(x => x.Account).ToList();
                    if ((losers.Count > 0) && (winners.Count > 0))
                    {

                        List<float> probabilities = RatingSystems.GetRatingSystem(GetRatingCategory()).PredictOutcome(new List<ICollection<Account>> { winners, losers }, StartTime);
                        var eWin = probabilities[0];
                        var eLose = probabilities[1];

                        WinnerTeamXpChange = (int)(20 + (300 + 600 * (1 - eWin)) / (3.0 + winners.Count)); // a bit ugly this sets to battle directly
                        LoserTeamXpChange = (int)(20 + (200 + 400 * (1 - eLose)) / (2.0 + losers.Count));
                    }
                }

                ApplyXpChanges();
            }

            IsEloProcessed = true;
        }

        public List<float> GetAllyteamWinChances()
        {
            if (!IsRatedMatch()) return new List<float>();
            return RatingSystems.GetRatingSystem(GetRatingCategory()).PredictOutcome(SpringBattlePlayers.Where(x => !x.IsSpectator).OrderBy(x => x.AllyNumber).GroupBy(x => x.AllyNumber).Select(x => x.Select(y => y.Account).ToList()).ToList(), StartTime);
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
    }
}
