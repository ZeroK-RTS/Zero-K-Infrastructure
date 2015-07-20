using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using LobbyClient;
using PlasmaShared;
using ZkData;
using ZkData.UnitSyncLib;
using Ping = LobbyClient.Ping;

namespace ZkLobbyServer
{
    public class ConnectedUser : ICommandSender
    {
        public ConcurrentDictionary<ClientConnection, bool> Connections = new ConcurrentDictionary<ClientConnection, bool>();
        ZkLobbyServer state;
        public User User = new User();

        public bool IsLoggedIn { get { return User != null && User.AccountID != 0; } }


        public override string ToString()
        {
            return string.Format("[{0}]", Name);
        }

        public string Name { get { return User.Name; } }

        public Battle MyBattle;

        public ConnectedUser(ZkLobbyServer state, User user)
        {
            this.state = state;

        }



        public async Task SendLine(string line)
        {
            await Task.WhenAll(Connections.Keys.Select(async (con) => { await con.SendLine(line); }));
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


        public async Task Process(SetRectangle rect)
        {
            if (!IsLoggedIn) return;

            var bat = MyBattle;
            if (bat == null || (bat.Founder != User && !User.IsAdmin))
            {
                await Respond("No rights to set rectangle");
                return;
            }

            if (rect.Rectangle == null)
            {
                BattleRect org;
                bat.Rectangles.TryRemove(rect.Number, out org);
            }
            else
            {
                bat.Rectangles[rect.Number] = rect.Rectangle;
            }
            await state.Broadcast(bat.Users.Keys, rect);
        }


        public async Task Process(KickFromBattle batKick)
        {
            if (!IsLoggedIn) return;

            if (batKick.BattleID == null && MyBattle != null) batKick.BattleID = MyBattle.BattleID;
            Battle bat;
            if (state.Battles.TryGetValue(batKick.BattleID.Value, out bat))
            {
                if (bat.Founder != User && !User.IsAdmin)
                {
                    await Respond("No rights to do a kick");
                    return;
                }

                UserBattleStatus user;
                if (bat.Users.TryGetValue(batKick.Name, out user))
                {
                    var client = state.ConnectedUsers[batKick.Name];
                    await client.Respond(string.Format("You were kicked from battle by {0} : {1}", Name, batKick.Reason));
                    await client.Process(new LeaveBattle() { BattleID = batKick.BattleID.Value });
                }
            }
        }

        public async Task Process(ForceJoinBattle forceJoin)
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
                ConnectedUser connectedUser;
                if (state.ConnectedUsers.TryGetValue(forceJoin.Name, out connectedUser))
                {
                    if (connectedUser.MyBattle != null) await connectedUser.Process(new LeaveBattle());
                    await connectedUser.Process(new JoinBattle() { BattleID = forceJoin.BattleID, Password = bat.Password });
                }
            }
        }


        public async Task Process(KickFromChannel chanKick)
        {
            if (!IsLoggedIn) return;

            Channel channel;
            User user;
            if (state.Rooms.TryGetValue(chanKick.ChannelName, out channel) && channel.Users.TryGetValue(chanKick.UserName, out user))
            {
                if (!User.IsAdmin)
                {
                    await Respond("No rights to execute kick");
                    return;
                }

                var client = state.ConnectedUsers[chanKick.UserName];
                await client.Respond(string.Format("You were kicked from channel {0} by {1} : {2}", chanKick.ChannelName, Name, chanKick.Reason));
                await client.Process(new LeaveChannel() { ChannelName = chanKick.ChannelName });
            }
        }

        public async Task Process(KickFromServer kick)
        {
            if (!IsLoggedIn) return;

            ConnectedUser connectedUser;
            if (state.ConnectedUsers.TryGetValue(kick.Name, out connectedUser))
            {
                if (!User.IsAdmin)
                {
                    await Respond("No rights to execute kick");
                    return;
                }

                state.KickFromServer(Name, kick.Name, kick.Reason);
            }
        }

        public void RequestCloseAll()
        {
            foreach (var c in Connections.Keys) c.RequestClose();
        }

        public async Task Process(ForceJoinChannel forceJoin)
        {
            if (!IsLoggedIn) return;

            if (!User.IsAdmin)
            {
                await Respond("No rights to execute forcejoin");
                return;
            }


            ConnectedUser connectedUser;
            if (state.ConnectedUsers.TryGetValue(forceJoin.UserName, out connectedUser))
            {
                Channel channel;
                state.Rooms.TryGetValue(forceJoin.ChannelName, out channel);

                await connectedUser.Process(new JoinChannel() { ChannelName = forceJoin.ChannelName, Password = channel != null ? channel.Password : null });
            }
        }




