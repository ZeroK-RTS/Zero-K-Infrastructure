using System;
using System.Collections.Generic;
using ZkData;

namespace Ratings
{
    public interface IRatingSystem
    {

        void ProcessBattle(SpringBattle battle);

        PlayerRating GetPlayerRating(int accountID);

        Dictionary<DateTime, float> GetPlayerRatingHistory(int accountID);

        List<Account> GetTopPlayers(int count);

        List<Account> GetTopPlayers(int count, Func<Account, bool> selector);

        List<float> PredictOutcome(List<ICollection<Account>> teams);
        
        void AddTopPlayerUpdateListener(ITopPlayersUpdateListener listener, int topX);
        
        void RemoveTopPlayerUpdateListener(ITopPlayersUpdateListener listener, int topX);
        
        event EventHandler<RatingUpdate> RatingsUpdated;
    }
}
