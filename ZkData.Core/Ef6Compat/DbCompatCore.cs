using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ZkData
{
    /// <summary>
    /// The EF Core twin of ZkData/DbCompat.cs. Same method names, so the linked call sites
    /// compile against either model unchanged.
    /// </summary>
    public static class DbCompat
    {
        public static void SetCommandTimeoutCompat(this DatabaseFacade database, int seconds)
        {
            database.SetCommandTimeout(seconds);
        }

        /// <summary>
        /// EF6's Database.ExecuteSqlCommand. ExecuteSqlRaw takes the same {0} placeholders and
        /// parameterises them the same way, so the SQL that reaches the server is unchanged -
        /// which matters, because the one call site interpolates nothing and passes the id as a
        /// parameter precisely so it is not string-concatenated.
        /// </summary>
        public static int ExecuteSqlCommandCompat(this DatabaseFacade database, string sql, params object[] parameters)
        {
            return database.ExecuteSqlRaw(sql, parameters);
        }

        public static void MarkModified<T>(this ZkDataContext db, T entity) where T : class
        {
            db.Entry(entity).State = EntityState.Modified;
        }

        /// <summary>
        /// The EF Core half of the raw-SQL call. <c>FromSqlRaw</c> takes the same
        /// <c>{0}</c>-style placeholders as EF6's <c>SqlQuery</c> and parameterises them the same
        /// way, so the SQL string itself is unchanged.
        ///
        /// Two differences the caller does not see, and one it might:
        ///
        /// - EF Core requires the query to return every property the entity is mapped to;
        ///   EF6 tolerated a narrower result set. The one call site is a SELECT *, so it is not
        ///   affected, but a future SELECT of a few columns would compile and fail at runtime.
        /// - EF Core's FromSqlRaw is composable and EF6's SqlQuery is not, which is why this
        ///   returns a List: the narrower promise is the one both can keep.
        ///
        /// **This path is compile-verified only.** Nothing in this repository exercises
        /// LobbyController's chat history query - it wants a populated LobbyChatHistories table
        /// and a request context - so the claim here is that it compiles and that the SQL is
        /// byte-identical, not that it has been run on EF Core.
        /// </summary>
        public static List<T> SqlQueryCompat<T>(this DbSet<T> set, string sql, params object[] parameters)
            where T : class
        {
            return set.FromSqlRaw(sql, parameters).ToList();
        }
    }
}
