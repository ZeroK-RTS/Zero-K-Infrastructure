using System;
using System.Collections.Generic;
using IndexAttribute = System.ComponentModel.DataAnnotations.Schema.IndexAttribute;  // not EF Core's class-level one
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace ZkData.Core.Ef6Compat
{
    /// <summary>
    /// Turns the EF6 <c>[Index]</c> attributes on entity properties into EF Core indexes.
    ///
    /// EF6 applied this attribute by convention; EF Core has no such convention, so it has
    /// to be done explicitly. Composite indexes are expressed in EF6 by repeating a name
    /// across properties and ordering them with <c>Order</c> - that is reassembled here.
    /// </summary>
    public static class IndexConventions
    {
        public static void ApplyEf6IndexAttributes(this ModelBuilder modelBuilder)
        {
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                var clrType = entity.ClrType;
                if (clrType == null) continue;

                // EF Core materialises many-to-many join tables as shared-type entities
                // over Dictionary<string, object>. They carry no [Index] attributes, and
                // asking modelBuilder.Entity() for them throws.
                if (entity.HasSharedClrType) continue;

                // name -> the properties carrying that index, with their declared order
                var named = new Dictionary<string, List<Tuple<int, string, IndexAttribute>>>();
                var unnamed = new List<Tuple<string, IndexAttribute>>();

                foreach (var property in clrType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    // only properties EF Core actually maps can be indexed
                    if (entity.FindProperty(property.Name) == null) continue;

                    foreach (var attribute in property.GetCustomAttributes<IndexAttribute>(inherit: false))
                    {
                        if (string.IsNullOrEmpty(attribute.Name)) unnamed.Add(Tuple.Create(property.Name, attribute));
                        else
                        {
                            if (!named.TryGetValue(attribute.Name, out var members))
                                named[attribute.Name] = members = new List<Tuple<int, string, IndexAttribute>>();
                            members.Add(Tuple.Create(attribute.Order, property.Name, attribute));
                        }
                    }
                }

                var builder = modelBuilder.Entity(clrType);

                foreach (var single in unnamed)
                {
                    var index = builder.HasIndex(single.Item1);
                    // The filter, if any, is set from db/schema/schema.txt afterwards -
                    // EF Core's default is right for some of these indexes and wrong for
                    // others, so it is not a decision to make here.
                    if (single.Item2.IsUnique) index.IsUnique();
                }

                foreach (var group in named)
                {
                    var columns = group.Value.OrderBy(x => x.Item1).Select(x => x.Item2).ToArray();
                    var index = builder.HasIndex(columns).HasDatabaseName(group.Key);
                    if (group.Value.Any(x => x.Item3.IsUnique)) index.IsUnique();
                }
            }
        }
    }
}
