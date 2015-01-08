using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Principal;
using Microsoft.Linq.Translations;
using PlasmaShared;
using ZkData;

namespace ZkData
{
    public class Account: IPrincipal, IIdentity
    {

        public Account()
        {
            Elo = 1500;
            Elo1v1 = 1500;
            EloPw = 1500;
            EloWeight = 1;
            Elo1v1Weight = 1;
            SpringieLevel = 1;
            FirstLogin = DateTime.UtcNow;
            LastLogin = DateTime.UtcNow;
            

            AbuseReportsByAccountID = new HashSet<AbuseReport>();
            AbuseReportsByReporterAccountID = new HashSet<AbuseReport>();
            AccountBattleAwards = new HashSet<AccountBattleAward>();
            AccountCampaignJournalProgresses = new HashSet<AccountCampaignJournalProgress>();
            AccountCampaignProgress = new HashSet<AccountCampaignProgress>();
            AccountCampaignVars = new HashSet<AccountCampaignVar>();
            AccountForumVotes = new HashSet<AccountForumVote>();
            AccountIPs = new HashSet<AccountIP>();
            AccountRolesByAccountID = new HashSet<AccountRole>();
            AccountUnlocks = new HashSet<AccountUnlock>();
            AccountUserIDs = new HashSet<AccountUserID>();
            CampaignEvents = new HashSet<CampaignEvent>();
            Commanders = new HashSet<Commander>();
            ContributionsByAccountID = new HashSet<Contribution>();
            ContributionsByManuallyAddedAccountID = new HashSet<Contribution>();
            ContributionJars = new HashSet<ContributionJar>();
            FactionTreatiesByProposingAccount = new HashSet<FactionTreaty>();
            FactionTreatiesByAcceptedAccountID = new HashSet<FactionTreaty>();
            ForumLastReads = new HashSet<ForumLastRead>();
            ForumPosts = new HashSet<ForumPost>();
            ForumPostEdits = new HashSet<ForumPostEdit>();
            ForumThreads = new HashSet<ForumThread>();
            ForumThreadsByLastPostAccountID = new HashSet<ForumThread>();
            ForumThreadLastReads = new HashSet<ForumThreadLastRead>();
            KudosPurchases = new HashSet<KudosPurchase>();
            LobbyChannelSubscriptions = new HashSet<LobbyChannelSubscription>();
            Missions = new HashSet<Mission>();
            MissionScores = new HashSet<MissionScore>();
            News = new HashSet<News>();
            Planets = new HashSet<Planet>();
            PlanetOwnerHistories = new HashSet<PlanetOwnerHistory>();
            PlanetStructures = new HashSet<PlanetStructure>();
            AccountPlanets = new HashSet<AccountPlanet>();
            PollsByRoleTargetAccountID = new HashSet<Poll>();
            PollsByCreatedAccountID = new HashSet<Poll>();
            PollVotes = new HashSet<PollVote>();
            PunishmentsByAccountID = new HashSet<Punishment>();
            PunishmentsByCreatedAccountID = new HashSet<Punishment>();
            Ratings = new HashSet<Rating>();
            Resources = new HashSet<Resource>();
            MapRatings = new HashSet<MapRating>();
            SpringBattles = new HashSet<SpringBattle>();
            SpringBattlePlayers = new HashSet<SpringBattlePlayer>();
            Events = new HashSet<Event>();

        }

