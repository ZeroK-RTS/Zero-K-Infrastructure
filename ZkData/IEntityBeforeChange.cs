using System.Data.Entity.Infrastructure;

namespace ZkData
{
    public interface IEntityBeforeChange
    {
        void BeforeChange(DbEntityEntry entry);
    }
}