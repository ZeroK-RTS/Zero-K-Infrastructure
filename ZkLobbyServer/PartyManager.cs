using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LobbyClient;
using PlasmaShared;
using ZkData;

namespace ZkLobbyServer
{
    public class PartyManager
    {
        private const int inviteTimeoutSeconds = 60;
        public const string PartyChannelPrefix = "party_";

        private List<Party> parties = new List<Party>();

        private int partyCounter;

        private List<PartyInvite> partyInvites = new List<PartyInvite>();

        private ZkLobbyServer server;


        public PartyManager(ZkLobbyServer zkLobbyServer)
        {
            server = zkLobbyServer;
        }

        public Party GetParty(string name)
        {
            return parties.FirstOrDefault(x => x.UserNames.Contains(name));
        }

        public async Task OnUserDisconnected(string name)
        {
            var party = parties.FirstOrDefault(x => x.UserNames.Contains(name));
            if (party != null) await RemoveFromParty(party, name);
        }

        public async Task ProcessInviteToParty(ConnectedUser usr, InviteToParty msg)
        {
            ConnectedUser target;
            if (server.ConnectedUsers.TryGetValue(msg.UserName, out target))
            {
                if (target.Ignores.Contains(usr.Name)) return;
                var myParty = GetParty(usr.Name);
                var targetParty = GetParty(target.Name);
                if ((myParty != null) && (myParty == targetParty)) return;

                // if i dont have battle but target has, join him 
                if (myParty == null && usr.MyBattle == null && target.MyBattle != null && !target.MyBattle.IsPassworded) await server.ForceJoinBattle(usr.Name, target.MyBattle);

                RemoveOldInvites();
                var partyInvite = partyInvites.FirstOrDefault(x => (x.Inviter == usr.Name) && (x.Invitee == target.Name));

                if (partyInvite == null)
                {
                    partyInvite = new PartyInvite()
                    {
                        PartyID = myParty?.PartyID ?? Interlocked.Increment(ref partyCounter),
                        Inviter = usr.Name,
                        Invitee = target.Name
                    };
                    partyInvites.Add(partyInvite);
                }

                await
                    target.SendCommand(new OnPartyInvite()
                    {
                        PartyID = partyInvite.PartyID,
                        UserNames = myParty?.UserNames?.ToList() ?? new List<string>() { usr.Name },
                        TimeoutSeconds = inviteTimeoutSeconds
                    });
            }
        }

        public async Task ProcessLeaveParty(ConnectedUser usr, LeaveParty msg)
        {
            var party = parties.FirstOrDefault(x => x.PartyID == msg.PartyID);
            if (party != null) await RemoveFromParty(party, usr.Name);
        }


        public async Task ProcessPartyInviteResponse(ConnectedUser usr, PartyInviteResponse response)
        {
            RemoveOldInvites();

            if (response.Accepted)
            {
                var inv = partyInvites.FirstOrDefault(x => x.PartyID == response.PartyID);
                if ((inv != null) && (inv.Invitee == usr.Name))
                {
                    
                    var inviteeUser = usr;
                    var inviterUser = server.ConnectedUsers.Get(inv.Inviter);

                    if (inviterUser != null)
                    {
                        var targetBattle = inviterUser.MyBattle ?? inviteeUser.MyBattle; // join inviter user's battle, if its empty join invitee user's battle
                        if (targetBattle != null)
                        {
                            if (inviteeUser.MyBattle != targetBattle) await server.ForceJoinBattle(inviteeUser.Name, targetBattle);
                            if (inviterUser.MyBattle != targetBattle) await server.ForceJoinBattle(inviterUser.Name, targetBattle);
                        }
                    }


                    var inviterParty = parties.FirstOrDefault(x => x.PartyID == response.PartyID);
                    var inviteeParty = parties.FirstOrDefault(x => x.UserNames.Contains(usr.Name));

                    Party party = null;

                    if ((inviterParty == null) && (inviteeParty != null)) party = inviteeParty;
                    if ((inviterParty == null) && (inviteeParty == null))
                    {
                        party = new Party(inv.PartyID);
                        parties.Add(party);
                    }
                    if ((inviterParty != null) && (inviteeParty == null)) party = inviterParty;
                    if ((inviterParty != null) && (inviteeParty != null))
                    {
                        await RemoveFromParty(inviteeParty, inv.Invitee);
                        party = inviterParty;
                    }


                    await AddToParty(party, inv.Invitee, inv.Inviter);
                }
            }
        }

