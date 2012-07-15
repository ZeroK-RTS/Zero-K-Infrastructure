using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Principal;

namespace ZkData
{
    partial class Account: IPrincipal, IIdentity
    {
        public static Func<ZkDataContext, int, Account> AccountByAccountID =
            CompiledQuery.Compile<ZkDataContext, int, Account>((db, accountID) => db.Accounts.SingleOrDefault(x => x.AccountID == accountID));

        public static Func<ZkDataContext, int, Account> AccountByLobbyID =
            CompiledQuery.Compile<ZkDataContext, int, Account>((db, lobbyID) => db.Accounts.FirstOrDefault(x => x.LobbyID == lobbyID));

        public static Func<ZkDataContext, string, Account> AccountByName =
            CompiledQuery.Compile<ZkDataContext, string, Account>((db, name) => db.Accounts.FirstOrDefault(x => x.Name == name && x.LobbyID != null));

        public static Func<ZkDataContext, string, string, Account> AccountVerify =
            CompiledQuery.Compile<ZkDataContext, string, string, Account>(
                (db, login, passwordHash) => db.Accounts.FirstOrDefault(x => x.Name == login && x.Password == passwordHash && x.LobbyID != null));
        Dictionary<AutohostMode, GamePreference> preferences;
        public int AvailableXP { get { return GetXpForLevel(Level) - AccountUnlocks.Sum(x => (int?)(x.Unlock.XpCost*x.Count)) ?? 0; } }
        public double EffectiveElo { get { return Elo + WeightEloMalus; } }
        public double EloInvWeight { get { return GlobalConst.EloWeightMax + 1 - EloWeight; } }


        public Dictionary<AutohostMode, GamePreference> Preferences {
            get {
                if (preferences != null) return preferences;
                else {
                    preferences = new Dictionary<AutohostMode, GamePreference>();
                    if (!string.IsNullOrEmpty(GamePreferences)) {
                        foreach (string line in GamePreferences.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)) {
                            string[] parts = line.Split('=');

                            var mode = (AutohostMode)int.Parse(parts[0]);
                            var preference = (GamePreference)int.Parse(parts[1]);
                            if (Enum.IsDefined(typeof(AutohostMode), mode) && Enum.IsDefined(typeof(GamePreference), preference)) preferences[mode] = preference;
                        }
                    }
                    foreach (AutohostMode v in Enum.GetValues(typeof(AutohostMode))) if (!preferences.ContainsKey(v)) preferences[v] = GamePreference.Like;
                    if (preferences.Where(x => x.Key != AutohostMode.None).All(x => x.Value == GamePreference.Never)) foreach (var p in preferences.ToList()) preferences[p.Key] = GamePreference.Like;
                }
                return preferences;
            }
        }
        public double WeightEloMalus { get { return (GlobalConst.EloWeightMax - EloWeight)*GlobalConst.EloWeightMalusFactor; } }

        #region IIdentity Members

        public string AuthenticationType { get { return "LobbyServer"; } }
        public bool IsAuthenticated { get { return true; } }

        #endregion

        #region IPrincipal Members

        public bool IsInRole(string role) {
            if (role == "LobbyAdmin") return IsLobbyAdministrator;
            if (role == "ZkAdmin") return IsZeroKAdmin;
            else return string.IsNullOrEmpty(role);
        }

        public IIdentity Identity { get { return this; } }

        #endregion


        public double GetMetalAvailable() {
            if (Faction != null) return Math.Min(GetMetalQuota(), Faction.Metal);
            else return 0;
        }

        public double GetDropshipsAvailable()
        {
            if (Faction != null) return Math.Min(GetDropshipQuota(), Faction.Dropships);
            else return 0;
        }

        public double GetBombersAvailable()
        {
            if (Faction != null) return Math.Min(GetBomberQuota(), Faction.Bombers);
            else return 0;
        }


        public bool HasClanRight(Func<RoleType, bool> test) {
            return AccountRolesByAccountID.Where(x => x.RoleType.IsClanOnly).Select(x=>x.RoleType).Any(test);
        }

        public bool HasFactionRight(Func<RoleType, bool> test)
        {
            return AccountRolesByAccountID.Where(x => !x.RoleType.IsClanOnly).Select(x => x.RoleType).Any(test);
        }


        public double GetDropshipQuota() {
            return GetQuota(x => x.PwDropshipsProduced, x => x.PwDropshipsUsed, x => x.RightDropshipQuota);
        }

        public double GetMetalQuota() {
            return GetQuota(x => x.PwMetalProduced, x => x.PwMetalUsed, x => x.RightMetalQuota);
        }

