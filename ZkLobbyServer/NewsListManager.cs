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
            ZkDataContext.AfterEntityChange += DbEntityChanged;
            server = zkLobbyServer;
        }

        private void CacheNewsList()
        {
            using (var db = new ZkDataContext())
            {
                cachedNewsList = new NewsList()
                {
                    NewsItems = db.News.OrderByDescending(x => x.Created).Take(10).Select(x => new NewsItem
                    {
                        Time = x.Created,
                        Header = x.Title,
                        Text = x.Text,
                        Image = x.ImageRelativeUrl,
                        Url = $"{GlobalConst.BaseSiteUrl}/Forum/Thread/{x.ForumThreadID}" // not very nice hardcode..
                    }).ToList()
                };
            }
        }

        private async void DbEntityChanged(object sender, ZkDataContext.EntityEntry e)
        {
            if (sender is News)
            {
                CacheNewsList();
                await server.Broadcast(cachedNewsList);
            }
        }

        public NewsList GetCurrentNewsList() => cachedNewsList;

    }
}