using System;
using System.Threading.Tasks;
using ZkData;

namespace ZkLobbyServer
{
    public interface ITransportServerListener {
        bool Bind(int retryCount);
        Task RunLoop(Action<ITransport> onTransportAcccepted);
    }
}