using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using LobbyClient;
using RestSharp.Extensions;
using ZkData;
using Timer = System.Timers.Timer;

namespace ZkLobbyServer
{
    public class ClientConnection:ICommandSender
    {
        string Name
        {
            get
            {
                if (connectedUser != null) return connectedUser.Name;
                else return null;
            }
        }
        ConnectedUser connectedUser;

        DateTime lastPingFromClient;
        readonly int number;

        readonly SharedServerState state;
        readonly Timer timer;

        ITransport transport;
        public string RemoteEndpointIP
        {
            get { return transport.RemoteEndpointAddress; }
        }


        public ClientConnection(ITransport transport, SharedServerState state)
        {
            this.state = state;
            number = Interlocked.Increment(ref state.ClientCounter);
            this.transport = transport;
            timer = new Timer(GlobalConst.LobbyProtocolPingInterval * 1000);
            timer.Elapsed += TimerOnElapsed;

            transport.ConnectAndRun(OnCommandReceived, OnConnected, OnConnectionClosed);
        }

        public async Task OnCommandReceived(string line)
        {
            try
            {
                dynamic obj = state.Serializer.DeserializeLine(line);
                if (obj is Ping || obj is Login || obj is Register) await Process(obj);
                else await connectedUser.Process(obj);
            }
            catch (Exception ex)
            {
                var message = string.Format("{0} error processing line {1} : {2}", this, line, ex);
                Trace.TraceError(message);
                SendCommand(new Say() { Place = SayPlace.MessageBox, Target = Name, User = Name, Text = message });
            }
        }

        public async Task OnConnected()
        {
            Trace.TraceInformation("{0} connected", this);
            await SendCommand(new Welcome() { Engine = state.Engine, Game = state.Game, Version = state.Version });
            lastPingFromClient = DateTime.UtcNow;
            timer.Start();
        }


        public async Task OnConnectionClosed(bool wasRequested)
        {
            timer.Stop();
            var reason = wasRequested ? "quit" : "connection failed";
            if (!string.IsNullOrEmpty(Name)) await connectedUser.RemoveConnection(this, reason);
            Trace.TraceInformation("{0} {1}", this, reason);
        }


        public async Task Process(Login login)
        {
            var user = new User();
            var response = await Task.Run(() => state.LoginChecker.Login(user, login, this));
            if (response.ResultCode == LoginResponse.Code.Ok)
            {
                connectedUser = state.ConnectedUsers.GetOrAdd(user.Name, (n) => new ConnectedUser(state, user));
                connectedUser.Connections.TryAdd(this, true);
                connectedUser.User = user;

                Trace.TraceInformation("{0} login: {1}", this, response.ResultCode.Description());
                
                await connectedUser.Broadcast(state.ConnectedUsers.Values, connectedUser.User); // send self to all

                await SendCommand(response); // login accepted

                foreach (var c in state.ConnectedUsers.Values.Where(x => x != connectedUser)) await SendCommand(c.User); // send others to self

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

                        foreach (var u in b.Users.Values.Select(x => x.ToUpdateBattleStatus()).ToList()) await SendCommand(new JoinedBattle() { BattleID = b.BattleID, User = u.Name });
                    }
                }


                await state.OfflineMessageHandler.SendMissedMessages(this, SayPlace.User, Name, user.AccountID);
            }
            else
            {
                await SendCommand(response);
                if (response.ResultCode == LoginResponse.Code.Banned) transport.RequestClose();
            }
        }



        public async Task Process(Register register)
        {
            var response = new RegisterResponse();
            if (!Utils.IsValidLobbyName(register.Name) || string.IsNullOrEmpty(register.PasswordHash)) response.ResultCode = RegisterResponse.Code.InvalidCharacters;
            else if (state.ConnectedUsers.ContainsKey(register.Name)) response.ResultCode = RegisterResponse.Code.AlreadyConnected;
            else
            {
                await Task.Run(() =>
                {
                    using (var db = new ZkDataContext())
                    {
                        var acc = db.Accounts.FirstOrDefault(x => x.Name == register.Name);
                        if (acc != null) response.ResultCode = RegisterResponse.Code.InvalidName;
                        else
                        {
                            if (string.IsNullOrEmpty(register.PasswordHash)) response.ResultCode = RegisterResponse.Code.InvalidPassword;
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

        public void RequestClose()
        {
            transport.RequestClose();
        }

        public async Task SendCommand<T>(T data)
        {
            try
            {
                var line = state.Serializer.SerializeToLine(data);
                await SendLine(line);
            }
            catch (Exception ex)
            {
                Trace.TraceError("{0} error sending {1} : {2}", data, ex);
            }
        }


        public async Task SendLine(string line)
        {
            try
            {
                await transport.SendLine(line);
            }
            catch (Exception ex)
            {
                Trace.TraceError("{0} error sending {1} : {2}", line, ex);
            }
        }


        public override string ToString()
        {
            return string.Format("[{0} {1}:{2} {3}]", number, transport.RemoteEndpointAddress, transport.RemoteEndpointPort, Name);
        }

        void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            if (DateTime.UtcNow.Subtract(lastPingFromClient).TotalSeconds >= GlobalConst.LobbyProtocolPingTimeout) transport.RequestClose();
            else SendCommand(new Ping() { });
        }
    }
}