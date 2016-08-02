#region using

using System.Diagnostics;
using System.Reflection;
using PlasmaDownloader;
using ZkData;

#endregion

namespace Springie
{
	public class MainConfig
	{
		public static string SpringieVersion = "Springie " + Assembly.GetEntryAssembly()?.GetName().Version;


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
		public string IpOverride;
        public int MaxInstances = 100;

        public int RestartCounter { get; set; }

	    public string DataDir { get; set; }

	    public bool RedirectGameChat { get { return redirectGameChat; } set { redirectGameChat = value; } }

		public string ServerHost { get { return serverHost; } set { serverHost = value; } }

		public int ServerPort { get { return serverPort; } set { serverPort = value; } }

		public string SpringVersion { get; set; }

	    public MainConfig()
	    {
	        ClusterNode = GlobalConst.SpringieNode;
	        serverHost = GlobalConst.LobbyServerHost;
	        serverPort = GlobalConst.LobbyServerPort;
	    }
	} ;
}
