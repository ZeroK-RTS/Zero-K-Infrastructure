using System;
using System.Collections.Concurrent;
using System.Data.Entity;
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
        ConcurrentDictionary<int, ForumList> cachedAccountForumLists = new ConcurrentDictionary<int, ForumList>();

        public ForumListManager(ZkLobbyServer zkLobbyServer)
        {
            server = zkLobbyServer;
            CachePublicForumList();

            ZkDataContext.AfterEntityChange += ZkDataContextOnAfterEntityChange;
        }

        private void ZkDataContextOnAfterEntityChange(object sender, ZkDataContext.EntityEntry entityEntry)
        {
            int? changedThreadID = null;
            int? changedAccountID = null;

            if (entityEntry.State == EntityState.Deleted || entityEntry.State == EntityState.Unchanged) return;

            var entity = entityEntry.Entity;
            if (entity is ForumThread)
            {
                changedThreadID = ((ForumThread)entity).ForumThreadID;

                CachePublicForumList();

                using (var db = new ZkDataContext())
                {
                    var t = db.ForumThreads.Find(changedThreadID);
                    var item = new ForumItem()
                    {
                        Time = t.LastPost ?? t.Created,
                        Url = $"{GlobalConst.BaseSiteUrl}/Forum/Thread/{t.ForumThreadID}",
                        Header = t.Title,
                        IsRead = false
                    };

                    foreach (var user in server.ConnectedUsers.Values.Where(x => x != null && x.IsLoggedIn))
                    {
                        var list = GetCurrentForumList(user.User.AccountID);
                        var existing = list.ForumItems.FirstOrDefault(x => x.Url == item.Url);
                        if (existing != null)
                        {
                            existing.Header = item.Header;
                            existing.IsRead = item.IsRead;
                            existing.Time = item.Time;
                        }
                        else
                        {
                            list.ForumItems.Insert(0, item);
                        }

                        list.ForumItems = list.ForumItems.OrderByDescending(x => x.Time).ToList();

                        user.SendCommand(list);
                    }
                }
            }
            else if (entity is ForumThreadLastRead)
            {
                changedAccountID = ((ForumThreadLastRead)entity).AccountID;

                var list = CachePrivateForumList(changedAccountID.Value);
                var conus = server.ConnectedUsers.Values.FirstOrDefault(x => x != null && x.User.AccountID == changedAccountID);
                if (conus != null) conus.SendCommand(list);
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


        private ForumList CachePrivateForumList(int accountID)
        {
            using (var db = new ZkDataContext())
            {
                var account = db.Accounts.Find(accountID);
                
                var accessibleThreads = db.ForumThreads.Where(x => x.RestrictedClanID == null || x.RestrictedClanID == account.ClanID);
                accessibleThreads = accessibleThreads.Where(x => x.ForumCategory.ForumMode != ForumMode.Archive);

                var threads = (from t in accessibleThreads
                    let read = t.ForumThreadLastReads.FirstOrDefault(x => x.AccountID == account.AccountID)
                    orderby t.LastPost descending
                    select new { Thread = t, Read = read != null && read.LastRead >= t.LastPost }).Take(10).ToList();
                    
                
                var list = new ForumList()
                {
                    ForumItems = threads.Select(x =>
                        new ForumItem()
                        {
                            Time = x.Thread.LastPost ?? x.Thread.Created,
                            Url = $"{GlobalConst.BaseSiteUrl}/Forum/Thread/{x.Thread.ForumThreadID}",
                            Header = x.Thread.Title,
                            IsRead = x.Read
                        }).ToList()
                };

                cachedAccountForumLists[account.AccountID] = list;

                return list;
            }
        }

        public ForumList GetCurrentForumList(int? accountID)
        {
            if (accountID == null) return cachedPublicForumList;
            else
            {
                ForumList list;
                if (cachedAccountForumLists.TryGetValue(accountID.Value, out list)) return list;
                else
                {
                    return CachePrivateForumList(accountID.Value);  
                }
            }
            
        }


        public void OnUserDisconnected(int accountID)
        {
            ForumList removed;
            cachedAccountForumLists.TryRemove(accountID, out removed);
        }

    }
}