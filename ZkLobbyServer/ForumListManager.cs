using System;
using System.Linq;
using LobbyClient;
using PlasmaShared;
using Ratings;
using ZkData;

namespace ZkLobbyServer {
    public class ForumListManager
    {
        private ZkLobbyServer server;

        private ForumList cachedPublicForumList;

        public ForumListManager(ZkLobbyServer zkLobbyServer)
        {
            server = zkLobbyServer;
            CachePublicForumList();

            ZkDataContext.AfterEntityChange += ZkDataContextOnAfterEntityChange;
        }

        private void ZkDataContextOnAfterEntityChange(object sender, ZkDataContext.EntityEntry entityEntry)
        {
            ForumThread changedThread = null;
            if (sender is ForumThread thread)
            {
                changedThread = thread;
            } else if (sender is ForumPost post)
            {
                changedThread = post.ForumThread;
            }

            if (changedThread != null)
            {
                CachePublicForumList();
            }
        }

        private void CachePublicForumList()
        {
            using (var db = new ZkDataContext())
            {

                var accessibleThreads = db.ForumThreads.Where(x => x.RestrictedClanID == null && x.ForumCategory.ForumMode != ForumMode.Archive);

                cachedPublicForumList = new ForumList()
                {
                    ForumItems = accessibleThreads.OrderByDescending(x => x.LastPost).Take(10).ToList().Select(x =>
                        new ForumItem()
                        {
                            Time = x.LastPost ?? x.Created,
                            Url = $"{GlobalConst.BaseSiteUrl}/Forum/Thread/{x.ForumThreadID}",
                            Header = x.Title,
                            IsRead = false
                        }).ToList()
                };
            }

        }

        public ForumList GetCurrentForumList(int? accountID)
        {
            return cachedPublicForumList;
            if (accountID == null) return cachedPublicForumList;
            return null;
        }

    }
}