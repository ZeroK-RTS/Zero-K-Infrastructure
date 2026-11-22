using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using ZkData;
using System.Data.Entity.SqlServer;

namespace ZkData.Core
{
    /// <summary>
    /// Reads Zero-K's data through the EF Core model.
    ///
    /// The schema diff proves the model would BUILD the right database. It cannot prove the
    /// model can READ the existing one: a column mapped to the wrong name, a value
    /// conversion EF6 did implicitly and EF Core does not, a navigation that produces
    /// invalid SQL - none of those change the schema the model emits, so none of them show
    /// up in db/schema/schema.txt. They show up here, as an exception on the first row.
    ///
    ///     ZK_CONNECTION_STRING=...zk_test... dotnet run -- read
    ///
    /// Two passes. The sweep selects from every mapped table, which makes SQL Server check
    /// every column name the model believes in, and materialises whatever rows it finds,
    /// which checks every conversion those rows exercise. The query pass then runs the
    /// shapes production actually issues - the WHR loader's battle query above all - because
    /// a model can map every column correctly and still fail to translate a join.
    /// </summary>
    public static class ReadVerification
    {
        private const int ProbeRows = 20;

        public static int Run(ZkDataContext db)
        {
            var failures = new List<string>();
            int tables = 0, tablesWithRows = 0, rows = 0;

            {
                Console.WriteLine("== sweep: select from every mapped table");
                foreach (var entity in db.Model.GetEntityTypes().OrderBy(e => e.Name))
                {
                    if (entity.GetTableName() == null) continue;
                    tables++;
                    try
                    {
                        int read = entity.HasSharedClrType
                            ? db.Set<Dictionary<string, object>>(entity.Name).AsNoTracking().Take(ProbeRows).ToList().Count
                            : (int)ProbeMethod.MakeGenericMethod(entity.ClrType).Invoke(null, new object[] { db });
                        rows += read;
                        if (read > 0) tablesWithRows++;
                    }
                    catch (Exception ex)
                    {
                        failures.Add(entity.GetTableName() + ": " + Innermost(ex));
                    }
                }

                Console.WriteLine("   " + tables + " tables read, " + tablesWithRows + " of them with rows (" +
                                  rows + " rows materialised)");
                Console.WriteLine("   the empty ones prove their columns exist and are selectable, not their conversions");

                // A fixture that failed to load reads every table cleanly and materialises nothing,
                // which passes by seeing nothing: the conversions this check exists to exercise never
                // run. The floor is simply "any", so no fixture change can move it.
                //
                // The queries below are the same story one at a time. Each returned a cheerful string
                // on an empty result - "no battles", "no accounts", "no named account" - and counted
                // as ok, so against a schema-only database this whole command printed "the EF Core
                // model reads this database" and exited 0. They throw now. The one exception is the
                // navigation probe, which prints "empty" rather than "ok" and is listed in the summary
                // under "not exercised by this fixture": that one reports itself.
                if (rows == 0)
                    failures.Add("materialised no rows at all - every table read cleanly because every "
                                 + "table was empty, so no conversion was exercised. Load the fixture "
                                 + "with db/load-fixture.sh and run this again.");

                foreach (var failure in failures) Console.WriteLine("   FAILED " + failure);

                Console.WriteLine();
                Console.WriteLine("== queries: the shapes production issues");
                RunQueries(db, failures);
            }

            Console.WriteLine();
            if (Unexercised.Count > 0)
                Console.WriteLine("not exercised by this fixture, valid SQL only: " + string.Join(", ", Unexercised));
            if (failures.Count == 0)
            {
                Console.WriteLine("the EF Core model reads this database.");
                return 0;
            }
            Console.WriteLine(failures.Count + " failure(s) - the model does not read this database yet.");
            return 1;
        }

