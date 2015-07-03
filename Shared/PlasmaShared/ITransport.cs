using System;
using System.Threading.Tasks;

namespace ZkData
{
    public interface ITransport
    {
        bool IsConnected { get; }
        Func<string, Task> OnCommandReceived { get; set; }
        Func<Task> OnConnected { get; set; }
        Func<bool, Task> OnConnectionClosed { get; set; }
        string RemoteEndpointAddress { get; }
        int RemoteEndpointPort { get; }
        void RequestClose();
        Task SendLine(string command);
    }
}