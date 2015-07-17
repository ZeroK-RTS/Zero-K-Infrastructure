using System.Threading.Tasks;

namespace ZkLobbyServer
{
    public interface ICommandSender
    {
        Task SendCommand<T>(T obj);
    }
}