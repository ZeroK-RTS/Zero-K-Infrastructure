using System;
using System.Linq;
using System.Linq.Expressions;
using EFCore = Microsoft.EntityFrameworkCore;

namespace System.Data.Entity
{
    /// <summary>
    /// Makes <c>using System.Data.Entity;</c> resolve on .NET 9, mapping the handful of
    /// EF6 APIs the entity classes use onto their EF Core equivalents.
    ///
    /// Same reasoning as the <c>[Index]</c> shim: EF6 still runs production, so the entity
    /// sources cannot be rewritten yet, and they are linked into both models unmodified so
    /// that a schema difference means a real difference. The surface needed turns out to be
    /// tiny - <c>Include</c>, <c>AsNoTracking</c> and <c>EntityState</c> - because the
    /// entity classes do very little data access themselves.
    ///
    /// This is scaffolding with an expiry date. When the entity sources move to EF Core
    /// for good, their usings change and this file goes.
    /// </summary>
    public static class QueryableExtensions
    {
        public static IQueryable<T> Include<T, TProperty>(this IQueryable<T> source,
            Expression<Func<T, TProperty>> path) where T : class
            => EFCore.EntityFrameworkQueryableExtensions.Include(source, path);

        public static IQueryable<T> Include<T>(this IQueryable<T> source, string path) where T : class
            => EFCore.EntityFrameworkQueryableExtensions.Include(source, path);

        public static IQueryable<T> AsNoTracking<T>(this IQueryable<T> source) where T : class
            => EFCore.EntityFrameworkQueryableExtensions.AsNoTracking(source);
    }

}
