using System;
using System.Threading.Tasks;
using ZkData;

namespace ZkLobbyServer
{
    public interface ITransportServerListener {
        bool Bind(int retryCount);
        void RunLoop(Action<ITransport> onTransportAcccepted);
    }
}