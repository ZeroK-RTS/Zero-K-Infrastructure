using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ZkData;

namespace ZkLobbyServer
{
    public class TcpTransportServerListener: ITransportServerListener
    {
        TcpListener listener;
        private bool stopped;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetHandleInformation(IntPtr hObject, HANDLE_FLAGS dwMask, HANDLE_FLAGS dwFlags);
    
        [Flags]
        enum HANDLE_FLAGS : uint
        {
            None = 0,
            INHERIT = 1,
            PROTECT_FROM_CLOSE = 2
        }
        
        public bool Bind(int retryCount)
        {
            listener = null;
            var ok = false;
            do {
                try {
                    listener = new TcpListener(new IPEndPoint(IPAddress.Any, GlobalConst.LobbyServerPort));
                    listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, new LingerOption(GlobalConst.TcpLingerStateEnabled, GlobalConst.TcpLingerStateSeconds));
                    listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 0);
                    if (!SetHandleInformation(listener.Server.Handle, HANDLE_FLAGS.INHERIT | HANDLE_FLAGS.PROTECT_FROM_CLOSE, 0)) throw new ApplicationException("Unable to set socket flags: " + Marshal.GetLastWin32Error());

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
            while (!stopped) {
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
                stopped = true;
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