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

        List<float> PredictOutcome(IEnumerable<IEnumerable<Account>> teams);
        
        void AddTopPlayerUpdateListener(ITopPlayersUpdateListener listener, int topX);
        
        void RemoveTopPlayerUpdateListener(ITopPlayersUpdateListener listener, int topX);

        RankBracket GetPercentileBracket(int rank);

        RatingCategory GetRatingCategory();

        int GetActivePlayers();

        event EventHandler<RatingUpdate> RatingsUpdated;
    }
}
