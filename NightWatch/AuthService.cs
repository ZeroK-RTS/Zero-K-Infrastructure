using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using LobbyClient;
using PlasmaShared.LobbyMessages;
using ZkData;

namespace NightWatch
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
    public class AuthService
    {
        public const string ModeratorChannel = "zkadmin";
        public const string Top20Channel = "zktop20";

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
                    using (var db = new ZkDataContext())
                    {
                        var acc = db.Accounts.Find(e.Data.LobbyID);
                        if (acc != null)
                        {
                            this.client.Extensions.PublishAccountData(acc);
                            if (acc.SpringieLevel > 2 || acc.IsZeroKAdmin) client.ForceJoinChannel(e.Data.Name, ModeratorChannel);
                            if (topPlayers.IsTop20(e.Data.LobbyID)) client.ForceJoinChannel(e.Data.Name, Top20Channel);
                            if (acc.Clan != null) client.ForceJoinChannel(e.Data.Name, acc.Clan.GetClanChannel(), acc.Clan.Password);
                            if (acc.Faction != null && acc.Level >= GlobalConst.FactionChannelMinLevel && acc.CanPlayerPlanetWars()) client.ForceJoinChannel(e.Data.Name, acc.Faction.Shortcut);
                        }
                    }
                };

            // TODO set bot mode
            //this.client.BattleFound +=
              //  (s, e) => { if (e.Data.Founder.IsZkLobbyUser && !e.Data.Founder.IsBot) client.SetBotMode(e.Data.Founder.Name, true); };

            this.client.ChannelUserAdded += (sender, args) =>
                {
                    try
                    {
                        var channel = args.ServerParams[0];
                        var user = args.ServerParams[1];
                        if (channel == ModeratorChannel)
                        {
                            var u = client.ExistingUsers[user];
                            if (u.SpringieLevel <= 2 && !u.IsAdmin) client.ForceLeaveChannel(user, ModeratorChannel);
                        }
                        else if (channel == Top20Channel)
                        {
                            var u = client.ExistingUsers[user];
                            if (!topPlayers.IsTop20(u.LobbyID) && u.Name != client.UserName) client.ForceLeaveChannel(user, Top20Channel);
                        }
                        else
                        {
                            using (var db = new ZkDataContext())
                            {
                                var fac = db.Factions.FirstOrDefault(x => x.Shortcut == channel);
                                if (fac != null)
                                {
                                    // faction channel
                                    var u = client.ExistingUsers[user];
                                    var acc = db.Accounts.Find(u.LobbyID);
                                    if (acc == null || acc.FactionID != fac.FactionID || acc.Level < GlobalConst.FactionChannelMinLevel) client.ForceLeaveChannel(user, channel);
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
                        var channel = args.ServerParams[0];
                        var user = args.ServerParams[1];
                        if (channel == ModeratorChannel)
                        {
                            var u = client.ExistingUsers[user];
                            if (u.SpringieLevel > 2 || u.IsAdmin) client.ForceJoinChannel(user, ModeratorChannel);
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
            if (client.ExistingUsers.TryGetValue(account.Name, out ex)) client.Say(TasClient.SayPlace.User, account.Name, text, false);
            else
            {
                var message = new LobbyMessage
                {
                    SourceLobbyID = client.MyUser != null ? client.MyUser.LobbyID : 0,
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
                if (!GlobalConst.IsZkMod(b.ModName)) continue;
                foreach (var u in b.Users.Select(x => x.LobbyUser))
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
