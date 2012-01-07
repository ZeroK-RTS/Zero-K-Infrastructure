#region using

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

#endregion

namespace LobbyClient
{
    /// <summary>
    /// User - on the server
    /// </summary>
    public class User
    {
        public static readonly Dictionary<string, string> CountryNames = new Dictionary<string, string>();

        /// <summary>
        /// times (in hours) to reach given rank
        /// </summary>
        public static readonly int[] RankLimits = { 0, 5, 15, 30, 100, 300, 1000, 3000, 5000 }; // last two are just a guess

        string country;

        public DateTime? AwaySince { get; protected set; }
        public string Clan { get; private set; }
        // todo: set by tasclient (use "User Battle" instead?)
        
        public string Avatar { get; private set; }

        public string Country
        {
            get { return country; }
            set
            {
                country = value;
                string name;
                if (CountryNames.TryGetValue(country, out name)) CountryName = name;
                else CountryName = "Unknown";
            }
        }


        public string CountryName { get; protected set; }

        public int Cpu { get; set; }
        public int EffectiveElo { get; private set; }
        public Dictionary<string, string> Extensions { get; private set; }
        public string Faction { get; private set; }
        public DateTime? InGameSince { get; protected set; }
        public bool IsAdmin { get; protected set; }
        public bool IsAway { get; set; }
        public bool IsBot { get; protected set; }
        public bool IsInBattleRoom { get; set; }
        public bool IsInGame { get; set; }
        public bool BanMute { get; set; }
        public bool BanLobby { get; set; }

        public int Level { get; private set; }

        public int LobbyID;
        public bool IsZkLobbyUser { get { return Cpu == 6666; } }
        public string Name { get; protected set; }
        public int Rank { get; protected set; }

        static User()
        {
            var assembly = Assembly.GetAssembly(typeof(User));
            var reader = new StreamReader(assembly.GetManifestResourceStream("Resources.CountryNames.txt"));

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var parts = line.Split('\t');
                CountryNames.Add(parts[0], parts[1]);
            }
        }

        public User Clone()
        {
            return (User)MemberwiseClone();
        }

        public User() {
        }

        public static User Create(string name)
        {
            var u = new User();
            u.Name = name;
            return u;
        }

        public void FromInt(int status)
        {
            var old = IsInGame;
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

        internal void SetExtension(Dictionary<string, string> data)
        {
            Extensions = data;
            int ret;
            if (int.TryParse(GetExtension(ProtocolExtension.Keys.Level), out ret)) Level = ret;
            if (int.TryParse(GetExtension(ProtocolExtension.Keys.EffectiveElo), out ret)) EffectiveElo = ret;
            if (int.TryParse(GetExtension(ProtocolExtension.Keys.SpringieLevel), out ret)) SpringieLevel = ret;

            Faction = GetExtension(ProtocolExtension.Keys.Faction);
            Clan = GetExtension(ProtocolExtension.Keys.Clan);
            Avatar = GetExtension(ProtocolExtension.Keys.Avatar);

            if (GetExtension(ProtocolExtension.Keys.ZkAdmin) == "1") IsZeroKAdmin =true;
            if (GetExtension(ProtocolExtension.Keys.BanMute) == "1") BanMute = true;
            if (GetExtension(ProtocolExtension.Keys.BanLobby) == "1") BanLobby = true;
            
        }

        public int SpringieLevel =1;
        public bool IsZeroKAdmin { get; protected set; }

        public int ToInt()
        {
            var res = 0;
            res |= IsInGame ? 1 : 0;
            res |= IsAway ? 2 : 0;
            return res;
        }

        public override string ToString()
        {
            return Name;
        }

        string GetExtension(ProtocolExtension.Keys key)
        {
            string txt;
            Extensions.TryGetValue(key.ToString(), out txt);
            return txt;
        }
    }
}