        private List<string> AddFriendsBy(IEnumerable<string> people)
        {
            var result = new List<string>();
            foreach (var p in people)
            {
                if (!result.Contains(p)) result.Add(p);
                ConnectedUser usr;
                if (server.ConnectedUsers.TryGetValue(p, out usr)) foreach (var f in usr.FriendBy) if (server.ConnectedUsers.ContainsKey(f) && !result.Contains(f)) result.Add(f);
            }
            return result;
        }

        private async Task AddToParty(Party party, params string[] names)
        {
            var isChange = false;
            foreach (var n in names)
                if (!party.UserNames.Contains(n))
                {
                    var conus = server.ConnectedUsers.Get(n);
                    var lobus = conus?.User;
                    if (lobus != null) lobus.PartyID = party.PartyID;
                    party.UserNames.Add(n);
                    isChange = true;

                    if (conus != null) await conus.Process(new JoinChannel() { ChannelName = party.ChannelName });
                }

            var ps = new OnPartyStatus() { PartyID = party.PartyID, UserNames = party.UserNames };

            if (isChange)
            {
                // remove all people from this party from mm 
                if (await server.MatchMaker.RemoveUser(names.First(), true))
                {
                    await server.UserLogSay($"Removed {names.First()}'s party from MM because someone joined the party");
                }
            }

            
            await server.Broadcast(AddFriendsBy(party.UserNames), ps);
        }

        private async Task RemoveFromParty(Party party, params string[] names)
        {
            if (party.UserNames.Count == 2 && names.Any(x => party.UserNames.Contains(x))) names = party.UserNames.ToArray(); // party has just two people and we remove one of them -> remove all

            // removing user before changing party removes all party users
            if (await server.MatchMaker.RemoveUser(names.First(), true))
            {
                await server.UserLogSay($"Removed {names.First()}'s party from MM because someone left the party");
            }
            
            var broadcastNames = party.UserNames.ToList();
            foreach (var n in names)
            {
                var conus = server.ConnectedUsers.Get(n);
                var lobus = conus?.User;
                if (lobus != null) lobus.PartyID = null;
                party.UserNames.Remove(n);
                broadcastNames.Add(n);
                if (conus != null) await conus.Process(new LeaveChannel() { ChannelName = party.ChannelName });

            }
            var ps = new OnPartyStatus() { PartyID = party.PartyID, UserNames = party.UserNames };

            if (party.UserNames.Count == 0) parties.Remove(party);

            await server.Broadcast(AddFriendsBy(broadcastNames), ps);
        }

        private void RemoveOldInvites()
        {
            var now = DateTime.UtcNow;
            partyInvites.RemoveAll(x => now.Subtract(x.Issued).TotalSeconds > inviteTimeoutSeconds);
        }

        public class Party
        {
            public int PartyID { get; private set; }
            public List<string> UserNames { get; private set; } = new List<string>();


            public string ChannelName => PartyManager.PartyChannelPrefix + PartyID;

            public Party(int partyID)
            {
                PartyID = partyID;
            }
        }

        public class PartyInvite
        {
            public string Invitee;
            public string Inviter;
            public DateTime Issued = DateTime.UtcNow;
            public int PartyID;
        }

        public bool CanJoinChannel(string name, string channel)
        {
            try
            {
                var party =  parties.FirstOrDefault(x=>x.ChannelName == channel);
                if (party != null) return party.UserNames.Contains(name);
                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error checking party channel entrance {0} {1} : {2}" , name, channel, ex);
            }
            return true;
        }
    }
}