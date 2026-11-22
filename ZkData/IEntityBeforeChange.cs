
namespace ZkData
{
    public interface IEntityBeforeChange
    {
        void BeforeChange(ZkDataContext.EntityEntry entry);
    }
}