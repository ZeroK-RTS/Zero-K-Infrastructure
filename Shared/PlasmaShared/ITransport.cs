using System;
using System.Threading.Tasks;

namespace ZkData
{
    public interface ITransport
    {
        bool IsConnected { get; }
        Task ConnectAndRun(Func<string, Task> onLineReceived, Func<Task> onConnected, Func<bool, Task> onConnectionClosed);
        string RemoteEndpointAddress { get; }
        int RemoteEndpointPort { get; }
        void RequestClose();
        Task SendLine(string command);
    }
}