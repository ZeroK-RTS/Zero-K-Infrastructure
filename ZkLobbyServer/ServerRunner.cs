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
        public ZkLobbyServer ZkLobbyServer { get; private set; }
        

        List<Thread> listenThreads = new List<Thread>();
        List<ITransportServerListener> listeners = new List<ITransportServerListener>();

        public ServerRunner(string geoIPpath)
        {
            ZkLobbyServer = new ZkLobbyServer(geoIPpath);
        }



        public void Run()
        {
            SynchronizationContext.SetSynchronizationContext(null);

            listeners.Add(new TcpTransportServerListener());
            //listeners.Add(new WebSocketTransportServerListener());

            foreach (var listener in listeners) {
                if (listener.Bind(20)) {
                    ITransportServerListener l = listener;
                    var thread = new Thread(() =>
                    {
                        SynchronizationContext.SetSynchronizationContext(null);
                        l.RunLoop((t) => { var client = new ClientConnection(t, ZkLobbyServer); });
                    });
                    listenThreads.Add(thread);
                    thread.Start();
                    thread.Priority = ThreadPriority.AboveNormal;                    
                }
            }
        }

        public void Stop()
        {
            foreach (var l in listeners) {
                try {
                    l.Stop();
                }
                catch { }
            }

            ZkLobbyServer.MarkDisconnectAll();

            /*foreach (var t in listenThreads) {
                try {
                    t.Abort();
                }
                catch { }
            }*/

        }
    }
}