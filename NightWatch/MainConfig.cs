using System.Linq;
using ZkData;

namespace CaTracker
{
    public class Config
    {
        string accountName = GlobalConst.NightwatchName;

        string accountPassword = new Secrets().GetNightwatchPassword();
        int attemptReconnectInterval = 60;
        bool attemptToRecconnect = true;
        string[] joinChannels = new[] { "main","zk" };

        string serverHost = GlobalConst.LobbyServerHost;

        int serverPort = GlobalConst.LobbyServerPort;
        public string AccountName { get { return accountName; } set { accountName = value; } }

        public string AccountPassword { get { return accountPassword; } set { accountPassword = value; } }
        public int AttemptReconnectInterval { get { return attemptReconnectInterval; } set { attemptReconnectInterval = value; } }
        public bool AttemptToRecconnect { get { return attemptToRecconnect; } set { attemptToRecconnect = value; } }

        

        public string[] JoinChannels { get { return joinChannels; } set { joinChannels = value; } }
        public string ServerHost { get { return serverHost; } set { serverHost = value; } }
        public int ServerPort { get { return serverPort; } set { serverPort = value; } }
    } ;
}