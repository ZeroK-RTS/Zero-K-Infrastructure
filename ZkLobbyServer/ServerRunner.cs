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
    public class ServerRunner
    {
        public SharedServerState SharedState { get; private set; }
        

        public ServerRunner(string geoIPpath)
        {
            SharedState = new SharedServerState(geoIPpath);
        }


        public void Run()
        {
            SynchronizationContext.SetSynchronizationContext(null);

            var tcpServerListener = new TcpTransportServerListener();
            if (tcpServerListener.Bind(20)) {
                var thread = new Thread(() => {
                    SynchronizationContext.SetSynchronizationContext(null);
                    tcpServerListener.RunLoop((t) => { var client = new ClientConnection(t, SharedState); });
                });
                thread.Start();
                thread.Priority = ThreadPriority.AboveNormal;
            }

            var wscServerListener = new WebSocketTransportServerListener();
            if (wscServerListener.Bind(20)) {
                var thread = new Thread(() => {
                    SynchronizationContext.SetSynchronizationContext(null);
                    wscServerListener.RunLoop((t) => { var client = new ClientConnection(t, SharedState); });
                });
                thread.Start();
                thread.Priority = ThreadPriority.AboveNormal;
            }
        }
    }
}