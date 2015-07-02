using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using LobbyClient;
using ZkData;

namespace ZkLobbyServer
{
    public abstract class WebSocketServerConnection: Connection
    {
        

    }

    public class ClientConnection: TcpConnection
    {

        int number;
        
        SharedServerState state;
        Client client;

        DateTime lastPingFromClient;
        System.Timers.Timer timer;

        

        public ClientConnection(SharedServerState state)
        {
            this.state = state;
            number = Interlocked.Increment(ref state.ClientCounter);

            Trace.TraceInformation("{0} accepted", this);
            timer = new System.Timers.Timer(GlobalConst.LobbyProtocolPingInterval * 1000);
            timer.Elapsed += TimerOnElapsed;

        }

        void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            if (DateTime.UtcNow.Subtract(lastPingFromClient).TotalSeconds >= GlobalConst.LobbyProtocolPingTimeout)
            {
                RequestClose();
            }
            else
            {
                SendCommand(new Ping() { });
            }
        }


        public async Task Process(Login login)
        {
            var user = new User();
            var response = await Task.Run(() => state.LoginChecker.Login(user, login, this));
            if (response.ResultCode == LoginResponse.Code.Ok)
            {

                Trace.TraceInformation("{0} login: {1}", this, response.ResultCode.Description());
                
                client = state.Clients.GetOrAdd(user.Name, (n) => new Client(state, user));
                client.Connections.TryAdd(this, true);
                client.User = user;

                await client.Broadcast(state.Clients.Values, client.User); // send self to all
                
                await SendCommand(response); // login accepted

                foreach (var c in state.Clients.Values.Where(x=>x!=client)) await SendCommand(c.User); // send others to self
                
                foreach (var b in state.Battles.Values)
                {
                    if (b != null)
                    {
                        
                        await
                            SendCommand(new BattleAdded()
                            {
                                Header =
                                    new BattleHeader()
                                    {
                                        BattleID = b.BattleID,
                                        Engine = b.EngineVersion,
                                        Game = b.ModName,
                                        Founder = b.Founder.Name,
                                        Map = b.MapName,
                                        Ip = b.Ip,
                                        Port = b.HostPort,
                                        Title = b.Title,
                                        SpectatorCount = b.SpectatorCount,
                                        MaxPlayers = b.MaxPlayers,
                                        Password = b.Password != null ? "?" : null
                                    }
                            });

                        foreach (var u in b.Users.Values.Select(x => x.ToUpdateBattleStatus()).ToList())
                        {
                            await SendCommand(new JoinedBattle() { BattleID = b.BattleID, User = u.Name });
                        }
                    }
                }
            }
            else
            {
                await SendCommand(response);
                if (response.ResultCode == LoginResponse.Code.Banned) RequestClose();
            }


        }


        public async Task Process(Register register)
        {
            var response = new RegisterResponse();
            if (!Utils.IsValidLobbyName(register.Name) || string.IsNullOrEmpty(register.PasswordHash))
            {
                response.ResultCode = RegisterResponse.Code.InvalidCharacters;
            }
            else if (state.Clients.ContainsKey(register.Name))
            {
                response.ResultCode = RegisterResponse.Code.AlreadyConnected;
            }
            else
            {
                await Task.Run(() =>
                {
                    using (var db = new ZkDataContext())
                    {
                        var acc = db.Accounts.FirstOrDefault(x => x.Name == register.Name);
                        if (acc != null)
                        {
                            response.ResultCode = RegisterResponse.Code.InvalidName;
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(register.PasswordHash))
                            {
                                response.ResultCode = RegisterResponse.Code.InvalidPassword;
                            }
                            else
                            {
                                acc = new Account() { Name = register.Name };
                                acc.SetPasswordHashed(register.PasswordHash);
                                acc.SetName(register.Name);
                                acc.SetAvatar();
                                db.Accounts.Add(acc);
                                db.SaveChanges();

                                response.ResultCode = RegisterResponse.Code.Ok;
                            }
                        }
                    }
                });
            }

            Trace.TraceInformation("{0} login: {1}", this, response.ResultCode.Description());
            await SendCommand(response);
        }

        public async Task Process(Ping ping)
        {
            lastPingFromClient = DateTime.UtcNow;
        }

        string Name
        {
            get
            {
                if (client != null) return client.Name;
                else return null;
            }
        }

        public override async Task OnLineReceived(string line)
        {
            try
            {
                dynamic obj = state.Serializer.DeserializeLine(line);
                if (obj is Ping || obj is Login || obj is Register) {
                    await Process(obj);
                } else await client.Process(obj);
            }
            catch (Exception ex)
            {
                var message = string.Format("{0} error processing line {1} : {2}", this, line, ex);
                Trace.TraceError(message);
                SendCommand(new Say() { Place = SayPlace.MessageBox, Target = Name, User = Name, Text = message });
            }
        }

        public async Task SendCommand<T>(T data)
        {
            try
            {
                var line = state.Serializer.SerializeToLine(data);
                var bytes = Connection.Encoding.GetBytes(line);
                await SendData(bytes);
            }
            catch (Exception ex)
            {
                Trace.TraceError("{0} error sending {1} : {2}", data, ex);
            }
        }





        public override async Task OnConnected()
        {
            Trace.TraceInformation("{0} connected", this);
            await SendCommand(new Welcome() { Engine = state.Engine, Game = state.Game, Version = state.Version });
            lastPingFromClient = DateTime.UtcNow;
            timer.Start();
        }



        public override async Task OnConnectionClosed(bool wasRequested)
        {
            timer.Stop();
            string reason = wasRequested ? "quit" : "connection failed";
            if (!string.IsNullOrEmpty(Name)) {
                await client.RemoveConnection(this, reason);
            }
            Trace.TraceInformation("{0} {1}", this, reason);
        }


      
        public override string ToString()
        {
            return string.Format("[{0} {1}:{2} {3}]", number, RemoteEndpointIP, RemoteEndpointPort, Name);
        }

    }
}
