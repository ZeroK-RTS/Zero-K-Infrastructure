using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using LobbyClient;
using ZkData;

namespace ZkLobbyServer
{
    public class Server
    {
        SharedServerState sharedState = new SharedServerState();
        SelfUpdater selfUpdater = new SelfUpdater("ZkLobbyServer");

        public void Run()
        {
            selfUpdater.ProgramUpdated += s => {
                {
                    Task.WaitAll(sharedState.Clients.Values.Select((client) => client.SendCommand(new Say {
                        IsEmote = true,
                        Place = SayPlace.MessageBox,
                        Text = "Server self-updating to new version",
                        User = client.User.Name
                    })).ToArray());

                    Process.Start(s);
                    Environment.Exit(0);
                }
            };
#if !DEBUG
            if (!Debugger.IsAttached) selfUpdater.StartChecking();
#endif

            bool ok = false;
            TcpListener listener = null;
            do {
                try {
                    listener = new TcpListener(IPAddress.Any, GlobalConst.LobbyServerPort);
                    listener.Start(200);
                    ok = true;
                } catch (Exception ex) {
                    Trace.TraceError("Error binding:{0}",ex);
                    Thread.Sleep(1000);
                }
            } while (!ok);
            
            while (true)
            {
                var tcp = listener.AcceptTcpClient();
                Task.Run(() => {
                    var client = new Client(sharedState);
                    client.RunOnExistingTcp(tcp);
                });
                
            }
        }
    }
}