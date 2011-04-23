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


		public void CalculateElo(bool planetWars =false)
		{
			if (IsEloProcessed || Duration < 120)
			{
				IsEloProcessed = true;
				return;
			}

			if (IsMission || HasBots || PlayerCount < 2)
			{
				WinnerTeamXpChange = FactorTime(GlobalConst.XpForMissionOrBotsVictory, Duration);
				LoserTeamXpChange = FactorTime(GlobalConst.XpForMissionOrBots, Duration);

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

			double winnerElo = 1;
			double loserElo = 1;

			var losers = SpringBattlePlayers.Where(x => !x.IsSpectator && !x.IsInVictoryTeam).Select(x => new { Player = x, x.Account }).ToList();
			var winners = SpringBattlePlayers.Where(x => !x.IsSpectator && x.IsInVictoryTeam).Select(x => new { Player = x, x.Account }).ToList();

			if (losers.Count == 0 || winners.Count == 0)
			{
				IsEloProcessed = true;
				return;
			}

			foreach (var r in winners)
			{
				var elo = r.Account.Elo;
				var w = r.Account.EloWeight;
				winnerW += w;
				winnerInvW += r.Account.EloInvWeight;
				winnerElo += elo*w;
			}
			foreach (var r in losers)
			{
				var elo = r.Account.Elo;
				var w = r.Account.EloWeight;
				loserW += w;
				loserInvW += r.Account.EloInvWeight;
				loserElo += elo*w;
			}

			winnerElo = winnerElo/winnerW;
			loserElo = loserElo/loserW;

			var eWin = 1/(1 + Math.Pow(10, (loserElo - winnerElo)/400));
			var eLose = 1/(1 + Math.Pow(10, (winnerElo - loserElo)/400));

			var sumCount = losers.Count + winners.Count;
			var scoreWin = Math.Sqrt(sumCount/2.0)*32*(1 - eWin)/winnerInvW;
			var scoreLose = Math.Sqrt(sumCount/2.0)*32*(0 - eLose)/loserInvW;

			var sumW = winnerW + loserW;

			WinnerTeamXpChange = (int)(20 + (300 + 600*(1 - eWin))/(2.0 + winners.Count));
			WinnerTeamXpChange = FactorTime(WinnerTeamXpChange, Duration);

			foreach (var r in winners)
			{
				var change = (float)(scoreWin*r.Account.EloInvWeight);
				r.Player.EloChange = change;
				r.Account.Elo += change;

				r.Account.XP += WinnerTeamXpChange.Value;
				r.Player.XpChange = WinnerTeamXpChange;

				if (planetWars)
				{
					r.Player.Influence = WinnerTeamXpChange;
				}

				if (r.Account.EloWeight < GlobalConst.EloWeightMax)
				{
					r.Account.EloWeight = (float)(r.Account.EloWeight + ((sumW - r.Account.EloWeight)/(sumCount - 1))/GlobalConst.EloWeightLearnFactor);
					if (r.Account.EloWeight > GlobalConst.EloWeightMax) r.Account.EloWeight = (float)GlobalConst.EloWeightMax;
				}
			}

			LoserTeamXpChange = (int)(20 + (200 + 400*(1 - eLose))/(2.0 + losers.Count));
			LoserTeamXpChange = FactorTime(LoserTeamXpChange, Duration);

			foreach (var r in losers)
			{
				var change = (float)(scoreLose*r.Account.EloInvWeight);
				r.Player.EloChange = change;
				r.Account.Elo += change;

				r.Account.XP += LoserTeamXpChange.Value;
				r.Player.XpChange = LoserTeamXpChange.Value;

				
				if (r.Account.EloWeight < GlobalConst.EloWeightMax)
				{
					r.Account.EloWeight = (float)(r.Account.EloWeight + ((sumW - r.Account.EloWeight)/(sumCount - 1))/GlobalConst.EloWeightLearnFactor);
					if (r.Account.EloWeight > GlobalConst.EloWeightMax) r.Account.EloWeight = (float)GlobalConst.EloWeightMax;
				}
			}

			IsEloProcessed = true;
		}

		static int FactorTime(int? xp, int duration)
		{
			xp = xp ?? 0;
			if (duration < 480) return 0;
			else if (duration < 720) return (int)(xp*(720 - duration)/240.0);
			else return xp.Value;
		}
	}
}