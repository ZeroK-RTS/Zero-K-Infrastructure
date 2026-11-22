using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ZkData.Core.Ef6Compat
{
    /// <summary>
    /// EF6 validated the data annotations on every entity it was about to save and threw
    /// DbEntityValidationException; ZkDataContext turned that into an ApplicationException
    /// carrying a readable list of what was wrong. EF Core does not validate at all - it
    /// sends the row and lets SQL Server object.
    ///
    /// It is tempting to argue that the database catches this anyway. Mostly it does, and
    /// better than I expected: SQL Server 2022 answers an over-long value with "String or
    /// binary data would be truncated in table 'dbo.Resources', column 'InternalName'.
    /// Truncated value: 'xxx...'", naming the table, the column and the value.
    ///
    /// Two things it does not catch, both measured by `ZkData.Core -- write`:
    ///
    /// - **A column wider than its annotation.** `Accounts.Name` is `varchar(2000)` while
    ///   `[StringLength(200)]` says 200 - the schema drifted past the model years ago. A
    ///   201-character name is simply stored. No error, no truncation, no sign. It is one
    ///   column, and it is the one holding every player's name.
    /// - **`[Required]` on a string rejects null *and* the empty string**; the column's
    ///   NOT NULL only rejects null. 39 mapped string properties are `[Required]`, so an
    ///   empty forum title or clan name that EF6 refused would now be written.
    ///
    /// So the behaviour is reproduced rather than dropped, deliberately and with one known
    /// narrowing: only MAPPED properties are validated. EF6 validated what it mapped, and
    /// Validator.TryValidateObject would also walk [NotMapped] properties and computed
    /// ones, which would reject entities EF6 accepted.
    ///
    /// EF6's own async path re-threw the raw DbEntityValidationException instead of the
    /// ApplicationException the sync path threw. That looks like an oversight rather than a
    /// decision, and both paths here throw the same thing.
    /// </summary>
    public static class EntityValidation
    {
        public static void Validate(ZkDataContext db, IEnumerable<ZkDataContext.EntityEntry> changes)
        {
            StringBuilder message = null;

            foreach (var change in changes)
            {
                // EF6 did not validate what it was about to delete, and neither does this:
                // a row on its way out does not have to be well formed.
                if (change.State == EntityState.Deleted) continue;

                var entry = db.Entry(change.Entity);

                // entry.Metadata.ClrType, not change.Entity.GetType(). With lazy-loading proxies
                // enabled the runtime type is a generated subclass - Castle.Proxies.AccountProxy -
                // whose overriding properties do not carry the base class's [StringLength] and
                // [Required] attributes, so reflecting on it finds nothing to validate and every
                // value passes. EF's metadata gives the real entity type either way.
                var clrType = entry.Metadata.ClrType;

                foreach (var validated in ValidatedProperties(db, clrType))
                {
                    var value = entry.Property(validated.Name).CurrentValue;
                    var context = new ValidationContext(change.Entity)
                    {
                        MemberName = validated.Name,
                        DisplayName = validated.Name,
                    };

                    var results = new List<ValidationResult>();
                    if (Validator.TryValidateValue(value, context, results, validated.Attributes)) continue;

                    if (message == null) message = new StringBuilder();
                    message.AppendFormat("Entity of type \"{0}\" in state \"{1}\" has the following validation errors:\n",
                        clrType.Name, change.State);
                    foreach (var result in results)
                        message.AppendFormat("- Property: \"{0}\", Value: \"{1}\", Error: \"{2}\"\n",
                            validated.Name, value, result.ErrorMessage);
                }
            }

            if (message != null) throw new ApplicationException(message.ToString());
        }

        private sealed class ValidatedProperty
        {
            public string Name;
            public ValidationAttribute[] Attributes;
        }

        // SaveChanges is hot, and reflecting over every property of every changed entity on
        // every save is not. The mapped properties that carry validation attributes are a
        // fixed fact about the model, so they are worked out once per type.
        private static readonly ConcurrentDictionary<Type, ValidatedProperty[]> Cache =
            new ConcurrentDictionary<Type, ValidatedProperty[]>();

        private static ValidatedProperty[] ValidatedProperties(ZkDataContext db, Type clrType)
        {
            return Cache.GetOrAdd(clrType, type =>
            {
                var entityType = db.Model.FindEntityType(type);
                if (entityType == null) return Array.Empty<ValidatedProperty>();

                return entityType.GetProperties()
                    .Select(p => new { p.Name, Info = p.PropertyInfo })
                    .Where(p => p.Info != null)   // shadow properties have nothing to validate
                    .Select(p => new ValidatedProperty
                    {
                        Name = p.Name,
                        Attributes = p.Info.GetCustomAttributes<ValidationAttribute>(true).ToArray(),
                    })
                    .Where(p => p.Attributes.Length > 0)
                    .ToArray();
            });
        }
    }
}
