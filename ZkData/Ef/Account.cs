using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using LobbyClient;
using Microsoft.Linq.Translations;
using PlasmaShared;
using ZkData;

namespace ZkData
{
    public class Account : IPrincipal, IIdentity
    {

        public Account()
        {
            Elo = 1500;
            EloMm = 1500;
            EloPw = 1500;
            EloWeight = 1;
            EloMmWeight = 1;
            FirstLogin = DateTime.UtcNow;
            LastLogin = DateTime.UtcNow;
            LastLogout = DateTime.UtcNow;
            Country = "??";


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
        
        public DevLevel DevLevel { get; set; }
        [StringLength(200)]
        public string SpecialNote { get; set; }

        public AdminLevel AdminLevel { get; set; }


        public int AccountID { get; set; }
        [Required]
        [StringLength(200)]
        [Index]
        public string Name { get; set; }
        [StringLength(200)]
        public string Email { get; set; }
        public DateTime FirstLogin { get; set; }
        public DateTime LastLogin { get; set; }
        public DateTime LastLogout { get; set; }

        [StringLength(8000)]
        public string Aliases { get; set; }
        public double Elo { get; set; }
        public double EloWeight { get; set; }
        public double EloMm { get; set; }
        public double EloMmWeight { get; set; }
        public double EloPw { get; set; }
        public int? CasualRank { get; set; }
        public int? CompetitiveRank { get; set; }
        public bool IsBot { get; set; }
        public bool CanPlayMultiplayer { get; set; } = true;
       
        [NotMapped]
        public string NewPasswordPlain {set {SetPasswordPlain(value);}}

        [StringLength(150)]
        public string PasswordBcrypt { get; set; }

        [StringLength(5)]
        public string Country { get; set; }
        public int MissionRunCount { get; set; }
        public int Xp { get; set; }
        public int Level { get; set; }
        public int? ClanID { get; set; }
        public DateTime? LastNewsRead { get; set; }
        public int? FactionID { get; set; }
        public bool IsDeleted { get; set; }
        [StringLength(50)]
        public string Avatar { get; set; }
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
        public bool HasVpnException { get; set; }
        public int Kudos { get; set; }
        public int ForumTotalUpvotes { get; set; }
        public int ForumTotalDownvotes { get; set; }
        public int? VotesAvailable { get; set; }

        [Index(IsUnique = true)]
        public decimal? SteamID { get; set; }

        [StringLength(200)]
        public string SteamName { get; set; }
        public int Cpu { get; set; }

        [Obsolete("Do not use")]
        public int? LobbyID { get; set; }

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
        [InverseProperty("Owner")]
        public virtual ICollection<AccountRelation> RelalationsByOwner { get; set; } = new List<AccountRelation>();
        [InverseProperty("Target")]
        public virtual ICollection<AccountRelation> RelalationsByTarget { get; set; } = new List<AccountRelation>();
        public virtual Clan Clan { get; set; }




        private static readonly CompiledExpression<Account, double> effectiveEloExpression = DefaultTranslationOf<Account>.Property(e => e.EffectiveElo).Is(e => e.Elo + (GlobalConst.EloWeightMax - e.EloWeight) * GlobalConst.EloWeightMalusFactor);

        public double EffectiveElo => effectiveEloExpression.Evaluate(this);


        private static readonly CompiledExpression<Account, double> effectiveEloMmExpression = DefaultTranslationOf<Account>.Property(e => e.EffectiveMmElo).Is(e => e.EloMm + (GlobalConst.EloWeightMax - e.EloMmWeight) * GlobalConst.EloWeightMalusFactor);

        public double EffectiveMmElo => effectiveEloMmExpression.Evaluate(this);

        public double BestEffectiveElo => Math.Max(EffectiveMmElo, EffectiveElo);


        [NotMapped]
        public int AvailableXP
        {
            get
            {
                return GetXpForLevel(Level) -
                       AccountUnlocks.Sum(x => (int?)(x.Unlock.XpCost * (x.Count - KudosPurchases.Count(y => y.UnlockID == x.UnlockID)))) ?? 0;
            }
        }




        [NotMapped]
        public double EffectivePwElo { get { return EloPw + (GlobalConst.EloWeightMax - EloWeight) * GlobalConst.EloWeightMalusFactor; } }


        [NotMapped]
        public int KudosGained { get { return ContributionsByAccountID.Sum(x => x.KudosValue); } }

        [NotMapped]
        public int KudosSpent { get { return KudosPurchases.Sum(x => x.KudosValue); } }


        public static Account AccountByName(ZkDataContext db, string name)
        {
            return db.Accounts.FirstOrDefault(x => x.Name == name);
        }

        public static Account AccountVerify(ZkDataContext db, string login, string passwordHash)
        {
            var acc = db.Accounts.FirstOrDefault(x => x.Name == login && !x.IsDeleted);
            if (acc != null && acc.VerifyPassword(passwordHash)) return acc;
            return null;
        }


        public bool VerifyPassword(string passwordHash)
        {
            if (!string.IsNullOrEmpty(PasswordBcrypt)) return BCrypt.Net.BCrypt.Verify(passwordHash, PasswordBcrypt);
            return false;
        }


        public void SetPasswordHashed(string passwordHash)
        {
            if (string.IsNullOrEmpty(passwordHash)) PasswordBcrypt = null;
            else PasswordBcrypt = BCrypt.Net.BCrypt.HashPassword(passwordHash, 4);
        }

        public void SetPasswordPlain(string passwordPlain)
        {
            SetPasswordHashed(Utils.HashLobbyPassword(passwordPlain));
        }



        public static double AdjustEloWeight(double currentWeight, double sumWeight, int sumCount)
        {
            if (currentWeight < GlobalConst.EloWeightMax)
            {
                currentWeight = (currentWeight + ((sumWeight - currentWeight - (sumCount - 1)) / (sumCount - 1)) / GlobalConst.EloWeightLearnFactor);
                if (currentWeight > GlobalConst.EloWeightMax) currentWeight = GlobalConst.EloWeightMax;
            }
            return currentWeight;
        }

        public bool CanAppoint(Account targetAccount, RoleType roleType)
        {
            if (targetAccount.AccountID != AccountID && targetAccount.FactionID == FactionID &&
                (!roleType.IsClanOnly || targetAccount.ClanID == ClanID) &&
                (roleType.RestrictFactionID == null || roleType.RestrictFactionID == FactionID))
            {
                return AccountRolesByAccountID.Any(
                        x => x.RoleType.RoleTypeHierarchiesByMasterRoleTypeID.Any(y => y.CanAppoint && y.SlaveRoleTypeID == roleType.RoleTypeID));
            }
            else return false;
        }

        public bool CanRecall(Account targetAccount, RoleType roleType)
        {
            if (targetAccount.AccountID != AccountID && targetAccount.FactionID == FactionID &&
                (!roleType.IsClanOnly || targetAccount.ClanID == ClanID))
            {
                return
                    AccountRolesByAccountID.Any(
                        x => x.RoleType.RoleTypeHierarchiesByMasterRoleTypeID.Any(y => y.CanRecall && y.SlaveRoleTypeID == roleType.RoleTypeID));
            }
            else return false;
        }


        public bool CanPlayerPlanetWars()
        {
            return FactionID != null && Level >= GlobalConst.MinPlanetWarsLevel && EffectiveMmElo > GlobalConst.MinPlanetWarsElo;
        }


        public bool CanSetPriority(PlanetStructure ps)
        {
            if (Faction == null) return false;
            if (ps.Planet.OwnerFactionID == FactionID && HasFactionRight(x => x.RightSetEnergyPriority)) return true;
            if (ClanID != null && ps.Account != null && ps.Account.ClanID == ClanID && HasClanRight(x => x.RightSetEnergyPriority)) return true;
            return false;
        }

        public bool CanSetStructureTarget(PlanetStructure ps)
        {
            if (Faction == null) return false;
            if (ps.OwnerAccountID == AccountID || ps.Planet.OwnerAccountID == AccountID) return true; // owner of planet or owner of structure
            if (ps.Planet.OwnerFactionID == FactionID && HasFactionRight(x => x.RightDropshipQuota > 0)) return true;
            return false;
        }

        public bool CanVoteRecall(Account targetAccount, RoleType roleType)
        {
            if (roleType.IsVoteable && targetAccount.FactionID == FactionID && (!roleType.IsClanOnly || targetAccount.ClanID == ClanID)) return true;
            else return false;
        }

        public void CheckLevelUp()
        {
            if (Xp > GetXpForLevel(Level + 1)) Level++;
        }

        public int GetBomberCapacity()
        {
            if (Faction == null) return 0;
            return GlobalConst.DefaultBomberCapacity +
                   (Faction.Planets.SelectMany(x => x.PlanetStructures).Where(x => x.IsActive).Sum(x => x.StructureType.EffectBomberCapacity) ?? 0);
        }

        public double GetBomberQuota()
        {
            if (PwBombersProduced < PwBombersUsed) PwBombersUsed = PwBombersProduced;

            return GetQuota(x => x.PwBombersProduced, x => x.PwBombersUsed, x => x.RightBomberQuota, x => x.Bombers);
        }

        public double GetBombersAvailable()
        {
            if (Faction != null) return Math.Min(GetBomberQuota(), Faction.Bombers);
            else return 0;
        }

        public int GetDropshipCapacity()
        {
            if (Faction == null) return 0;
            return GlobalConst.DefaultDropshipCapacity +
                   (Faction.Planets.SelectMany(x => x.PlanetStructures).Where(x => x.IsActive).Sum(x => x.StructureType.EffectDropshipCapacity) ?? 0);
        }


        public double GetDropshipQuota()
        {
            if (PwDropshipsProduced < PwDropshipsUsed) PwDropshipsUsed = PwDropshipsProduced;

            return GetQuota(x => x.PwDropshipsProduced, x => x.PwDropshipsUsed, x => x.RightDropshipQuota, x => x.Dropships);
        }

        public double GetDropshipsAvailable()
        {
            if (Faction != null)
            {
                var q = GetDropshipQuota();
                return Math.Min(q, Faction.Dropships);
            }
            else return 0;
        }

        public double GetMetalAvailable()
        {
            if (Faction != null) return Math.Min(GetMetalQuota(), Faction.Metal);
            else return 0;
        }

        public double GetMetalQuota()
        {
            if (PwMetalProduced < PwMetalUsed) PwMetalUsed = PwMetalProduced;

            return GetQuota(x => x.PwMetalProduced, x => x.PwMetalUsed, x => x.RightMetalQuota, x => x.Metal);
        }

        public double GetQuota(Func<Account, double> producedSelector,
                               Func<Account, double> usedSelector,
                               Func<RoleType, double?> quotaSelector,
                               Func<Faction, double> factionResources)
        {
            var total = producedSelector(this) - usedSelector(this);
            if (total < 0) total = 0;

            if (Faction != null)
            {
                var clanQratio = AccountRolesByAccountID.Where(x => x.RoleType.IsClanOnly).Select(x => x.RoleType).Max(quotaSelector) ?? 0;
                var factionQratio = AccountRolesByAccountID.Where(x => !x.RoleType.IsClanOnly).Select(x => x.RoleType).Max(quotaSelector) ?? 0;

                var facRes = factionResources(Faction);

                if (factionQratio >= clanQratio) total += facRes * factionQratio;
                else
                {
                    if (clanQratio > 0 && Clan != null)
                    {
                        var sumClanProd = Clan.Accounts.Sum(producedSelector);
                        var sumFacProd = Faction.Accounts.Sum(producedSelector);
                        if (sumFacProd > 0)
                        {
                            var clanRes = facRes * sumClanProd / sumFacProd;
                            total += clanRes * clanQratio + (facRes - clanRes) * factionQratio;
                        }
                    }
                }
            }

            return Math.Floor(total);
        }

        public double GetWarpAvailable()
        {
            if (Faction != null) return Math.Min(GetWarpQuota(), Faction.Warps);
            else return 0;
        }

        public double GetWarpQuota()
        {
            if (PwWarpProduced < PwWarpUsed) PwWarpUsed = PwWarpProduced;

            return GetQuota(x => x.PwWarpProduced, x => x.PwWarpUsed, x => x.RightWarpQuota, x => x.Warps);
        }

        public static int GetXpForLevel(int level)
        {
            if (level < 0) return 0;
            return level * 80 + 20 * level * level;
        }

        public bool HasClanRight(Func<RoleType, bool> test)
        {
            return AccountRolesByAccountID.Where(x => x.RoleType.IsClanOnly).Select(x => x.RoleType).Any(test);
        }

        public bool HasFactionRight(Func<RoleType, bool> test)
        {
            return AccountRolesByAccountID.Where(x => !x.RoleType.IsClanOnly).Select(x => x.RoleType).Any(test);
        }

        public void ProduceBombers(double count)
        {
            PwBombersProduced += count;
            Faction.Bombers += count;

            if (PwBombersUsed > PwBombersProduced) PwBombersUsed = PwBombersProduced;
        }


        public void ProduceDropships(double count)
        {
            PwDropshipsProduced += count;
            Faction.Dropships += count;

            if (PwDropshipsProduced < PwDropshipsUsed) PwDropshipsUsed = PwDropshipsProduced;
        }


        public void ProduceMetal(double count)
        {
            PwMetalProduced += count;
            Faction.Metal += count;
            if (PwMetalUsed > PwMetalProduced) PwMetalUsed = PwMetalProduced;
        }

        public void ProduceWarps(double count)
        {
            PwWarpProduced += count;
            Faction.Warps += count;

            if (PwWarpUsed > PwWarpProduced) PwWarpUsed = PwWarpProduced;
        }

        /// <summary>
        /// Todo distribute among clan and faction members
        /// </summary>
        public void ResetQuotas()
        {
            PwBombersProduced = 0;
            PwBombersUsed = 0;
            PwDropshipsProduced = 0;
            PwDropshipsUsed = 0;
            PwMetalProduced = 0;
            PwMetalUsed = 0;
        }



        public void SpendBombers(double count)
        {
            PwBombersUsed += count;
            Faction.Bombers -= count;

            if (PwBombersUsed > PwBombersProduced) PwBombersUsed = PwBombersProduced;
        }

        public void SpendDropships(double count)
        {
            PwDropshipsUsed += count;
            Faction.Dropships -= count;

            if (PwDropshipsProduced < PwDropshipsUsed) PwDropshipsUsed = PwDropshipsProduced;
        }

        public void SpendMetal(double count)
        {
            PwMetalUsed += count;
            Faction.Metal -= count;

            if (PwMetalUsed > PwMetalProduced) PwMetalUsed = PwMetalProduced;
        }

        public void SpendWarps(double count)
        {
            PwWarpUsed += count;
            Faction.Warps -= count;

            if (PwWarpUsed > PwWarpProduced) PwWarpUsed = PwWarpProduced;
        }


        public static IEnumerable<Poll> ValidPolls(Account acc, ZkDataContext db = null)
        {
            if (db == null) db = new ZkDataContext();
            var clanID = acc?.ClanID;
            var facID = acc?.FactionID;

            return
                db.Polls.Where(
                    x =>
                    (x.ExpireBy == null || x.ExpireBy > DateTime.UtcNow) && (x.RestrictClanID == null || x.RestrictClanID == clanID) &&
                    (x.RestrictFactionID == null || x.RestrictFactionID == facID));
        }

        public override string ToString()
        {
            return Name;
        }


        public void SetName(string value)
        {
            if (!String.IsNullOrEmpty(Name) && !String.IsNullOrEmpty(value) && Name!=value)
            {
                List<string> aliases = null;
                if (!String.IsNullOrEmpty(Aliases)) aliases = new List<string>(Aliases.Split(','));
                else aliases = new List<string>();

                if (!aliases.Contains(Name)) aliases.Add(Name);
                Aliases = String.Join(",", aliases.ToArray());
            }
            Name = value;
        }

        public void SetAvatar()
        {
            if (String.IsNullOrEmpty(Avatar))
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

        public bool IsInRole(string role)
        {
            var al = (AdminLevel)Enum.Parse(typeof(AdminLevel), role);
            return AdminLevel >= al;
        }

        [NotMapped]
        public IIdentity Identity { get { return this; } }
        public bool CanEditWiki() => Level >= GlobalConst.WikiEditLevel;


        public static bool IsValidLobbyName(string name)
        {
            return !string.IsNullOrEmpty(name) && name.Length <= GlobalConst.MaxUsernameLength && name.All(Utils.ValidLobbyNameCharacter);
        }


        public enum BadgeType
        {
            [Description("Exceedingly high level")]
            player_level = 0,
            [Description("Top 3 player")]
            player_elo = 1,
            [Description("Bronze donator")]
            donator_0 = 2,
            [Description("Silver donator")]
            donator_1 = 3,
            [Description("Gold donator")]
            donator_2 = 4,
            [Description("External developer")]
            dev_content =  5,
            [Description("Game developer")]
            dev_game = 6,
            [Description("Lead developer")]
            dev_adv = 7
        }

        /// <summary>
        /// Gets account name without extension
        /// </summary>
        public string GetIconName()
        {
            var clampedLevel = System.Math.Max(0, System.Math.Min(7, (int)System.Math.Floor((-0.12 / Math.Cosh((Level - 61.9) / 7.08) + 1)
    * 2.93 * Math.Log(Math.Exp(-2.31) * Level + 1) - 0.89 / Math.Cosh((Level - 28.55) / 3.4))));
            //0, 5, 10, 20, 35, 50, 75, 100 -> 0, 1, 2, 3, 4, 5, 6, 7

            int clampedSkill = 0;

            if (EloWeight > 1) clampedSkill = System.Math.Max(0, System.Math.Min(7, (int)System.Math.Floor((Math.Max(EffectiveMmElo, EffectiveElo) - 1000.0)) / 200));

            return $"{clampedLevel}_{clampedSkill}";
        }

        public List<BadgeType> GetBadges()
        {
            var ret = new List<BadgeType>();
            if (Level > 200) ret.Add(BadgeType.player_level); 
            if (CompetitiveRank <= 3 || CasualRank <= 3) ret.Add(BadgeType.player_elo); // top 3 best
            var total = Kudos> 0 ? ContributionsByAccountID.Where(x=>x.OriginalAmount > 0).Sum(x => (int?)x.KudosValue) : 0;

            if (total >= GlobalConst.KudosForGold) ret.Add(BadgeType.donator_2);
            else if (total >= GlobalConst.KudosForSilver) ret.Add(BadgeType.donator_1);
            else if (total >= GlobalConst.KudosForBronze) ret.Add(BadgeType.donator_0);

            if (DevLevel >= DevLevel.CoreDeveloper) ret.Add(BadgeType.dev_adv);
            else if (DevLevel >= DevLevel.Developer) ret.Add(BadgeType.dev_game);
            else if (DevLevel >= DevLevel.Contributor) ret.Add(BadgeType.dev_content);

            return ret.OrderByDescending(x => (int)x).ToList();
        }
    }
}


