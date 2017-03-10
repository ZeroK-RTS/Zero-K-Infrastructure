using System;
using System.Collections.Generic;
using ZkData;

namespace Ratings
{
    public interface IRatingSystem
    {

        void ProcessBattle(SpringBattle battle);

        float GetPlayerRating(Account account);

        float GetPlayerRatingUncertainty(Account account);

        List<Account> GetTopPlayers(int count);

        List<float> PredictOutcome(List<ICollection<Account>> teams);
    }
}
