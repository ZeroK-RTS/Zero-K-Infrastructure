using System;
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
                    listener.Start(1000);
                    ok = true;
                } catch (Exception ex) {
                    Trace.TraceError("Error binding:{0}", ex);
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
    }
}