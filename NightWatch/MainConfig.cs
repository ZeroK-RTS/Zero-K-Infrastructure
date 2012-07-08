namespace CaTracker
{
    public class Config
    {
#if !DEPLOY
        string accountName = "NightwatchDev";
#else 
        string accountName = "Nightwatch";
#endif
        string accountPassword = "secret";
        int attemptReconnectInterval = 60;
        bool attemptToRecconnect = true;
        string[] joinChannels = new[] { "main","zk" };

        string serverHost = "springrts.com";

        int serverPort = 8200;
        public string AccountName { get { return accountName; } set { accountName = value; } }

        public string AccountPassword { get { return accountPassword; } set { accountPassword = value; } }
        public int AttemptReconnectInterval { get { return attemptReconnectInterval; } set { attemptReconnectInterval = value; } }
        public bool AttemptToRecconnect { get { return attemptToRecconnect; } set { attemptToRecconnect = value; } }

        

        public string[] JoinChannels { get { return joinChannels; } set { joinChannels = value; } }
        public string ServerHost { get { return serverHost; } set { serverHost = value; } }
        public int ServerPort { get { return serverPort; } set { serverPort = value; } }
    } ;
}