using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using ZkData;

namespace ZkLobbyServer
{
    public class Server
    {
        SharedServerState sharedState = new SharedServerState();

        public void Run()
        {
            var listener = new TcpListener(IPAddress.Any, GlobalConst.LobbyServerPort);
            listener.Start(200);
            while (true) {
                var tcp = listener.AcceptTcpClient();
                Task.Run(() => {
                    var client = new Client(sharedState);
                    client.RunOnExistingTcp(tcp);
                });
                
            }
        }
    }
}