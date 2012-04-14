#region using

using System.Diagnostics;
using PlasmaDownloader;

#endregion

namespace Springie
{
	public class MainConfig: IPlasmaDownloaderConfig
	{
		public const string SpringieVersion = "Springie 2.6.0";

		string executableName = "spring-dedicated.exe";
		bool gargamelMode = true;
		ProcessPriorityClass hostingProcessPriority = ProcessPriorityClass.AboveNormal;
		bool redirectGameChat = true;
		string serverHost = "lobby.springrts.com";
		int serverPort = 8200;
	    public string ClusterNode { get; set; }
        public string ExecutableName { get { return executableName; } set { executableName = value; } }
	    public bool UseHolePunching { get; set; }
	    public bool GargamelMode { get { return gargamelMode; } set { gargamelMode = value; } }
		public int HostingPortStart = 8452;
        public ProcessPriorityClass HostingProcessPriority { get { return hostingProcessPriority; } set { hostingProcessPriority = value; } }
		public string IpOverride;
        public int MaxInstances = 100;

        public int RestartCounter { get; set; }

	    public string DataDir { get; set; }

	    public bool RedirectGameChat { get { return redirectGameChat; } set { redirectGameChat = value; } }

		public string ServerHost { get { return serverHost; } set { serverHost = value; } }

		public int ServerPort { get { return serverPort; } set { serverPort = value; } }

		public string SpringVersion { get; set; }

		public int RepoMasterRefresh { get { return 120; } }
		public string PackageMasterUrl { get { return "http://repos.springrts.com/"; } }

	    public MainConfig() {
            ClusterNode = "omega";
        }
	} ;
}