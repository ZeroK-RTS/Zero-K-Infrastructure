using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LobbyClient;
using PlasmaShared;
using ZkData;

namespace ZkLobbyServer
{
    public class ConnectedUser: ICommandSender
    {
        public ConcurrentDictionary<ClientConnection, bool> Connections = new ConcurrentDictionary<ClientConnection, bool>();

        public ServerBattle MyBattle;
        private ZkLobbyServer state;
        public User User = new User();
        public HashSet<string> FriendBy { get; set; }
        public HashSet<string> Friends { get; set; }
        public HashSet<string> IgnoredBy { get; set; }
        public HashSet<string> Ignores { get; set; }

        public bool IsLoggedIn { get { return (User != null) && (User.AccountID != 0); } }

        public string Name { get { return User.Name; } }


        public ConnectedUser(ZkLobbyServer server, User user)
        {
            state = server;
            User = user;

            LoadFriendsIgnores();
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
            await Task.WhenAll(Connections.Keys.Select(async (con) => { await con.SendLine(line); }));
        }

        public void LoadFriendsIgnores()
        {
            using (var db = new ZkDataContext())
            {
                var rels =
                    db.AccountRelations.Where(x => (x.TargetAccountID == User.AccountID) || (x.OwnerAccountID == User.AccountID))
                        .Select(x => new { OwnerAccountID = x.OwnerAccountID, Owner = x.Owner.Name, Target = x.Target.Name, Relation = x.Relation })
                        .ToList();

                Friends =
                    new HashSet<string>(rels.Where(x => (x.Relation == Relation.Friend) && (x.OwnerAccountID == User.AccountID)).Select(x => x.Target));
                FriendBy =
                    new HashSet<string>(rels.Where(x => (x.Relation == Relation.Friend) && (x.OwnerAccountID != User.AccountID)).Select(x => x.Owner));

                Ignores =
                    new HashSet<string>(rels.Where(x => (x.Relation == Relation.Ignore) && (x.OwnerAccountID == User.AccountID)).Select(x => x.Target));
                IgnoredBy =
                    new HashSet<string>(rels.Where(x => (x.Relation == Relation.Ignore) && (x.OwnerAccountID != User.AccountID)).Select(x => x.Owner));
            }
        }


        public async Task Process(MatchMakerQueueRequest queueRequest)
        {
            await state.MatchMaker.QueueRequest(this, queueRequest);
        }

        public async Task Process(KickFromBattle batKick)
        {
            if (!IsLoggedIn) return;

            if ((batKick.BattleID == null) && (MyBattle != null)) batKick.BattleID = MyBattle.BattleID;
            ServerBattle bat;
            if (state.Battles.TryGetValue(batKick.BattleID.Value, out bat))
            {
                if ((bat.FounderName != Name) && !User.IsAdmin && (Name != batKick.Name))
                {
                    await Respond("No rights to do a kick");
                    return;
                }
                await bat.KickFromBattle(batKick.Name, batKick.Reason);
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

            ServerBattle bat;
            if (state.Battles.TryGetValue(forceJoin.BattleID, out bat)) await state.ForceJoinBattle(forceJoin.Name, bat);
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

                await
                    connectedUser.Process(new JoinChannel()
                    {
                        ChannelName = forceJoin.ChannelName,
                        Password = channel != null ? channel.Password : null
                    });
            }
        }


