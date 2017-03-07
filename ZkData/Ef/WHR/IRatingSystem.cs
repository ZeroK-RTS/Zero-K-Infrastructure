using System.Collections.Generic;
using ZkData;

namespace Ratings
{
    public interface IRatingSystem
    {

        void ProcessBattle(SpringBattle battle);

        double GetPlayerRating(Account account);

        double GetPlayerRatingUncertainty(Account account);

        List<double> PredictOutcome(List<List<Account>> teams);
    }
}
