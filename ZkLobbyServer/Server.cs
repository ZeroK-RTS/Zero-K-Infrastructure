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
            var tcpServerListener = new TcpTransportServerListener();
            if (tcpServerListener.Bind(10)) {
                var thread = new Thread(() => {
                    SynchronizationContext.SetSynchronizationContext(null);
                    tcpServerListener.RunLoop((t) => { var client = new ClientConnection(t, sharedState); });
                });
                thread.Start();
                thread.Priority = ThreadPriority.AboveNormal;
            }

            var wscServerListener = new WebSocketTransportServerListener();
            if (wscServerListener.Bind(10)) {
                var thread = new Thread(() => {
                    SynchronizationContext.SetSynchronizationContext(null);
                    wscServerListener.RunLoop((t) => { var client = new ClientConnection(t, sharedState); });
                });
                thread.Start();
                thread.Priority = ThreadPriority.AboveNormal;
            }
        }
    }
}