using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZkData
{
    partial class FactionTreaty
    {
        public bool CanCancel(Account account) {
            if (account == null) return false;
            if (!account.HasFactionRight(x => x.RightDiplomacy)) return false;
            if (TurnsRemaining == null || TreatyState == TreatyState.Proposed) {
                if (ProposingFactionID == account.FactionID || AcceptingFactionID == account.FactionID) return true; // can canel
            } 
            return false;
        }

        public override string ToString() {
            return "TR" + FactionTreatyID;
        }

        public bool CanAccept(Account account)
        {
            if (account == null) return false;
            if (!account.HasFactionRight(x => x.RightDiplomacy)) return false;
            if (TreatyState == TreatyState.Proposed && AcceptingFactionID == account.FactionID) return true; 
            return false;
        }

    }
}
