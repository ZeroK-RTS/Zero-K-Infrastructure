using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PlasmaShared.LobbyMessages;
using ZkData;

namespace ZkLobbyServer
{
    public class Client : Connection
    {
        SharedServerState state;
        int number;
        string name;
        int accountID;



        public override string ToString()
        {
            return string.Format("[{0}:{1}]", number, name);
        }

        public Client(SharedServerState state)
        {
            this.state = state;
            number = Interlocked.Increment(ref state.ClientCounter);
            Trace.TraceInformation("{0} connected", this);
        }

        public override async Task OnConnectionClosed(bool wasRequested)
        {
            if (!string.IsNullOrEmpty(name))
            {
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

        public Task SendCommand<T>(T data)
        {
            var line = state.Serializer.SerializeToLine(data);
            return SendData(Encoding.GetBytes(line));
        }



        async Task Process(Login login)
        {
            name = login.Name;

            var response = new LoginResponse();
            await Task.Run(() => {
                using (var db = new ZkDataContext()) {
                    var acc = db.Accounts.FirstOrDefault(x => x.Name == name);
                    if (acc == null) {
                        response.ResultCode = LoginResponse.Code.InvalidName;
                    } else {
                        if (!acc.VerifyPassword(login.PasswordHash)) {
                            response.ResultCode = LoginResponse.Code.InvalidPassword;
                        } else {
                            // TODO banhammer check
                            // TODO country check
                            if (!state.Clients.TryAdd(name, this)) {
                                response.ResultCode = LoginResponse.Code.AlreadyConnected;
                            } else {
                                response.ResultCode = LoginResponse.Code.Ok;
                            }
                        }
                    }
                }
            });
            
            Trace.TraceInformation("{0} login: {1}", this, response.ResultCode.Description());
            await SendCommand(response);
        }

        async Task Process(Register register)
        {
            var response = new LoginResponse();
            if (state.Clients.ContainsKey(name))
            {
                response.ResultCode = LoginResponse.Code.AlreadyConnected;
            }
            else {
                await Task.Run(() => {
                    using (var db = new ZkDataContext()) {
                        var acc = db.Accounts.FirstOrDefault(x => x.Name == name);
                        if (acc != null) {
                            response.ResultCode = LoginResponse.Code.InvalidName;
                        } else {
                            if (string.IsNullOrEmpty(register.PasswordHash)) {
                                response.ResultCode = LoginResponse.Code.InvalidPassword;
                            } else {
                                acc = new Account() { Name = register.Name };
                                acc.SetPasswordHashed(register.PasswordHash);
                                db.Accounts.Add(acc);
                                db.SaveChanges();

                                response.ResultCode = LoginResponse.Code.Ok;
                            }
                        }
                    }
                });
            }

            Trace.TraceInformation("{0} login: {1}", this, response.ResultCode.Description());
            await SendCommand(response);
        }


        async Task Process(JoinChannel joinChannel)
        {
            var roomDetail = state.Rooms.GetOrAdd(joinChannel.Name, (n) => { return new Channel() { Name = joinChannel.Name, }; });
            if (roomDetail.Password != joinChannel.Password) {
                await SendCommand(new JoinChannelResponse() { Success = false, Reason = "invalid password" });
            }

            roomDetail.Users.Add(name);
            await SendCommand(new JoinChannelResponse() { Success = true, Name = joinChannel.Name, Channel = roomDetail });
        }


    }
}