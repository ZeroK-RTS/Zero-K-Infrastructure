using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using LobbyClient;
using ZkData;
using ZkData.UnitSyncLib;
using Ping = LobbyClient.Ping;

namespace ZkLobbyServer
{
    public class Client : Connection
    {
        SharedServerState state;
        int number;
        public User User = new User();
        public int UserVersion;
        public ConcurrentDictionary<string, int?> LastKnownUserVersions = new ConcurrentDictionary<string, int?>();

        public bool IsLoggedIn { get { return User != null && User.AccountID != 0; } }

        public override string ToString()
        {
            return string.Format("[{0} {1}:{2} {3}]", number, RemoteEndpointIP,RemoteEndpointPort, Name);
        }

        public string Name { get { return User.Name; } }

        public Battle MyBattle;

        public Client(SharedServerState state)
        {
            this.state = state;
            number = Interlocked.Increment(ref state.ClientCounter);
            Trace.TraceInformation("{0} accepted", this);
        }

        public override async Task OnConnectionClosed(bool wasRequested)
        {
            string reason = wasRequested ? "quit" : "connection failed";
            if (!string.IsNullOrEmpty(Name))
            {
                ClearMyLastKnownStateForOtherClients();

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
                if (synchronizeUsers != null) await client.SynchronizeUsersToMe(synchronizeUsers);
                await client.SendData(bytes);
            }));
        }


   


        public override async Task OnConnected()
        {
            Trace.TraceInformation("{0} connected", this);
            await SendCommand(new Welcome() { Engine = state.Engine, Game = state.Game, Version = state.Version });
        }


        public override async Task OnLineReceived(string line)
        {
            try {
                dynamic obj = state.Serializer.DeserializeLine(line);
                await Process(obj);
            } catch (Exception ex) {
                var message = string.Format("{0} error processing line {1} : {2}", this, line, ex);
                Trace.TraceError(message);
                Respond(message);
            }
        }

        public async Task SendCommand<T>(T data)
        {
            try {
                var line = state.Serializer.SerializeToLine(data);
                await SendString(line);
            } catch (Exception ex) {
                Trace.TraceError("{0} error sending {1} : {2}", data, ex);
            }
        }


        async Task Process(Ping ping)
        {
            
        }


        async Task Process(Login login)
        {
            var response = await Task.Run(() => state.LoginChecker.Login(User, login, this));
            if (response.ResultCode == LoginResponse.Code.Ok) {
                ClearMyLastKnownStateForOtherClients();

                Trace.TraceInformation("{0} login: {1}", this, response.ResultCode.Description());
                await SendCommand(response); // login accepted
                await SendCommand(User); // self data

                foreach (var b in state.Battles.Values) {
                    if (b != null) {
                        await SynchronizeUsersToMe(b.Founder.Name);
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
                                        SpectatorCount = b.SpectatorCount,
                                        MaxPlayers = b.MaxPlayers,
                                        Password = b.Password != null ? "?" : null
                                    }
                            });

                        foreach (var u in b.Users.Values.Select(x => x.ToUpdateBattleStatus()).ToList()) {
                            await SynchronizeUsersToMe(u.Name);
                            await SendCommand(new JoinedBattle() { BattleID = b.BattleID, User = u.Name });
                            await SendCommand(u);
                        }
                    }
                }
            } else await SendCommand(response);
        }

        

        public void ClearMyLastKnownStateForOtherClients()
        {
            foreach (var c in state.Clients.Values.ToList()) {
                int? orgval;
                c.LastKnownUserVersions.TryRemove(Name, out orgval);
            }
        }

        private async Task SynchronizeUsersToMe(params string[] names)
        {
            foreach (var n in names)
            {
                Client client;
                if (state.Clients.TryGetValue(n, out client))
                {
                    int? lastKnownVersion;
                    var version = client.UserVersion;
                    if (!LastKnownUserVersions.TryGetValue(n, out lastKnownVersion) || lastKnownVersion == null || lastKnownVersion != version)
                    {
                        await SendCommand(client.User);
                        LastKnownUserVersions[n] = version;
                    }
                }
            }
        }

        private async Task UpdateSelfToWhoKnowsMe()
        {
            var version = Interlocked.Increment(ref UserVersion);
            int? ver;
            var clients = state.Clients.Values.Where(x => x.LastKnownUserVersions.TryGetValue(Name, out ver) && ver != version).ToList();
            foreach (var cli in clients) {
                cli.LastKnownUserVersions[Name] = version;
            }
            await Broadcast(clients, User);
        }


        private async Task Process(SetRectangle rect)
        {
            if (!IsLoggedIn) return;

            var bat = MyBattle;
            if (bat == null || (bat.Founder != User && !User.IsAdmin)) {
                await Respond("No rights to set rectangle");
                return;
            }

            if (rect.Rectangle == null) {
                BattleRect org;
                bat.Rectangles.TryRemove(rect.Number, out org);
            } else {
                bat.Rectangles[rect.Number] = rect.Rectangle;
            }
            await Broadcast(bat.Users.Keys, rect);
        }


        private async Task Process(KickFromBattle batKick)
        {
            if (!IsLoggedIn) return;

            if (batKick.BattleID == null && MyBattle != null) batKick.BattleID = MyBattle.BattleID;
            Battle bat;
            if (state.Battles.TryGetValue(batKick.BattleID.Value, out bat)) {
                if (bat.Founder != User && !User.IsAdmin) {
                    await Respond("No rights to do a kick");
                    return;
                }

                UserBattleStatus user;
                if (bat.Users.TryGetValue(batKick.Name, out user)) {
                    var client = state.Clients[batKick.Name];
                    await client.Respond(string.Format("You were kicked from battle by {0} : {1}",  Name, batKick.Reason));
                    await client.Process(new LeaveBattle() { BattleID = batKick.BattleID.Value });
                }
            }
        }

        private async Task Process(ForceJoinBattle forceJoin)
        {
            if (!IsLoggedIn) return;

            if (!User.IsAdmin)
            {
                await Respond("No rights for force join");
                return;
            }

            Battle bat;
            if (state.Battles.TryGetValue(forceJoin.BattleID, out bat))
            {
                Client client;
                if (state.Clients.TryGetValue(forceJoin.Name, out client)) {
                    if (client.MyBattle != null) await client.Process(new LeaveBattle());
                    await client.Process(new JoinBattle() { BattleID = forceJoin.BattleID,Password = bat.Password});
                }
            }
        }


        private async Task Process(KickFromChannel chanKick)
        {
            if (!IsLoggedIn) return;

            Channel channel;
            User user;
            if (state.Rooms.TryGetValue(chanKick.ChannelName, out channel) && channel.Users.TryGetValue(chanKick.UserName, out user)) {
                if (!User.IsAdmin) {
                    await Respond("No rights to execute kick");
                    return;
                }

                var client = state.Clients[chanKick.UserName];
                await client.Respond(string.Format("You were kicked from channel {0} by {1} : {2}", chanKick.ChannelName, Name, chanKick.Reason));
                await client.Process(new LeaveChannel() { ChannelName = chanKick.ChannelName });
            }
        }

        private async Task Process(KickFromServer kick)
        {
            if (!IsLoggedIn) return;

            Client client;
            if (state.Clients.TryGetValue(kick.Name, out client))
            {
                if (!User.IsAdmin)
                {
                    await Respond("No rights to execute kick");
                    return;
                }

                await client.Respond(string.Format("You were kicked by {0} : {1}", Name, kick.Reason));
                client.RequestClose();
            }
        }

        private async Task Process(ForceJoinChannel forceJoin)
        {
            if (!IsLoggedIn) return;

            Channel channel;
            Client client;
            if (state.Rooms.TryGetValue(forceJoin.ChannelName, out channel) && state.Clients.TryGetValue(forceJoin.UserName, out client))
            {
                if (!User.IsAdmin)
                {
                    await Respond("No rights to execute forcejoin");
                    return;
                }

                if (!channel.Users.ContainsKey(forceJoin.UserName)) await client.Process(new JoinChannel() { ChannelName = forceJoin.ChannelName, Password = channel.Password });
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
            var channel = state.Rooms.GetOrAdd(joinChannel.ChannelName, (n) => { return new Channel() { Name = joinChannel.ChannelName, }; });
            if (channel.Password != joinChannel.Password)
            {
                await SendCommand(new JoinChannelResponse() { Success = false, Reason = "invalid password" });
            }

            if (channel.Users.TryAdd(Name, User)) {
                var users = channel.Users.Keys.ToArray();
                
                await SynchronizeUsersToMe(users);
                await
                    SendCommand(new JoinChannelResponse() {
                        Success = true,
                        ChannelName = joinChannel.ChannelName,
                        Channel =
                            new ChannelHeader() {
                                ChannelName = channel.Name,
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
            if (state.Rooms.TryGetValue(leaveChannel.ChannelName, out channel)) {
                User user;
                if (channel.Users.TryRemove(Name, out user)) {
                    var users = channel.Users.Keys.ToArray();
                    await Broadcast(users, new ChannelUserRemoved() { ChannelName = channel.Name, UserName = Name });
                    await SendCommand(new ChannelUserRemoved() { ChannelName = channel.Name, UserName = Name });
                }
            }
        }



        public async Task Process(Say say)
        {
            if (!IsLoggedIn) return;

            say.User = Name;
            
            if (say.Ring) { // ring permissions - bot/admin anywhere, others only to own battle 
                if (!User.IsAdmin) {
                    if ((say.Place != SayPlace.Battle && say.Place != SayPlace.BattlePrivate) || MyBattle == null || MyBattle.Founder != User) say.Ring = false;
                }
            }

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
                        await client.SynchronizeUsersToMe(Name);
                        await client.SendCommand(say);

                        await SynchronizeUsersToMe(say.Target);
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
                    if (User.IsAdmin) {
                        await Broadcast(state.Clients.Values, say);
                    }
                    break;

            }
        }


        Task Respond(string message)
        {
            return SendCommand(new Say() { Place = SayPlace.MessageBox, Target = Name, User = Name, Text = message });
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
            var battle = new Battle();
            battle.UpdateWith(h, (n)=>state.Clients[n].User);
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
                await RecalcSpectators(battle);

                foreach (var u in battle.Users.Values.Select(x=>x.ToUpdateBattleStatus()).ToList()) await SendCommand(u);
                foreach (var u in battle.Bots.Values.Select(x => x.ToUpdateBotStatus()).ToList()) await SendCommand(u);
                foreach (var u in battle.Rectangles) await SendCommand(new SetRectangle(){Number = u.Key,Rectangle = u.Value});
                await SendCommand(new SetModOptions() {Options = battle.ModOptions});
            }
        }

        async Task Process(BattleUpdate battleUpdate)
        {
            if (!IsLoggedIn) return;

            var h = battleUpdate.Header;
            if (h.BattleID == null && MyBattle != null) h.BattleID = MyBattle.BattleID;
            Battle bat;
            if (!state.Battles.TryGetValue(h.BattleID.Value, out bat)) {
                await Respond("No such battle exists");
                return;
            }
            if (bat.Founder != User && !User.IsAdmin) {
                await Respond("You don't have permission to edit this battle");
                return;
            }
            
            bat.UpdateWith(h,(n)=>state.Clients[n].User);
            await Broadcast(state.Clients.Keys, battleUpdate, Name);
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
                    await RecalcSpectators(bat);
                }
            }
        }

        async Task RecalcSpectators(Battle bat)
        {
            var specCount = bat.Users.Values.Count(x => x.IsSpectator);
            if (specCount != bat.SpectatorCount) {
                bat.SpectatorCount = specCount;
                await Broadcast(state.Clients.Values, new BattleUpdate() { Header = new BattleHeader() { SpectatorCount = specCount, BattleID = bat.BattleID} }, bat.Founder.Name);
            }
        }


        async Task Process(LeaveBattle leave)
        {
            if (!IsLoggedIn) return;


            if (leave.BattleID == null && MyBattle != null) leave.BattleID = MyBattle.BattleID;

            Battle battle;
            if (state.Battles.TryGetValue(leave.BattleID.Value, out battle)) {
                await LeaveBattle(battle);
                await RecalcSpectators(battle);
            }
        }

        async Task Process(ChangeUserStatus userStatus)
        {
            if (!IsLoggedIn) return;
            bool changed = false;
            if (userStatus.IsInGame != null && User.IsInGame != userStatus.IsInGame) {
                User.IsInGame = userStatus.IsInGame.Value;
                changed = true;
            }
            if (userStatus.IsAfk != null && User.IsAway != userStatus.IsAfk) {
                User.IsAway = userStatus.IsAfk.Value;
                changed = true;
            }
            if (changed) await UpdateSelfToWhoKnowsMe();
        }


        async Task Process(UpdateBotStatus add)
        {
            if (!IsLoggedIn) return;

            var battle = MyBattle;
            if (battle != null) {
                BotBattleStatus ubs;
                if (!battle.Bots.TryGetValue(add.Name, out ubs)) ubs = new BotBattleStatus(add.Name, Name, add.AiLib);
                else if (add.Owner != Name && !User.IsAdmin && User != battle.Founder) {
                    await Respond(string.Format("No permissions to edit bot {0}", add.Name));
                    return;
                }
                ubs.UpdateWith(add);
                battle.Bots[ubs.Name] = ubs;
                await Broadcast(battle.Users.Keys, ubs.ToUpdateBotStatus());
            }
        }


        async Task Process(RemoveBot rem)
        {
            if (!IsLoggedIn) return;

            var battle = MyBattle;
            if (battle != null)
            {
                var bot = battle.Bots[rem.Name];
                if (bot.owner != Name  && !User.IsAdmin && User != battle.Founder)
                {
                    await Respond(string.Format("No permissions to edit bot {0}", rem.Name));
                    return;
                }
                BotBattleStatus ubs;
                if (battle.Bots.TryRemove(rem.Name, out ubs)) {
                    await Broadcast(battle.Users.Keys, rem);
                }
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
                    var bots = battle.Bots.Values.Where(x => x.owner == Name).ToList();
                    foreach (var b in bots) {
                        BotBattleStatus obs;
                        if (battle.Bots.TryRemove(b.Name, out obs)) await Broadcast(battle.Users.Keys, new RemoveBot() { Name = b.Name });
                    }
                }
            }
        }

        async Task Process(SetModOptions options)
        {
            if (!IsLoggedIn) return;

            var bat = MyBattle;
            if (bat != null)
            {
                if (bat.Founder != User && !User.IsAdmin) {
                    await Respond("You don't have permissions to change mod options here");
                    return;
                }
                bat.ModOptions = options.Options;
                await Broadcast(bat.Users.Keys, options);
            }
        }


        async Task RemoveBattle(Battle battle)
        {
            foreach (var u in battle.Users.Keys) {
                Client client;
                if (state.Clients.TryGetValue(u, out client)) client.MyBattle = null;
                await Broadcast(state.Clients.Values, new LeftBattle() { BattleID = battle.BattleID, User = u });
            }
            Battle bat;
            state.Battles.TryRemove(battle.BattleID, out bat);
            await Broadcast(state.Clients.Values, new BattleRemoved() { BattleID = battle.BattleID });
        }
    }

}