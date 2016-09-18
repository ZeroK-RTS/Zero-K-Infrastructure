using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LobbyClient;
using ZkData;

namespace ChobbyLauncher
{
    public class ChobbylaLocalListener
    {
        private CommandJsonSerializer serializer;

        public class DummyMessage {}

        public async Task Listen(TcpListener listener)
        {
            var tcp = await listener.AcceptTcpClientAsync();
            TcpTransport transport = new TcpTransport(tcp);
            await transport.ConnectAndRun(OnCommandReceived, OnConnected, OnConnectionClosed);

            serializer = new CommandJsonSerializer(new List<Type> { typeof(DummyMessage) });

        }

        public static TcpListener Init()
        {
            var listener = new TcpListener(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Server.SetSocketOption(SocketOptionLevel.Socket,
                SocketOptionName.Linger,
                new LingerOption(GlobalConst.TcpLingerStateEnabled, GlobalConst.TcpLingerStateSeconds));
            listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 0);
            listener.Start();

            return listener;
        }

        private async Task OnConnectionClosed(bool arg)
        {
            Trace.TraceInformation("Chobby closed, existing");
            Application.Exit();
        }

        private async Task OnConnected()
        {
            Trace.TraceInformation("Chobby connected to wrapper");
        }

        private async Task OnCommandReceived(string line)
        {
            try
            {
                dynamic obj = serializer.DeserializeLine(line);
                await Process(obj);
            }
            catch (Exception ex)
            {
                Trace.TraceError("{0} error processing line {1} : {2}", this, line, ex);
            }

        }

        public async Task Process(DummyMessage dummy)
        {
            
        }
    }
}
