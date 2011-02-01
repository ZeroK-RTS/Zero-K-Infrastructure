using System;
using System.Linq;

namespace ZkData
{
  partial class SpringBattle
  {
    public void CalculateElo()
    {
      if (IsEloProcessed || IsMission || HasBots || PlayerCount < 2 || !SpringBattlePlayers.Any(x => !x.IsSpectator && x.IsInVictoryTeam) ||
          !SpringBattlePlayers.Any(x => !x.IsSpectator && x.IsInVictoryTeam)  || Duration < 120)
      {
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

      foreach (var r in winners)
      {
        r.Account.Elo += (float)(scoreWin*r.Account.EloInvWeight);

        r.Account.XP += (int)(20 +9* scoreWin*winnerInvW /(2.0+winners.Count));
        
        if (r.Account.EloWeight < GlobalConst.EloWeightMax)
        {
          r.Account.EloWeight = (float)(r.Account.EloWeight + ((sumW - r.Account.EloWeight)/(sumCount - 1))/GlobalConst.EloWeightLearnFactor);
          if (r.Account.EloWeight > GlobalConst.EloWeightMax) r.Account.EloWeight = (float)GlobalConst.EloWeightMax;
        }
      }

      foreach (var r in losers)
      {
        r.Account.Elo += (float)(scoreLose*r.Account.EloInvWeight);

        r.Account.XP += (int)(10 + (32+scoreLose * loserInvW) * 3.0 / (2.0 + losers.Count));

        
        if (r.Account.EloWeight < GlobalConst.EloWeightMax)
        {
          r.Account.EloWeight = (float)(r.Account.EloWeight + ((sumW - r.Account.EloWeight)/(sumCount - 1))/GlobalConst.EloWeightLearnFactor);
          if (r.Account.EloWeight > GlobalConst.EloWeightMax) r.Account.EloWeight = (float)GlobalConst.EloWeightMax;
        }
      }

      // check for level ups
      foreach (var a in losers.Union(winners).Select(x=>x.Account))
      {
        if (a.XP > Account.GetXpForLevel(a.Level + 1))
        {
          a.Level++;
          AuthServiceClient.SendLobbyMessage(a,
                                             string.Format("Congratulations! You just leveled up to level {0}. spring://http://zero-k.info/Users.mvc/{1}",
                                                           a.Level,
                                                           a.Name));
        }
      }


      IsEloProcessed = true;
    }

  }
}