        public async Task Process(JoinChannel joinChannel)
        {
            if (!IsLoggedIn) return;

            if (!await state.ChannelManager.CanJoin(User.AccountID, joinChannel.ChannelName))
            {
                await
                    SendCommand(new JoinChannelResponse()
                    {
                        Success = false,
                        Reason = "you don't have permission to join this channel",
                        ChannelName = joinChannel.ChannelName
                    });
                return;
            }

            var channel = state.Rooms.GetOrAdd(joinChannel.ChannelName, (n) => new Channel() { Name = joinChannel.ChannelName, });
            if (!string.IsNullOrEmpty(channel.Password) && (channel.Password != joinChannel.Password))
            {
                await SendCommand(new JoinChannelResponse() { Success = false, Reason = "invalid password", ChannelName = joinChannel.ChannelName });
                return;
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


        public async Task Process(RequestConnectSpring connectSpring)
        {
            if (!IsLoggedIn) return;

            ServerBattle battle;
            if (state.Battles.TryGetValue(connectSpring.BattleID, out battle) && battle.IsInGame)
            {
                UserBattleStatus ubs;
                if (!battle.Users.TryGetValue(Name, out ubs))
                {
                    if (battle.IsPassworded && (battle.Password != connectSpring.Password))
                    {
                        await Respond("Invalid password");
                        return;
                    }

                    ubs = new UserBattleStatus(Name, User, Guid.NewGuid().ToString());
                    battle.Users[Name] = ubs;
                    battle.ValidateBattleStatus(ubs);
                    await battle.ProcessPlayerJoin(ubs);
                }


                await SendCommand(battle.GetConnectSpringStructure(ubs));
            }
            else await Respond("No such running battle found");
        }


        public async Task Process(Say say)
        {
            if (!IsLoggedIn) return;
            if (User.BanMute) return; // block all say for muted

            say.User = Name;
            say.Time = DateTime.UtcNow;

            if (say.Ring)
                if (!User.IsAdmin)
                    if (((say.Place != SayPlace.Battle) && (say.Place != SayPlace.BattlePrivate)) || (MyBattle == null) ||
                        (MyBattle.FounderName != Name)) say.Ring = false;

            switch (say.Place)
            {
                case SayPlace.Channel:
                    Channel channel;
                    if (state.Rooms.TryGetValue(say.Target, out channel))
                        if (channel.Users.ContainsKey(Name))
                        {
                            await state.Broadcast(channel.Users.Keys.Where(x => state.CanChatTo(say.User, x)), say);
                            await state.OfflineMessageHandler.StoreChatHistory(say);
                        }
                    break;

                case SayPlace.User:
                    ConnectedUser connectedUser;
                    if (state.ConnectedUsers.TryGetValue(say.Target, out connectedUser) && state.CanChatTo(say.User, say.Target)) await connectedUser.SendCommand(say);
                    else await state.OfflineMessageHandler.StoreChatHistory(say);
                    await SendCommand(say);

                    break;

                case SayPlace.Battle:
                    if (MyBattle != null)
                    {
                        say.Target = MyBattle?.FounderName ?? "";
                        await state.Broadcast(MyBattle?.Users?.Keys.Where(x => state.CanChatTo(say.User, x)), say);
                        await MyBattle.ProcessBattleSay(say);
                        await state.OfflineMessageHandler.StoreChatHistory(say);
                    }
                    break;

                case SayPlace.BattlePrivate:
                    if ((MyBattle != null) && (MyBattle.FounderName == Name))
                    {
                        ConnectedUser cli;
                        if (MyBattle.Users.ContainsKey(say.Target))
                            if (state.ConnectedUsers.TryGetValue(say.Target, out cli) && state.CanChatTo(say.User, say.Target))
                            {
                                await cli.SendCommand(say);
                                await MyBattle.ProcessBattleSay(say);
                            }
                    }
                    break;
                case SayPlace.MessageBox:
                    if (User.IsAdmin) await state.Broadcast(state.ConnectedUsers.Values, say);
                    break;
            }

            await state.OnSaid(say);
        }

        public async Task Process(OpenBattle openBattle)
        {
            if (!IsLoggedIn) return;

            if (MyBattle != null)
            {
                await Respond("You are already in a battle");
                return;
            }

            var battleID = Interlocked.Increment(ref state.BattleCounter);

            openBattle.Header.BattleID = battleID;
            openBattle.Header.Founder = Name;
            var battle = new ServerBattle(state, false);
            battle.UpdateWith(openBattle.Header);
            state.Battles[battleID] = battle;

            //battle.Users[Name] = new UserBattleStatus(Name, User, Guid.NewGuid().ToString());
            //MyBattle = battle;

            await state.Broadcast(state.ConnectedUsers.Keys, new BattleAdded() { Header = battle.GetHeader() });
            await Process(new JoinBattle() { BattleID = battleID, Password = openBattle.Header.Password, });
        }


        public async Task Process(JoinBattle join)
        {
            if (!IsLoggedIn) return;

            if (MyBattle != null)
            {
                await Respond("You are already in other battle");
                return;
            }

            ServerBattle battle;
            if (state.Battles.TryGetValue(join.BattleID, out battle))
            {
                if (battle.IsPassworded && (battle.Password != @join.Password))
                {
                    await Respond("Invalid password");
                    return;
                }
                var ubs = new UserBattleStatus(Name, User, Guid.NewGuid().ToString());
                battle.Users[Name] = ubs;
                battle.ValidateBattleStatus(ubs);
                MyBattle = battle;

                await state.Broadcast(state.ConnectedUsers.Keys, new JoinedBattle() { BattleID = battle.BattleID, User = Name });
                await RecalcSpectators(battle);
                await state.Broadcast(battle.Users.Keys.Where(x => x != Name), ubs.ToUpdateBattleStatus()); // send my UBS to others in battle
                foreach (var u in battle.Users.Values.Select(x => x.ToUpdateBattleStatus()).ToList()) await SendCommand(u); // send other's status to self
                foreach (var u in battle.Bots.Values.Select(x => x.ToUpdateBotStatus()).ToList()) await SendCommand(u);
                await SendCommand(new SetModOptions() { Options = battle.ModOptions });

                await battle.ProcessPlayerJoin(ubs);
            }
        }

        public async Task Process(BattleUpdate battleUpdate)
        {
            if (!IsLoggedIn) return;

            var h = battleUpdate.Header;
            if ((h.BattleID == null) && (MyBattle != null)) h.BattleID = MyBattle.BattleID;
            ServerBattle bat;
            if (!state.Battles.TryGetValue(h.BattleID.Value, out bat))
            {
                await Respond("No such battle exists");
                return;
            }
            if ((bat.FounderName != Name) && !User.IsAdmin)
            {
                await Respond("You don't have permission to edit this battle");
                return;
            }

            bat.UpdateWith(h);
            await state.Broadcast(state.ConnectedUsers.Keys, battleUpdate);
        }


        public async Task Process(UpdateUserBattleStatus status)
        {
            if (!IsLoggedIn) return;
            var bat = MyBattle;

            if (bat == null) return;

            if ((Name == bat.FounderName) || (Name == status.Name))
            {
                // founder can set for all, others for self
                UserBattleStatus ubs;
                if (bat.Users.TryGetValue(status.Name, out ubs))
                {
                    // enfoce player count limit
                    if ((status.IsSpectator == false) && (bat.Users[status.Name].IsSpectator == true) &&
                        (bat.Users.Values.Count(x => !x.IsSpectator) >= bat.MaxPlayers)) status.IsSpectator = true;

                    ubs.UpdateWith(status);
                    bat.ValidateBattleStatus(ubs);

                    await state.Broadcast(bat.Users.Keys, ubs.ToUpdateBattleStatus());
                    await RecalcSpectators(bat);
                }
            }
        }


        public async Task Process(LeaveBattle leave)
        {
            if (!IsLoggedIn) return;

            if ((leave.BattleID == null) && (MyBattle != null)) leave.BattleID = MyBattle.BattleID;

            ServerBattle battle;
            if (state.Battles.TryGetValue(leave.BattleID.Value, out battle))
            {
                await LeaveBattle(battle);
                await RecalcSpectators(battle);
            }
        }

        public async Task Process(ChangeUserStatus userStatus)
        {
            if (!IsLoggedIn) return;
            var changed = false;
            if ((userStatus.IsInGame != null) && (User.IsInGame != userStatus.IsInGame))
            {
                if (userStatus.IsInGame == true) User.InGameSince = DateTime.UtcNow;
                else User.InGameSince = null;
                changed = true;
            }
            if ((userStatus.IsAfk != null) && (User.IsAway != userStatus.IsAfk))
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
            if ((battle != null) && !battle.IsInGame)
            {
                if (battle.Mode != AutohostMode.None && battle.Mode != AutohostMode.GameChickens)
                {
                    await Respond("Sorry, this room type does not support bots, please use cooperative or custom");
                    return;
                }

                BotBattleStatus ubs;
                if (!battle.Bots.TryGetValue(add.Name, out ubs))
                {
                    if (battle.Bots.Count < 50)
                    {
                        ubs = new BotBattleStatus(add.Name, Name, add.AiLib);
                    }
                    else
                    {
                        await Respond("Maximal number of bots reached");
                        return;
                    }
                }
                else if ((ubs.owner != Name) && !User.IsAdmin && (Name != battle.FounderName))
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
            if ((battle != null) && !battle.IsInGame)
            {
                var bot = battle.Bots[rem.Name];
                if ((bot.owner != Name) && !User.IsAdmin && (Name != battle.FounderName))
                {
                    await Respond(string.Format("No permissions to edit bot {0}", rem.Name));
                    return;
                }
                BotBattleStatus ubs;
                if (battle.Bots.TryRemove(rem.Name, out ubs)) await state.Broadcast(battle.Users.Keys, rem);
            }
        }

        public async Task Process(SetModOptions options)
        {
            if (!IsLoggedIn) return;

            var bat = MyBattle;
            if (bat != null)
            {
                if ((bat.FounderName != Name) && !User.IsAdmin)
                {
                    await Respond("You don't have permissions to change mod options here");
                    return;
                }
                await bat.SetModOptions(options.Options);
            }
        }

        public async Task Process(LinkSteam linkSteam)
        {
            await Task.Delay(2000); // steam is slow to get the ticket from client .. wont verify if its checked too soon

            try
            {
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
            catch (Exception ex)
            {
                Trace.TraceError("Error linking steam: {0}", ex);
            }
        }

        public async Task Process(SetAccountRelation rel)
        {
            if (!IsLoggedIn) return;

            if (string.IsNullOrEmpty(rel.TargetName)) return;

            using (var db = new ZkDataContext())
            {
                var srcAccount = db.Accounts.Find(User.AccountID);
                var trgtAccount = Account.AccountByName(db, rel.TargetName);
                if (trgtAccount == null)
                {
                    await Respond("No such account found");
                    return;
                }

                var entry = srcAccount.RelalationsByOwner.FirstOrDefault(x => x.TargetAccountID == trgtAccount.AccountID);
                if ((rel.Relation == Relation.None) && (entry != null)) db.AccountRelations.Remove(entry);
                if (rel.Relation != Relation.None)
                    if (entry == null)
                    {
                        entry = new AccountRelation() { Owner = srcAccount, Target = trgtAccount, Relation = rel.Relation };
                        srcAccount.RelalationsByOwner.Add(entry);
                    }
                    else entry.Relation = rel.Relation;
                db.SaveChanges();

                ConnectedUser connectedUser;
                if (state.ConnectedUsers.TryGetValue(trgtAccount.Name, out connectedUser)) connectedUser.LoadFriendsIgnores();
                if (state.ConnectedUsers.TryGetValue(srcAccount.Name, out connectedUser))
                {
                    connectedUser.LoadFriendsIgnores();
                    await connectedUser.SendCommand(new FriendList() { Friends = Friends.ToList() });
                    await connectedUser.SendCommand(new IgnoreList() { Ignores = Ignores.ToList() });
                }
            }
        }

        public async Task RecalcSpectators(Battle bat)
        {
            var specCount = bat.Users.Values.Count(x => x.IsSpectator);
            if (specCount != bat.SpectatorCount)
            {
                bat.SpectatorCount = specCount;
                await
                    state.Broadcast(state.ConnectedUsers.Values,
                        new BattleUpdate() { Header = new BattleHeader() { SpectatorCount = specCount, BattleID = bat.BattleID } });
            }
        }


        public async Task RemoveConnection(ClientConnection con, string reason)
        {
            bool dummy;
            if (Connections.TryRemove(con, out dummy) && (Connections.Count == 0))
            {
                // notify all channels where i am to all users that i left 
                foreach (var chan in state.Rooms.Values.Where(x => x.Users.ContainsKey(Name)).ToList()) await Process(new LeaveChannel() { ChannelName = chan.Name });

                foreach (var b in state.Battles.Values.Where(x => x.Users.ContainsKey(Name)))
                {
                    await LeaveBattle(b);
                    await RecalcSpectators(b);
                }


                await state.MatchMaker.RemoveUser(Name);

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

        public void RequestCloseAll()
        {
            foreach (var c in Connections.Keys) c.RequestClose();
        }

        public Task Respond(string message)
        {
            return SendCommand(new Say() { Place = SayPlace.MessageBox, Target = Name, User = Name, Text = message });
        }


        public async Task Process(AreYouReadyResponse response)
        {
            await state.MatchMaker.AreYouReadyResponse(this, response);
        }

        public override string ToString()
        {
            return string.Format("[{0}]", Name);
        }


        private async Task LeaveBattle(Battle battle)
        {
            if (battle.Users.ContainsKey(Name))
                if (battle.Users.Count == 1) // last user remove entire battle
                {
                    await state.RemoveBattle(battle);
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
}