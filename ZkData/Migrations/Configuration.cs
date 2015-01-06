using System.Data.Entity.Migrations;
using System.Linq;
using ZkData;

namespace ZkData.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<ZkDataContext>
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

                db.MiscVars.AddOrUpdate(x => x.VarName, new MiscVar() { VarName = "NightwatchPassword", VarValue = "dummy" });

                db.Accounts.AddOrUpdate(x=>x.Name, new Account() {
                    Name = "test",
                    Password = Utils.HashLobbyPassword("test"),
                    IsZeroKAdmin = true,
                    Kudos = 200,
                    Elo = 1700,
                    EloWeight = 2,
                    SpringieLevel = 4,
                    Country = "cz",
                    LobbyID = 2
                });
            }

        }
    }
}
