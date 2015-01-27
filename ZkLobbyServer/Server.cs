using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using ZkData;

namespace ZkLobbyServer
{
    public class Server
    {
        SharedServerState sharedState = new SharedServerState();

        public async Task Run()
        {
            var listener = new TcpListener(IPAddress.Any, GlobalConst.LobbyServerPort);
            listener.Start();
            while (true)
            {
                var tcp = await listener.AcceptTcpClientAsync();
                var client = new Client(sharedState);
                client.RunOnExistingTcp(tcp);
            }
        }
    }
}