using System;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace ZkData.Migrations
{
    sealed class Configuration: DbMigrationsConfiguration<ZkDataContext>
    {
        public Configuration() {
            ContextKey = "PlasmaShared.Migrations.Configuration";
                // if you change this, you also ahve to change content of __MigrationHistory table in DB
            AutomaticMigrationsEnabled = false;
        }

        public static void ImportWiki() {
            var wikiNodes = ZkDataResources.wikiIndex.Lines().Select(x => x.Trim().Split(' ', '\t')[0]).ToList();
            foreach (var node in wikiNodes)
            {
                using (var wc = new WebClient())
                {
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

                        var improtOwner = db.Accounts.First(); // note .. meh...

                        var title = node;
                        str = Regex.Replace(
                            str,
                            "\\#summary ([^\n\r]+)",
                            me =>
                            {
                                title = me.Groups[1].Value;
                                return "";
                            });

                        str = Regex.Replace(str, "\\#labels ([^\n\r]+)", "");

                        thread.Title = title.Substring(0, Math.Min(300, title.Length));

                        thread.WikiKey = node;
                        thread.AccountByCreatedAccountID = improtOwner;

                        var post = thread.ForumPosts.OrderBy(x => x.ForumPostID).FirstOrDefault();
                        if (post == null)
                        {
                            post = new ForumPost();
                            thread.ForumPosts.Add(post);
                        }
                        post.Text = str;
                        post.Account = improtOwner;

                        db.SaveChanges();
                    }
                }
            }
        }

        protected override void Seed(ZkDataContext db) {
            //  This method will be called after migrating to the latest version.
            if (GlobalConst.Mode == ModeType.Local)
            {
                // fill local DB with some basic test data
                db.MiscVars.AddOrUpdate(
                    x => x.VarName,
                    new MiscVar { VarName = "NightwatchPassword", VarValue = "dummy" },
                    new MiscVar { VarName = "GithubHookKey", VarValue = "secret" });
                
                if (!db.MiscVars.Any(y=>y.VarName=="SteamBuildPassword"))
                    db.MiscVars.AddOrUpdate(x => x.VarName, new MiscVar { VarName = "SteamBuildPassword", VarValue = "secret" });

                if (!db.MiscVars.Any(y => y.VarName == "GlacierSecretKey"))
                    db.MiscVars.AddOrUpdate(x => x.VarName, new MiscVar { VarName = "GlacierSecretKey", VarValue = "secret" });


                db.Accounts.AddOrUpdate(
                    x => x.Name,
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
                        Country = "cz"
                    },
                    new Account { Name = GlobalConst.NightwatchName, NewPasswordPlain = "dummy", IsBot = true, IsZeroKAdmin = true });

                db.AutohostConfigs.AddOrUpdate(
                    x => x.Login,
                    new AutohostConfig
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
                        MaxPlayers = 10
                    },
                    new AutohostConfig
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
                        MaxPlayers = 10
                    },
                    new AutohostConfig
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
                        MaxPlayers = 10
                    });
            }

            db.ForumCategories.AddOrUpdate(
                x => x.Title,
                new ForumCategory { Title = "General discussion", ForumMode = ForumMode.General, SortOrder = 1 },
                new ForumCategory { Title = "News", ForumMode = ForumMode.News, IsLocked = true, SortOrder = 9},
                new ForumCategory { Title = "Wiki", ForumMode = ForumMode.Wiki, IsLocked = true, SortOrder = 10 },
                new ForumCategory { Title = "Maps", ForumMode = ForumMode.Maps, IsLocked = true, SortOrder = 18 },
                new ForumCategory { Title = "Missions", ForumMode = ForumMode.Missions, IsLocked = true, SortOrder = 18 },
                new ForumCategory { Title = "Battles", ForumMode = ForumMode.SpringBattles, IsLocked = true, SortOrder = 19 },
                new ForumCategory { Title = "Off topic", ForumMode = ForumMode.General, SortOrder = 20 });

            db.SaveChanges();

            var genId = db.ForumCategories.First(x => x.Title == "General discussion").ForumCategoryID;
            var wikiId = db.ForumCategories.First(x => x.Title == "Wiki").ForumCategoryID;
            var offtopic = db.ForumCategories.First(x => x.Title == "Off topic").ForumCategoryID;

            db.ForumCategories.AddOrUpdate(
                x => x.Title,
                new ForumCategory { Title = "Help and bugs", ForumMode = ForumMode.General, SortOrder = 2, ParentForumCategoryID = genId },
                new ForumCategory { Title = "Strategy and tips", ForumMode = ForumMode.General, SortOrder = 3, ParentForumCategoryID = genId },
                new ForumCategory { Title = "Development", ForumMode = ForumMode.General, SortOrder = 4, ParentForumCategoryID = genId },
                new ForumCategory { Title = "PlanetWars", ForumMode = ForumMode.General, SortOrder = 5, ParentForumCategoryID = genId},
                new ForumCategory { Title = "Deutsches Forum", ForumMode = ForumMode.General, SortOrder = 8, ParentForumCategoryID = genId });

            db.SaveChanges();

            var pwId = db.ForumCategories.First(x => x.Title == "PlanetWars").ForumCategoryID;

            db.ForumCategories.AddOrUpdate(
                x => x.Title,
                new ForumCategory { Title = "Planets", ForumMode = ForumMode.Planets, IsLocked = true, SortOrder = 6, ParentForumCategoryID = pwId },
                new ForumCategory { Title = "Clans", ForumMode = ForumMode.Clans, SortOrder = 7, IsLocked = true, ParentForumCategoryID = pwId });

            db.ForumCategories.AddOrUpdate(
                x => x.Title,
                new ForumCategory { Title = "Manual", ForumMode = ForumMode.Wiki, IsLocked = false, SortOrder = 11, ParentForumCategoryID = wikiId },
                new ForumCategory { Title = "Tutorials", ForumMode = ForumMode.Wiki, IsLocked = false, SortOrder = 12, ParentForumCategoryID = wikiId },
                new ForumCategory { Title = "DeveloperGuide", ForumMode = ForumMode.Wiki, IsLocked = false, SortOrder = 13, ParentForumCategoryID = wikiId },
                new ForumCategory { Title = "MissionEditor", ForumMode = ForumMode.Wiki, IsLocked = false, SortOrder = 14, ParentForumCategoryID = wikiId },
                new ForumCategory { Title = "Notes", ForumMode = ForumMode.Wiki, IsLocked = false, SortOrder = 15, ParentForumCategoryID = wikiId },
                new ForumCategory { Title = "Site", ForumMode = ForumMode.Wiki, IsLocked = false, SortOrder = 16, ParentForumCategoryID = wikiId });

            db.ForumCategories.AddOrUpdate(
                x => x.Title,
                new ForumCategory { Title = "Asylum", ForumMode = ForumMode.Archive, IsLocked = false, SortOrder = 21, ParentForumCategoryID = offtopic });


            db.SaveChanges();


            if (db.ForumThreads.Count(x => !Equals(x.WikiKey, null)) == 0) ImportWiki();
        }
    }
}