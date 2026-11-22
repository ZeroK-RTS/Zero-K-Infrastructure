using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ZkData;

namespace ZkData.Core
{
    /// <summary>
    /// Writes through the EF Core model, and checks that saving still does what EF6's
    /// SaveChanges did rather than only what EF Core's does.
    ///
    ///     ZK_CONNECTION_STRING=...zk_test... dotnet run -- write
    ///
    /// Everything happens inside one transaction that is always rolled back, so the
    /// committed fixture is unchanged afterwards and the harness can run twice in a row -
    /// which the last check confirms rather than assumes.
    /// </summary>
    public static class WriteVerification
    {
        public static int Run(ZkDataContext db)
        {
            var failures = new List<string>();
            var before = db.Accounts.AsNoTracking().Count();

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    Insert(db, failures);
                    Update(db, failures);
                    MarkModified(db, failures);
                    Hooks(db, failures);
                    Validation(db, failures);
                    Delete(db, failures);
                }
                finally
                {
                    transaction.Rollback();
                }
            }

            db.ChangeTracker.Clear();
            var after = db.Accounts.AsNoTracking().Count();
            Check("the fixture is left as it was found", failures, () =>
            {
                if (before != after) throw new Exception(before + " accounts before, " + after + " after");
                return before + " accounts, unchanged";
            });

            ReportUnguardedColumns(db);

