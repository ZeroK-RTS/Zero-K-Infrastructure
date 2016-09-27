using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using LobbyClient;
using ZkData;

namespace ChobbyLauncher
{
    public class ChobbylaLocalListener
    {
        private CommandJsonSerializer serializer;

        public ChobbylaLocalListener()
        {
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

        public Thread Listen(TcpListener listener)
        {
            var th = new Thread(() =>
            {
                SynchronizationContext.SetSynchronizationContext(null);
                var tcp = listener.AcceptTcpClient();
                var transport = new TcpTransport(tcp);
                transport.ConnectAndRun(OnCommandReceived, OnConnected, OnConnectionClosed);
            });
            th.Start();
            return th;
        }

        public async Task Process(DummyMessage dummy) { }

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

        private async Task OnConnected()
        {
            Trace.TraceInformation("Chobby connected to wrapper");
        }

        private async Task OnConnectionClosed(bool arg)
        {
            Trace.TraceInformation("Chobby closed, existing");
            Application.Exit();
        }

        public class DummyMessage { }
    }
}