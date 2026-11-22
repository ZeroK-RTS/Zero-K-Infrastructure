using System;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Linq;
using ZkData;

namespace DbSetup
{
    class Program
    {
        // Mirrors ZkData.Migrations.Configuration, which is internal to ZkData.
        // ContextKey must match it exactly or EF treats the history as someone else's.
        static DbMigrationsConfiguration<ZkDataContext> Config(string cs)
        {
            return new DbMigrationsConfiguration<ZkDataContext>
            {
                ContextKey = "PlasmaShared.Migrations.Configuration",
                AutomaticMigrationsEnabled = false,
                MigrationsAssembly = typeof(ZkDataContext).Assembly,
                MigrationsNamespace = "ZkData.Migrations",
                TargetDatabase = new DbConnectionInfo(cs, "System.Data.SqlClient"),
            };
        }

        static int Main(string[] args)
        {
            var cs = Environment.GetEnvironmentVariable("ZK_CONNECTION_STRING");
            var command = args.Length > 0 ? args[0] : "status";
            try
            {
                var migrator = new DbMigrator(Config(cs));
                switch (command)
                {
                    case "status":
                        var applied = migrator.GetDatabaseMigrations().ToList();
                        var pending = migrator.GetPendingMigrations().ToList();
                        Console.WriteLine("applied: " + applied.Count + ", pending: " + pending.Count);
                        if (applied.Any()) Console.WriteLine("current: " + applied.First());
                        foreach (var p in pending.Take(20)) Console.WriteLine("  pending: " + p);
                        break;

                    case "to":
                        var target = args[1];
                        Console.WriteLine("migrating to " + target + " ...");
                        migrator.Update(target);
                        Console.WriteLine("now at: " + migrator.GetDatabaseMigrations().First());
                        break;

                    case "latest":
                        Console.WriteLine("migrating to latest ...");
                        migrator.Update();
                        Console.WriteLine("now at: " + migrator.GetDatabaseMigrations().First());
                        break;

                    case "list":
                        foreach (var m in migrator.GetLocalMigrations().Reverse().Take(12)) Console.WriteLine("  " + m);
                        break;
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("FAILED: " + ex.GetType().Name + ": " + ex.Message);
                var inner = ex.InnerException;
                while (inner != null) { Console.WriteLine("  inner: " + inner.Message); inner = inner.InnerException; }
                return 1;
            }
        }
    }
}
