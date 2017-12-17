using System;
using System.Collections.Generic;
using System.Linq;
using LobbyClient;
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
            TopPlayersUpdated(new List<Account>()); //make sure ladderlist is never null
            Ratings.RatingSystems.GetRatingSystem(RatingCategory.MatchMaking).AddTopPlayerUpdateListener(this, LadderListLength);
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