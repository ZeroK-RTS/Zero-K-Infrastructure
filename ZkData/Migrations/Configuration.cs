using System;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Ratings;

namespace ZkData.Migrations
{
    sealed class Configuration: DbMigrationsConfiguration<ZkDataContext>
    {
        public Configuration() {
            ContextKey = "PlasmaShared.Migrations.Configuration";
                // if you change this, you also ahve to change content of __MigrationHistory table in DB
            AutomaticMigrationsEnabled = false;
        }

        private static void InitializeBattleRatings(ZkDataContext db)
        {
            DateTime minStartTime = DateTime.Now.AddMonths(-1);

            foreach (SpringBattle battle in db.SpringBattles.Where(x => x.StartTime > minStartTime).ToList())
            {
                try
                {
                    int val = 0;
                    if (!battle.HasBots)
                    {
                        val += (!(battle.IsMission || battle.HasBots || (battle.PlayerCount < 2) || (battle.ResourceByMapResourceID?.MapIsSpecial == true) || battle.Duration < GlobalConst.MinDurationForElo)) ? (int)(RatingCategory.Casual) : 0;
                        val += (battle.IsMatchMaker || battle.Title?.Contains("[T]") == true || battle.Title?.Contains("Tournament") == true || battle.Title?.Contains("Tourney") == true) ? (int)RatingCategory.MatchMaking : 0;
                        val += (battle.Mode == PlasmaShared.AutohostMode.Planetwars) ? (int)RatingCategory.Planetwars : 0;
                    }
                    battle.ApplicableRatings = (RatingCategory)val;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError("Error applying ratings: " + ex);
                }
            }
        }

        protected override void Seed(ZkDataContext db) {
            InitializeBattleRatings(db); //remove this after execution
            db.SaveChanges();

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
                        AdminLevel = AdminLevel.SuperAdmin,
                        Kudos = 200,
                        Elo = 1700,
                        Level = 50,
                        EloWeight = 2,
                        Country = "cz"
                    },
                    new Account { Name = GlobalConst.NightwatchName, NewPasswordPlain = "dummy", IsBot = true, AdminLevel = AdminLevel.SuperAdmin});
            }

            db.Resources.AddOrUpdate(x=>x.InternalName, new Resource()
            {
                InternalName = "Zero-K $VERSION",
                RapidTag = "zk:dev",
                MapSupportLevel = ZkData.MapSupportLevel.Featured,
                TypeID = ResourceType.Mod,
            });

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

            db.RoleTypes.AddOrUpdate(x=>x.Name, new RoleType() {Name = "Clan Leader", Description = "Clan founder or elected leader", IsClanOnly = true, IsOnePersonOnly = true, IsVoteable = true, PollDurationDays = 2, RightDropshipQuota = 1, RightBomberQuota = 1, RightMetalQuota = 1, RightWarpQuota = 1, RightEditTexts = true, RightSetEnergyPriority = true, RightKickPeople = true, DisplayOrder = 0});


            db.SaveChanges();

        }
    }
}