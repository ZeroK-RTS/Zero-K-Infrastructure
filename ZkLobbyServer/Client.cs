using System.Data.Entity;
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
            if (!string.IsNullOrEmpty(name)) {
                Client client;
                state.Clients.TryRemove(name, out client);
            }
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

            var response = new LoginResponse();
            if (!state.Clients.TryAdd(name, this)) {
                response.ResultCode = LoginResponse.Code.AlreadyConnected;
            } else {
                using (var db = new ZkDataContext()) {
                    var acc = await db.Accounts.FirstOrDefaultAsync(x => x.Name == name);
                    if (acc == null) {
                        response.ResultCode = LoginResponse.Code.InvalidName;
                    } else {
                        if (!acc.VerifyPassword(login.PasswordHash)) {
                            response.ResultCode = LoginResponse.Code.InvalidPassword;
                        } else {
                            // TODO banhammer check
                            response.ResultCode = LoginResponse.Code.Ok;
                        }
                    }
                }
            }

            Trace.TraceInformation("{0} login: {1}", this, response.ResultCode.Description());
            await SendCommand(response);
            if (response.ResultCode != LoginResponse.Code.Ok) RequestClose();
        }
        
    }
}