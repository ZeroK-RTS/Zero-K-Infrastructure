#region using

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ZkData;

#endregion

namespace LobbyClient.Legacy
{
    /// <summary>
    /// User - on the server
    /// </summary>
    [Obsolete]
    public class User
    {
        public int LobbyID;
		
		//This is only called once
        static User() {
        }

        public DateTime? AwaySince { get; protected set; }
        public int Cpu { get; set; }
        public DateTime? InGameSince { get; protected set; }
        public bool IsAdmin { get; protected set; }
        public bool IsAway { get; set; }
        public bool IsBot { get; protected set; }
        public bool IsInBattleRoom { get; set; }
        public bool IsInGame { get; set; }
        public string Name { get; protected set; }
        public int Rank { get; protected set; }
        public string Country { get; set; }

        public User Clone() {
            return (User)MemberwiseClone();
        }

        public static User Create(string name) {
            var u = new User();
            u.Name = name;
            return u;
        }

        public void FromInt(int status) {
            bool old = IsInGame;
            IsInGame = (status & 1) > 0;
            if (IsInGame && !old) InGameSince = DateTime.Now;
            if (!IsInGame) InGameSince = null;

            old = IsAway;
            IsAway = (status & 2) > 0;
            if (IsAway && !old) AwaySince = DateTime.Now;
            if (!IsAway) AwaySince = null;

            IsAdmin = (status & 32) > 0;
            IsBot = (status & 64) > 0;
            Rank = (status & 28) >> 2;
        }


        public int ToInt() {
            int res = 0;
            res |= IsInGame ? 1 : 0;
            res |= IsAway ? 2 : 0;
            return res;
        }

        public override string ToString() {
            return Name;
        }

    }
}