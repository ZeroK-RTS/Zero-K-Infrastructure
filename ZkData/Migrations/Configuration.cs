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

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data. E.g.
            //
            //    context.People.AddOrUpdate(
            //      p => p.FullName,
            //      new Person { FullName = "Andrew Peters" },
            //      new Person { FullName = "Brice Lambson" },
            //      new Person { FullName = "Rowan Miller" }
            //    );
            //
            
            if (!db.MiscVars.Any(x => x.VarName == "NightwatchPassword")) db.MiscVars.Add(new MiscVar() { VarName = "NightwatchPassword", VarValue = "dummy" });
        }
    }
}
