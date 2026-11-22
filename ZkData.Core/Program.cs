using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ZkData;

namespace ZkData.Core
{
    /// <summary>
    /// Builds a database from the EF Core model so its schema can be compared with the one
    /// the EF6 migrations produce (db/schema/schema.txt).
    ///
    ///     ZK_CONNECTION_STRING=... dotnet run -- create   build a database from the model
    ///     ZK_CONNECTION_STRING=... dotnet run -- read     read an existing one through it
    ///     ZK_CONNECTION_STRING=... dotnet run -- write    write to one through it (rolled back)
    ///     ZK_CONNECTION_STRING=... dotnet run -- rate     run the rating pipeline, print the result
    ///     ZK_CONNECTION_STRING=... dotnet run -- minimaps <dir>   report stored images that predate the ToBytes fix
    ///     ZK_CONNECTION_STRING=... dotnet run -- backfill-minimaps <dir> [--apply]   correct them
    ///
    /// The comparison is the point: it is how the port knows whether the model is right,
    /// rather than whether it compiles.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Refuses to hand out a password or an admin level unless the connection string names a
        /// database this repository creates.
        ///
        /// `set-password` and `grant` write to whatever ZK_CONNECTION_STRING points at, and one of
        /// them is privilege escalation. The failure being guarded against is not malice, it is a
        /// terminal that still has a production connection string exported from an hour ago.
        ///
        /// It is a name check, not a security boundary - anyone who means to can set
        /// ZK_ALLOW_DANGEROUS_WRITE=1. That is the point: the barrier is there to be noticed, not
        /// to be unbreakable.
        /// </summary>
        private static bool IsADevelopmentDatabase()
        {
            if (Environment.GetEnvironmentVariable("ZK_ALLOW_DANGEROUS_WRITE") == "1") return true;

            var catalog = (ZkDataContext.ConnectionString ?? "")
                .Split(';')
                .Select(part => part.Split('=', 2))
                .Where(pair => pair.Length == 2 && pair[0].Trim().Equals("Initial Catalog", StringComparison.OrdinalIgnoreCase))
                .Select(pair => pair[1].Trim())
                .FirstOrDefault() ?? "";

            var known = new[] { "zk_test", "zk_efcore", "zk_local", "zero-k_local" };
            if (known.Any(k => catalog.Equals(k, StringComparison.OrdinalIgnoreCase))) return true;

            Console.Error.WriteLine("refusing to write credentials to database '" + catalog + "'.");
            Console.Error.WriteLine("this command is for a local test database (" + string.Join(", ", known) + ").");
            Console.Error.WriteLine("if you really mean it: ZK_ALLOW_DANGEROUS_WRITE=1");
            return false;
        }