        public double GetBomberQuota()
        {
            return GetQuota(x => x.PwBombersProduced, x => x.PwBombersUsed, x => x.RightBomberQuota);
        }


        public double GetQuota(Func<Account, double> producedSelector, Func<Account, double> usedSelector, Func<RoleType, double?> quotaSelector)
        {
            double? clanMetalQ = AccountRolesByAccountID.Where(x => x.RoleType.IsClanOnly).Select(x => x.RoleType).Max(quotaSelector);
            double? factionMetalQ = AccountRolesByAccountID.Where(x => !x.RoleType.IsClanOnly).Select(x => x.RoleType).Max(quotaSelector); ;

            double total = producedSelector(this) - usedSelector(this);

            if (clanMetalQ != null && Clan != null)
            {
                double clanMetalSum = Clan.Accounts.Where(x => x.AccountID != AccountID).Sum(x => producedSelector(x) - usedSelector(x));
                total += clanMetalSum * clanMetalQ.Value;
            }
            if (factionMetalQ != null && Faction != null)
            {
                double factionMetalSum =
                    Faction.Accounts.Where(x => x.AccountID != AccountID && (ClanID == null || x.ClanID != ClanID)).Sum(x=>producedSelector(x) - usedSelector(x));
                total += factionMetalSum * factionMetalQ.Value;
            }

            return Math.Floor(total);
        }



        public void SetPreferences(Dictionary<AutohostMode, GamePreference> data) {
            string str = "";
            foreach (var kvp in data) str += string.Format("{0}={1}\n", (int)kvp.Key, (int)kvp.Value);
            GamePreferences = str;
            preferences = null;
        }


        public void CheckLevelUp() {
            if (XP > GetXpForLevel(Level + 1)) Level++;
        }


        public IEnumerable<Poll> ValidPolls(ZkDataContext db = null) {
            if (db == null) db = new ZkDataContext();
            return
                db.Polls.Where(
                    x =>
                    (x.ExpireBy == null || x.ExpireBy > DateTime.UtcNow) && (x.RestrictClanID == null || x.RestrictClanID == ClanID) &&
                    (x.RestrictFactionID == null || x.RestrictFactionID == FactionID));
        }


        public int GetDropshipCapacity() {
            return GlobalConst.DefaultDropshipCapacity +
                   (Planets.SelectMany(x => x.PlanetStructures).Where(x => !x.IsDestroyed).Sum(x => x.StructureType.EffectDropshipCapacity) ?? 0);
        }


        public int GetJumpGateCapacity() {
            return Planets.SelectMany(x => x.PlanetStructures).Where(x => !x.IsDestroyed).Sum(x => x.StructureType.EffectWarpGateCapacity) ?? 0;
        }

        public static int GetXpForLevel(int level) {
            if (level < 0) return 0;
            return level*80 + 20*level*level;
        }

        partial void OnCreated() {
            FirstLogin = DateTime.UtcNow;
            Elo = 1500;
            EloWeight = 1;
            SpringieLevel = 1;
        }


        partial void OnGamePreferencesChanged() {
            preferences = null;
        }

        partial void OnNameChanging(string value) {
            if (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(value)) {
                List<string> aliases = null;
                if (!string.IsNullOrEmpty(Aliases)) aliases = new List<string>(Aliases.Split(','));
                else aliases = new List<string>();

                if (!aliases.Contains(Name)) aliases.Add(Name);
                Aliases = string.Join(",", aliases.ToArray());
            }
        }


        partial void OnValidate(ChangeAction action) {
            if (action == ChangeAction.Update || action == ChangeAction.Insert) {
                if (string.IsNullOrEmpty(Avatar)) {
                    var rand = new Random();
                    List<Avatar> avatars = ZkData.Avatar.GetCachedList();
                    if (avatars.Any()) Avatar = avatars[rand.Next(avatars.Count)].AvatarName;
                }
            }
        }

        partial void OnXPChanged() {
            CheckLevelUp();
        }
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

    public class UserLanguageNoteAttribute: Attribute
    {
        protected String note;

        public UserLanguageNoteAttribute(string note) {
            this.note = note;
        }

        public String Note { get { return note; } }
    }

    public enum UserLanguage
    {
        [Description("Auto, let the system decide")]
        auto,
        [Description("English")]
        en,
        [UserLanguageNote(
            "В текущий момент ведется локализация материалов сайта на русский язык, с целью помочь начинающим игрокам и сделать игру доступнее!<br /><a href='/Wiki/Localization'>Присоединяйся</a>"
            )]
        [Description("Йа креведко!")]
        ru
    }
}