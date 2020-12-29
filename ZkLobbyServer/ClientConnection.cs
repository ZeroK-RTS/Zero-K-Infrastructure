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

        static List<Welcome.FactionInfo> cachedFactions = new List<Welcome.FactionInfo>();
        static List<string> blacklist = new List<string>() { "dakeys", "xtrasauce" };

        string challengeToken;

        static ClientConnection()
        {
            using (var db = new ZkDataContext())
            {
                cachedFactions = db.Factions.Where(x => !x.IsDeleted).ToList().Select(x => x.ToFactionInfo()).ToList();
            }
        }


        public ClientConnection(ITransport transport, ZkLobbyServer server)
        {
            this.server = server;
            number = Interlocked.Increment(ref server.ClientCounter);
            this.transport = transport;

            challengeToken = Guid.NewGuid().ToString(); // generate random challenge token

            transport.ConnectAndRun(OnCommandReceived, OnConnected, OnConnectionClosed).ConfigureAwait(false);
        }

        public async Task OnCommandReceived(string line)
        {
            try
            {

                dynamic obj = server.Serializer.DeserializeLine(line);
                if (obj is Login || obj is Register) await Process(obj);
                else
                {
                    await connectedUser.Throttle(line.Length);
                    await connectedUser.Process(obj);
                }
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
            await SendCommand(new Welcome() { Engine = server.Engine, Game = server.Game, Blacklist = blacklist, Version = server.Version, UserCount = server.ConnectedUsers.Count, Factions = cachedFactions, UserCountLimited = MiscVar.ZklsMaxUsers > 0, ChallengeToken = challengeToken, ServerPubKey = server.LoginChecker.ServerPubKey});
        }


        public async Task OnConnectionClosed(bool wasRequested)
        {
            var reason = wasRequested ? "quit" : "connection failed";
            if (!string.IsNullOrEmpty(Name)) await connectedUser.RemoveConnection(this, reason);
            //Trace.TraceInformation("{0} {1}", this, reason);
        }


        public async Task Process(Login login)
        {
            var ret = await Task.Run(() => server.LoginChecker.DoLogin(login, RemoteEndpointIP, login.Dlc, challengeToken));
            if (ret.LoginResponse.ResultCode == LoginResponse.Code.Ok)
            {
                var user = ret.User;
                //Trace.TraceInformation("{0} login: {1}", this, response.ResultCode.Description());

                await this.SendCommand(user); // send self to self first

                connectedUser = server.ConnectedUsers.GetOrAdd(user.Name, (n) => new ConnectedUser(server, user));
                connectedUser.User = user;
                connectedUser.Connections.TryAdd(this, true);

                // close other connections
                foreach (var otherConnection in connectedUser.Connections.Keys.Where(x => x != null && x != this).ToList())
                {
                    otherConnection.RequestClose();
                    bool oth;
                    connectedUser.Connections.TryRemove(otherConnection, out oth);
                }

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


                foreach (var bat in server.Battles.Values.Where(x => x != null && x.IsInGame))
                {
                    var s = bat.spring;
                    if (s.LobbyStartContext.Players.Any(x => !x.IsSpectator && x.Name == Name) && !s.Context.ActualPlayers.Any(x=>x.Name == Name && x.LoseTime != null))
                    {
                        await SendCommand(new RejoinOption() { BattleID = bat.BattleID });
                        await bat.ProcessPlayerJoin(connectedUser, bat.Password);
                    }
                }


                await SendCommand(new FriendList() { Friends = connectedUser.FriendEntries.ToList() });
                await SendCommand(new IgnoreList() { Ignores = connectedUser.Ignores.ToList() });

                await server.MatchMaker.OnLoginAccepted(connectedUser);
                await server.PlanetWarsMatchMaker.OnLoginAccepted(connectedUser);

                await SendCommand(server.NewsListManager.GetCurrentNewsList());
                await SendCommand(server.LadderListManager.GetCurrentLadderList());
                await SendCommand(server.ForumListManager.GetCurrentForumList(user.AccountID));

                using (var db = new ZkDataContext())
                {
                    var acc = db.Accounts.Find(user.AccountID);
                    if (acc != null) await server.PublishUserProfileUpdate(acc);
                }
            }
            else
            {
                await SendCommand(ret.LoginResponse);

                if (ret.LoginResponse.ResultCode == LoginResponse.Code.Banned)
                {
                    await Task.Delay(500); // this is needed because socket writes are async and might not be queued yet
                    await transport.Flush();
                    transport.RequestClose();
                }
            }
        }



        public async Task Process(Register register)
        {
            var response = new RegisterResponse();
            await Task.Run(async () => response = await server.LoginChecker.DoRegister(register, RemoteEndpointIP));
            await SendCommand(response);
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