        public int AccountID { get; set; }
        [Required]
        [StringLength(200)]
        [Index]
        public string Name { get; set; }
        [StringLength(200)]
        public string Email { get; set; }
        public DateTime FirstLogin { get; set; }
        public DateTime LastLogin { get; set; }
        [StringLength(8000)]
        public string Aliases { get; set; }
        public double Elo { get; set; }
        public double EloWeight { get; set; }
        public double Elo1v1 { get; set; }
        public double Elo1v1Weight { get; set; }
        public double EloPw { get; set; }
        public bool IsLobbyAdministrator { get; set; }
        public bool IsBot { get; set; }
        [StringLength(100)]
        public string Password { get; set; }
        [StringLength(5)]
        public string Country { get; set; }
        public int LobbyTimeRank { get; set; }
        public int MissionRunCount { get; set; }
        public bool IsZeroKAdmin { get; set; }
        public int Xp { get; set; }
        public int Level { get; set; }
        public int? ClanID { get; set; }
        public DateTime? LastNewsRead { get; set; }
        public int? FactionID { get; set; }
        [Index]
        public int? LobbyID { get; set; }
        public bool IsDeleted { get; set; }
        [StringLength(50)]
        public string Avatar { get; set; }
        public int SpringieLevel { get; set; }
        [StringLength(200)]
        public string LobbyVersion { get; set; }
        public double PwDropshipsProduced { get; set; }
        public double PwDropshipsUsed { get; set; }
        public double PwBombersProduced { get; set; }
        public double PwBombersUsed { get; set; }
        public double PwMetalProduced { get; set; }
        public double PwMetalUsed { get; set; }
        public double PwWarpProduced { get; set; }
        public double PwWarpUsed { get; set; }
        public double PwAttackPoints { get; set; }
        [StringLength(2)]
        public string Language { get; set; }
        public bool HasVpnException { get; set; }
        public int Kudos { get; set; }
        public int ForumTotalUpvotes { get; set; }
        public int ForumTotalDownvotes { get; set; }
        public int? VotesAvailable { get; set; }
        public decimal? SteamID { get; set; }
        [StringLength(200)]
        public string SteamName { get; set; }

        public virtual ICollection<AbuseReport> AbuseReportsByAccountID { get; set; }
        public virtual ICollection<AbuseReport> AbuseReportsByReporterAccountID { get; set; }
        public virtual Faction Faction { get; set; }
        public virtual ICollection<AccountBattleAward> AccountBattleAwards { get; set; }
        public virtual ICollection<AccountCampaignJournalProgress> AccountCampaignJournalProgresses { get; set; }
        public virtual ICollection<AccountCampaignProgress> AccountCampaignProgress { get; set; }
        public virtual ICollection<AccountCampaignVar> AccountCampaignVars { get; set; }
        public virtual ICollection<AccountForumVote> AccountForumVotes { get; set; }
        public virtual ICollection<AccountIP> AccountIPs { get; set; }
        public virtual ICollection<AccountRole> AccountRolesByAccountID { get; set; }
        public virtual ICollection<AccountUnlock> AccountUnlocks { get; set; }
        public virtual ICollection<AccountUserID> AccountUserIDs { get; set; }
        public virtual ICollection<CampaignEvent> CampaignEvents { get; set; }
        public virtual ICollection<Commander> Commanders { get; set; }
        public virtual ICollection<Contribution> ContributionsByAccountID { get; set; }
        public virtual ICollection<Contribution> ContributionsByManuallyAddedAccountID { get; set; }
        public virtual ICollection<ContributionJar> ContributionJars { get; set; }
        public virtual ICollection<FactionTreaty> FactionTreatiesByProposingAccount { get; set; }
        public virtual ICollection<FactionTreaty> FactionTreatiesByAcceptedAccountID { get; set; }
        public virtual ICollection<ForumLastRead> ForumLastReads { get; set; }
        public virtual ICollection<ForumPostEdit> ForumPostEdits { get; set; }
        public virtual ICollection<ForumPost> ForumPosts { get; set; }
        public virtual ICollection<ForumThread> ForumThreads { get; set; }
        public virtual ICollection<ForumThread> ForumThreadsByLastPostAccountID { get; set; }
        public virtual ICollection<ForumThreadLastRead> ForumThreadLastReads { get; set; }
        public virtual ICollection<KudosPurchase> KudosPurchases { get; set; }
        public virtual ICollection<LobbyChannelSubscription> LobbyChannelSubscriptions { get; set; }
        public virtual ICollection<Mission> Missions { get; set; }
        public virtual ICollection<MissionScore> MissionScores { get; set; }
        public virtual ICollection<News> News { get; set; }
        public virtual ICollection<Planet> Planets { get; set; }
        public virtual ICollection<PlanetOwnerHistory> PlanetOwnerHistories { get; set; }
        public virtual ICollection<PlanetStructure> PlanetStructures { get; set; }
        public virtual ICollection<AccountPlanet> AccountPlanets { get; set; }
        public virtual ICollection<Poll> PollsByRoleTargetAccountID { get; set; }
        public virtual ICollection<Poll> PollsByCreatedAccountID { get; set; }
        public virtual ICollection<PollVote> PollVotes { get; set; }
        public virtual ICollection<Punishment> PunishmentsByAccountID { get; set; }
        public virtual ICollection<Punishment> PunishmentsByCreatedAccountID { get; set; }
        public virtual ICollection<Rating> Ratings { get; set; }
        public virtual ICollection<Resource> Resources { get; set; }
        public virtual ICollection<MapRating> MapRatings { get; set; }
        public virtual ICollection<SpringBattle> SpringBattles { get; set; }
        public virtual ICollection<SpringBattlePlayer> SpringBattlePlayers { get; set; }
        public virtual ICollection<Event> Events { get; set; }
        public virtual Clan Clan { get; set; }




