using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LobbyClient;
using PlasmaShared;
using ZkData;

namespace ZkLobbyServer
{
    public class ConnectedUser : ICommandSender
    {
        public ConcurrentDictionary<ClientConnection, bool> Connections = new ConcurrentDictionary<ClientConnection, bool>();

        private ServerBattle myBattle;
        public ServerBattle MyBattle
        {
            get { return myBattle; }
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
        private DateTime chatWait = DateTime.UtcNow;
        public User User = new User();
        public HashSet<string> FriendBy { get; set; }
        public HashSet<string> FriendNames { get; set; }

        public List<FriendEntry> FriendEntries { get; set; } = new List<FriendEntry>();
        public HashSet<string> IgnoredBy { get; set; }
        public HashSet<string> Ignores { get; set; }

        public ConcurrentDictionary<string, int> HasSeenUserVersion { get; set; } = new ConcurrentDictionary<string, int>();

        public bool IsLoggedIn => (User != null) && (User.AccountID != 0);

        public string Name => User.Name;

        private PartyManager partyManager => server.PartyManager;


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
                        .Select(x => new { OwnerAccountID = x.OwnerAccountID, Owner = x.Owner.Name, Target = x.Target.Name, Relation = x.Relation, SteamID = x.Target.SteamID })
                        .ToList();

                FriendNames =
                    new HashSet<string>(rels.Where(x => (x.Relation == Relation.Friend) && (x.OwnerAccountID == User.AccountID)).Select(x => x.Target));
                FriendBy =
                    new HashSet<string>(rels.Where(x => (x.Relation == Relation.Friend) && (x.OwnerAccountID != User.AccountID)).Select(x => x.Owner));

                FriendEntries = new List<FriendEntry>(rels.Where(x => (x.Relation == Relation.Friend) && (x.OwnerAccountID == User.AccountID)).Select(x => new FriendEntry() { Name = x.Target, SteamID = x.SteamID?.ToString() }));

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

        public async Task Process(PwJoinPlanet args)
        {
            await server.PlanetWarsMatchMaker.OnJoinPlanet(this, args);
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

                await server.KickFromServer(Name, kick.Name, kick.Reason);
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
            if (!added)
            {
                await
                    SendCommand(new JoinChannelResponse()
                    {
                        Success = false,
                        Reason = "You are already in this channel",
                        ChannelName = joinChannel.ChannelName
                    });
                return;
            }
            var visibleUsers = !channel.IsDeluge ? channel.Users.Keys.ToList() : channel.Users.Keys.Where(x => server.CanUserSee(Name, x)).ToList();
            var canSeeMe = !channel.IsDeluge ? channel.Users.Keys.ToList() : channel.Users.Keys.Where(x => server.CanUserSee(x, Name)).ToList();

            await server.TwoWaySyncUsers(Name, canSeeMe); // mutually sync user statuses

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
                            Users = visibleUsers, // for zk use cansee test to not send all users
                            IsDeluge = channel.IsDeluge
                        }
            });

