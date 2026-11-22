using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace ZkData
{
    /// <summary>
    /// The LINQ-to-SQL-era helper names that entity code still uses, over EF Core's DbSet.
    ///
    /// ZkData/DbExtensions.cs has the same methods for EF6. It is not linked here because
    /// importing EF Core's namespace into every linked file makes the EF6 <c>[Index]</c>
    /// attribute ambiguous with EF Core's class-level one - so the methods are restated
    /// against EF Core types instead. Small enough to duplicate; the alternative was worse.
    /// </summary>
    public static class DbSetCompatExtensions
    {
        public static void DeleteAllOnSubmit<T>(this DbSet<T> dbSet, IEnumerable<T> toDelete) where T : class
        {
            foreach (var item in toDelete.ToList()) dbSet.Remove(item);
        }

        public static void InsertAllOnSubmit<T>(this DbSet<T> dbSet, IEnumerable<T> toAdd) where T : class
        {
            foreach (var item in toAdd.ToList()) dbSet.Add(item);
        }

        public static void InsertOnSubmit<T>(this DbSet<T> dbSet, T target) where T : class => dbSet.Add(target);

        public static void DeleteOnSubmit<T>(this DbSet<T> dbSet, T target) where T : class => dbSet.Remove(target);
    }
}
