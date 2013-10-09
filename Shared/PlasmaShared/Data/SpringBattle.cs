using System;
using System.Linq;

namespace ZkData
{
	partial class SpringBattle
	{
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
	    
        
        public override string ToString() {
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
						a.Account.XP += WinnerTeamXpChange.Value;
						a.XpChange = WinnerTeamXpChange.Value;
					}
					else
					{
						a.Account.XP += LoserTeamXpChange.Value;
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

			var eWin = 1/(1 + Math.Pow(10, (loserElo - winnerElo)/400));
			var eLose = 1/(1 + Math.Pow(10, (winnerElo - loserElo)/400));

			var sumCount = losers.Count + winners.Count;
			var scoreWin = Math.Sqrt(sumCount/2.0)*32*(1 - eWin)/winnerInvW;
			var scoreLose = Math.Sqrt(sumCount/2.0)*32*(0 - eLose)/loserInvW;

			var sumW = winnerW + loserW;

            if (Duration >= GlobalConst.MinDurationForXP) {
                WinnerTeamXpChange = (int)(20 + (300 + 600*(1 - eWin))/(3.0 + winners.Count));
                LoserTeamXpChange = (int)(20 + (200 + 400*(1 - eLose))/(2.0 + losers.Count));
            } else {
                WinnerTeamXpChange = 0;
                LoserTeamXpChange = 0;
            }

            if (noElo ||ResourceByMapResourceID.MapIsSpecial == true)   // silly map, just process XP
            {
                foreach (var r in winners)
                {
                    r.Account.XP += WinnerTeamXpChange.Value;
                    r.Player.XpChange = WinnerTeamXpChange;
                }
                foreach (var r in losers)
                {
                    r.Account.XP += LoserTeamXpChange.Value;
                    r.Player.XpChange = LoserTeamXpChange.Value;
                }
                IsEloProcessed = true;
                return;
            }

			foreach (var r in winners)
			{
				var change = (float)(scoreWin*r.Account.EloInvWeight);
				r.Player.EloChange = change;
                if (planetwars)	r.Account.EloPw += change;
                else r.Account.Elo += change;

				r.Account.XP += WinnerTeamXpChange.Value;
				r.Player.XpChange = WinnerTeamXpChange;

                r.Account.EloWeight = Account.AdjustEloWeight(r.Account.EloWeight, sumW, sumCount);
			}

			foreach (var r in losers)
			{
				var change = (float)(scoreLose*r.Account.EloInvWeight);
				r.Player.EloChange = change;
                if (planetwars) r.Account.EloPw += change;
                else r.Account.Elo += change;

				r.Account.XP += LoserTeamXpChange.Value;
				r.Player.XpChange = LoserTeamXpChange.Value;


			    r.Account.EloWeight = Account.AdjustEloWeight(r.Account.EloWeight, sumW, sumCount);
			}

	        Calculate1v1Elo();

			IsEloProcessed = true;
		}

        public void Calculate1v1Elo() {
            if (!HasBots) {
                var losers = SpringBattlePlayers.Where(x => !x.IsSpectator && !x.IsInVictoryTeam).Select(x => x.Account).ToList();
                var winners = SpringBattlePlayers.Where(x => !x.IsSpectator && x.IsInVictoryTeam).Select(x => x.Account).ToList();
                if (losers.Count == 1 && winners.Count == 1) {
                    var winner = winners.First();
                    var loser = losers.First();

                    var winnerElo = winner.Elo1v1;
                    var loserElo = loser.Elo1v1;

                    var eWin = 1/(1 + Math.Pow(10, (loserElo - winnerElo)/400));
                    var eLose = 1/(1 + Math.Pow(10, (winnerElo - loserElo)/400));

                    var scoreWin = 32*(1 - eWin);
                    var scoreLose = 32*(0 - eLose);

                    winner.Elo1v1 += scoreWin;
                    loser.Elo1v1 += scoreLose;

                    var sumW = winner.Elo1v1Weight + loser.Elo1v1Weight;
                    winner.Elo1v1Weight  = Account.AdjustEloWeight(winner.Elo1v1Weight, sumW, 2);
                    loser.Elo1v1Weight = Account.AdjustEloWeight(loser.Elo1v1Weight, sumW, 2);
                }
            }
        }

	}
}