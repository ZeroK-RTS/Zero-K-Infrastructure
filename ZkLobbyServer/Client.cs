using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LobbyClient;
using ZkData;
using ZkData.UnitSyncLib;

namespace ZkLobbyServer
{
    public class Client : Connection
    {
        SharedServerState state;
        int number;

        User User = new User();
        public int UserVersion;

        public ConcurrentDictionary<string, int?> LastKnownUserVersions = new ConcurrentDictionary<string, int?>();

        public bool IsLoggedIn { get { return User != null && User.AccountID != 0; } }

        public override string ToString()
        {
            return string.Format("[{0}:{1}]", number, Name);
        }

        public string Name { get { return User.Name; } }

        public Client(SharedServerState state)
        {
            this.state = state;
            number = Interlocked.Increment(ref state.ClientCounter);
            Trace.TraceInformation("{0} connected", this);
        }

        public override async Task OnConnectionClosed(bool wasRequested)
        {
            string reason = wasRequested ? "quit" : "connection failed";
            if (!string.IsNullOrEmpty(Name))
            {

                Client client;

                // notify all channels where i am to all users that i left 
                foreach (var chan in state.Rooms.Values.ToList())
                {
                    List<string> usersToNotify = null;
                    lock (chan.Users)
                    {
                        if (chan.Users.Contains(Name))
                        {
                            chan.Users.Remove(Name);
                            usersToNotify = chan.Users.ToList();
                        }
                    }
                    if (usersToNotify != null) await Broadcast(usersToNotify, new ChannelUserRemoved() { ChannelName = chan.Name, UserName = Name });
                }

                // notify clients which know about me that i left server
                var knowMe = state.Clients.Values.Where(x =>
                {
                    int? last;
                    return x != this && x.LastKnownUserVersions.TryGetValue(Name, out last) && last != null;
                }).ToList();

                await Broadcast(knowMe, new UserDisconnected() { Name = Name, Reason = reason });

                state.Clients.TryRemove(Name, out client);
            }
            Trace.TraceInformation("{0} {1}", this, reason);
        }

        /// <summary>
        /// Broadcasts to all targets in paralell
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="targets"></param>
        /// <param name="data"></param>
        /// <param name="synchronizeUsers">synchronize these users to targets first</param>
        /// <returns></returns>
        public Task Broadcast<T>(IEnumerable<string> targets, T data, params string[] synchronizeUsers)
        {
            return Broadcast(targets.Select(x =>
            {
                Client cli;
                state.Clients.TryGetValue(x, out cli);
                return cli;
            }), data, synchronizeUsers);
        }


