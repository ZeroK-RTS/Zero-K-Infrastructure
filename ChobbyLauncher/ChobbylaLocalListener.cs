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
    public class Message
    {
        public string Text { get; set; }
    }

    public class ChobbylaLocalListener
    {
        private CommandJsonSerializer serializer;
        private TcpTransport transport;

        public ChobbylaLocalListener()
        {
            serializer = new CommandJsonSerializer(new List<Type> { typeof(Message) });
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
                transport = new TcpTransport(tcp);
                transport.ConnectAndRun(OnCommandReceived, OnConnected, OnConnectionClosed).Wait();
            });
            th.Start();
            return th;
        }

        public async Task Process(Message msg)
        {
            await SendCommand(msg);
        }

        public async Task SendCommand<T>(T data)
        {
            try
            {
                var line = serializer.SerializeToLine(data);
                await transport.SendLine(line);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Wrapper error sending {0} : {1}", data, ex);
            }
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

        private async Task OnConnected()
        {
            Trace.TraceInformation("Chobby connected to wrapper");
        }

        private async Task OnConnectionClosed(bool arg)
        {
            Trace.TraceInformation("Chobby closed, existing");
            Application.Exit();
        }
    }
}