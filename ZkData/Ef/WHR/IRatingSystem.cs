using System;
using System.Collections.Generic;
using ZkData;

namespace Ratings
{
    public interface IRatingSystem
    {

        void ProcessBattle(SpringBattle battle);

        PlayerRating GetPlayerRating(Account account);

        List<Account> GetTopPlayers(int count);

        List<Account> GetTopPlayers(int count, Func<Account, bool> selector);

        List<float> PredictOutcome(List<ICollection<Account>> teams);
    }
}
