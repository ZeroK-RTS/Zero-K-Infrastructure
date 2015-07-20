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
        

        List<Thread> listenThreads = new List<Thread>();
        List<ITransportServerListener> listeners = new List<ITransportServerListener>();

        public ServerRunner(string geoIPpath)
        {
            SharedState = new SharedServerState(geoIPpath);
        }



        public void Run()
        {
            SynchronizationContext.SetSynchronizationContext(null);

            listeners.Add(new TcpTransportServerListener());
            listeners.Add(new WebSocketTransportServerListener());

            foreach (var listener in listeners) {
                if (listener.Bind(20)) {
                    ITransportServerListener l = listener;
                    var thread = new Thread(() =>
                    {
                        SynchronizationContext.SetSynchronizationContext(null);
                        l.RunLoop((t) => { var client = new ClientConnection(t, SharedState); });
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

            /*foreach (var t in listenThreads) {
                try {
                    t.Abort();
                }
                catch { }
            }*/
            
        }
    }
}