        private static readonly CompiledExpression<Account, double> effectiveEloExpression = DefaultTranslationOf<Account>.Property(e => e.EffectiveElo).Is(e => e.Elo + (GlobalConst.EloWeightMax - e.EloWeight) * GlobalConst.EloWeightMalusFactor);
        
        public double EffectiveElo { get { return effectiveEloExpression.Evaluate(this); }}


        private static readonly CompiledExpression<Account, double> effectiveElo1v1Expression = DefaultTranslationOf<Account>.Property(e => e.Effective1v1Elo).Is(e => e.Elo1v1 + (GlobalConst.EloWeightMax - e.Elo1v1Weight) * GlobalConst.EloWeightMalusFactor);

        public double Effective1v1Elo { get { return effectiveElo1v1Expression.Evaluate(this); } }


        [NotMapped]
        public int AvailableXP { get {
            return GetXpForLevel(Level) -
                   AccountUnlocks.Sum(x => (int?)(x.Unlock.XpCost*(x.Count - KudosPurchases.Count(y => y.UnlockID == x.UnlockID)))) ?? 0;
        } }

        


        [NotMapped]
        public double EffectivePwElo { get { return EloPw + (GlobalConst.EloWeightMax - EloWeight) * GlobalConst.EloWeightMalusFactor; } }

        [NotMapped]
        public double EloInvWeight { get { return GlobalConst.EloWeightMax + 1 - EloWeight; } }

        [NotMapped]
        public int KudosGained { get { return ContributionsByAccountID.Sum(x => x.KudosValue); } }

        [NotMapped]
        public int KudosSpent { get { return KudosPurchases.Sum(x => x.KudosValue); } }



        public static Func<ZkDataContext, int, Account> AccountByAccountID = (db, accountID) => db.Accounts.FirstOrDefault(x => x.AccountID == accountID);

        public static Func<ZkDataContext, int, Account> AccountByLobbyID = (db, lobbyID) => db.Accounts.FirstOrDefault(x => x.LobbyID == lobbyID);

        public static Func<ZkDataContext, string, Account> AccountByName = (db, name) => db.Accounts.FirstOrDefault(x => x.Name == name && x.LobbyID != null);

        public static Func<ZkDataContext, string, string, Account> AccountVerify = (db, login, passwordHash) => db.Accounts.FirstOrDefault(x => x.Name == login && x.Password == passwordHash && x.LobbyID != null);


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
            Name = value;
        }

        public void SetAvatar()
        {
            if (string.IsNullOrEmpty(Avatar))
            {
                var rand = new Random();
                var avatars = ZkData.Avatar.GetCachedList();
                if (avatars.Any()) Avatar = avatars[rand.Next(avatars.Count)].AvatarName;
            }
        }

        [NotMapped]
        public string AuthenticationType { get { return "LobbyServer"; } }

        [NotMapped]
        public bool IsAuthenticated { get { return true; } }

        public bool IsInRole(string role) {
            if (role == "LobbyAdmin") return IsLobbyAdministrator;
            if (role == "ZkAdmin") return IsZeroKAdmin;
            else return string.IsNullOrEmpty(role);
        }

        [NotMapped]
        public IIdentity Identity { get { return this; } }

        public int GetEffectiveSpringieLevel()
        {
            if (PunishmentsByAccountID.Any(x => x.SetRightsToZero && !x.IsExpired)) return 0;
            return SpringieLevel;
        }
    }
}


