using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ZkData;

namespace ChobbyLauncher
{
    public class ChobbylaLocalListener
    {

        public async Task Listen()
        {
            var listener = new TcpListener(new IPEndPoint(IPAddress.Any,0));
            listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, new LingerOption(GlobalConst.TcpLingerStateEnabled, GlobalConst.TcpLingerStateSeconds));
            listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 0);

        }

    }
}