        /// <summary>
        /// Broadcast to all targets in paralell
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="targets"></param>
        /// <param name="data"></param>
        /// <param name="synchronizeUsers">synchronize these users first</param>
        /// <returns></returns>
        public async Task Broadcast<T>(IEnumerable<Client> targets, T data, params string[] synchronizeUsers)
        {
            //send identical command to many clients
            var bytes = Encoding.GetBytes(state.Serializer.SerializeToLine(data));

            await Task.WhenAll(targets.Where(x => x != null).Select(async (client) =>
            {
                if (synchronizeUsers != null) await client.SynchronizeUsers(synchronizeUsers);
                await client.SendData(bytes);
            }));
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
            await Task.Run(async () =>
            {
                using (var db = new ZkDataContext())
                {
                    var acc = db.Accounts.Include(x => x.Clan).Include(x => x.Faction).FirstOrDefault(x => x.Name == login.Name);
                    if (acc == null)
                    {
                        response.ResultCode = LoginResponse.Code.InvalidName;
                    }
                    else
                    {
                        if (!acc.VerifyPassword(login.PasswordHash))
                        {
                            response.ResultCode = LoginResponse.Code.InvalidPassword;
                        }
                        else
                        {
                            // TODO banhammer check
                            // TODO country check
                            if (!state.Clients.TryAdd(login.Name, this))
                            {
                                response.ResultCode = LoginResponse.Code.AlreadyConnected;
                            }
                            else
                            {
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

                                foreach (var b in state.Battles.Values)
                                {
                                    if (b != null) {
                                        await
                                            SendCommand(new BattleOpened() {
                                                Header =
                                                    new BattleHeader() {
                                                        BattleID = b.BattleID,
                                                        Engine = b.EngineVersion,
                                                        Game = b.ModName,
                                                        Founder = b.Founder.Name,
                                                        Map = b.MapName,
                                                        Ip = b.Ip,
                                                        Port = b.HostPort,
                                                        Title = b.Title,
                                                        PlayerCount = b.NonSpectatorCount,
                                                        SpectatorCount = b.SpectatorCount,
                                                        MaxPlayers = b.MaxPlayers,
                                                        Password = b.Password != null ? "?" : null
                                                    }
                                            });
                                    }
                                }

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
            foreach (var c in state.Clients.Values.ToList()) c.LastKnownUserVersions[Name] = null;
        }

        private async Task SynchronizeUsers(params string[] names)
        {
            foreach (var n in names)
            {
                Client client;
                if (state.Clients.TryGetValue(n, out client))
                {
                    int? lastKnownVersion;
                    LastKnownUserVersions.TryGetValue(n, out lastKnownVersion);
                    var version = client.UserVersion;
                    if (lastKnownVersion == null || lastKnownVersion != version)
                    {
                        await SendCommand(client.User);
                        LastKnownUserVersions[n] = version;
                    }
                }
            }
        }


        async Task Process(Register register)
        {
            var response = new RegisterResponse();
            if (state.Clients.ContainsKey(register.Name))
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


        async Task Process(JoinChannel joinChannel)
        {
            if (!IsLoggedIn) return;
            var channel = state.Rooms.GetOrAdd(joinChannel.Name, (n) => { return new Channel() { Name = joinChannel.Name, }; });
            if (channel.Password != joinChannel.Password)
            {
                await SendCommand(new JoinChannelResponse() { Success = false, Reason = "invalid password" });
            }

            List<string> users;
            lock (channel.Users)
            {
                if (!channel.Users.Contains(Name)) channel.Users.Add(Name);
                users = channel.Users.ToList();
            }

            await SynchronizeUsers(users.ToArray());
            await
                SendCommand(new JoinChannelResponse()
                {
                    Success = true,
                    Name = joinChannel.Name,
                    Channel =
                        new Channel()
                        {
                            Name = channel.Name,
                            Password = channel.Password,
                            Topic = channel.Topic,
                            TopicSetBy = channel.TopicSetBy,
                            TopicSetDate = channel.TopicSetDate,
                            Users = users
                        }
                });

            await Broadcast(users.Where(x => x != Name), new ChannelUserAdded { ChannelName = channel.Name, UserName = Name }, Name);
        }


        async Task Process(Say say)
        {
            if (!IsLoggedIn) return;

            say.User = Name;

            switch (say.Place)
            {
                case SayPlace.Channel:
                    Channel channel;
                    if (state.Rooms.TryGetValue(say.Target, out channel))
                    {
                        bool isJoined;
                        lock (channel.Users)
                        {
                            isJoined = channel.Users.Contains(Name);
                        }
                        if (isJoined) await Broadcast(channel.Users, say, Name);
                    }
                    break;

                case SayPlace.User:
                    Client client;
                    if (state.Clients.TryGetValue(say.Target, out client))
                    {
                        await client.SynchronizeUsers(Name);
                        await client.SendCommand(say);

                        await SynchronizeUsers(say.Target);
                        await SendCommand(say);
                    } // todo else offline message?
                    break;
            }
        }


        async Task Process(OpenBattle openBattle)
        {
            if (!IsLoggedIn) return;

            if (state.Battles.Values.Any(y => y.Founder == User))
            {
                // already opened a battle 
                await SendCommand(new Say() { Place = SayPlace.MessageBox, Target = Name, Text = "You already opened a battle" });
                return;
            }

            var alreadyJoinedBattle = state.Battles.Values.FirstOrDefault(x => x.Users.Any(y => y.LobbyUser == User));
            if (alreadyJoinedBattle != null)
            {
                // already in battle quit first
                await Broadcast(alreadyJoinedBattle.Users.Select(x => x.Name), new LeftBattle { BattleID = alreadyJoinedBattle.BattleID, User = Name });

            }
            var battleID = Interlocked.Increment(ref state.BattleCounter);

            var h = openBattle.Header;
            h.BattleID = battleID;
            h.Founder = Name;
            h.PlayerCount = 1; // is he reall spec?
            var battle = new Battle(h.Engine, h.Password, h.Port, h.MaxPlayers, h.Map, h.Title, new Mod() { Name = h.Game })
            {
                Ip = h.Ip,
                BattleID = battleID,
                Founder = User
            };
            battle.Users.Add(new UserBattleStatus(Name, User));
            state.Battles[battleID] = battle;
            h.Password = h.Password != null ? "?" : null; // dont send pw to client
            await Broadcast(state.Clients.Values, new BattleOpened() { Header = h }, Name);
            await SendCommand(new JoinedBattle() { BattleID = battleID, User = Name });

        }


        async Task Process(JoinBattle join)
        {
            if (!IsLoggedIn) return;

            var alreadyJoinedBattle = state.Battles.Values.FirstOrDefault(x => x.Users.Any(y => y.LobbyUser == User));
            if (alreadyJoinedBattle != null)
            {
                if (alreadyJoinedBattle.BattleID != join.BattleID) {
                    // already in battle quit first
                    await
                        Broadcast(alreadyJoinedBattle.Users.Select(x => x.Name),
                            new LeftBattle { BattleID = alreadyJoinedBattle.BattleID, User = Name });
                } else {
                    //already in same battle, do nothing
                    return;
                }
            }

            Battle battle;
            if (state.Battles.TryGetValue(join.BattleID, out battle))
            {
                if (battle.Password != join.Password)
                {
                    await SendCommand(new Say() { Place = SayPlace.MessageBox, Target = Name, Text = "Invalid password" });
                    return;
                }
                battle.Users.Add(new UserBattleStatus(Name, User));
                await Broadcast(battle.Users.Select(x => x.Name), new JoinedBattle() { BattleID = battle.BattleID, User = Name }, Name);
            }
        }


        async Task Process(LeaveBattle leave)
        {
            if (!IsLoggedIn) return;

            Battle battle;
            if (state.Battles.TryGetValue(leave.BattleID, out battle))
            {
                var item = battle.Users.FirstOrDefault(x => x.Name == Name);
                if (item != null) {
                    battle.Users.Remove(item);
                    await SendCommand(new LeftBattle() { BattleID = battle.BattleID, User = Name });
                    await Broadcast(battle.Users.Select(x => x.Name), new LeftBattle() { BattleID = battle.BattleID, User = Name });
                    
                }
            }
        }



    }

}