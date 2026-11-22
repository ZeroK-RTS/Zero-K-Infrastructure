using System;
using Microsoft.EntityFrameworkCore;

namespace ZkData.Core.Ef6Compat
{
    /// <summary>
    /// Column facets where EF Core's defaults differ from EF6's.
    ///
    /// EF6 mapped <see cref="DateTime"/> to SQL Server's <c>datetime</c>; EF Core maps it to
    /// <c>datetime2</c>. Both store a date and a time, so nothing fails - the column type
    /// simply changes under an existing database, which is precisely the kind of difference
    /// a port is supposed to not make. 45 columns are affected.
    ///
    /// The range differs too: <c>datetime</c> starts at 1753 and <c>datetime2</c> at year 1,
    /// so moving to datetime2 would silently accept values the old schema rejected.
    /// </summary>
    public static class ColumnFacetConventions
    {
        public static void ApplyEf6DateTimeMapping(this ModelBuilder modelBuilder)
        {
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                if (entity.ClrType == null || entity.HasSharedClrType) continue;

                foreach (var property in entity.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                        property.SetColumnType("datetime");
                }
            }
        }
    }
}
