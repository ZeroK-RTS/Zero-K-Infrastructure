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

        public TcpTransportServerListener()
        {
            
        }

        public bool Bind(int retryCount)
        {
            listener = null;
            bool ok = false;
            do
            {
                try
                {
                    listener = new TcpListener(new IPEndPoint(IPAddress.Any, GlobalConst.LobbyServerPort));
                    listener.Start(200);
                    ok = true;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error binding:{0}", ex);
                    Thread.Sleep(1000);
                }
            } while (!ok && retryCount-- > 0);
            return ok;
        }

        public async Task RunLoop(Action<ITransport> onTransportAcccepted)
        {
            while (true)
            {
                var tcp = await listener.AcceptTcpClientAsync();
                Task.Run(() =>
                {
                    var transport = new TcpTransport();
                    onTransportAcccepted(transport);
                    transport.RunOnExistingTcp(tcp);
                });
            }
        }
    }
}