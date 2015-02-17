namespace PlasmaDownloader
{
    public interface IPlasmaDownloaderConfig
    {
        string PackageMasterUrl { get; }
        int RepoMasterRefresh { get; }
    }
}