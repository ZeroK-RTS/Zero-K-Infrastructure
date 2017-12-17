using System;
using System.Linq;
using LobbyClient;
using PlasmaShared;
using ZkData;

namespace ZkLobbyServer {
    public class NewsListManager
    {
        private ZkLobbyServer server;
        private NewsList cachedNewsList;

        public NewsListManager(ZkLobbyServer zkLobbyServer)
        {
            server = zkLobbyServer;
            CacheNewsList();
        }

        private void CacheNewsList()
        {
            using (var db = new ZkDataContext())
            {
                cachedNewsList = new NewsList()
                {
                    NewsItems = db.LobbyNews.OrderByDescending(x => x.Created).Take(10).ToList().Select(x => new NewsItem
                    {
                        Time = x.EventTime,
                        Header = x.Title,
                        Text = x.Text,
                        Image = $"{GlobalConst.BaseSiteUrl}{x.ImageRelativeUrl}",
                        Url = x.Url
                    }).ToList()
                };
            }
        }

        public NewsList GetCurrentNewsList() => cachedNewsList;

        public void OnNewsChanged()
        {
            CacheNewsList();
            server.Broadcast(cachedNewsList);
        }
    }
}