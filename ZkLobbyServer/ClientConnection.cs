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
    public class ClientConnection : ICommandSender
    {
        string Name => connectedUser?.Name;
        ConnectedUser connectedUser;

        readonly int number;

        readonly ZkLobbyServer server;

        ITransport transport;

        public string RemoteEndpointIP => transport.RemoteEndpointAddress;


        public ClientConnection(ITransport transport, ZkLobbyServer server)
        {
            this.server = server;
            number = Interlocked.Increment(ref server.ClientCounter);
            this.transport = transport;

            transport.ConnectAndRun(OnCommandReceived, OnConnected, OnConnectionClosed).ConfigureAwait(false);
        }

        public async Task OnCommandReceived(string line)
        {
            try
            {
                dynamic obj = server.Serializer.DeserializeLine(line);
                if (obj is Login || obj is Register) await Process(obj);
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
            //Trace.TraceInformation("{0} connected", this);
            await SendCommand(new Welcome() { Engine = server.Engine, Game = server.Game, Version = server.Version, UserCount = server.ConnectedUsers.Count });
        }


        public async Task OnConnectionClosed(bool wasRequested)
        {
            var reason = wasRequested ? "quit" : "connection failed";
            if (!string.IsNullOrEmpty(Name)) await connectedUser.RemoveConnection(this, reason);
            //Trace.TraceInformation("{0} {1}", this, reason);
        }


        public async Task Process(Login login)
        {
            var ret = await Task.Run(() => server.LoginChecker.Login(login, RemoteEndpointIP));
            if (ret.LoginResponse.ResultCode == LoginResponse.Code.Ok)
            {
                var user = ret.User;
                //Trace.TraceInformation("{0} login: {1}", this, response.ResultCode.Description());

                await this.SendCommand(user); // send self to self first

                connectedUser = server.ConnectedUsers.GetOrAdd(user.Name, (n) => new ConnectedUser(server, user));
                connectedUser.User = user;
                connectedUser.Connections.TryAdd(this, true);

                server.SessionTokens[ret.LoginResponse.SessionToken] = user.AccountID;

                await SendCommand(ret.LoginResponse); // login accepted

                connectedUser.ResetHasSeen();


                foreach (var b in server.Battles.Values.Where(x => x != null)) await SendCommand(new BattleAdded() { Header = b.GetHeader() });

                // mutually syncs users based on visibility rules
                await server.TwoWaySyncUsers(Name, server.ConnectedUsers.Keys);


                server.OfflineMessageHandler.SendMissedMessagesAsync(this, SayPlace.User, Name, user.AccountID);

                var defChans = await server.ChannelManager.GetDefaultChannels(user.AccountID);
                defChans.AddRange(server.Channels.Where(x => x.Value.Users.ContainsKey(user.Name)).Select(x => x.Key)); // add currently connected channels to list too

                foreach (var chan in defChans.ToList().Distinct())
                {
                    await connectedUser.Process(new JoinChannel()
                    {
                        ChannelName = chan,
                        Password = null
                    });
                }


                await SendCommand(new FriendList() { Friends = connectedUser.FriendEntries.ToList() });
                await SendCommand(new IgnoreList() { Ignores = connectedUser.Ignores.ToList() });

                await server.MatchMaker.OnLoginAccepted(connectedUser);
            }
            else
            {
                await SendCommand(ret.LoginResponse);
                if (ret.LoginResponse.ResultCode == LoginResponse.Code.Banned) transport.RequestClose();
            }
        }



        public async Task Process(Register register)
        {
            var response = new RegisterResponse();
            await Task.Run(async () => response = await DoRegister(register));
            await SendCommand(response);
        }

        private async Task<RegisterResponse> DoRegister(Register register)
        {
            if (!Account.IsValidLobbyName(register.Name)) return new RegisterResponse(RegisterResponse.Code.InvalidCharacters, "Name contains invalid characters");

            if (server.ConnectedUsers.ContainsKey(register.Name)) return new RegisterResponse(RegisterResponse.Code.AlreadyConnected, "You are already connected");

            if (string.IsNullOrEmpty(register.PasswordHash) && string.IsNullOrEmpty(register.SteamAuthToken)) return new RegisterResponse(RegisterResponse.Code.InvalidPassword, "Missing both password and steam token");

            if (!server.LoginChecker.VerifyIp(RemoteEndpointIP)) return new RegisterResponse(RegisterResponse.Code.Banned, "Too many connection attempts");

            var banPenalty = Punishment.GetActivePunishment(null, RemoteEndpointIP, register.UserID, x => x.BanLobby);
            if (banPenalty != null) return new RegisterResponse(RegisterResponse.Code.Banned, banPenalty.Reason);

            SteamWebApi.PlayerInfo info = null;
            if (!string.IsNullOrEmpty(register.SteamAuthToken))
            {
                info = await server.SteamWebApi.VerifyAndGetAccountInformation(register.SteamAuthToken);
                if (info == null) return new RegisterResponse(RegisterResponse.Code.InvalidSteamToken, "Steam token is invalid or could not be validated");
            }


            using (var db = new ZkDataContext())
            {
                var existingByName = db.Accounts.FirstOrDefault(x => x.Name.ToUpper() == register.Name.ToUpper());
                if (existingByName != null) return new RegisterResponse(RegisterResponse.Code.InvalidName, "Name already taken");

                var acc = new Account() { Name = register.Name };
                acc.SetPasswordHashed(register.PasswordHash);
                acc.SetName(register.Name);
                acc.SetAvatar();
                if (info != null)
                {
                    var existingBySteam = db.Accounts.FirstOrDefault(x => x.SteamID == info.steamid);
                    if (existingBySteam != null)
                        return new RegisterResponse(RegisterResponse.Code.SteamAlreadyRegistered,
                            "Your steam account is already registered as " + existingBySteam.Name);

                    acc.SteamID = info.steamid;
                    acc.SteamName = info.personaname;
                }
                db.Accounts.Add(acc);
                db.SaveChanges();
            }
            return new RegisterResponse(RegisterResponse.Code.Ok, "Registered");
        }


        public void RequestClose()
        {
            transport.RequestClose();
        }

        public async Task SendCommand<T>(T data)
        {
            try
            {
                var line = server.Serializer.SerializeToLine(data);
                await SendLine(line);
            }
            catch (Exception ex)
            {
                Trace.TraceError("{0} error sending {1} : {2}", this, data, ex);
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
                Trace.TraceError("{0} error sending {1} : {2}", this, line, ex);
            }
        }


        public override string ToString()
        {
            return string.Format("[{0} {1}:{2} {3}]", number, transport.RemoteEndpointAddress, transport.RemoteEndpointPort, Name);
        }

    }
}