        public async Task Process(JoinChannel joinChannel)
        {
            if (!IsLoggedIn) return;
            var channel = state.Rooms.GetOrAdd(joinChannel.ChannelName, (n) => new Channel() { Name = joinChannel.ChannelName, });
            if (channel.Password != joinChannel.Password)
            {
                await SendCommand(new JoinChannelResponse() { Success = false, Reason = "invalid password", ChannelName = joinChannel.ChannelName });
            }


            var added = channel.Users.TryAdd(Name, User);
            var users = channel.Users.Keys.ToArray();

            await
                SendCommand(new JoinChannelResponse()
                {
                    Success = true,
                    ChannelName = joinChannel.ChannelName,
                    Channel =
                        new ChannelHeader()
                        {
                            ChannelName = channel.Name,
                            Password = channel.Password,
                            Topic = channel.Topic,
                            Users = new List<string>(users)
                        }
                });

            await state.OfflineMessageHandler.SendMissedMessages(this, SayPlace.Channel, joinChannel.ChannelName, User.AccountID);

            if (added) await state.Broadcast(users, new ChannelUserAdded { ChannelName = channel.Name, UserName = Name });
        }


        public async Task Process(LeaveChannel leaveChannel)
        {
            if (!IsLoggedIn) return;

            Channel channel;
            if (state.Rooms.TryGetValue(leaveChannel.ChannelName, out channel))
            {
                User user;
                if (channel.Users.TryRemove(Name, out user))
                {
                    var users = channel.Users.Keys.ToArray();
                    await SendCommand(new ChannelUserRemoved() { ChannelName = channel.Name, UserName = Name });
                    await state.Broadcast(users, new ChannelUserRemoved() { ChannelName = channel.Name, UserName = Name });
                }
            }
        }



        public async Task Process(Say say)
        {
            if (!IsLoggedIn) return;
            if (User.BanMute) return; // block all say for muted

            say.User = Name;
            say.Time = DateTime.UtcNow;

            if (say.Ring)
            { // ring permissions - bot/admin anywhere, others only to own battle 
                if (!User.IsAdmin)
                {
                    if ((say.Place != SayPlace.Battle && say.Place != SayPlace.BattlePrivate) || MyBattle == null || MyBattle.Founder != User) say.Ring = false;
                }
            }

            switch (say.Place)
            {
                case SayPlace.Channel:
                    Channel channel;
                    if (state.Rooms.TryGetValue(say.Target, out channel))
                    {
                        if (channel.Users.ContainsKey(Name))
                        {
                            await state.Broadcast(channel.Users.Keys, say);
                            await state.OfflineMessageHandler.StoreChatHistory(say);
                        }
                    }
                    break;

                case SayPlace.User:
                    ConnectedUser connectedUser;
                    if (state.ConnectedUsers.TryGetValue(say.Target, out connectedUser)) await connectedUser.SendCommand(say);
                    else await state.OfflineMessageHandler.StoreChatHistory(say);
                    await SendCommand(say);

                    break;

                case SayPlace.Battle:
                    if (MyBattle != null)
                    {
                        await state.Broadcast(MyBattle.Users.Keys, say);
                    }
                    break;

                case SayPlace.BattlePrivate:
                    if (MyBattle != null && MyBattle.Founder.Name == Name)
                    {
                        ConnectedUser cli;
                        if (MyBattle.Users.ContainsKey(say.Target))
                        {
                            if (state.ConnectedUsers.TryGetValue(say.Target, out cli)) await cli.SendCommand(say);
                        }
                    }
                    break;
                case SayPlace.MessageBox:
                    if (User.IsAdmin)
                    {
                        await state.Broadcast(state.ConnectedUsers.Values, say);
                    }
                    break;

            }

            await state.OnSaid(say);
        }


        public async Task RemoveConnection(ClientConnection con, string reason)
        {
            bool dummy;
            if (Connections.TryRemove(con, out dummy) && Connections.Count == 0)
            {
                // notify all channels where i am to all users that i left 
                foreach (var chan in state.Rooms.Values.Where(x => x.Users.ContainsKey(Name)).ToList())
                {
                    await Process(new LeaveChannel() { ChannelName = chan.Name });
                }

                foreach (var b in state.Battles.Values.Where(x => x.Users.ContainsKey(Name)))
                {
                    await LeaveBattle(b);
                    await RecalcSpectators(b);
                }

                await state.Broadcast(state.ConnectedUsers.Values, new UserDisconnected() { Name = Name, Reason = reason });

                ConnectedUser connectedUser;
                state.ConnectedUsers.TryRemove(Name, out connectedUser);

                using (var db = new ZkDataContext())
                {
                    var acc = await db.Accounts.FindAsync(User.AccountID);
                    acc.LastLogout = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }
            }
        }

