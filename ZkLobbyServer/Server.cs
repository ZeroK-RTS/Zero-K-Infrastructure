using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

            var listener = new TcpListener(IPAddress.Any, GlobalConst.LobbyServerPort);
            listener.Start(200);
            while (true) {
                var tcp = listener.AcceptTcpClient();
                Task.Run(() => {
                    var client = new Client(sharedState);
                    client.RunOnExistingTcp(tcp);
                });
                
            }
        }
    }
}