using System;
using System.Collections.Generic;
using System.Linq;
using LobbyClient;
using PlasmaShared;
using Ratings;
using ZkData;

namespace ZkLobbyServer {
    public class LadderListManager : Ratings.ITopPlayersUpdateListener
    {
        const int LadderListLength = 10;

        private ZkLobbyServer server;
        private LadderList cachedLadderList;

        public LadderListManager(ZkLobbyServer zkLobbyServer)
        {
            server = zkLobbyServer;
            RatingSystems.GetRatingSystem(RatingCategory.MatchMaking).AddTopPlayerUpdateListener(this, LadderListLength);
            TopPlayersUpdated(RatingSystems.GetRatingSystem(DynamicConfig.Instance.LadderSeasonOngoing ? RatingCategory.Ladder : RatingCaregory.MatchMaker).GetTopPlayers(LadderListLength)); //make sure ladderlist is never null
        }

        public LadderList GetCurrentLadderList() => cachedLadderList;

        public void TopPlayersUpdated(IEnumerable<Account> players)
        {
            cachedLadderList = new LadderList()
            {
                LadderItems = players.Select(x => x.ToLadderItem()).ToList()
            };
            server.Broadcast(cachedLadderList);
        }
    }
}
