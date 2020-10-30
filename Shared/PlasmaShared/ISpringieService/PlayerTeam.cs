using System;

namespace PlasmaShared
{
    public class PlayerTeam
    {
        public int LobbyID;
        public int AllyID;
        public string Name;
        public bool IsSpectator;
        public string ScriptPassword;
        public string Clan;
        public string Faction;
        public int? PartyID;
        public int QueueOrder;
        public DateTime JoinTime;
    }
}