        public Task Respond(string message)
        {
            return SendCommand(new Say() { Place = SayPlace.MessageBox, Target = Name, User = Name, Text = message });
        }

        public async Task Process(OpenBattle openBattle)
        {
            if (!IsLoggedIn) return;

            if (state.Battles.Values.Any(y => y.Founder == User))
            {
                // already opened a battle 
                await Respond("You already opened a battle");
                return;
            }

            if (MyBattle != null)
            {
                await Respond("You are already in a battle");
                return;
            }

            var battleID = Interlocked.Increment(ref state.BattleCounter);

            var h = openBattle.Header;
            h.BattleID = battleID;
            h.Founder = Name;
            var battle = new Battle();
            battle.UpdateWith(h, (n) => state.ConnectedUsers[n].User);
            battle.Users[Name] = new UserBattleStatus(Name, User);
            state.Battles[battleID] = battle;
            MyBattle = battle;
            h.Password = h.Password != null ? "?" : null; // dont send pw to client
            var clis = state.ConnectedUsers.Values.ToList();
            await state.Broadcast(clis, new BattleAdded() { Header = h });
            await state.Broadcast(clis, new JoinedBattle() { BattleID = battleID, User = Name });
        }


        public async Task Process(JoinBattle join)
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
                var ubs = new UserBattleStatus(Name, User);
                if (battle.Users.Values.Count(x => !x.IsSpectator) >= battle.MaxPlayers)
                {
                    ubs.IsSpectator = true;
                }
                battle.Users[Name] = ubs;
                MyBattle = battle;
                await state.Broadcast(state.ConnectedUsers.Values, new JoinedBattle() { BattleID = battle.BattleID, User = Name });
                await RecalcSpectators(battle);
                await state.Broadcast(battle.Users.Keys.Where(x => x != Name), battle.Users[Name].ToUpdateBattleStatus());// send my UBS to others in battle

