using System;

namespace ZkLobbyServer
{
    /// <summary>
    /// The two rating shapes the website needs that the database does not hold.
    ///
    /// Most of what the site asks about ratings is answered from the AccountRatings table - see
    /// RatingSystems.CreateRatingSystems. These two are not in any table: they are the Whole
    /// History Rating pass's own working state, which only the process running that pass has.
    /// </summary>
    public class InternalRatingInfo
    {
        public float Elo;
        public float EloStdev;
    }

    /// <summary>
    /// One map's place in the ranking. Carries the resource ID rather than the Resource, because
    /// the row it names is in a database the website already has - and because an EF entity is
    /// the one thing this interface refuses to pass.
    /// </summary>
    public class MapRatingInfo
    {
        public int ResourceID;
        public float Elo;
        public float EloStdev;
        public float Percentile;
        public int Rank;
    }
}
