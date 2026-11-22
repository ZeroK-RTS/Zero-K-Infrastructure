using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZkData.Core.Ef6Compat;

namespace ZkData
{
    /// <summary>
    /// EF6's SaveChanges was never a plain save: it collected the pending changes, ran a
    /// hook on each one, saved, and ran a second hook. Three forum caches subscribe to the
    /// events and <see cref="Punishment"/> implements the interface, so dropping this in
    /// the port would not fail anywhere - it would quietly stop invalidating caches.
    ///
    /// The overrides are on the (bool) and (bool, CancellationToken) overloads rather than
    /// the parameterless ones, because that is where every EF Core save path converges;
    /// overriding SaveChanges() alone leaves SaveChanges(true) going straight past.
    /// </summary>
    public partial class ZkDataContext
    {
        public static event EventHandler<EntityEntry> BeforeEntityChange;
        public static event EventHandler<EntityEntry> AfterEntityChange;

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            var changes = GetChanges();
            RunBeforeEntityChangeEvents(changes);
            EntityValidation.Validate(this, changes);
            var ret = base.SaveChanges(acceptAllChangesOnSuccess);
            RunAfterEntityChangeEvents(changes);
            return ret;
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            var changes = GetChanges();
            RunBeforeEntityChangeEvents(changes);
            EntityValidation.Validate(this, changes);
            var ret = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            RunAfterEntityChangeEvents(changes);
            return ret;
        }

        /// <summary>
        /// The state is captured here, before the save, because afterwards every entry is
        /// Unchanged or Detached - an after-hook asking "was I inserted or deleted?" would
        /// get the same answer either way.
        /// </summary>
        private List<EntityEntry> GetChanges()
        {
            return ChangeTracker.Entries()
                .Where(x => x.State == EntityState.Modified
                            || x.State == EntityState.Added
                            || x.State == EntityState.Deleted)
                .Select(x => new EntityEntry(x.Entity, x.State, this))
                .ToList();
        }

        private void RunBeforeEntityChangeEvents(List<EntityEntry> changes)
        {
            foreach (var change in changes)
            {
                var ic = change.Entity as IEntityBeforeChange;
                ic?.BeforeChange(change);
                BeforeEntityChange?.Invoke(this, change);
            }
        }

        private void RunAfterEntityChangeEvents(List<EntityEntry> changes)
        {
            foreach (var change in changes)
            {
                var ic = change.Entity as IEntityAfterChange;
                ic?.AfterChange(change);
                AfterEntityChange?.Invoke(this, change);
            }
        }
    }
}
