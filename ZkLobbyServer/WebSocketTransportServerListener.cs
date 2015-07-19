using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using vtortola.WebSockets;
using vtortola.WebSockets.Rfc6455;
using ZkData;

namespace ZkLobbyServer
{
    public class WebSocketTransportServerListener: ITransportServerListener
    {
        WebSocketListener listener;


        public bool Bind(int retryCount)
        {
            var ok = false;
            
            do {
                try {
                    listener = new WebSocketListener(new IPEndPoint(IPAddress.Any, GlobalConst.LobbyServerPort+1));
                    var rfc6455 = new WebSocketFactoryRfc6455(listener);
                    listener.Standards.RegisterStandard(rfc6455);
                    listener.Start();
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
            var token = new CancellationToken();
            while (true) {
                var wsc = listener.AcceptWebSocketAsync(token).Result;
                Task.Run(() => {
                    var transport = new WebSocketServerTransport(wsc);
                    onTransportAcccepted(transport);
                });
            }
        }

        public void Stop()
        {
            listener.Stop();
        }
    }
}