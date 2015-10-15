using System;
using System.Data.Entity.Migrations;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace ZkData.Migrations
{
    sealed class Configuration : DbMigrationsConfiguration<ZkDataContext>
    {
        public Configuration()
        {
            ContextKey = "PlasmaShared.Migrations.Configuration"; // if you change this, you also ahve to change content of __MigrationHistory table in DB
            AutomaticMigrationsEnabled = false;
        }

        public static void ImportWiki()
        {
            var wikiNodes = ZkDataResources.wikiIndex.Lines().Select(x => x.Trim().Split(new[] { ' ', '\t' })[0]).ToList();
            foreach (var node in wikiNodes)
            {
                using (var wc = new WebClient())
                using (var db = new ZkDataContext())
                {
                    var str = wc.DownloadString($"https://zero-k.googlecode.com/svn/wiki/{node}.wiki");

                    var thread = db.ForumThreads.FirstOrDefault(x => x.WikiKey == node);
                    if (thread == null)
                    {
                        thread = new ForumThread();
                        thread.ForumCategory = db.ForumCategories.First(x => x.ForumMode == ForumMode.Wiki);
                        db.ForumThreads.Add(thread);
                    }

                    var licho = db.Accounts.First(x => x.Name == "Licho");

                    var title = node;
                    str = Regex.Replace(str, "\\#summary ([^\n\r]+)",
                        me =>
                        {
                            title = me.Groups[1].Value;
                            return "";
                        });

                    str = Regex.Replace(str, "\\#labels ([^\n\r]+)", "");

                    thread.Title = title.Substring(0, Math.Min(300, title.Length));

                    thread.WikiKey = node;
                    thread.AccountByCreatedAccountID = licho;

                    var post = thread.ForumPosts.OrderBy(x => x.ForumPostID).FirstOrDefault();
                    if (post == null)
                    {
                        post = new ForumPost();
                        thread.ForumPosts.Add(post);
                    }
                    post.Text = str;
                    post.Account = licho;

                    db.SaveChanges();
                }
            }
        }

        protected override void Seed(ZkDataContext db)
        {
            //  This method will be called after migrating to the latest version.
            if (GlobalConst.Mode == ModeType.Local)
            {
                // fill local DB with some basic test data
                db.MiscVars.AddOrUpdate(x => x.VarName, new MiscVar { VarName = "NightwatchPassword", VarValue = "dummy" }, new MiscVar() { VarName = "GithubHookKey", VarValue = "secret" });

                db.Accounts.AddOrUpdate(x => x.Name,
                    new Account
                    {
                        Name = "test",
                        NewPasswordPlain = "test",
                        IsZeroKAdmin = true,
                        Kudos = 200,
                        Elo = 1700,
                        Level = 50,
                        EloWeight = 2,
                        SpringieLevel = 4,
                        Country = "cz",
                    },
                    new Account()
                    {
                        Name = GlobalConst.NightwatchName,
                        NewPasswordPlain = "dummy",
                        IsBot = true,
                        IsZeroKAdmin = true,
                    }
                 );


                db.AutohostConfigs.AddOrUpdate(x => x.Login, new AutohostConfig()
                {
                    Login = "Springiee",
                    Title = "Local springie test",
                    Password = "dummy",
                    AutoSpawn = true,
                    AutoUpdateRapidTag = "zk:stable",
                    Mod = "zk:stable",
                    ClusterNode = "alpha",
                    JoinChannels = "bots",
                    Map = "Dual Icy Run v3",
                    SpringVersion = GlobalConst.DefaultEngineOverride,
                    MaxPlayers = 10,
                }, new AutohostConfig()
                {
                    Login = "Fungiee",
                    Title = "Local fungicide test",
                    Password = "dummy",
                    AutoSpawn = true,
                    AutoUpdateRapidTag = "zk:stable",
                    Mod = "zk:stable",
                    ClusterNode = "alpha",
                    JoinChannels = "bots",
                    Map = "Dual Icy Run v3",
                    SpringVersion = GlobalConst.DefaultEngineOverride,
                    MaxPlayers = 10,
                }, new AutohostConfig()
                {
                    Login = "Trifliee",
                    Title = "Local triplicator test",
                    Password = "dummy",
                    AutoSpawn = true,
                    AutoUpdateRapidTag = "zk:test",
                    Mod = "zk:test",
                    ClusterNode = "alpha",
                    JoinChannels = "bots",
                    Map = "Dual Icy Run v3",
                    SpringVersion = GlobalConst.DefaultEngineOverride,
                    MaxPlayers = 10,
                });

            }

            db.ForumCategories.AddOrUpdate(x => x.Title,
                new ForumCategory { Title = "General discussion",ForumMode = ForumMode.General},
                new ForumCategory { Title = "News", ForumMode = ForumMode.News, IsLocked = true},
                new ForumCategory { Title = "Maps", ForumMode = ForumMode.Maps, IsLocked =true},
                new ForumCategory { Title = "Battles", ForumMode = ForumMode.SpringBattles, IsLocked = true},
                new ForumCategory { Title = "Missions", ForumMode = ForumMode.Missions, IsLocked = true},
                new ForumCategory { Title = "Clans", ForumMode = ForumMode.Clans, IsLocked = true},
                new ForumCategory { Title = "Planets", ForumMode = ForumMode.Planets, IsLocked = true},
                new ForumCategory { Title = "Wiki", ForumMode = ForumMode.Wiki, IsLocked = true});


            db.SaveChanges();
            if (db.ForumThreads.Count(x => !object.Equals(x.WikiKey, null)) == 0)
            {
                ImportWiki();
            }

        }
    }
}