        private static void RunQueries(ZkDataContext db, List<string> failures)
        {
            // RatingSystems.Init: the query the whole rating pipeline starts from. Its shape is
            // the point - a range filter, a collection Include, AsNoTracking and an OrderBy, all
            // in one statement - so it is reproduced rather than simplified.
            Check("whr loader (filter + Include + AsNoTracking + OrderBy)", failures, () =>
            {
                var from = new DateTime(2000, 1, 1);
                var to = DateTime.Now.AddYears(1);
                var battles = db.SpringBattles
                    .Where(x => x.StartTime > from && x.StartTime < to)
                    .Include(x => x.SpringBattlePlayers)
                    .AsNoTracking()
                    .OrderBy(x => x.StartTime)
                    .ToList();
                var players = battles.Sum(b => b.SpringBattlePlayers.Count);
                return battles.Count + " battles, " + players + " player rows through the navigation";
            });

            // The rating pipeline reads these off the materialised battle, so a battle whose
            // players did not come back would silently rate nothing rather than fail.
            Check("battle players carry their flags", failures, () =>
            {
                var battle = db.SpringBattles.Include(x => x.SpringBattlePlayers).AsNoTracking()
                    .FirstOrDefault(x => x.SpringBattlePlayers.Any(p => !p.IsSpectator));
                // Returned as a pass until 2026-10-01, which let an empty fixture read as a
                // verified one. This check has nothing to say unless it finds a battle.
                if (battle == null)
                    throw new InvalidOperationException(
                        "no battle with a non-spectator in the fixture - nothing to read the flags off");
                var teams = battle.SpringBattlePlayers.Where(p => !p.IsSpectator)
                    .Select(p => p.AllyNumber).Distinct().Count();
                var winners = battle.SpringBattlePlayers.Count(p => p.IsInVictoryTeam && !p.IsSpectator);
                return "battle " + battle.SpringBattleID + ": " + teams + " teams, " + winners + " winners";
            });

            // EF6 lazy-loads a navigation by default. EF Core does not unless
            // UseLazyLoadingProxies is called AND the property is virtual, and ZkDataContext calls
            // it - but nothing ever read a navigation off an entity fetched WITHOUT Include to
            // find out whether it works. The failure mode is why that matters: a navigation that
            // stops loading comes back NULL rather than throwing, so a view renders "Nobody" or an
            // empty list and the page still answers 200. Every one of the 119 views walks these.
            // Each gets its OWN context, and that is not tidiness. EF Core fixes a tracked
            // entity up into the navigations of other tracked entities, so the collection check
            // run on the shared context passed with lazy loading TURNED OFF - it found the single
            // player the reference check above had just loaded, and reported "1 players" where
            // the battle has two. A clean context has nothing to fix up from.
            Check("a reference navigation loads with no Include", failures, () =>
            {
                using (var own = new ZkDataContext())
                {
                    // Tracked on purpose: EF Core does not lazy-load into a no-tracking query,
                    // and every other read here is AsNoTracking - so this one would have proved
                    // nothing.
                    var player = own.SpringBattlePlayers.FirstOrDefault();
                    if (player == null)
                        throw new InvalidOperationException(
                            "no battle players in the fixture - nothing to load a navigation off");
                    var account = player.Account;
                    if (account == null)
                        throw new InvalidOperationException(
                            "SpringBattlePlayer.Account came back null with the context still open"
                            + " - lazy loading is off, and every view reading a navigation renders"
                            + " empty rather than failing");
                    return "battle " + player.SpringBattleID + " player " + player.AccountID
                           + " -> " + account.Name;
                }
            });

            Check("a collection navigation loads with no Include", failures, () =>
            {
                int battleID, loaded;
                using (var own = new ZkDataContext())
                {
                    var battle = own.SpringBattles.FirstOrDefault();
                    if (battle == null) throw new InvalidOperationException("no battles in the fixture");
                    battleID = battle.SpringBattleID;
                    loaded = battle.SpringBattlePlayers.Count;
                }

                // Against the row count, not against zero. "Not empty" is satisfied by one
                // fixed-up entity, which is exactly how this read as a pass with the proxies off.
                var actual = db.SpringBattlePlayers.Count(x => x.SpringBattleID == battleID);
                if (loaded != actual)
                    throw new InvalidOperationException(
                        "SpringBattle.SpringBattlePlayers came back with " + loaded + " of "
                        + actual + " rows and no Include - a collection that does not lazy-load is"
                        + " indistinguishable from one with no rows");
                return "battle " + battleID + " -> " + loaded + " players, which is all of them";
            });

            Check("enum column filtered server side", failures, () =>
            {
                var byMode = db.SpringBattles.AsNoTracking()
                    .GroupBy(x => x.Mode).Select(g => new { g.Key, Count = g.Count() })
                    .ToList();
                return string.Join(", ", byMode.Select(m => m.Key + "=" + m.Count));
            });

            Check("aggregate translated to SQL", failures, () =>
            {
                var top = db.Accounts.AsNoTracking()
                    .OrderByDescending(a => a.SpringBattlePlayers.Count())
                    .Select(a => new { a.AccountID, Battles = a.SpringBattlePlayers.Count() })
                    .FirstOrDefault();
                if (top == null)
                    throw new InvalidOperationException("no accounts - the aggregate translated, but nothing ran through it");
                return "busiest account " + top.AccountID + " with " + top.Battles + " battles";
            });

            // EF6's SqlFunctions.PatIndex, which ClansController uses four times to reject a
            // clan whose name or shortcut collides with an existing one. The mapping is
            // asserted on the SQL, not just on the result: a shim that silently returned
            // null would still "pass" a row count here, because no clan collides in the
            // fixture, and every clan name would then be accepted in production.
            Check("SqlFunctions.PatIndex translates to the built-in PATINDEX", failures, () =>
            {
                var shortcut = "ZK";
                var query = db.Clans.AsNoTracking()
                    .Where(x => SqlFunctions.PatIndex(shortcut, x.Shortcut) > 0);
                var sql = query.ToQueryString();
                if (!sql.Contains("PATINDEX("))
                    throw new Exception("no PATINDEX in the generated SQL: " + sql);
                // IsBuiltIn(); without it EF Core emits [dbo].[PATINDEX], which SQL Server
                // rejects, and EF6 never did.
                if (sql.Contains("[PATINDEX]"))
                    throw new Exception("PATINDEX emitted as a user function: " + sql);
                return query.Count() + " clan(s) match, from SQL that SQL Server accepted";
            });

            // The five many-to-many joins are the part of the model written by hand, so they
            // are the part most likely to be wrong. Each one is both translated and executed:
            // the generated SQL has to name the join table the database actually has, and SQL
            // Server has to accept the statement. That holds even where the fixture has no
            // rows - which is the case for all five, and is why the result says so rather
            // than reporting a pass.
            JoinTable("Clan.Events", "EventClan", failures, db.Clans.SelectMany(c => c.Events));
            JoinTable("Event.Accounts", "EventAccount", failures, db.Events.SelectMany(e => e.Accounts));
            JoinTable("Event.Factions", "EventFaction", failures, db.Events.SelectMany(e => e.Factions));
            JoinTable("Event.Planets", "EventPlanet", failures, db.Events.SelectMany(e => e.Planets));
            JoinTable("Event.SpringBattles", "EventSpringBattle", failures, db.Events.SelectMany(e => e.SpringBattles));

            Check("datetime round trip", failures, () =>
            {
                var span = db.SpringBattles.AsNoTracking()
                    .GroupBy(x => 1)
                    .Select(g => new { Min = g.Min(x => x.StartTime), Max = g.Max(x => x.StartTime) })
                    .FirstOrDefault();
                if (span == null)
                    throw new InvalidOperationException("no battles - nothing to round-trip a datetime through");
                if (span.Min.Year < 2000 || span.Max > DateTime.Now.AddDays(1))
                    throw new Exception("implausible range " + span.Min + " .. " + span.Max);
                return span.Min.ToString("yyyy-MM-dd") + " .. " + span.Max.ToString("yyyy-MM-dd");
            });

            Check("string facets read back (varchar and nvarchar)", failures, () =>
            {
                var account = db.Accounts.AsNoTracking().FirstOrDefault(a => a.Name != null);
                if (account == null)
                    throw new InvalidOperationException("no named account - no varchar or nvarchar facet was read back");
                return "account " + account.AccountID + " name length " + account.Name.Length;
            });

            Check("command timeout through DbCompat", failures, () =>
            {
                db.Database.SetCommandTimeoutCompat(240);
                return "set to " + db.Database.GetCommandTimeout();
            });
        }


