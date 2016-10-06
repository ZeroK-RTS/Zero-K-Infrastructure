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

        private ServerBattle myBattle;
        public ServerBattle MyBattle { get {return myBattle;}
            set
            {
                myBattle = value;
                var bid = value?.BattleID;
                if (User.BattleID != bid)
                {
                    User.BattleID = bid;
                    Interlocked.Increment(ref User.SyncVersion);
                }
            }
        }
        private ZkLobbyServer server;
        public User User = new User();
        public HashSet<string> FriendBy { get; set; }
        public HashSet<string> Friends { get; set; }
        public HashSet<string> IgnoredBy { get; set; }
        public HashSet<string> Ignores { get; set; }

        public ConcurrentDictionary<string, int> HasSeenUserVersion { get; set; } = new ConcurrentDictionary<string, int>();

        public bool IsLoggedIn => (User != null) && (User.AccountID != 0);

        public string Name => User.Name;


        public ConnectedUser(ZkLobbyServer server, User user)
        {
            this.server = server;
            User = user;

            LoadFriendsIgnores();
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
            await server.MatchMaker.QueueRequest(this, queueRequest);
        }

        public async Task Process(KickFromBattle batKick)
        {
            if (!IsLoggedIn) return;

            if ((batKick.BattleID == null) && (MyBattle != null)) batKick.BattleID = MyBattle.BattleID;
            ServerBattle bat;
            if (server.Battles.TryGetValue(batKick.BattleID.Value, out bat))
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
            if (server.Battles.TryGetValue(forceJoin.BattleID, out bat)) await server.ForceJoinBattle(forceJoin.Name, bat);
        }


        public async Task Process(KickFromChannel chanKick)
        {
            if (!IsLoggedIn) return;

            Channel channel;
            User user;
            if (server.Channels.TryGetValue(chanKick.ChannelName, out channel) && channel.Users.TryGetValue(chanKick.UserName, out user))
            {
                if (!User.IsAdmin)
                {
                    await Respond("No rights to execute kick");
                    return;
                }

                var client = server.ConnectedUsers[chanKick.UserName];
                await client.Respond(string.Format("You were kicked from channel {0} by {1} : {2}", chanKick.ChannelName, Name, chanKick.Reason));
                await client.Process(new LeaveChannel() { ChannelName = chanKick.ChannelName });
            }
        }

        public async Task Process(KickFromServer kick)
        {
            if (!IsLoggedIn) return;

            ConnectedUser connectedUser;
            if (server.ConnectedUsers.TryGetValue(kick.Name, out connectedUser))
            {
                if (!User.IsAdmin)
                {
                    await Respond("No rights to execute kick");
                    return;
                }

                server.KickFromServer(Name, kick.Name, kick.Reason);
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
            if (server.ConnectedUsers.TryGetValue(forceJoin.UserName, out connectedUser))
            {
                Channel channel;
                server.Channels.TryGetValue(forceJoin.ChannelName, out channel);

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

            if (!await server.ChannelManager.CanJoin(User.AccountID, joinChannel.ChannelName))
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

            var channel = server.Channels.GetOrAdd(joinChannel.ChannelName, (n) => new Channel() { Name = joinChannel.ChannelName, });
            if (!string.IsNullOrEmpty(channel.Password) && (channel.Password != joinChannel.Password))
            {
                await SendCommand(new JoinChannelResponse() { Success = false, Reason = "invalid password", ChannelName = joinChannel.ChannelName });
                return;
            }

            var added = channel.Users.TryAdd(Name, User);
            var visibleUsers = channel.Name != "zk" ? channel.Users.Keys.ToList() : channel.Users.Keys.Where(x => server.CanUserSee(Name, x)).ToList();
            var canSeeMe = channel.Name != "zk" ? channel.Users.Keys.ToList() : channel.Users.Keys.Where(x => server.CanUserSee(x, Name)).ToList();

            await server.TwoWaySyncUsers(Name, channel.Users.Keys); // mutually sync user statuses

            // send response with the list
            await SendCommand(new JoinChannelResponse()
                {
                    Success = true,
                    ChannelName = joinChannel.ChannelName,
                    Channel =
                        new ChannelHeader()
                        {
                            ChannelName = channel.Name,
                            Password = channel.Password,
                            Topic = channel.Topic,
                            UserCount = channel.Users.Count,
                            Users = visibleUsers // for zk use cansee test to not send all users
                        }
                });

            // send missed messages
            await server.OfflineMessageHandler.SendMissedMessages(this, SayPlace.Channel, joinChannel.ChannelName, User.AccountID);

            // send self to other users who can see 
            if (added) await server.Broadcast(canSeeMe, new ChannelUserAdded { ChannelName = channel.Name, UserName = Name });
        }


        public async Task Process(LeaveChannel leaveChannel)
        {
            if (!IsLoggedIn) return;

            Channel channel;
            if (server.Channels.TryGetValue(leaveChannel.ChannelName, out channel))
            {
                User user;
                var users = channel.Name != "zk" ? channel.Users.Keys.ToList() : channel.Users.Keys.Where(x => server.CanUserSee(x, Name)).ToList();
                if (channel.Users.TryRemove(Name, out user))
                {
                    await server.Broadcast(users, new ChannelUserRemoved() { ChannelName = channel.Name, UserName = Name });
                }
            }
        }


        public async Task Process(RequestConnectSpring connectSpring)
        {
            if (!IsLoggedIn) return;

            ServerBattle battle;
            if (server.Battles.TryGetValue(connectSpring.BattleID, out battle) && battle.IsInGame)
            {
                await battle.RequestConnectSpring(this, connectSpring.Password);
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
                    if (server.Channels.TryGetValue(say.Target, out channel))
                        if (channel.Users.ContainsKey(Name))
                        {
                            await server.Broadcast(channel.Users.Keys.Where(x => server.CanChatTo(say.User, x)), say);
                            server.OfflineMessageHandler.StoreChatHistory(say);
                        }
                    break;

                case SayPlace.User:
                    ConnectedUser connectedUser;
                    if (server.ConnectedUsers.TryGetValue(say.Target, out connectedUser) && server.CanChatTo(say.User, say.Target)) await connectedUser.SendCommand(say);
                    else server.OfflineMessageHandler.StoreChatHistory(say);
                    await SendCommand(say);

                    break;

                case SayPlace.Battle:
                    if (MyBattle != null)
                    {
                        say.Target = MyBattle?.FounderName ?? "";
                        await server.Broadcast(MyBattle?.Users?.Keys.Where(x => server.CanChatTo(say.User, x)), say);
                        await MyBattle.ProcessBattleSay(say);
                        server.OfflineMessageHandler.StoreChatHistory(say);
                    }
                    break;

                case SayPlace.BattlePrivate:
                    if ((MyBattle != null) && (MyBattle.FounderName == Name))
                    {
                        ConnectedUser cli;
                        if (MyBattle.Users.ContainsKey(say.Target))
                            if (server.ConnectedUsers.TryGetValue(say.Target, out cli) && server.CanChatTo(say.User, say.Target))
                            {
                                await cli.SendCommand(say);
                                await MyBattle.ProcessBattleSay(say);
                            }
                    }
                    break;
                case SayPlace.MessageBox:
                    if (User.IsAdmin) await server.Broadcast(server.ConnectedUsers.Values, say);
                    break;
            }

            await server.OnSaid(say);
        }

        public async Task Process(OpenBattle openBattle)
        {
            if (!IsLoggedIn) return;

            if (MyBattle != null)
            {
                await Respond("You are already in a battle");
                return;
            }

            var battle = new ServerBattle(server, Name);
            battle.UpdateWith(openBattle.Header);
            server.Battles[battle.BattleID] = battle;


            await server.Broadcast(server.ConnectedUsers.Keys, new BattleAdded() { Header = battle.GetHeader() });
            await Process(new JoinBattle() { BattleID = battle.BattleID, Password = openBattle.Header.Password, });
        }


        public async Task Process(JoinBattle join)
        {
            if (!IsLoggedIn) return;

            ServerBattle battle;
            if (server.Battles.TryGetValue(join.BattleID, out battle))
            {
                await battle.ProcessPlayerJoin(this, join.Password);
            }
        }

        public async Task Process(BattleUpdate battleUpdate)
        {
            if (!IsLoggedIn) return;

            var h = battleUpdate.Header;
            if ((h.BattleID == null) && (MyBattle != null)) h.BattleID = MyBattle.BattleID;
            ServerBattle bat;
            if (!server.Battles.TryGetValue(h.BattleID.Value, out bat))
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
            await server.Broadcast(server.ConnectedUsers.Keys, battleUpdate);
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

                    await server.Broadcast(bat.Users.Keys, ubs.ToUpdateBattleStatus());
                    await bat.RecalcSpectators();
                }
            }
        }


        public async Task Process(LeaveBattle leave)
        {
            if (!IsLoggedIn) return;

            if ((leave.BattleID == null) && (MyBattle != null)) leave.BattleID = MyBattle.BattleID;

            ServerBattle battle;
            if (server.Battles.TryGetValue(leave.BattleID.Value, out battle))
            {
                await LeaveBattle(battle);
                await battle.RecalcSpectators();
            }
        }

        public async Task Process(ChangeUserStatus userStatus)
        {
            if (!IsLoggedIn || userStatus == null) return;
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
            if (changed)
            {
                Interlocked.Increment(ref User.SyncVersion);
                await server.SyncUserToOthers(this);
            }
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
                await server.Broadcast(battle.Users.Keys, ubs.ToUpdateBotStatus());
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
                if (battle.Bots.TryRemove(rem.Name, out ubs)) await server.Broadcast(battle.Users.Keys, rem);
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
                var steamID = server.SteamWebApi.WebValidateAuthToken(linkSteam.Token);
                var info = server.SteamWebApi.WebGetPlayerInfo(steamID);

                using (var db = new ZkDataContext())
                {
                    var acc = await db.Accounts.FindAsync(User.AccountID);
                    acc.SteamID = steamID;
                    acc.SteamName = info.personaname;
                    await db.SaveChangesAsync();
                    await server.PublishAccountUpdate(acc);
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
                if (server.ConnectedUsers.TryGetValue(trgtAccount.Name, out connectedUser)) connectedUser.LoadFriendsIgnores();
                if (server.ConnectedUsers.TryGetValue(srcAccount.Name, out connectedUser))
                {
                    connectedUser.LoadFriendsIgnores();
                    await connectedUser.SendCommand(new FriendList() { Friends = Friends.ToList() });
                    await connectedUser.SendCommand(new IgnoreList() { Ignores = Ignores.ToList() });
                }
            }
        }

        public async Task RemoveConnection(ClientConnection con, string reason)
        {
            bool dummy;
            if (Connections.TryRemove(con, out dummy) && (Connections.Count == 0))
            {
                // notify all channels where i am to all users that i left 
                foreach (var chan in server.Channels.Values.Where(x => x.Users.ContainsKey(Name)).ToList()) await Process(new LeaveChannel() { ChannelName = chan.Name });

                foreach (var b in server.Battles.Values.Where(x => x.Users.ContainsKey(Name)))
                {
                    await LeaveBattle(b);
                    await b.RecalcSpectators();
                }


                await server.MatchMaker.RemoveUser(Name, true);

                await server.Broadcast(server.ConnectedUsers.Values.Where(x=>x!=null && server.CanUserSee(x, this)), new UserDisconnected() { Name = Name, Reason = reason });

                ConnectedUser connectedUser;
                server.ConnectedUsers.TryRemove(Name, out connectedUser);

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
            await server.MatchMaker.AreYouReadyResponse(this, response);
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
                    await server.RemoveBattle(battle);
                }
                else
                {
                    MyBattle = null;
                    UserBattleStatus oldVal;
                    var seers = server.ConnectedUsers.Values.Where(x => x != null && server.CanUserSee(x, this)).ToList();
                    if (battle.Users.TryRemove(Name, out oldVal)) await server.Broadcast(seers, new LeftBattle() { BattleID = battle.BattleID, User = Name });
                    await server.SyncUserToOthers(this);
                    var bots = battle.Bots.Values.Where(x => x.owner == Name).ToList();
                    foreach (var b in bots)
                    {
                        BotBattleStatus obs;
                        if (battle.Bots.TryRemove(b.Name, out obs)) await server.Broadcast(battle.Users.Keys, new RemoveBot() { Name = b.Name });
                    }
                }
        }


    }
}