using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace ZkData.Core.Ef6Compat
{
    /// <summary>
    /// Declares composite primary keys that EF6 inferred from attributes.
    ///
    /// EF6 accepted several <c>[Key]</c> properties on one class and ordered them with
    /// <c>[Column(Order = n)]</c>. EF Core refuses that - it wants <c>[PrimaryKey]</c> on
    /// the class or <c>HasKey</c> here - and says so by throwing while building the model.
    /// This reads the EF6 spelling and issues the EF Core one, so the entity classes stay
    /// unmodified and the keys come out in the same column order.
    ///
    /// Column order matters: it decides the clustered index's column order, which the
    /// schema diff would otherwise flag.
    /// </summary>
    public static class CompositeKeyConventions
    {
        public static void ApplyEf6CompositeKeys(this ModelBuilder modelBuilder)
        {
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                if (entity.ClrType == null || entity.HasSharedClrType) continue;

                var keyProperties = entity.ClrType
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.GetCustomAttribute<KeyAttribute>() != null)
                    .ToList();
                if (keyProperties.Count < 2) continue;

                var ordered = keyProperties
                    .OrderBy(p => p.GetCustomAttribute<ColumnAttribute>()?.Order ?? int.MaxValue)
                    .ThenBy(p => p.Name)
                    .Select(p => p.Name)
                    .ToArray();

                modelBuilder.Entity(entity.ClrType).HasKey(ordered);
            }
        }
    }
}
