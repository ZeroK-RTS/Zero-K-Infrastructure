using System.Data.Entity.Migrations;

namespace ZkData.Migrations
{
    sealed class Configuration: DbMigrationsConfiguration<ZkDataContext>
    {
        public Configuration()
        {
            ContextKey = "PlasmaShared.Migrations.Configuration"; // if you change this, you also ahve to change content of __MigrationHistory table in DB
               
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(ZkDataContext db)
        {
            //  This method will be called after migrating to the latest version.

            if (GlobalConst.Mode == ModeType.Local) {
                // fill local DB with some basic test data

                db.MiscVars.AddOrUpdate(x => x.VarName, new MiscVar { VarName = "NightwatchPassword", VarValue = "dummy" });

                db.Accounts.AddOrUpdate(x => x.Name,
                    new Account {
                        Name = "test",
                        NewPassword = Utils.HashLobbyPassword("test"),
                        IsZeroKAdmin = true,
                        Kudos = 200,
                        Elo = 1700,
                        Level = 50,
                        EloWeight = 2,
                        SpringieLevel = 4,
                        Country = "cz",
                    },
                    new Account() {
                        Name = GlobalConst.NightwatchName,
                        NewPassword = Utils.HashLobbyPassword("dummy"),
                        IsBot = true,
                        IsZeroKAdmin = true,
                    }
                 );

                db.ForumCategories.AddOrUpdate(x => x.Title, 
                    new ForumCategory { Title = "General discussion", },
                    new ForumCategory { Title = "News", IsNews = true }, 
                    new ForumCategory { Title = "Maps", IsMaps = true },
                    new ForumCategory { Title = "Battles", IsSpringBattles = true }, 
                    new ForumCategory { Title = "Missions", IsMissions = true },
                    new ForumCategory { Title = "Clans", IsClans = true }, 
                    new ForumCategory { Title = "Planets", IsPlanets = true });

                db.AutohostConfigs.AddOrUpdate(x=>x.Login, new AutohostConfig() {
                    Login = "Springiee",
                    Title = "Local springie test",
                    Password = "dummy",
                    AutoSpawn = true,
                    AutoUpdateRapidTag = "zk:stable",
                    Mod="zk:stable",
                    ClusterNode = "alpha",
                    JoinChannels = "bots",
                    Map = "dual_icy_run_v3",
                    SpringVersion = GlobalConst.DefaultEngineOverride,
                    MaxPlayers = 10,
                });

            }
        }
    }
}