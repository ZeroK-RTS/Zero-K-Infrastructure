using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq;
using System.Linq;
using System.Security.Principal;

namespace ZkData
{
    partial class Account: IPrincipal, IIdentity
    {
        Dictionary<AutohostMode, GamePreference> preferences;
        public int AvailableXP { get { return GetXpForLevel(Level) - AccountUnlocks.Sum(x => (int?)(x.Unlock.XpCost*x.Count)) ?? 0; } }
        public double EffectiveElo { get { return Elo + WeightEloMalus; } }
        public double EloInvWeight { get { return GlobalConst.EloWeightMax + 1 - EloWeight; } }


        public static Func<ZkDataContext, int, Account> AccountByAccountID = CompiledQuery.Compile<ZkDataContext, int, Account>(
            (db, accountID) => db.Accounts.SingleOrDefault(x=>x.AccountID==accountID)
            );

        public static Func<ZkDataContext, int, Account> AccountByLobbyID = CompiledQuery.Compile<ZkDataContext, int, Account>(
            (db, lobbyID) => db.Accounts.FirstOrDefault(x => x.LobbyID == lobbyID)
            );

        public static Func<ZkDataContext, string, Account> AccountByName = CompiledQuery.Compile<ZkDataContext, string, Account>(
            (db, name) => db.Accounts.FirstOrDefault(x => x.Name == name && x.LobbyID != null)
            );

        public static Func<ZkDataContext, string, string, Account> AccountVerify = CompiledQuery.Compile<ZkDataContext, string,string, Account>(
            (db, login, passwordHash) => db.Accounts.FirstOrDefault(x => x.Name== login && x.Password == passwordHash && x.LobbyID!=null)
            );


        public Dictionary<AutohostMode, GamePreference> Preferences
        {
            get
            {
                if (preferences != null) return preferences;
                else
                {
                    preferences = new Dictionary<AutohostMode, GamePreference>();
                    if (!string.IsNullOrEmpty(GamePreferences))
                    {
                        foreach (var line in GamePreferences.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            var parts = line.Split('=');
                            preferences[(AutohostMode)int.Parse(parts[0])] = (GamePreference)int.Parse(parts[1]);
                        }
                    }
                    foreach (AutohostMode v in Enum.GetValues(typeof(AutohostMode))) if (!preferences.ContainsKey(v)) preferences[v] = GamePreference.Never;
                    /*if (preferences.Where(x=>x.Key != AutohostMode.None).All(x=>x.Value == GamePreference.Never)) {
                        foreach (var p in preferences.ToList()) preferences[p.Key] = GamePreference.Like;
                    }*/
                }
                return preferences;
            }
        }

        public void SetPreferences(Dictionary<AutohostMode, GamePreference> data) {
            var str = "";
            foreach (var kvp in data) {
                str += string.Format("{0}={1}\n", (int)kvp.Key, (int)kvp.Value);
            }
            GamePreferences = str;
        }

        public double WeightEloMalus { get { return (GlobalConst.EloWeightMax - EloWeight)*GlobalConst.EloWeightMalusFactor; } }


        public void CheckLevelUp()
        {
            if (XP > GetXpForLevel(Level + 1)) Level++;
        }


        public IEnumerable<Poll> ValidPolls(ZkDataContext db = null) {
            if (db ==null) db = new ZkDataContext();
            return db.Polls.Where(x => (x.ExpireBy == null || x.ExpireBy > DateTime.UtcNow) && (x.RestrictClanID == null || x.RestrictClanID == ClanID) && (x.RestrictFactionID == null || x.RestrictFactionID == FactionID));
        }


        public int GetDropshipCapacity()
        {
            return GlobalConst.DefaultDropshipCapacity +
                   (Planets.SelectMany(x => x.PlanetStructures).Where(x => !x.IsDestroyed).Sum(x => x.StructureType.EffectDropshipCapacity) ?? 0);
        }


        public int GetJumpGateCapacity()
        {
            return Planets.SelectMany(x => x.PlanetStructures).Where(x => !x.IsDestroyed).Sum(x => x.StructureType.EffectWarpGateCapacity) ?? 0;
        }

        public static int GetXpForLevel(int level)
        {
            if (level < 0) return 0;
            return level*80 + 20*level*level;
        }

        partial void OnCreated()
        {
            FirstLogin = DateTime.UtcNow;
            Elo = 1500;
            EloWeight = 1;
            DropshipCount = 1;
            SpringieLevel = 1;
        }

        partial void OnCreditsChanging(int value)
        {
            if (value > Credits) CreditsIncome += value - Credits;
            else CreditsExpense += Credits - value;
        }

        partial void OnGamePreferencesChanged()
        {
            preferences = null;
        }

        partial void OnNameChanging(string value)
        {
            if (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(value))
            {
                List<string> aliases = null;
                if (!string.IsNullOrEmpty(Aliases)) aliases = new List<string>(Aliases.Split(','));
                else aliases = new List<string>();

                if (!aliases.Contains(Name)) aliases.Add(Name);
                Aliases = string.Join(",", aliases.ToArray());
            }
        }

        partial void OnValidate(ChangeAction action)
        {
            if (action == ChangeAction.Update || action == ChangeAction.Insert)
            {
                if (string.IsNullOrEmpty(Avatar))
                {
                    var rand = new Random();
                    var avatars = ZkData.Avatar.GetCachedList();
                    if (avatars.Any()) Avatar = avatars[rand.Next(avatars.Count)].AvatarName;
                }
            }
        }

        partial void OnXPChanged()
        {
            CheckLevelUp();
        }

        public string AuthenticationType { get { return "LobbyServer"; } }
        public bool IsAuthenticated { get { return true; } }

        public bool IsInRole(string role)
        {
            if (role == "LobbyAdmin") return IsLobbyAdministrator;
            if (role == "ZkAdmin") return IsZeroKAdmin;
            else return string.IsNullOrEmpty(role);
        }

        public IIdentity Identity { get { return this; } }
    }

    public enum GamePreference
    {
        [Description("0: Never")]
        Never = -2,
        [Description("+1: Ok")]
        Ok = -1,
        [Description("+2: Like")]
        Like = 0,
        [Description("+3: Best")]
        Best = 1
    }
}