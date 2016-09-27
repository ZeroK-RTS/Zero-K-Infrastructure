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
        private TcpTransport transport;

        public ChobbylaLocalListener()
        {
            serializer = new CommandJsonSerializer(Utils.GetAllTypesWithAttribute<ChobbyMessageAttribute>());
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
                transport.ConnectAndRun(OnCommandReceived, OnConnected, OnConnectionClosed);
            });
            th.Start();
            return th;
        }

        public async Task Process(OpenUrl args)
        {
            try
            {
                System.Diagnostics.Process.Start(args.Url);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error opening URL {0} : {1}", args.Url, ex);
            }
        }


        public async Task Process(Restart args)
        {
            try
            {
                System.Diagnostics.Process.Start(Application.ExecutablePath);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error restarting: {0}", ex);
            }
        }


        public async Task Process(OpenFolder args)
        {
            try
            {
                System.Diagnostics.Process.Start(args.Folder);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error opening folder {0} : {1}", args.Folder, ex);
            }
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
            Trace.TraceInformation("Chobby closed connection");
            //Application.Exit();
        }
    }
}