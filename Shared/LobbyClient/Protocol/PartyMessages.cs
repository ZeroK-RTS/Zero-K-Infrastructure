using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LobbyClient
{
    [Message(Origin.Client)]
    public class InviteToParty
    {
        public string UserName { get; set; }
    }

    [Message(Origin.Client)]
    public class LeaveParty
    {
        public int PartyID { get; set; }
    }

    [Message(Origin.Client)]
    public class PartyInviteResponse
    {
        public int PartyID { get; set; }
        public bool Accepted { get; set; }
    }

    [Message(Origin.Server)]
    public class OnPartyInvite
    {
        public int PartyID { get; set; }
        public List<string> UserNames { get; set; }
        public int TimeoutSeconds { get; set; }
    }


    [Message(Origin.Server)]
    public class OnPartyStatus
    {
        public int PartyID { get; set; }
        public List<string> UserNames { get; set; }
    }

}