        /// <summary>
        /// Translates and runs a many-to-many navigation, checking the SQL names the join
        /// table the database has rather than one EF Core invented.
        /// </summary>
        private static void JoinTable<T>(string navigation, string table, List<string> failures, IQueryable<T> query)
            where T : class
        {
            try
            {
                var sql = query.AsNoTracking().Take(ProbeRows).ToQueryString();
                if (!sql.Contains("[" + table + "]"))
                    throw new Exception("the SQL does not mention [" + table + "]");
                var rows = query.AsNoTracking().Take(ProbeRows).ToList().Count;
                Console.WriteLine("   " + (rows > 0 ? "ok    " : "empty ") + navigation + " through " + table +
                                  " - " + (rows > 0 ? rows + " rows" : "valid SQL, no rows in this fixture"));
                if (rows == 0) Unexercised.Add(navigation);
            }
            catch (Exception ex)
            {
                Console.WriteLine("   FAIL  " + navigation + " through " + table);
                failures.Add(navigation + ": " + Innermost(ex));
            }
        }

        private static readonly List<string> Unexercised = new List<string>();

        private static void Check(string name, List<string> failures, Func<string> body)
        {
            try
            {
                Console.WriteLine("   ok    " + name + " - " + body());
            }
            catch (Exception ex)
            {
                Console.WriteLine("   FAIL  " + name);
                failures.Add(name + ": " + Innermost(ex));
            }
        }

        private static readonly MethodInfo ProbeMethod =
            typeof(ReadVerification).GetMethod(nameof(Probe), BindingFlags.NonPublic | BindingFlags.Static);

        private static int Probe<T>(ZkDataContext db) where T : class
            => db.Set<T>().AsNoTracking().Take(ProbeRows).ToList().Count;

        private static string Innermost(Exception ex)
        {
            while (ex is TargetInvocationException && ex.InnerException != null) ex = ex.InnerException;
            var message = ex.Message;
            for (var inner = ex.InnerException; inner != null; inner = inner.InnerException)
                message = inner.Message;
            return message.Replace("\r", " ").Replace("\n", " ");
        }
    }
}
