using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LobbyClient;

namespace ZkLobbyServer
{
    public class PartyManager
    {
        private const int inviteTimeoutSeconds = 60;

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
                    var inviterParty = parties.FirstOrDefault(x => x.PartyID == response.PartyID);
                    var inviteeParty = parties.FirstOrDefault(x => x.UserNames.Contains(usr.Name));

                    Party party = null;

                    if ((inviterParty == null) && (inviteeParty != null)) party = inviteeParty;
                    if ((inviterParty == null) && (inviteeParty == null)) party = new Party(inv.PartyID);
                    if ((inviterParty != null) && (inviteeParty == null)) party = inviterParty;
                    if ((inviterParty != null) && (inviteeParty != null))
                    {
                        await RemoveFromParty(inviterParty, inv.Invitee);
                        party = inviterParty;
                    }

                    await AddToParty(party, inv.Invitee, inv.Inviter);
                }
            }
        }

        private async Task AddToParty(Party party, params string[] names)
        {
            foreach (var n in names) if (!party.UserNames.Contains(n)) party.UserNames.Add(n);
            var ps = new OnPartyStatus() { PartyID = party.PartyID, UserNames = party.UserNames };
            await server.Broadcast(party.UserNames, ps);
        }

        private async Task RemoveFromParty(Party party, params string[] names)
        {
            var broadcastNames = party.UserNames.ToList();
            foreach (var n in names)
            {
                party.UserNames.Remove(n);
                broadcastNames.Add(n);
            }
            var ps = new OnPartyStatus() { PartyID = party.PartyID, UserNames = party.UserNames };
            broadcastNames = broadcastNames.Distinct().ToList();
            if (party.UserNames.Count == 0) parties.Remove(party);
            await server.Broadcast(broadcastNames, ps);
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

            public Party(int partyID)
            {
                PartyID = partyID;
            }
        }

        public class PartyInvite
        {
            public string Invitee;
            public string Inviter;
            public DateTime Issued;
            public int PartyID;
        }
    }
}