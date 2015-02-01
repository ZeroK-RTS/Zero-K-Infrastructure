using System;
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

        public Battle MyBattle;

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
                    if (chan.Users.ContainsKey(Name))
                    {
                        User org;
                        chan.Users.TryRemove(Name, out org);
                        usersToNotify = chan.Users.Keys.ToList();
                    }
                    if (usersToNotify != null) await Broadcast(usersToNotify, new ChannelUserRemoved() { ChannelName = chan.Name, UserName = Name });
                }


                foreach (var b in state.Battles.Values.Where(x => x.Users.ContainsKey(Name))) {
                    await LeaveBattle(b);
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
            try {
                dynamic obj = state.Serializer.DeserializeLine(line);
                await Process(obj);
            } catch (Exception ex) {
                Trace.TraceError("{0} error processing line {1} : {2}", this, line, ex);
            }
        }

        public async Task SendCommand<T>(T data)
        {
            try {
                var line = state.Serializer.SerializeToLine(data);
                await SendData(Encoding.GetBytes(line));
            } catch (Exception ex) {
                Trace.TraceError("{0} error sending {1} : {2}", data, ex);
            }
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


                                Trace.TraceInformation("{0} login: {1}", this, response.ResultCode.Description());
                                await SendCommand(response); // login accepted
                                await SendCommand(User); // self data
                                
                                foreach (var b in state.Battles.Values)
                                {
                                    if (b != null) {
                                        await SynchronizeUsers(b.Founder.Name);
                                        await
                                            SendCommand(new BattleAdded() {
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

                                        foreach (var u in b.Users.Values.Select(x=>x.ToUpdateBattleStatus()).ToList()) {
                                            await SynchronizeUsers(u.Name);
                                            await SendCommand(new JoinedBattle() { BattleID = b.BattleID, User = u.Name });
                                            await SendCommand(u);
                                        }
                                    }
                                }
                                return;

                            }
                        }
                    }
                }
                await SendCommand(response);
            });

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

            if (channel.Users.TryAdd(Name, User)) {
                var users = channel.Users.Keys.ToArray();
                
                await SynchronizeUsers(users);
                await
                    SendCommand(new JoinChannelResponse() {
                        Success = true,
                        Name = joinChannel.Name,
                        Channel =
                            new ChannelHeader() {
                                Name = channel.Name,
                                Password = channel.Password,
                                Topic = channel.Topic,
                                TopicSetBy = channel.TopicSetBy,
                                TopicSetDate = channel.TopicSetDate,
                                Users = new List<string>(users)
                            }
                    });

                await Broadcast(users.Where(x => x != Name), new ChannelUserAdded { ChannelName = channel.Name, UserName = Name }, Name);
            }
        }

        async Task Process(LeaveChannel leaveChannel)
        {
            if (!IsLoggedIn) return;

            Channel channel;
            if (state.Rooms.TryGetValue(leaveChannel.Name, out channel)) {
                User user;
                if (channel.Users.TryRemove(Name, out user)) {
                    var users = channel.Users.Keys.ToArray();
                    await Broadcast(users, new ChannelUserRemoved() { ChannelName = channel.Name, UserName = Name });
                    await SendCommand(new ChannelUserRemoved() { ChannelName = channel.Name, UserName = Name });
                }
            }
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
                        if (channel.Users.ContainsKey(Name)) await Broadcast(channel.Users.Keys, say, Name);
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

                case SayPlace.Battle:
                    if (MyBattle != null) {
                        await Broadcast(MyBattle.Users.Keys, say);
                    }
                    break;

                case SayPlace.BattlePrivate:
                    if (MyBattle != null && MyBattle.Founder.Name == Name) {
                        Client cli;
                        if (MyBattle.Users.ContainsKey(say.Target) && state.Clients.TryGetValue(say.Target, out cli))
                        {
                            await cli.SendCommand(say);
                        }
                    }
                    break;
                case SayPlace.MessageBox:
                    if (User.IsAdmin || User.IsBot) {
                        await Broadcast(state.Clients.Values, say);
                    }
                    break;

            }
        }


        Task Respond(string message)
        {
            return SendCommand(new Say() { Place = SayPlace.MessageBox, Target = Name, Text = message });
        }

        async Task Process(OpenBattle openBattle)
        {
            if (!IsLoggedIn) return;

            if (state.Battles.Values.Any(y => y.Founder == User))
            {
                // already opened a battle 
                await Respond("You already opened a battle");
                return;
            }

            if (MyBattle != null) {
                await Respond("You are already in a battle");
                return;
            }

            var battleID = Interlocked.Increment(ref state.BattleCounter);

            var h = openBattle.Header;
            h.BattleID = battleID;
            h.Founder = Name;
            h.PlayerCount = 1; // is he reall spec?
            var battle = new Battle(h.Engine, h.Password, h.Port.Value, h.MaxPlayers.Value, h.Map, h.Title, new Mod() { Name = h.Game })
            {
                Ip = h.Ip,
                BattleID = battleID,
                Founder = User
            };
            battle.Users[Name] = new UserBattleStatus(Name, User);
            state.Battles[battleID] = battle;
            MyBattle = battle;
            h.Password = h.Password != null ? "?" : null; // dont send pw to client
            var clis = state.Clients.Values.ToList();
            await Broadcast(clis, new BattleAdded() { Header = h }, Name);
            await Broadcast(clis, new JoinedBattle() { BattleID = battleID, User = Name }, Name);
        }


        async Task Process(JoinBattle join)
        {
            if (!IsLoggedIn) return;

            if (MyBattle != null)
            {
                await Respond("You are already in other battle");
                return;
            }

            Battle battle;
            if (state.Battles.TryGetValue(join.BattleID, out battle))
            {
                if (battle.IsPassworded && battle.Password != join.Password)
                {
                    await Respond("Invalid password");
                    return;
                }
                battle.Users[Name] = new UserBattleStatus(Name, User);
                MyBattle = battle;
                await Broadcast(state.Clients.Values, new JoinedBattle() { BattleID = battle.BattleID, User = Name }, Name);
                
                foreach (var u in battle.Users.Values.Select(x=>x.ToUpdateBattleStatus()).ToList()) await SendCommand(u);
            }
        }


        async Task Process(UpdateUserBattleStatus status)
        {
            if (!IsLoggedIn) return;
            var bat = MyBattle;

            if (bat == null) return;

            if (Name == bat.Founder.Name || Name == status.Name) { // founder can set for all, others for self
                UserBattleStatus ubs;
                if (bat.Users.TryGetValue(status.Name, out ubs)) {
                    ubs.UpdateWith(status);
                    await Broadcast(bat.Users.Keys, status);
                }
            }
        }


        async Task Process(LeaveBattle leave)
        {
            if (!IsLoggedIn) return;

            Battle battle;
            if (state.Battles.TryGetValue(leave.BattleID, out battle)) {
                await LeaveBattle(battle);
            }
        }


        async Task LeaveBattle(Battle battle)
        {
            if (battle.Users.ContainsKey(Name)) {
                if (Name == battle.Founder.Name) { // remove entire battle
                    await RemoveBattle(battle);
                } else {
                    MyBattle = null;
                    UserBattleStatus oldVal;
                    if (battle.Users.TryRemove(Name, out oldVal)) await Broadcast(state.Clients.Values, new LeftBattle() { BattleID = battle.BattleID, User = Name });
                }
            }
        }

        async Task RemoveBattle(Battle battle)
        {
            foreach (var u in battle.Users.Keys) {
                Client client;
                if (state.Clients.TryGetValue(u, out client)) client.MyBattle = null;
                await Broadcast(state.Clients.Values, new LeftBattle() { BattleID = battle.BattleID, User = u });
            }
            await Broadcast(state.Clients.Values, new BattleRemoved() { BattleID = battle.BattleID });
        }
    }

}