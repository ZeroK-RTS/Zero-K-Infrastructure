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
        SharedServerState sharedState = new SharedServerState();
        SelfUpdater selfUpdater = new SelfUpdater("ZkLobbyServer");

        public void Run()
        {
            selfUpdater.ProgramUpdated += s => {
                {
                    Task.WaitAll(sharedState.ConnectedUsers.Values.Select((client) => client.SendCommand(new Say {
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

            var tcpServerListener = new TcpTransportServerListener();
            if (tcpServerListener.Bind(20)) {
                tcpServerListener.RunLoop((t) => { var client = new ClientConnection(t, sharedState); });
            }

            var wscServerListener = new WebSocketTransportServerListener();
            if (wscServerListener.Bind(20))
            {
                wscServerListener.RunLoop((t) => { var client = new ClientConnection(t, sharedState); });
            }

          
        }
    }
}