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
        public static Func<ZkDataContext, int, Account> AccountByAccountID = (db, accountID) => db.Accounts.SingleOrDefault(x => x.AccountID == accountID);

        public static Func<ZkDataContext, int, Account> AccountByLobbyID = (db, lobbyID) => db.Accounts.FirstOrDefault(x => x.LobbyID == lobbyID);

        public static Func<ZkDataContext, string, Account> AccountByName = (db, name) => db.Accounts.FirstOrDefault(x => x.Name == name && x.LobbyID != null);

        public static Func<ZkDataContext, string, string, Account> AccountVerify = (db, login, passwordHash) => db.Accounts.FirstOrDefault(x => x.Name == login && x.Password == passwordHash && x.LobbyID != null);

        public int AvailableXP { get {
            return GetXpForLevel(Level) -
                   AccountUnlocks.Sum(x => (int?)(x.Unlock.XpCost*(x.Count - KudosPurchases.Count(y => y.UnlockID == x.UnlockID)))) ?? 0;
        } }

        public double Effective1v1Elo { get { return Elo1v1Weight > 1 ? Elo1v1 + (GlobalConst.EloWeightMax - Elo1v1Weight)*GlobalConst.EloWeightMalusFactor : 0; } }
        public double EffectiveElo { get { return Elo + (GlobalConst.EloWeightMax - EloWeight)*GlobalConst.EloWeightMalusFactor; } }
        public double EffectivePwElo { get { return EloPw + (GlobalConst.EloWeightMax - EloWeight) * GlobalConst.EloWeightMalusFactor; } }

        public double EloInvWeight { get { return GlobalConst.EloWeightMax + 1 - EloWeight; } }
        public int KudosGained { get { return ContributionsByAccountID.Sum(x => x.KudosValue); } }
        public int KudosSpent { get { return KudosPurchases.Sum(x => x.KudosValue); } }



        public static double AdjustEloWeight(double currentWeight, double sumWeight, int sumCount) {
            if (currentWeight < GlobalConst.EloWeightMax) {
                currentWeight = (currentWeight + ((sumWeight - currentWeight - (sumCount - 1))/(sumCount - 1))/GlobalConst.EloWeightLearnFactor);
                if (currentWeight > GlobalConst.EloWeightMax) currentWeight = GlobalConst.EloWeightMax;
            }
            return currentWeight;
        }

        public bool CanAppoint(Account targetAccount, RoleType roleType) {
            if (targetAccount.AccountID != AccountID && targetAccount.FactionID == FactionID &&
                (!roleType.IsClanOnly || targetAccount.ClanID == ClanID) &&
                (roleType.RestrictFactionID == null || roleType.RestrictFactionID == FactionID)) {
                return AccountRolesByAccountID.Any(
                        x => x.RoleType.RoleTypeHierarchiesByMasterRoleTypeID.Any(y => y.CanAppoint && y.SlaveRoleTypeID == roleType.RoleTypeID));
            }
            else return false;
        }

        public bool CanRecall(Account targetAccount, RoleType roleType) {
            if (targetAccount.AccountID != AccountID && targetAccount.FactionID == FactionID &&
                (!roleType.IsClanOnly || targetAccount.ClanID == ClanID)) {
                return
                    AccountRolesByAccountID.Any(
                        x => x.RoleType.RoleTypeHierarchiesByMasterRoleTypeID.Any(y => y.CanRecall && y.SlaveRoleTypeID == roleType.RoleTypeID));
            }
            else return false;
        }


        public bool CanPlayerPlanetWars()
        {
            return FactionID != null && Level >= GlobalConst.MinPlanetWarsLevel && EffectiveElo > GlobalConst.MinPlanetWarsElo;
        }


        public bool CanSetPriority(PlanetStructure ps) {
            if (Faction == null) return false;
            if (ps.Planet.OwnerFactionID == FactionID && HasFactionRight(x => x.RightSetEnergyPriority)) return true;
            if (ClanID != null && ps.Account != null && ps.Account.ClanID == ClanID && HasClanRight(x => x.RightSetEnergyPriority)) return true;
            return false;
        }

        public bool CanSetStructureTarget(PlanetStructure ps) {
            if (Faction == null) return false;
            if (ps.OwnerAccountID == AccountID || ps.Planet.OwnerAccountID == AccountID) return true; // owner of planet or owner of structure
            if (ps.Planet.OwnerFactionID == FactionID && HasFactionRight(x => x.RightDropshipQuota > 0)) return true;
            return false;
        }

        public bool CanVoteRecall(Account targetAccount, RoleType roleType) {
            if (roleType.IsVoteable && targetAccount.FactionID == FactionID && (!roleType.IsClanOnly || targetAccount.ClanID == ClanID)) return true;
            else return false;
        }

        public void CheckLevelUp() {
            if (Xp > GetXpForLevel(Level + 1)) Level++;
            if (Level == GlobalConst.LevelForElevatedSpringieRights && SpringieLevel == 1) SpringieLevel = 2;
        }

        public int GetBomberCapacity() {
            if (Faction == null) return 0;
            return GlobalConst.DefaultBomberCapacity +
                   (Faction.Planets.SelectMany(x => x.PlanetStructures).Where(x => x.IsActive).Sum(x => x.StructureType.EffectBomberCapacity) ?? 0);
        }

        public double GetBomberQuota() {
            if (PwBombersProduced < PwBombersUsed) PwBombersUsed = PwBombersProduced;

            return GetQuota(x => x.PwBombersProduced, x => x.PwBombersUsed, x => x.RightBomberQuota, x => x.Bombers);
        }

        public double GetBombersAvailable() {
            if (Faction != null) return Math.Min(GetBomberQuota(), Faction.Bombers);
            else return 0;
        }

        public int GetDropshipCapacity() {
            if (Faction == null) return 0;
            return GlobalConst.DefaultDropshipCapacity +
                   (Faction.Planets.SelectMany(x => x.PlanetStructures).Where(x => x.IsActive).Sum(x => x.StructureType.EffectDropshipCapacity) ?? 0);
        }


        public double GetDropshipQuota() {
            if (PwDropshipsProduced < PwDropshipsUsed) PwDropshipsUsed = PwDropshipsProduced;

            return GetQuota(x => x.PwDropshipsProduced, x => x.PwDropshipsUsed, x => x.RightDropshipQuota, x => x.Dropships);
        }

        public double GetDropshipsAvailable() {
            if (Faction != null) {
                var q = GetDropshipQuota();
                return Math.Min(q, Faction.Dropships);
            }
            else return 0;
        }

        public double GetMetalAvailable() {
            if (Faction != null) return Math.Min(GetMetalQuota(), Faction.Metal);
            else return 0;
        }

        public double GetMetalQuota() {
            if (PwMetalProduced < PwMetalUsed) PwMetalUsed = PwMetalProduced;

            return GetQuota(x => x.PwMetalProduced, x => x.PwMetalUsed, x => x.RightMetalQuota, x => x.Metal);
        }

        public double GetQuota(Func<Account, double> producedSelector,
                               Func<Account, double> usedSelector,
                               Func<RoleType, double?> quotaSelector,
                               Func<Faction, double> factionResources) {
            var total = producedSelector(this) - usedSelector(this);
            if (total < 0) total = 0;

            if (Faction != null) {
                var clanQratio = AccountRolesByAccountID.Where(x => x.RoleType.IsClanOnly).Select(x => x.RoleType).Max(quotaSelector) ?? 0;
                var factionQratio = AccountRolesByAccountID.Where(x => !x.RoleType.IsClanOnly).Select(x => x.RoleType).Max(quotaSelector) ?? 0;

                var facRes = factionResources(Faction);

                if (factionQratio >= clanQratio) total += facRes*factionQratio;
                else {
                    if (clanQratio > 0 && Clan != null) {
                        var sumClanProd = Clan.Accounts.Sum(producedSelector);
                        var sumFacProd = Faction.Accounts.Sum(producedSelector);
                        if (sumFacProd > 0) {
                            var clanRes = facRes*sumClanProd/sumFacProd;
                            total += clanRes*clanQratio + (facRes - clanRes)*factionQratio;
                        }
                    }
                }
            }

            return Math.Floor(total);
        }

        public double GetWarpAvailable() {
            if (Faction != null) return Math.Min(GetWarpQuota(), Faction.Warps);
            else return 0;
        }

        public double GetWarpQuota() {
            if (PwWarpProduced < PwWarpUsed) PwWarpUsed = PwWarpProduced;

            return GetQuota(x => x.PwWarpProduced, x => x.PwWarpUsed, x => x.RightWarpQuota, x => x.Warps);
        }

        public static int GetXpForLevel(int level) {
            if (level < 0) return 0;
            return level*80 + 20*level*level;
        }

        public bool HasClanRight(Func<RoleType, bool> test) {
            return AccountRolesByAccountID.Where(x => x.RoleType.IsClanOnly).Select(x => x.RoleType).Any(test);
        }

        public bool HasFactionRight(Func<RoleType, bool> test) {
            return AccountRolesByAccountID.Where(x => !x.RoleType.IsClanOnly).Select(x => x.RoleType).Any(test);
        }

        public void ProduceBombers(double count) {
            PwBombersProduced += count;
            Faction.Bombers += count;

            if (PwBombersUsed > PwBombersProduced) PwBombersUsed = PwBombersProduced;
        }


        public void ProduceDropships(double count) {
            PwDropshipsProduced += count;
            Faction.Dropships += count;

            if (PwDropshipsProduced < PwDropshipsUsed) PwDropshipsUsed = PwDropshipsProduced;
        }


        public void ProduceMetal(double count) {
            PwMetalProduced += count;
            Faction.Metal += count;

            if (PwMetalUsed > PwMetalProduced) PwMetalUsed = PwMetalProduced;
        }

        public void ProduceWarps(double count) {
            PwWarpProduced += count;
            Faction.Warps += count;

            if (PwWarpUsed > PwWarpProduced) PwWarpUsed = PwWarpProduced;
        }

        /// <summary>
        /// Todo distribute among clan and faction members
        /// </summary>
        public void ResetQuotas() {
            PwBombersProduced = 0;
            PwBombersUsed = 0;
            PwDropshipsProduced = 0;
            PwDropshipsUsed = 0;
            PwMetalProduced = 0;
            PwMetalUsed = 0;
        }



        public void SpendBombers(double count) {
            PwBombersUsed += count;
            Faction.Bombers -= count;

            if (PwBombersUsed > PwBombersProduced) PwBombersUsed = PwBombersProduced;
        }

        public void SpendDropships(double count) {
            PwDropshipsUsed += count;
            Faction.Dropships -= count;

            if (PwDropshipsProduced < PwDropshipsUsed) PwDropshipsUsed = PwDropshipsProduced;
        }

        public void SpendMetal(double count) {
            PwMetalUsed += count;
            Faction.Metal -= count;

            if (PwMetalUsed > PwMetalProduced) PwMetalUsed = PwMetalProduced;
        }

        public void SpendWarps(double count) {
            PwWarpUsed += count;
            Faction.Warps -= count;

            if (PwWarpUsed > PwWarpProduced) PwWarpUsed = PwWarpProduced;
        }


        public IEnumerable<Poll> ValidPolls(ZkDataContext db = null) {
            if (db == null) db = new ZkDataContext();
            return
                db.Polls.Where(
                    x =>
                    (x.ExpireBy == null || x.ExpireBy > DateTime.UtcNow) && (x.RestrictClanID == null || x.RestrictClanID == ClanID) &&
                    (x.RestrictFactionID == null || x.RestrictFactionID == FactionID));
        }

        public override string ToString() {
            return Name;
        }


        public void SetName(string value) { 
            if (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(value)) {
                List<string> aliases = null;
                if (!string.IsNullOrEmpty(Aliases)) aliases = new List<string>(Aliases.Split(','));
                else aliases = new List<string>();

                if (!aliases.Contains(Name)) aliases.Add(Name);
                Aliases = string.Join(",", aliases.ToArray());
            }
        }

        // HACK implement these
        /*partial void OnValidate(ChangeAction action) {
            if (action == ChangeAction.Update || action == ChangeAction.Insert) {
                if (string.IsNullOrEmpty(Avatar)) {
                    var rand = new Random();
                    var avatars = ZkData.Avatar.GetCachedList();
                    if (avatars.Any()) Avatar = avatars[rand.Next(avatars.Count)].AvatarName;
                }
            }
        }

        partial void OnXPChanged() {
            CheckLevelUp();
        }*/

        public string AuthenticationType { get { return "LobbyServer"; } }
        public bool IsAuthenticated { get { return true; } }

        public bool IsInRole(string role) {
            if (role == "LobbyAdmin") return IsLobbyAdministrator;
            if (role == "ZkAdmin") return IsZeroKAdmin;
            else return string.IsNullOrEmpty(role);
        }

        public IIdentity Identity { get { return this; } }

        public int GetEffectiveSpringieLevel()
        {
            if (PunishmentsByAccountID.Any(x => x.SetRightsToZero && !x.IsExpired)) return 0;
            return SpringieLevel;
        }
    }

    public enum GamePreference
    {
        [Description("Never")]
        Never = -2,
        [Description("Ok")]
        Ok = -1,
        [Description("Like")]
        Like = 0,
        [Description("Best")]
        Best = 1
    }

    public class UserLanguageNoteAttribute: Attribute
    {
        protected String note;

        public String Note { get { return note; } }

        public UserLanguageNoteAttribute(string note) {
            this.note = note;
        }
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