namespace ZkData
{
    public interface IResourceInfo
    {
        string ArchiveName { get; set; }
        int Checksum { get; set; }
        string Name { get; set; }
    }
}