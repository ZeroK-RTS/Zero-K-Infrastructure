using System;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using EntityFramework.Extensions;
using PlasmaShared;
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
            db.SpringBattles.Where(battle => (!(battle.IsMission ||  battle.SpringBattlePlayers.Count > 0 && battle.SpringBattlePlayers.Any(x => x.IsInVictoryTeam) && 
                    (!battle.SpringBattlePlayers.Where(x => x.IsInVictoryTeam).FirstOrDefault().EloChange.HasValue || Math.Abs(battle.SpringBattlePlayers.Where(x => x.IsInVictoryTeam).FirstOrDefault().EloChange.Value) < 0.001)
            || battle.HasBots || (battle.PlayerCount < 2) || (battle.ResourceByMapResourceID != null && battle.ResourceByMapResourceID.MapIsSpecial == true) || battle.Duration < GlobalConst.MinDurationForElo))).Update(battle => new SpringBattle()
            {
                ApplicableRatings = RatingCategoryFlags.Casual
            });
            db.SpringBattles.Where(battle => (!battle.HasBots && (battle.IsMatchMaker || !string.IsNullOrEmpty(battle.Title) && (battle.Title.Contains("[T]") || battle.Title.ToLower().Contains("tourney") || battle.Title.ToLower().Contains("tournament") || battle.Title.ToLower().Contains("1v1")
            )))).Update(battle => new SpringBattle()
            {
                ApplicableRatings = RatingCategoryFlags.MatchMaking | RatingCategoryFlags.Casual
            });
            db.SpringBattles.Where(battle => (!battle.HasBots && battle.Mode == PlasmaShared.AutohostMode.Planetwars)).Update(battle => new SpringBattle()
            {
                ApplicableRatings = RatingCategoryFlags.Planetwars | RatingCategoryFlags.Casual
            });
        }

        /// <summary>
        /// This method is called after migration to latest version
        /// </summary>
        protected override void Seed(ZkDataContext db) {
            db.Accounts.Where(a => a.Rank < 0).Update(x => new Account() { Rank = 0 });

            db.Database.ExecuteSqlCommand($"truncate table {nameof(LogEntries)}");
           
            if (GlobalConst.Mode == ModeType.Local) LocalSeed(db);

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
                new ForumCategory { Title = "Missions", ForumMode = ForumMode.Missions, IsLocked = true, SortOrder = 28 },
                new ForumCategory { Title = "Battles", ForumMode = ForumMode.SpringBattles, IsLocked = true, SortOrder = 30 },
                new ForumCategory { Title = "Game modes", ForumMode = ForumMode.GameModes, IsLocked = true, SortOrder = 35 },
                new ForumCategory { Title = "Off topic", ForumMode = ForumMode.General, SortOrder = 40 });

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

        private static void LocalSeed(ZkDataContext db)
        {
            // fill local DB with some basic test data
            db.MiscVars.AddOrUpdate(x => x.VarName,
                new MiscVar { VarName = "NightwatchPassword", VarValue = "dummy" },
                new MiscVar { VarName = "GithubHookKey", VarValue = "secret" });

            if (!db.MiscVars.Any(y => y.VarName == "SteamBuildPassword"))
                db.MiscVars.AddOrUpdate(x => x.VarName, new MiscVar { VarName = "SteamBuildPassword", VarValue = "secret" });

            if (!db.MiscVars.Any(y => y.VarName == "GlacierSecretKey"))
                db.MiscVars.AddOrUpdate(x => x.VarName, new MiscVar { VarName = "GlacierSecretKey", VarValue = "secret" });

            db.Accounts.AddOrUpdate(x => x.Name,
            new Account
            {
                Name = "TestPlayer",
                NewPasswordPlain = "test",
                AdminLevel = AdminLevel.SuperAdmin,
                HasKudos = true,
                Level = 255,
                Xp = 1325900,
                Rank = 3,
                Country = "cz",
                Avatar = "amphimpulse",
                DevLevel = DevLevel.CoreDeveloper,
            },
            new Account { Name = "test", NewPasswordPlain = "test", AdminLevel = AdminLevel.None, HasKudos = true, Level = 50, Country = "cz" },
            new Account { Name = GlobalConst.NightwatchName, NewPasswordPlain = "dummy", IsBot = true, AdminLevel = AdminLevel.SuperAdmin });
        }
    }
}