        public static int Main(string[] args)
        {
            var command = args.FirstOrDefault() ?? "summary";

            // `seam` reads db.Model, which EF Core builds from the entity classes without ever
            // opening a connection - so it runs on a bare machine, and runs in the fast CI job
            // rather than behind a SQL Server container. Giving it a connection string it does
            // not use would have been the easy way to skip writing this comment.
            if (command == "seam") ZkDataContext.ConnectionString = ZkDataContext.ConnectionString
                ?? "Server=(seam-check-has-no-database);Database=none;Trusted_Connection=false";
            else if (string.IsNullOrEmpty(ZkDataContext.ConnectionString))
            {
                Console.Error.WriteLine("Set ZK_CONNECTION_STRING first - see db/README.md.");
                return 2;
            }

            try
            {
                using (var db = new ZkDataContext())
                {
                    switch (command)
                    {
                        case "create":
                            Console.WriteLine("dropping and recreating from the EF Core model...");
                            db.Database.EnsureDeleted();
                            db.Database.EnsureCreated();
                            Console.WriteLine("created");
                            break;

                        case "read":
                            return ReadVerification.Run(db);

                        case "write":
                            return WriteVerification.Run(db);

                        // Phase 1's gate. Needs no database, but lives here because this is where
                        // the EF Core model is - the entity set it refuses is read off that model
                        // rather than listed by hand.
                        case "seam":
                            return SeamVerification.Run(db, args.Skip(1).FirstOrDefault() ?? "--check");

                        case "rate":
                            return RatingRun.Run(db, args.Skip(1).FirstOrDefault());

                        // Which stored map images predate the 2026-09-25 ToBytes fix. Read-only;
                        // see LegacyMinimapReport and IMAGING-MIGRATION.md.
                        case "minimaps":
                            return LegacyMinimapReport.Run(db, args.Skip(1).FirstOrDefault());

                        // ...and the one that changes them. Dry run unless --apply.
                        case "backfill-minimaps":
                            return LegacyMinimapBackfill.Run(db, args.Skip(1).ToArray());

                        // Whether the two deprecated WCF endpoints can go yet. Read-only, and
                        // it refuses to conclude from a window with no instrumented start in
                        // it - see LegacyCallerReport.
                        case "legacy-callers":
                            return LegacyCallerReport.Run(db, args.Skip(1).ToArray());

                        // A development convenience, and deliberately a thin one: the fixture
                        // stores PasswordBcrypt as NULL for every account, so there is nothing to
                        // sign in as while testing the site by hand.
                        //
                        // It calls Account.SetPasswordPlain, which is production code - the same
                        // BCrypt(MD5-of-password) the lobby server writes at registration - so
                        // logging in afterwards exercises the real verification rather than a
                        // bypass. Point it at the test database, not at anything real.
                        case "set-password":
                        {
                            if (!IsADevelopmentDatabase()) return 2;
                            var name = args.Skip(1).FirstOrDefault();
                            var password = args.Skip(2).FirstOrDefault();
                            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(password))
                            {
                                Console.Error.WriteLine("usage: set-password <account name> <password>");
                                return 2;
                            }
                            var account = db.Accounts.FirstOrDefault(x => x.Name == name);
                            if (account == null)
                            {
                                Console.Error.WriteLine("no account named " + name);
                                return 2;
                            }
                            account.SetPasswordPlain(password);
                            db.SaveChanges();
                            Console.WriteLine("set a password for " + account.Name + " (AccountID "
                                              + account.AccountID + ")");
                            return 0;
                        }

                        // The other half of being able to look at the site by hand. Signing in
                        // gets an ordinary player; most of what is worth testing - the admin
                        // pages, the tournament console, moderation - is behind AdminLevel, and
                        // nothing in the fixture has any.
                        //
                        // Global.IsTourneyController is `AdminLevel >= Moderator || the flag`, so
                        // `tourney` exists to test the flag ON ITS OWN, which a moderator cannot.
                        case "grant":
                        {
                            if (!IsADevelopmentDatabase()) return 2;

                            var name = args.Skip(1).FirstOrDefault();
                            var role = args.Skip(2).FirstOrDefault()?.ToLowerInvariant();
                            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(role))
                            {
                                Console.Error.WriteLine(
                                    "usage: grant <account name> <none|moderator|superadmin|tourney|no-tourney>");
                                return 2;
                            }
                            var account = db.Accounts.FirstOrDefault(x => x.Name == name);
                            if (account == null)
                            {
                                Console.Error.WriteLine("no account named " + name);
                                return 2;
                            }

                            switch (role)
                            {
                                case "none": account.AdminLevel = AdminLevel.None; break;
                                case "moderator": account.AdminLevel = AdminLevel.Moderator; break;
                                case "superadmin": account.AdminLevel = AdminLevel.SuperAdmin; break;
                                case "tourney": account.IsTourneyController = true; break;
                                case "no-tourney": account.IsTourneyController = false; break;
                                default:
                                    Console.Error.WriteLine("unknown role '" + role
                                        + "' - one of none, moderator, superadmin, tourney, no-tourney");
                                    return 2;
                            }

                            db.SaveChanges();
                            Console.WriteLine(account.Name + " is now AdminLevel " + account.AdminLevel
                                              + ", IsTourneyController " + account.IsTourneyController);
                            return 0;
                        }

                        case "summary":
                        default:
                            var model = db.Model;
                            var entities = model.GetEntityTypes().ToList();
                            Console.WriteLine("entity types: " + entities.Count);
                            Console.WriteLine("properties:   " + entities.Sum(e => e.GetProperties().Count()));
                            Console.WriteLine("indexes:      " + entities.Sum(e => e.GetIndexes().Count()));
                            Console.WriteLine("foreign keys: " + entities.Sum(e => e.GetForeignKeys().Count()));
                            break;
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.GetType().Name + ": " + ex.Message);
                for (var inner = ex.InnerException; inner != null; inner = inner.InnerException)
                    Console.Error.WriteLine("  inner: " + inner.Message);
                return 1;
            }
        }
    }
}
