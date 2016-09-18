using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ZkData;

namespace ZkLobbyServer
{
    public class TcpTransportServerListener: ITransportServerListener
    {
        TcpListener listener;


        public bool Bind(int retryCount)
        {
            listener = null;
            var ok = false;
            do {
                try {
                    listener = new TcpListener(new IPEndPoint(IPAddress.Any, GlobalConst.LobbyServerPort));
                    listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, new LingerOption(GlobalConst.TcpLingerStateEnabled, GlobalConst.TcpLingerStateSeconds));
                    listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 0);

                    listener.Start();
                    Trace.TraceInformation("Listening at port {0}", GlobalConst.LobbyServerPort);
                    ok = true;
                } catch (Exception ex) {
                    Trace.TraceError("Error binding port {1} :{0}", ex, GlobalConst.LobbyServerPort);
                    Thread.Sleep(1000);
                }
            } while (!ok && retryCount-- > 0);
            return ok;
        }

        public void RunLoop(Action<ITransport> onTransportAcccepted)
        {
            while (true) {
                var tcp = listener.AcceptTcpClient();
                Task.Run(() => {
                    var transport = new TcpTransport(tcp);
                    onTransportAcccepted(transport);
                });
            }
        }

        public void Stop()
        {
            try
            {
                listener.Server.Shutdown(SocketShutdown.Both);
                listener.Server.Disconnect(true);
                listener.Server.Close(0);
                listener.Stop();
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error closing server: {0}",ex);
            }
        }
    }
}