            // send missed messages
            server.OfflineMessageHandler.SendMissedMessagesAsync(this, SayPlace.Channel, joinChannel.ChannelName, User.AccountID, channel.IsDeluge ? OfflineMessageHandler.DelugeMessageResendCount : OfflineMessageHandler.MessageResendCount);

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
                var users = !channel.IsDeluge ? channel.Users.Keys.ToList() : channel.Users.Keys.Where(x => server.CanUserSee(x, Name)).ToList();
                if (channel.Users.TryRemove(Name, out user))
                {
                    await server.Broadcast(users, new ChannelUserRemoved() { ChannelName = channel.Name, UserName = Name });
                }
            }
        }

        public async Task Process(JoinFactionRequest joinFaction)
        {
            var db = new ZkDataContext();
            var acc = db.Accounts.Find(User.AccountID);
            var fac = db.Factions.First(x => !x.IsDeleted && x.Shortcut == joinFaction.Faction);
            if (acc.FactionID == null)
            {
                if (acc.Clan != null && acc.Clan.FactionID == null) // if your clan is faction-less join the faciton too
                {
                    acc.Clan.FactionID = fac.FactionID;
                    foreach (Account member in acc.Clan.Accounts) member.FactionID = fac.FactionID;
                    db.SaveChanges();
                    db.Events.InsertOnSubmit(server.PlanetWarsEventCreator.CreateEvent("Clan {0} moved to faction {1}", acc.Clan, fac));
                }
                acc.FactionID = fac.FactionID;
            }
            db.SaveChanges();
            db.Events.InsertOnSubmit(server.PlanetWarsEventCreator.CreateEvent("{0} joins {1}", acc, fac));
            db.SaveChanges();
            await server.PublishAccountUpdate(acc);
            await server.PublishUserProfileUpdate(acc);
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
            if (DateTime.UtcNow < chatWait) return; //block all say for spam
            if (say.Text.Length > GlobalConst.LobbyMaxMessageSize) say.Text = say.Text.Substring(0, GlobalConst.LobbyMaxMessageSize);
            if (DateTime.UtcNow.AddMilliseconds(-5 * GlobalConst.MinMillisecondsBetweenMessages) > chatWait) chatWait = DateTime.UtcNow.AddMilliseconds(-5 * GlobalConst.MinMillisecondsBetweenMessages);
            chatWait = chatWait.AddMilliseconds(Math.Max(GlobalConst.MinMillisecondsBetweenMessages, GlobalConst.MillisecondsPerCharacter * say.Text.Length));

            say.User = Name;
            say.Time = DateTime.UtcNow;

            if (say.Ring)
                if (!User.IsAdmin)
                    if (((say.Place != SayPlace.Battle) && (say.Place != SayPlace.BattlePrivate)) || (MyBattle == null) ||
                        (MyBattle.FounderName != Name)) say.Ring = false;



            // verify basic permissions to talk
            switch (say.Place)
            {
                case SayPlace.Channel:
                    if (server.Channels.Get(say.Target)?.Users?.ContainsKey(Name) != true) return;
                    break;

                case SayPlace.Battle:
                    if (MyBattle?.Users?.Keys.Contains(Name) != true) return;
                    break;

                case SayPlace.BattlePrivate:
                    return;
                    break;

                case SayPlace.MessageBox:
                    if (!User.IsAdmin) return;
                    break;

            }

            await server.GhostSay(say, MyBattle?.BattleID);
        }

        public async Task Process(OpenBattle openBattle)
        {
            if (!IsLoggedIn) return;

            if (string.IsNullOrEmpty(openBattle.Header.Password) && User.BanVotes)
            {
                await Respond("Your rights have been restricted. You can only open passworded battles. Check your user page for details.");
                return;
            }

            if (MyBattle != null)
            {
                await Respond("You are already in a battle");
                return;
            }

            if (openBattle.Header.Mode != null 
                && !Enum.IsDefined(typeof(AutohostMode), openBattle.Header.Mode))
            {
                await Respond("Incorrect battle type");
                return;
            }

            openBattle.Header.Title = openBattle.Header.Title.Truncate(200);
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
            h.Title = h.Title.Truncate(200);

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

            if (battleUpdate.Header.Mode != null 
                && !Enum.IsDefined(typeof(AutohostMode), battleUpdate.Header.Mode))
            {
                await Respond("Incorrect battle type");
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
                await server.SyncUserToAll(this);
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
                if ((bat.FounderName != Name || bat.IsAutohost) && !User.IsAdmin)
                {
                    await Respond("You don't have permissions to change mod options here");
                    return;
                }
                await bat.SetModOptions(options.Options);
            }
        }

        public async Task Process(SetMapOptions options)
        {
            if (!IsLoggedIn) return;

            var bat = MyBattle;
            if (bat != null)
            {
                if ((bat.FounderName != Name || bat.IsAutohost) && !User.IsAdmin)
                {
                    await Respond("You don't have permissions to change mod options here");
                    return;
                }
                await bat.SetMapOptions(options.Options);
            }
        }

        public async Task Process(UserReport report)
        {
            if (!IsLoggedIn) return;

            if (string.IsNullOrEmpty(report.Username) && string.IsNullOrEmpty(report.Text)) return;

            using (var db = new ZkDataContext())
            {
                var reporter = db.Accounts.FirstOrDefault(x => x.AccountID == User.AccountID);
                var reported = db.Accounts.FirstOrDefault(x => x.Name == report.Username);
                if (reported == null || reporter == null) return;

                await server.ReportUser(db, reporter, reported, report.Text);
            }
        }
        
       
        public async Task Process(GetCustomGameMode modeRequest)
        {
            using (var db = new ZkDataContext())
            {
                var mode  = db.GameModes.FirstOrDefault(x => x.ShortName == modeRequest.ShortName);
                if (mode == null)
                {
                    await SendCommand(new CustomGameModeResponse() { ShortName = modeRequest.ShortName });
                }
                else
                {
                    await SendCommand(new CustomGameModeResponse()
                    {
                        ShortName = mode.ShortName,
                        DisplayName = mode.DisplayName,
                        GameModeJson = mode.GameModeJson
                    });
                }
            }
        }
        

        public async Task Process(SetAccountRelation rel)
        {
            if (!IsLoggedIn) return;

            if (string.IsNullOrEmpty(rel.TargetName) && string.IsNullOrEmpty(rel.SteamID)) return;

            using (var db = new ZkDataContext())
            {
                ulong steamId = 0;

                var srcAccount = db.Accounts.Find(User.AccountID);
                ulong.TryParse(rel.SteamID, out steamId);
                var trgtAccount = Account.AccountByName(db, rel.TargetName) ?? db.Accounts.FirstOrDefault(x => x.SteamID == steamId);
                if (trgtAccount == null)
                {
                    if (!string.IsNullOrEmpty(rel.TargetName)) await Respond("No such account found"); // only warn if name is set and not just steam id
                    return;
                }

                var friendAdded = false;

                var entry = srcAccount.RelalationsByOwner.FirstOrDefault(x => x.TargetAccountID == trgtAccount.AccountID);
                if ((rel.Relation == Relation.None) && (entry != null)) db.AccountRelations.Remove(entry);
                if (rel.Relation != Relation.None)
                    if (entry == null)
                    {
                        if (rel.Relation == Relation.Friend) friendAdded = true;
                        entry = new AccountRelation() { Owner = srcAccount, Target = trgtAccount, Relation = rel.Relation };
                        srcAccount.RelalationsByOwner.Add(entry);
                    }
                    else entry.Relation = rel.Relation;
                db.SaveChanges();

                ConnectedUser targetConnectedUser;
                if (server.ConnectedUsers.TryGetValue(trgtAccount.Name, out targetConnectedUser))
                {
                    targetConnectedUser.LoadFriendsIgnores(); // update partner's mutual lists

                    if (friendAdded) // friend added, sync new friend to me (user, battle and channels)
                    {
                        await server.TwoWaySyncUsers(Name, new List<string>() { targetConnectedUser.Name });

                        foreach (var chan in
                            server.Channels.Values.Where(
                                x => (x != null) && x.Users.ContainsKey(Name) && x.Users.ContainsKey(targetConnectedUser.Name))) await SendCommand(new ChannelUserAdded() { ChannelName = chan.Name, UserName = targetConnectedUser.Name });
                    }
                }

                LoadFriendsIgnores();
                await SendCommand(new FriendList() { Friends = FriendEntries.ToList() });
                await SendCommand(new IgnoreList() { Ignores = Ignores.ToList() });
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


                if (await server.MatchMaker.RemoveUser(Name, true))
                {
                    await server.UserLogSay($"{Name} disconnected, removing from MM.");
                }
                await server.PartyManager.OnUserDisconnected(Name);
                await server.PlanetWarsMatchMaker.OnUserDisconnected(Name);
                server.ForumListManager.OnUserDisconnected(User.AccountID);

                await server.Broadcast(server.ConnectedUsers.Values.Where(x => x != null && server.CanUserSee(x, this)), new UserDisconnected() { Name = Name, Reason = reason });

                server.RemoveSessionsForAccountID(User.AccountID);

                ConnectedUser connectedUser;

                if (server.ConnectedUsers.TryRemove(Name, out connectedUser))
                {
                    int accountID = User.AccountID;
                    connectedUser.ResetHasSeen();
                    connectedUser.User.AccountID = 0;

                    using (var db = new ZkDataContext())
                    {
                        var acc = await db.Accounts.FindAsync(accountID);
                        acc.LastLogout = DateTime.UtcNow;
                        acc.LastChatRead = DateTime.UtcNow;
                        await db.SaveChangesAsync();
                    }
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

        public async Task Process(InviteToParty invite)
        {
            await partyManager.ProcessInviteToParty(this, invite);
        }

        public async Task Process(PartyInviteResponse response)
        {
            await partyManager.ProcessPartyInviteResponse(this, response);
        }

        public async Task Process(LeaveParty message)
        {
            await partyManager.ProcessLeaveParty(this, message);
        }


        public override string ToString()
        {
            return string.Format("[{0}]", Name);
        }


        public void ResetHasSeen()
        {
            HasSeenUserVersion.Clear();
            foreach (var conus in server.ConnectedUsers.Values.Where(x => x != null))
            {
                int seenVersion;
                conus.HasSeenUserVersion.TryRemove(Name, out seenVersion);
            }
        }


        private async Task LeaveBattle(ServerBattle battle)
        {
            if (battle.Users.ContainsKey(Name))
            {
                MyBattle = null;
                UserBattleStatus oldVal;
                if (battle.Users.TryRemove(Name, out oldVal))
                {
                    await server.SyncUserToAll(this);
                    var bots = battle.Bots.Values.Where(x => x.owner == Name).ToList();
                    foreach (var b in bots)
                    {
                        BotBattleStatus obs;
                        if (battle.Bots.TryRemove(b.Name, out obs)) await server.Broadcast(battle.Users.Keys, new RemoveBot() { Name = b.Name });
                    }
                }
                await battle.CheckCloseBattle();
            }
        }


        private DateTime lastThrottleReset = DateTime.UtcNow;
        private int bytesSent;

        public async Task Throttle(int lineLength)
        {
            bytesSent += lineLength;
            if (bytesSent < GlobalConst.LobbyThrottleBytesPerSecond) return;

            var now = DateTime.UtcNow;
            var seconds = now.Subtract(lastThrottleReset).TotalSeconds;
            if (bytesSent <= GlobalConst.LobbyThrottleBytesPerSecond * seconds)
            {
                bytesSent = 0;
                lastThrottleReset = now;
            }
            else
            {
                var needForSleep = (double)bytesSent / GlobalConst.LobbyThrottleBytesPerSecond - seconds;
                await Task.Delay((int)Math.Round(needForSleep * 1000.0));
            }
        }
    }
}
