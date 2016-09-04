using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using LobbyClient;
using ZkData;

namespace ZkLobbyServer
{
    public class ServerRunner
    {
        public ZkLobbyServer ZkLobbyServer { get; private set; }
        

        List<Thread> listenThreads = new List<Thread>();
        List<ITransportServerListener> listeners = new List<ITransportServerListener>();

        public ServerRunner(string geoIPpath, IPlanetwarsEventCreator creator)
        {
            ZkLobbyServer = new ZkLobbyServer(geoIPpath, creator);
        }



        public void Run()
        {
            SynchronizationContext.SetSynchronizationContext(null);

            listeners.Add(new TcpTransportServerListener());

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
            Trace.TraceInformation("Stopping lobby server");
            foreach (var l in listeners) {
                try {
                    l.Stop();
                }
                catch { }
            }

            Trace.TraceInformation("Disconnecting clients");
            ZkLobbyServer.MarkDisconnectAll();


            Trace.TraceInformation("Killing threads");
            foreach (var t in listenThreads) {
                try {
                    t.Abort();
                }
                catch { }
            }

        }
    }
}