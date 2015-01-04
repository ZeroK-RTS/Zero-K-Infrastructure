#region using

using System.Diagnostics;
using System.Reflection;
using PlasmaDownloader;
using ZkData;

#endregion

namespace Springie
{
	public class MainConfig: IPlasmaDownloaderConfig
	{
		public static string SpringieVersion = "Springie " + Assembly.GetEntryAssembly().GetName().Version;
	    public const int MaxLockTime = 240;
	    public const int MapChangeDownloadWait = 120;


		string executableName = "spring-dedicated.exe";
		bool gargamelMode = true;
		ProcessPriorityClass hostingProcessPriority = ProcessPriorityClass.AboveNormal;
		bool redirectGameChat = true;
	    string serverHost = GlobalConst.LobbyServerHost;
	    int serverPort = GlobalConst.LobbyServerPort;
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

        //Note for testing, 
        //1) Set "ClusterNode" to "alpha"
        //2) Make sure AutoUpdater() wasn't run (in Program.cs),
        //3) Compile Springie & run Springie.exe
        //4) Login to server (as player) using Lobby, 
        //5) Join a room called "KingRaptor Secret Clubhouse",
        //6) Use "magic" for password.
	    public MainConfig()
	    {
	        ClusterNode = GlobalConst.SpringieNode;
	        serverHost = GlobalConst.LobbyServerHost;
	        serverPort = GlobalConst.LobbyServerPort;
	    }
	} ;
}