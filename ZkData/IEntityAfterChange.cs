using System.Data.Entity.Infrastructure;

namespace ZkData
{
    public interface IEntityAfterChange
    {
        void AfterChange(ZkDataContext.EntityEntry entry);
    }
}