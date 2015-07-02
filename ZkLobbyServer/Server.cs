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
        readonly SelfUpdater selfUpdater = new SelfUpdater("ZkLobbyServer");
        readonly SharedServerState sharedState = new SharedServerState();

        public async Task Run()
        {
            selfUpdater.ProgramUpdated += s => {
                {
                    Task.WaitAll(
                        sharedState.Clients.Values.Select(
                            (client) =>
                                client.SendCommand(new Say {
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

            var ok = false;
            var listener = new HttpListener();
            listener.Prefixes.Add(string.Format("http://localhost:{0}", GlobalConst.LobbyServerPort));

            do {
                try {
                    listener.Start();
                    ok = true;
                } catch (Exception ex) {
                    Trace.TraceError("Error binding:{0}", ex);
                    Thread.Sleep(1000);
                }
            } while (!ok);

            while (true) {
                var httpListenerContext = await listener.GetContextAsync();
                if (httpListenerContext.Request.IsWebSocketRequest) {
                    var webSocketContext = await httpListenerContext.AcceptWebSocketAsync(null);
                    var remoteIP = httpListenerContext.Request.RemoteEndPoint.Address.MapToIPv4().ToString();

                    var webSocket = webSocketContext.WebSocket;
                } else {
                    httpListenerContext.Response.StatusCode = 500;
                    httpListenerContext.Response.Close();
                }

                /*Task.Run(() => {
                    var client = new ClientConnection(sharedState);
                    client.RunOnExistingTcp(tcp);
                });*/
            }
        }
    }
}