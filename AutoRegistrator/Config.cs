using PlasmaDownloader;

namespace ZeroKWeb
{
    public class Config: IPlasmaDownloaderConfig {
        public int RepoMasterRefresh { get { return 20; } }
        public string PackageMasterUrl { get { return " http://repos.springrts.com/"; } }
    }
}