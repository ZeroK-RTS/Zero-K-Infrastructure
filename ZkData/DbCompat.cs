using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace ZkData
{
    /// <summary>
    /// Call sites that EF6 and EF Core spell differently, behind one name.
    ///
    /// EF6 has <c>Database.CommandTimeout</c> as a settable property; EF Core has
    /// <c>SetCommandTimeout</c>. C# has no extension properties, so the call site has to
    /// move rather than the API - this is that move, made once, with an EF Core twin in
    /// ZkData.Core. Same for marking an entity modified.
    ///
    /// See ZkData/EFCORE-MIGRATION.md. When the port completes, the EF6 version goes and
    /// the EF Core one stays.
    /// </summary>
    public static class DbCompat
    {
        public static void SetCommandTimeoutCompat(this Database database, int seconds)
        {
            database.CommandTimeout = seconds;
        }

        /// <summary>
        /// EF6 spells it ExecuteSqlCommand, EF Core spells it ExecuteSqlRaw, and the
        /// parameter placeholders are the same {0} form in both. ResourceLinkProvider uses it
        /// to bump a download counter without loading the row.
        /// </summary>
        public static int ExecuteSqlCommandCompat(this Database database, string sql, params object[] parameters)
        {
            return database.ExecuteSqlCommand(sql, parameters);
        }

        public static void MarkModified<T>(this ZkDataContext db, T entity) where T : class
        {
            db.Entry(entity).State = EntityState.Modified;
        }

        /// <summary>
        /// A raw SQL query against an entity type. EF6 spells it DbSet.SqlQuery, EF Core spells
        /// it FromSqlRaw, and the two differ in more than the name - see the EF Core twin.
        ///
        /// Returns a materialised list rather than a query, because that is the only shape both
        /// can honestly promise: EF6's DbSqlQuery is enumerable but not composable, so anything
        /// this returned that looked composable would be a lie on one side.
        /// </summary>
        public static List<T> SqlQueryCompat<T>(this DbSet<T> set, string sql, params object[] parameters)
            where T : class
        {
            return set.SqlQuery(sql, parameters).ToList();
        }
    }
}
