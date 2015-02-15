using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using LobbyClient;
using ZkData;

namespace ZeroKLobby
{
    class ServerImagesHandler
    {
        readonly string basePath;
        readonly Dictionary<string, Item> items = new Dictionary<string, Item>();

        readonly object locker = new object();

        public ServerImagesHandler(SpringPaths springPaths, TasClient tas) {
            basePath = Utils.MakePath(springPaths.WritableDirectory, "LuaUI", "Configs");
            tas.BattleUserJoined += (sender, args) =>
                {
                    // preload avatar images on user join battle so that they are available ingame
                    User us;
                    if (tas.ExistingUsers.TryGetValue(args.UserName, out us)) {
                        GetAvatarImage(us);
                        GetClanOrFactionImage(us);
                    }
                };
        }

        public Image GetImage(string urlPart) {
            Item item = GetImageItem(urlPart);
            if (item != null) return item.Image;
            else return null;
        }


        public Image GetAvatarImage(User user) {
            if (!string.IsNullOrEmpty(user.Avatar)) return GetImage(String.Format("Avatars/{0}.png", user.Avatar));
            else return null;
        }


        public Item GetImageItem(string urlPart) {
            lock (locker) {
                Item item;
                items.TryGetValue(urlPart, out item);
                if (item == null || item.IsError) {
                    item = new Item { Name = urlPart };
                    items[urlPart] = item;
                    item.LocalPath = Utils.MakePath(basePath, urlPart);
                    string dir = Path.GetDirectoryName(item.LocalPath);

                    try {
                        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                        if (File.Exists(item.LocalPath)) {
                            item.Image = Image.FromStream(new MemoryStream(File.ReadAllBytes(item.LocalPath)));
                            item.IsLoaded = true;
                        }
                    } catch (Exception ex) {
                        Trace.TraceWarning("Failed to load image:{0}", ex.Message);
                    }

                    if (!item.IsLoaded || DateTime.Now.Subtract(File.GetLastWriteTime(item.LocalPath)).TotalDays > 3) {
                        Task.Factory.StartNew((state) =>
                            {
                                var i = (Item)state;
                                string url = GlobalConst.BaseImageUrl + i.Name;
                                try {
                                    using (var wc = new WebClient()) wc.DownloadFile(url, i.LocalPath);
                                    i.Image = Image.FromStream(new MemoryStream(File.ReadAllBytes(item.LocalPath)));
                                    i.IsLoaded = true;
                                    File.SetLastWriteTime(i.LocalPath, DateTime.Now);
                                } catch (Exception ex) {
                                    Trace.TraceWarning("Failed to load server image: {0}: {1}", url, ex.Message);
                                    if (!i.IsLoaded) i.IsError = true;
                                }
                            },
                                              item);
                    }
                }
                return item;
            }
        }


        public static Tuple<Image, string> GetClanOrFactionImage(User user) {
            Image ret = null;
            string rets = null;
            if (!String.IsNullOrEmpty(user.Clan)) {
                Image clanImg = Program.ServerImages.GetImage(String.Format("Clans/{0}.png", user.Clan));
                ret = clanImg;
                rets = user.Clan + " " + user.Faction;
            }
            else if (!String.IsNullOrEmpty(user.Faction)) {
                Image facImg = Program.ServerImages.GetImage(String.Format("Factions/{0}.png", user.Faction));
                ret = facImg;
                rets = user.Faction;
            }
            return Tuple.Create(ret, rets);
        }

        #region Nested type: Item

        public class Item
        {
            public Image Image;
            public bool IsError;
            public bool IsLoaded;
            public string LocalPath;
            public string Name;
        }

        #endregion
    }
}