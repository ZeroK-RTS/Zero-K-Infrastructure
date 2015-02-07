using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using LobbyClient;
using ZkData;

namespace NightWatch
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
    public class AuthService
    {
        public const string ModeratorChannel = GlobalConst.ModeratorChannel;
        public const string Top20Channel = GlobalConst.Top20Channel;

        readonly TasClient client;

        int messageId;
        readonly TopPlayers topPlayers = new TopPlayers();

        public AuthService(TasClient client)
        {
            this.client = client;


            this.client.LoginAccepted += (s, e) =>
                {
                    client.JoinChannel(ModeratorChannel);
                    client.JoinChannel(Top20Channel);
                    using (var db = new ZkDataContext()) foreach (var fac in db.Factions.Where(x => !x.IsDeleted)) client.JoinChannel(fac.Shortcut);
                };


            this.client.UserAdded += (s, e) =>
                {
                    if (e.SpringieLevel > 2 || e.IsAdmin) client.ForceJoinChannel(e.Name, ModeratorChannel);
                    if (topPlayers.IsTop20(e.AccountID)) client.ForceJoinChannel(e.Name, Top20Channel);
                    if (e.Clan != null) client.ForceJoinChannel(e.Name, Clan.GetClanChannel(e.Clan));
                    if (e.Faction != null && e.Level >= GlobalConst.FactionChannelMinLevel) client.ForceJoinChannel(e.Name, e.Faction);
                };

            // TODO set bot mode
            //this.client.BattleFound +=
            //  (s, e) => { if (e.Data.Founder.IsZkLobbyUser && !e.Data.Founder.IsBot) client.SetBotMode(e.Data.Founder.Name, true); };

            this.client.ChannelUserAdded += (sender, args) =>
                {
                    try
                    {
                        var channel = args.Channel.Name;
                        foreach (var u in args.Users)
                        {
                            if (channel == ModeratorChannel)
                            {
                                if (u.SpringieLevel <= 2 && !u.IsAdmin) client.ForceLeaveChannel(u.Name, ModeratorChannel);
                            }
                            else if (channel == Top20Channel)
                            {
                                if (!topPlayers.IsTop20(u.AccountID) && u.Name != client.UserName) client.ForceLeaveChannel(u.Name, Top20Channel);
                            }
                            else
                            {
                                using (var db = new ZkDataContext())
                                {
                                    var fac = db.Factions.FirstOrDefault(x => x.Shortcut == channel);
                                    if (fac != null)
                                    {
                                        // faction channel
                                        var acc = db.Accounts.Find(u.AccountID);
                                        if (acc == null || acc.FactionID != fac.FactionID || acc.Level < GlobalConst.FactionChannelMinLevel) client.ForceLeaveChannel(u.Name, channel);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Error procesisng channel user added: {0}", ex);
                    }
                };
            this.client.ChannelUserRemoved += (sender, args) =>
                {
                    try
                    {
                        var channel = args.Channel.Name;
                        if (channel == ModeratorChannel)
                        {
                            var u = args.User;
                            if (u.SpringieLevel > 2 || u.IsAdmin) client.ForceJoinChannel(u.Name, ModeratorChannel);
                        }

                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Error procesisng channel user added: {0}", ex);
                    }
                };
        }





        public void SendLobbyMessage(Account account, string text)
        {
            User ex;
            if (client.ExistingUsers.TryGetValue(account.Name, out ex)) client.Say(SayPlace.User, account.Name, text, false);
            else
            {
                var message = new LobbyMessage
                {
                    SourceLobbyID = client.MyUser != null ? client.MyUser.AccountID : 0,
                    SourceName = client.UserName,
                    Created = DateTime.UtcNow,
                    Message = text,
                    TargetName = account.Name,
                    TargetLobbyID = account.AccountID
                };
                using (var db = new ZkDataContext())
                {
                    db.LobbyMessages.InsertOnSubmit(message);
                    db.SubmitChanges();
                }
            }
        }


        public CurrentLobbyStats GetCurrentStats()
        {
            var ret = new CurrentLobbyStats();
            foreach (var u in client.ExistingUsers.Values) if (!u.IsBot && !u.IsInGame && !u.IsInBattleRoom) ret.UsersIdle++;

            foreach (var b in client.ExistingBattles.Values)
            {
                foreach (var u in b.Users.Values.Select(x => x.LobbyUser))
                {
                    if (u.IsBot) continue;
                    if (u.IsInGame) ret.UsersFighting++;
                    else if (u.IsInBattleRoom) ret.UsersWaiting++;
                }
                if (b.IsInGame) ret.BattlesRunning++;
                else ret.BattlesWaiting++;
            }
            return ret;
        }


    }
}
