namespace ZkData
{
    public interface IResourceInfo
    {
        string ArchiveName { get; set; }
        string Name { get; set; }
        string[] Dependencies { get; set; }
    }
}