            Console.WriteLine();
            if (failures.Count == 0)
            {
                Console.WriteLine("the EF Core model writes to this database.");
                return 0;
            }
            foreach (var failure in failures) Console.WriteLine("   FAILED " + failure);
            Console.WriteLine(failures.Count + " failure(s) - the model does not write to this database yet.");
            return 1;
        }


        /// <summary>
        /// What the database would NOT catch on its own, so the size of what the validation
        /// shim is actually carrying.
        ///
        /// Two classes. A column wider than its [StringLength] takes the over-long value
        /// silently - no error at all. And a [Required] string rejects null AND empty,
        /// where the column's NOT NULL only rejects null, so an empty string that EF6
        /// refused would be stored.
        /// </summary>
        private static void ReportUnguardedColumns(ZkDataContext db)
        {
            var wider = new List<string>();
            var required = 0;
            foreach (var entity in db.Model.GetEntityTypes())
            {
                foreach (var property in entity.GetProperties())
                {
                    if (property.ClrType != typeof(string) || property.PropertyInfo == null) continue;

                    if (property.PropertyInfo
                        .GetCustomAttributes<System.ComponentModel.DataAnnotations.RequiredAttribute>(true)
                        .Any(a => !a.AllowEmptyStrings)) required++;

                    var annotated = property.PropertyInfo
                        .GetCustomAttributes<System.ComponentModel.DataAnnotations.StringLengthAttribute>(true)
                        .Select(a => (int?)a.MaximumLength).FirstOrDefault();
                    if (annotated == null) continue;
                    var column = property.GetMaxLength();
                    if (column == null || column > annotated)
                        wider.Add(entity.GetTableName() + "." + property.Name + " is " +
                                  (column == null ? "max" : column.ToString()) + " but [StringLength(" + annotated + ")]");
                }
            }

            Console.WriteLine();
            Console.WriteLine("   what the database would not catch by itself, and validation does:");
            Console.WriteLine("     " + wider.Count + " column(s) wider than the annotation - the over-long value is stored, silently");
            foreach (var column in wider.Take(10)) Console.WriteLine("       " + column);
            if (wider.Count > 10) Console.WriteLine("       ... and " + (wider.Count - 10) + " more");
            Console.WriteLine("     " + required + " [Required] string properties - NOT NULL rejects null, not the empty string");
        }

        // Built through SetName rather than an initializer, because an initializer walks past
        // the one place a player name is validated - see tools/check-name-invariant.py. The probe
        // name is write_probe and not write_probe for the same reason: a hyphen is not a valid
        // lobby name character, so the old name could only exist by going round the door.
        private static Account NewAccount(string name)
        {
            var account = new Account
            {
                FirstLogin = new DateTime(2026, 1, 1),
                LastLogin = new DateTime(2026, 1, 1),
                LastLogout = new DateTime(2026, 1, 1),
                LastChatRead = new DateTime(2026, 1, 1),
            };
            account.SetName(name);
            return account;
        }

        private static void Insert(ZkDataContext db, List<string> failures)
        {
            Check("insert, with the identity value coming back", failures, () =>
            {
                var account = NewAccount("write_probe");
                db.Accounts.Add(account);
                db.SaveChanges();
                if (account.AccountID == 0) throw new Exception("AccountID was not populated");
                var round = db.Accounts.AsNoTracking().Single(a => a.AccountID == account.AccountID);
                if (round.Name != "write_probe") throw new Exception("read back as " + round.Name);
                return "AccountID " + account.AccountID + " assigned and read back";
            });
        }

        private static void Update(ZkDataContext db, List<string> failures)
        {
            Check("update", failures, () =>
            {
                var account = db.Accounts.Single(a => a.Name == "write_probe");
                account.Level = 42;
                db.SaveChanges();
                db.ChangeTracker.Clear();
                var round = db.Accounts.AsNoTracking().Single(a => a.Name == "write_probe");
                if (round.Level != 42) throw new Exception("Level came back as " + round.Level);
                return "Level 42 round-tripped";
            });
        }

        private static void MarkModified(ZkDataContext db, List<string> failures)
        {
            // The compat shim ZkData/DbCompat.cs and its EF Core twin: call sites use it to
            // save an entity the context is not tracking as changed.
            Check("MarkModified through DbCompat", failures, () =>
            {
                db.ChangeTracker.Clear();
                var account = db.Accounts.AsNoTracking().Single(a => a.Name == "write_probe");
                account.Level = 43;
                db.MarkModified(account);
                db.SaveChanges();
                db.ChangeTracker.Clear();
                var round = db.Accounts.AsNoTracking().Single(a => a.Name == "write_probe");
                if (round.Level != 43) throw new Exception("Level came back as " + round.Level);
                return "an untracked entity saved as modified";
            });
        }

        private static void Hooks(ZkDataContext db, List<string> failures)
        {
            // The interface path (IEntityBeforeChange / IEntityAfterChange) runs through the
            // same loop as the events, and its only implementor is Punishment, whose
            // AfterChange refreshes a static cache through a context of its own. That second
            // connection would block on this transaction's uncommitted rows, so the dispatch
            // is exercised through the events instead - same loop, no second connection.
            Check("before and after hooks fire, with the pre-save state", failures, () =>
            {
                var seen = new List<string>();
                EventHandler<ZkDataContext.EntityEntry> before = (s, e) => seen.Add("before:" + e.State);
                EventHandler<ZkDataContext.EntityEntry> after = (s, e) => seen.Add("after:" + e.State);
                ZkDataContext.BeforeEntityChange += before;
                ZkDataContext.AfterEntityChange += after;
                try
                {
                    db.ChangeTracker.Clear();
                    var account = db.Accounts.Single(a => a.Name == "write_probe");
                    account.Level = 44;
                    db.SaveChanges();
                }
                finally
                {
                    ZkDataContext.BeforeEntityChange -= before;
                    ZkDataContext.AfterEntityChange -= after;
                }

                var expected = new[] { "before:Modified", "after:Modified" };
                if (!seen.SequenceEqual(expected))
                    throw new Exception("saw [" + string.Join(", ", seen) + "], wanted [" + string.Join(", ", expected) + "]");
                return string.Join(" then ", seen);
            });
        }

        private static void Validation(ZkDataContext db, List<string> failures)
        {
            // Accounts.Name is deliberate: the column is varchar(2000) while the annotation
            // says 200, so without the shim this save SUCCEEDS and stores a name no EF6
            // build would have accepted. Measured, not assumed - see the report below.
            Check("an over-long value is refused before it reaches the database", failures, () =>
            {
                db.ChangeTracker.Clear();
                var account = db.Accounts.Single(a => a.Name == "write_probe");
                account.Name = new string('x', 201);   // [StringLength(200)]
                try
                {
                    db.SaveChanges();
                    throw new Exception("the save was allowed");
                }
                catch (ApplicationException ex)
                {
                    if (!ex.Message.Contains("\"Name\"")) throw new Exception("message does not name the property: " + ex.Message);
                    return ex.Message.Split('\n')[1].Trim();
                }
                finally
                {
                    db.ChangeTracker.Clear();
                }
            });
        }

        private static void Delete(ZkDataContext db, List<string> failures)
        {
            Check("delete", failures, () =>
            {
                db.ChangeTracker.Clear();
                var account = db.Accounts.Single(a => a.Name == "write_probe");
                db.Accounts.Remove(account);
                db.SaveChanges();
                db.ChangeTracker.Clear();
                if (db.Accounts.AsNoTracking().Any(a => a.Name == "write_probe")) throw new Exception("still there");
                return "the row is gone";
            });
        }

        private static void Check(string name, List<string> failures, Func<string> body)
        {
            try
            {
                Console.WriteLine("   ok    " + name + " - " + body());
            }
            catch (Exception ex)
            {
                Console.WriteLine("   FAIL  " + name);
                var message = ex.Message;
                for (var inner = ex.InnerException; inner != null; inner = inner.InnerException) message = inner.Message;
                failures.Add(name + ": " + message.Replace("\r", " ").Replace("\n", " "));
            }
        }
    }
}
