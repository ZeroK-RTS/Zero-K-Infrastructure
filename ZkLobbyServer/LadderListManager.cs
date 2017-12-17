using System.Linq;
using LobbyClient;
using Ratings;

namespace ZkLobbyServer {
    public class LadderListManager
    {
        private ZkLobbyServer server;
        private LadderList cachedLadderList;

        public LadderListManager(ZkLobbyServer zkLobbyServer)
        {
            server = zkLobbyServer;
            CacheLadderList();
        }

        private void CacheLadderList()
        {
            cachedLadderList = new LadderList()
            {
                LadderItems = RatingSystems.GetRatingSystem(RatingCategory.MatchMaking).GetTopPlayers(10).ToList().Select(x => x.ToLadderItem())
                    .ToList()
            };
        }

        public LadderList GetCurrentLadderList() => cachedLadderList;

        public void OnLadderChange()
        {
            CacheLadderList();
            server.Broadcast(cachedLadderList);
        }
    }
}