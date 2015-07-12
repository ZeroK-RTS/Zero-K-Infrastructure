using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using LobbyClient;
using vtortola.WebSockets;
using ZkData;

namespace ZkLobbyServer
{
    public class Server
    {
        readonly SharedServerState sharedState;

        public Server(string geoIPpath)
        {
            sharedState = new SharedServerState(geoIPpath);
        }


        public void Run()
        {
            new Thread(() =>
            {
                SynchronizationContext.SetSynchronizationContext(null);

                var tasks = new List<Task>();

                var tcpServerListener = new TcpTransportServerListener();
                if (tcpServerListener.Bind(20)) tasks.Add(tcpServerListener.RunLoop((t) => { var client = new ClientConnection(t, sharedState); }));

                var wscServerListener = new WebSocketTransportServerListener();
                if (wscServerListener.Bind(20)) tasks.Add(wscServerListener.RunLoop((t) => { var client = new ClientConnection(t, sharedState); }));

                Task.WaitAll(tasks.ToArray());
            }).Start();
        }
    }
}