#region using

using System.Diagnostics;
using PlasmaDownloader;

#endregion

namespace Springie
{
	public class MainConfig: IPlasmaDownloaderConfig
	{
		public const string SpringieVersion = "Springie 1.49.10";

		string executableName = "spring-dedicated.exe";
		bool gargamelMode = true;
		ProcessPriorityClass hostingProcessPriority = ProcessPriorityClass.AboveNormal;
		bool redirectGameChat = true;
		string serverHost = "springrts.com";
		int serverPort = 8200;
		int springCoreAffinity = 1;

		public string ExecutableName { get { return executableName; } set { executableName = value; } }

		public bool GargamelMode { get { return gargamelMode; } set { gargamelMode = value; } }
		public int HostingPortStart = 8452;

		public ProcessPriorityClass HostingProcessPriority { get { return hostingProcessPriority; } set { hostingProcessPriority = value; } }
		public string IpOverride;
		public int MaxInstances = 100;

		public bool RedirectGameChat { get { return redirectGameChat; } set { redirectGameChat = value; } }

		public string ServerHost { get { return serverHost; } set { serverHost = value; } }

		public int ServerPort { get { return serverPort; } set { serverPort = value; } }

		public int SpringCoreAffinity { get { return springCoreAffinity; } set { springCoreAffinity = value; } }
		public string SpringVersion { get; set; }

		public int RepoMasterRefresh { get { return 120; } }
		public string PackageMasterUrl { get { return "http://repos.caspring.org/"; } }
	} ;
}