                foreach (var u in battle.Users.Values.Select(x => x.ToUpdateBattleStatus()).ToList()) await SendCommand(u); // send other's status to self
                foreach (var u in battle.Bots.Values.Select(x => x.ToUpdateBotStatus()).ToList()) await SendCommand(u);
                foreach (var u in battle.Rectangles) await SendCommand(new SetRectangle() { Number = u.Key, Rectangle = u.Value });
                await SendCommand(new SetModOptions() { Options = battle.ModOptions });
            }
        }

        public async Task Process(BattleUpdate battleUpdate)
        {
            if (!IsLoggedIn) return;

            var h = battleUpdate.Header;
            if (h.BattleID == null && MyBattle != null) h.BattleID = MyBattle.BattleID;
            Battle bat;
            if (!state.Battles.TryGetValue(h.BattleID.Value, out bat))
            {
                await Respond("No such battle exists");
                return;
            }
            if (bat.Founder != User && !User.IsAdmin)
            {
                await Respond("You don't have permission to edit this battle");
                return;
            }

            bat.UpdateWith(h, (n) => state.ConnectedUsers[n].User);
            await state.Broadcast(state.ConnectedUsers.Keys, battleUpdate);
        }


        public async Task Process(UpdateUserBattleStatus status)
        {
            if (!IsLoggedIn) return;
            var bat = MyBattle;

            if (bat == null) return;

            if (Name == bat.Founder.Name || Name == status.Name)
            { // founder can set for all, others for self
                UserBattleStatus ubs;
                if (bat.Users.TryGetValue(status.Name, out ubs))
                {

                    // enfoce player count limit
                    if (status.IsSpectator == false && bat.Users[status.Name].IsSpectator == true && bat.Users.Values.Count(x => !x.IsSpectator) >= bat.MaxPlayers)
                    {
                        // if unspeccing but there is already enough, force spec
                        status.IsSpectator = true;
                    }

                    ubs.UpdateWith(status);
                    await state.Broadcast(bat.Users.Keys, status);
                    await RecalcSpectators(bat);
                }
            }
        }

        public async Task RecalcSpectators(Battle bat)
        {
            var specCount = bat.Users.Values.Count(x => x.IsSpectator);
            if (specCount != bat.SpectatorCount)
            {
                bat.SpectatorCount = specCount;
                await state.Broadcast(state.ConnectedUsers.Values, new BattleUpdate() { Header = new BattleHeader() { SpectatorCount = specCount, BattleID = bat.BattleID } });
            }
        }


        public async Task Process(LeaveBattle leave)
        {
            if (!IsLoggedIn) return;


            if (leave.BattleID == null && MyBattle != null) leave.BattleID = MyBattle.BattleID;

            Battle battle;
            if (state.Battles.TryGetValue(leave.BattleID.Value, out battle))
            {
                await LeaveBattle(battle);
                await RecalcSpectators(battle);
            }
        }

        public async Task Process(ChangeUserStatus userStatus)
        {
            if (!IsLoggedIn) return;
            bool changed = false;
            if (userStatus.IsInGame != null && User.IsInGame != userStatus.IsInGame)
            {
                if (userStatus.IsInGame == true) User.InGameSince = DateTime.UtcNow;
                else User.InGameSince = null;
                changed = true;
            }
            if (userStatus.IsAfk != null && User.IsAway != userStatus.IsAfk)
            {
                if (userStatus.IsAfk == true) User.AwaySince = DateTime.UtcNow;
                else User.AwaySince = null;
                changed = true;
            }
            if (changed) await state.Broadcast(state.ConnectedUsers.Values, User);
        }


        public async Task Process(UpdateBotStatus add)
        {
            if (!IsLoggedIn) return;

            var battle = MyBattle;
            if (battle != null)
            {
                BotBattleStatus ubs;
                if (!battle.Bots.TryGetValue(add.Name, out ubs)) ubs = new BotBattleStatus(add.Name, Name, add.AiLib);
                else if (ubs.owner != Name && !User.IsAdmin && User != battle.Founder)
                {
                    await Respond(string.Format("No permissions to edit bot {0}", add.Name));
                    return;
                }
                ubs.UpdateWith(add);
                battle.Bots[ubs.Name] = ubs;
                await state.Broadcast(battle.Users.Keys, ubs.ToUpdateBotStatus());
            }
        }


        public async Task Process(RemoveBot rem)
        {
            if (!IsLoggedIn) return;

            var battle = MyBattle;
            if (battle != null)
            {
                var bot = battle.Bots[rem.Name];
                if (bot.owner != Name && !User.IsAdmin && User != battle.Founder)
                {
                    await Respond(string.Format("No permissions to edit bot {0}", rem.Name));
                    return;
                }
                BotBattleStatus ubs;
                if (battle.Bots.TryRemove(rem.Name, out ubs))
                {
                    await state.Broadcast(battle.Users.Keys, rem);
                }
            }
        }


        async Task LeaveBattle(Battle battle)
        {
            if (battle.Users.ContainsKey(Name))
            {
                if (Name == battle.Founder.Name)
                { // remove entire battle
                    await RemoveBattle(battle);
                }
                else
                {
                    MyBattle = null;
                    UserBattleStatus oldVal;
                    if (battle.Users.TryRemove(Name, out oldVal)) await state.Broadcast(state.ConnectedUsers.Values, new LeftBattle() { BattleID = battle.BattleID, User = Name });
                    var bots = battle.Bots.Values.Where(x => x.owner == Name).ToList();
                    foreach (var b in bots)
                    {
                        BotBattleStatus obs;
                        if (battle.Bots.TryRemove(b.Name, out obs)) await state.Broadcast(battle.Users.Keys, new RemoveBot() { Name = b.Name });
                    }
                }
            }
        }

        public async Task Process(SetModOptions options)
        {
            if (!IsLoggedIn) return;

            var bat = MyBattle;
            if (bat != null)
            {
                if (bat.Founder != User && !User.IsAdmin)
                {
                    await Respond("You don't have permissions to change mod options here");
                    return;
                }
                bat.ModOptions = options.Options;
                await state.Broadcast(bat.Users.Keys, options);
            }
        }


        async Task RemoveBattle(Battle battle)
        {
            foreach (var u in battle.Users.Keys)
            {
                ConnectedUser connectedUser;
                if (state.ConnectedUsers.TryGetValue(u, out connectedUser)) connectedUser.MyBattle = null;
                await state.Broadcast(state.ConnectedUsers.Values, new LeftBattle() { BattleID = battle.BattleID, User = u });
            }
            Battle bat;
            state.Battles.TryRemove(battle.BattleID, out bat);
            await state.Broadcast(state.ConnectedUsers.Values, new BattleRemoved() { BattleID = battle.BattleID });
        }

        public async Task Process(LinkSteam linkSteam)
        {
            await Task.Delay(2000); // steam is slow to get the ticket from client .. wont verify if its checked too soon
            var steamID = state.SteamWebApi.WebValidateAuthToken(linkSteam.Token);
            var info = state.SteamWebApi.WebGetPlayerInfo(steamID);

            using (var db = new ZkDataContext())
            {
                var acc = await db.Accounts.FindAsync(User.AccountID);
                acc.SteamID = steamID;
                acc.SteamName = info.personaname;
                await db.SaveChangesAsync();
                await state.PublishAccountUpdate(acc);
            }
        }
    }

}