using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using PlasmaShared.LobbyMessages;
using ZkData;

namespace ZkLobbyServer
{
    public class Client: Connection
    {
        SharedServerState state;
        int number;
        string name;
        int accountID;

        public override string ToString()
        {
            return string.Format("[{0}:{1}:{2}]", number, name, accountID);
        }

        public Client(SharedServerState state)
        {
            this.state = state;
            number = Interlocked.Increment(ref state.ClientCounter);
            Trace.TraceInformation("{0} connected", this);
        }

        public override async Task OnConnectionClosed(bool wasRequested)
        {
            Trace.TraceInformation("{0} {1}", this, wasRequested ? "quit" : "connection failed");
        }

        public override async Task OnConnected()
        {
            await SendCommand(new Welcome() { Engine = state.Engine, Game = state.Game, Version = state.Version });
        }

        
        public override async Task OnLineReceived(string line)
        {
            dynamic obj = state.Serializer.DeserializeLine(line);
            await Process(obj);
        }

        public async Task SendCommand<T>(T data)
        {
            var line = state.Serializer.SerializeToLine(data);
            await SendData(Encoding.GetBytes(line));
        }



        async Task Process(Login login)
        {
            name = login.Name;
            Trace.TraceInformation("{0} logged in", this);

            await SendCommand(new LoginResponse() { });
        }

    }
}