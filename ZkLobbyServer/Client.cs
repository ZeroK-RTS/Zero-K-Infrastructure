using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LobbyClient;
using PlasmaShared.LobbyMessages;
using ZkData;

namespace ZkLobbyServer
{
    public class Client : Connection
    {
        SharedServerState state;
        int number;

        User User = new User();
        public int UserVersion; 
        
        public ConcurrentDictionary<string, int?> LastKnownUserVersions = new ConcurrentDictionary<string, int?>();
        

        public override string ToString()
        {
            return string.Format("[{0}:{1}]", number, User.Name);
        }

        public Client(SharedServerState state)
        {
            this.state = state;
            number = Interlocked.Increment(ref state.ClientCounter);
            Trace.TraceInformation("{0} connected", this);
        }

        public override async Task OnConnectionClosed(bool wasRequested)
        {
            if (!string.IsNullOrEmpty(User.Name))
            {
                Client client;
                state.Clients.TryRemove(User.Name, out client);
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
            var response = new LoginResponse();
            await Task.Run(async () => {
                using (var db = new ZkDataContext()) {
                    var acc = db.Accounts.Include(x=>x.Clan).Include(x=>x.Faction).FirstOrDefault(x => x.Name == login.Name);
                    if (acc == null) {
                        response.ResultCode = LoginResponse.Code.InvalidName;
                    } else {
                        if (!acc.VerifyPassword(login.PasswordHash)) {
                            response.ResultCode = LoginResponse.Code.InvalidPassword;
                        } else {
                            // TODO banhammer check
                            // TODO country check
                            if (!state.Clients.TryAdd(login.Name, this)) {
                                response.ResultCode = LoginResponse.Code.AlreadyConnected;
                            } else {
                                response.ResultCode = LoginResponse.Code.Ok;
                                // user.BanMute todo banmute


                                User.Name = acc.Name;
                                User.DisplayName = acc.SteamName;
                                User.Avatar = acc.Avatar;
                                User.SpringieLevel = acc.SpringieLevel;
                                User.Level = acc.Level;
                                User.EffectiveElo = (int)acc.EffectiveElo;
                                User.Effective1v1Elo = (int)acc.Effective1v1Elo;
                                User.SteamID = (ulong?)acc.SteamID;
                                User.IsAdmin = acc.IsZeroKAdmin;
                                User.IsBot = acc.IsBot;
                                User.Country = acc.Country;
                                User.ClientType = login.ClientType;
                                User.Faction = acc.Faction != null ? acc.Faction.Shortcut : null;
                                User.Clan = acc.Clan != null ? acc.Clan.Shortcut : null;
                                User.AccountID = acc.AccountID;

                                ClearMyLastKnownStateForOtherClients();
                                
                                /*foreach (var c in state.Clients.Values.ToList()) {
                                    await c.SendCommand(User);
                                    if (c != this) await SendCommand(c.User);
                                }*/
                            }
                        }
                    }
                }
            });
            
            Trace.TraceInformation("{0} login: {1}", this, response.ResultCode.Description());
            await SendCommand(response);
        }

        public void ClearMyLastKnownStateForOtherClients()
        {
            foreach (var c in state.Clients.Values.ToList()) c.LastKnownUserVersions[User.Name] = null;
        }

        private async Task SynchronizeUsers(params string[] names)
        {
            foreach (var n in names) {
                Client client;
                if (state.Clients.TryGetValue(n, out client)) {
                    int? lastKnownVersion;
                    LastKnownUserVersions.TryGetValue(n, out lastKnownVersion);
                    var version = client.UserVersion;
                    if (lastKnownVersion == null || lastKnownVersion != version) {
                        await SendCommand(client.User);
                        LastKnownUserVersions[n] = version;
                    }
                }
            }
        }


        async Task Process(Register register)
        {
            var response = new LoginResponse();
            if (state.Clients.ContainsKey(register.Name))
            {
                response.ResultCode = LoginResponse.Code.AlreadyConnected;
            }
            else {
                await Task.Run(() => {
                    using (var db = new ZkDataContext()) {
                        var acc = db.Accounts.FirstOrDefault(x => x.Name == register.Name);
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
            var channel = state.Rooms.GetOrAdd(joinChannel.Name, (n) => { return new Channel() { Name = joinChannel.Name, }; });
            if (channel.Password != joinChannel.Password) {
                await SendCommand(new JoinChannelResponse() { Success = false, Reason = "invalid password" });
            }

            channel.Users.Add(User.Name);
            await SynchronizeUsers(channel.Users.ToArray());
            await SendCommand(new JoinChannelResponse() { Success = true, Name = joinChannel.Name, Channel = channel });

            foreach (var u in channel.Users.Where(x => x != User.Name)) {
                Client client;
                if (state.Clients.TryGetValue(u, out client)) {
                    await client.SynchronizeUsers(User.Name);
                    await client.SendCommand(new ChannelUserAdded { ChannelName = channel.Name, UserName = User.Name });

                }
            }
        }


    }
}