using System.Collections.Generic;
using System.Threading.Tasks;
using LobbyClient;
using System.Linq;
using PlasmaShared;
using ZeroKWeb;
using System;
using Ratings;
using ZkData;

namespace ZkLobbyServer
{
    /// <summary>
    /// <see cref="ILobbyServerApi"/> for a lobby server running in the same process. Pure
    /// forwarding - it exists so callers depend on the interface rather than on the server object,
    /// which is what makes a future out-of-process implementation a drop-in.
    /// </summary>
    public class InProcessLobbyServerApi : ILobbyServerApiInProcess
    {
        readonly ZkLobbyServer server;

        public InProcessLobbyServerApi(ZkLobbyServer server) {
            this.server = server;
        }

        public ZkLobbyServer InProcess => server;

        public Task GhostChanSay(string channelName, string text, bool isEmote = true, bool isRing = false) =>
            server.GhostChanSay(channelName, text, isEmote, isRing);

        public Task GhostPm(string name, string text) => server.GhostPm(name, text);

        public Task GhostSay(Say say, int? battleID = null) => server.GhostSay(say, battleID);

        public Task KickFromServer(string kickerName, string kickeeName, string reason) =>
            server.KickFromServer(kickerName, kickeeName, reason);

        public Task ForceJoinBattle(string playerName, string battleHost) =>
            server.ForceJoinBattle(playerName, battleHost);

        public Task ForceJoinBattle(string player, Battle bat) => server.ForceJoinBattle(player, bat);

        public Task SetTopic(string channel, string topic, string author) =>
            server.SetTopic(channel, topic, author);

        public Task RequestJoinPlanet(string name, int planetId, string attackerFaction) =>
            server.RequestJoinPlanet(name, planetId, attackerFaction);

        public Task SendSiteToLobbyCommand(string user, SiteToLobbyCommand command) =>
            server.SendSiteToLobbyCommand(user, command);

        public Task SetEngine(string engine) => server.SetEngine(engine);

        public Task SetGame(string game) => server.SetGame(game);

        public Task OnServerMapsChanged() => server.OnServerMapsChanged();

        /// <summary>
        /// Loads the account here rather than taking the caller's. Every call site saves before
        /// publishing - that was checked, on all nine - so a fresh read sees exactly what was
        /// committed, and the server stops receiving an entity attached to somebody else's
        /// context whose navigations lazy-load against it.
        /// </summary>
        public async Task PublishAccountUpdate(int accountID)
        {
            using (var db = new ZkDataContext())
            {
                var acc = db.Accounts.Find(accountID);
                if (acc != null) await server.PublishAccountUpdate(acc);
            }
        }

        public async Task PublishUserProfileUpdate(int accountID)
        {
            using (var db = new ZkDataContext())
            {
                var acc = db.Accounts.Find(accountID);
                if (acc != null) await server.PublishUserProfileUpdate(acc);
            }
        }

        public async Task ReportUser(int reporterAccountID, int reportedAccountID, string report)
        {
            using (var db = new ZkDataContext())
            {
                var reporter = db.Accounts.Find(reporterAccountID);
                var reported = db.Accounts.Find(reportedAccountID);
                if (reporter == null || reported == null) return;
                await server.ReportUser(db, reporter, reported, report);
            }
        }

        public bool IsLobbyConnected(string user) => server.IsLobbyConnected(user);

        public int GetDiscordUserCount() => server.GetDiscordUserCount();

        public int ConnectedUserCount => server.ConnectedUsers.Count;

        public List<Battle> GetPlanetBattles(Planet planet) => server.GetPlanetBattles(planet);

        public List<PlanetBattleInfo> GetPlanetBattles(string mapName) =>
            server.GetPlanetWarsBattles().Where(x => x.MapName == mapName).Select(Describe).ToList();

        public int? RedeemSessionToken(string token)
        {
            // ConcurrentDictionary.TryRemove throws on a null key, and the token arrives from a
            // query string.
            return server.SessionTokens.Redeem(token);
        }

        public List<PlanetBattleInfo> GetPlanetWarsBattles() =>
            server.GetPlanetWarsBattles().Select(Describe).ToList();

        /// <summary>
        /// The live battle reduced to what the website reads. Same filter the server's own
        /// GetPlanetBattles(Planet) applies - it compares MapName to planet.Resource.InternalName
        /// - with the difference that the caller now reads that property instead of the server.
        /// </summary>
        private static PlanetBattleInfo Describe(Battle b) => new PlanetBattleInfo()
        {
            BattleID = b.BattleID,
            MapName = b.MapName,
            IsInGame = b.IsInGame,
            UserCount = b.Users.Count,
        };

        public Task AddBattle(ServerBattle battle) => server.AddBattle(battle);

        public Task RemoveBattle(Battle battle) => server.RemoveBattle(battle);

        // ---- tournaments ----------------------------------------------------------------
        //
        // The only place in this class that converts rather than forwards. Everything else
        // hands a live object straight through; these map TourneyBattle onto TourneyBattleInfo,
        // which is what lets the calls leave the process. The mapping is the interesting part of
        // the tournament API - the rest of it is the same three operations TourneyController
        // already performs through InProcess.

        public List<TourneyBattleInfo> GetTourneyBattles() =>
            server.Battles.Values.Where(x => x != null).OfType<TourneyBattle>().Select(Describe).ToList();

        public TourneyBattleInfo GetTourneyBattle(int battleID)
        {
            server.Battles.TryGetValue(battleID, out var battle);
            return battle is TourneyBattle tourney ? Describe(tourney) : null;
        }

        public async Task<int> CreateTourneyBattle(TourneyPrototypeInfo prototype)
        {
            var battle = new TourneyBattle(server, new TourneyBattle.TourneyPrototype
            {
                Title = prototype.Title,
                FounderName = prototype.FounderName,
                TeamPlayers = prototype.TeamPlayers,
                ModOptions = prototype.ModOptions,
                MapOptions = prototype.MapOptions,
            });
            await AddBattle(battle);
            return battle.BattleID;
        }

        public async Task<bool> RemoveTourneyBattle(int battleID)
        {
            server.Battles.TryGetValue(battleID, out var battle);
            if (!(battle is TourneyBattle)) return false;
            await server.RemoveBattle(battle);
            return true;
        }

        public async Task ForceJoinTourneyBattle(string player, int battleID)
        {
            server.Battles.TryGetValue(battleID, out var battle);
            if (battle is TourneyBattle) await server.ForceJoinBattle(player, battle);
        }

        /// <summary>
        /// A live tournament battle as data. Users and Debriefings are copied rather than
        /// shared: the originals keep changing on the server, and a caller in another process
        /// would receive a snapshot, so a caller in this one should see the same thing.
        /// </summary>
        private static TourneyBattleInfo Describe(TourneyBattle battle) => new TourneyBattleInfo
        {
            BattleID = battle.BattleID,
            Title = battle.Title,
            FounderName = battle.FounderName,
            MaxPlayers = battle.MaxPlayers,
            SpectatorCount = battle.SpectatorCount,
            NonSpectatorCount = battle.NonSpectatorPlayerCount,
            Users = new Dictionary<string, UserBattleStatus>(battle.Users),
            Debriefings = new List<BattleDebriefing>(battle.Debriefings),
            Prototype = battle.Prototype == null ? new TourneyPrototypeInfo() : new TourneyPrototypeInfo
            {
                Title = battle.Prototype.Title,
                FounderName = battle.Prototype.FounderName,
                TeamPlayers = battle.Prototype.TeamPlayers,
                ModOptions = battle.Prototype.ModOptions,
                MapOptions = battle.Prototype.MapOptions,
            },
        };

        public NewsList GetCurrentNewsList() => server.NewsListManager.GetCurrentNewsList();

        public LadderList GetCurrentLadderList() => server.LadderListManager.GetCurrentLadderList();

        public ForumList GetCurrentForumList(int? accountID) => server.ForumListManager.GetCurrentForumList(accountID);

        public void OnNewsChanged() => server.NewsListManager.OnNewsChanged();

        public void AddClanChannel(int clanID)
        {
            using (var db = new ZkDataContext())
            {
                var clan = db.Clans.Find(clanID);
                if (clan != null) server.ChannelManager.AddClanChannel(clan);
            }
        }

        /// <summary>
        /// One primary-key read per call, which the old signature did not pay because the website
        /// handed over an account it had already loaded. That is the honest cost of the seam: a
        /// remote server would have to read it too, and an authorization check that trusts the
        /// caller's copy of AdminLevel is not one.
        /// </summary>
        public bool CanJoinChannel(int accountID, string channel)
        {
            using (var db = new ZkDataContext())
            {
                var acc = db.Accounts.Find(accountID);
                return acc != null && server.ChannelManager.CanJoin(acc, channel);
            }
        }

        public bool VerifyIp(string ip) => server.LoginChecker.VerifyIp(ip);

        public void LogIpFailure(string ip) => server.LoginChecker.LogIpFailure(ip);

        // ---- ratings ---------------------------------------------------------------------
        // These reach the statics directly rather than through the server object, because that
        // is where they live: Ratings.RatingSystems is in ZkData and shared by both halves. The
        // coupling was never the call, it was that only this process ever ran the pass that
        // fills them - which is why the website now creates its systems and reads the database,
        // and asks here only for what no table holds.

        public void ForceRatingsUpdate()
        {
            foreach (var system in RatingSystems.GetRatingSystems())
                (system as WholeHistoryRating)?.ForceRatingsUpdate();
        }

        public void ResetPlanetwarsRatings() =>
            (RatingSystems.GetRatingSystem(RatingCategory.Planetwars) as WholeHistoryRating)?.ResetAll();

        public Dictionary<DateTime, float> GetPlayerRatingHistory(RatingCategory category, int accountID) =>
            (RatingSystems.GetRatingSystem(category) as WholeHistoryRating)?.GetPlayerRatingHistory(accountID)
            ?? new Dictionary<DateTime, float>();

        public InternalRatingInfo GetInternalRating(RatingCategory category, int accountID, DateTime time)
        {
            var day = (RatingSystems.GetRatingSystem(category) as WholeHistoryRating)?.GetInternalRating(accountID, time);
            // Null means "no rating that day", which the caller renders as no number. Not an
            // error, and not a zero either.
            return day == null ? null : new InternalRatingInfo { Elo = day.GetElo(), EloStdev = day.GetEloStdev() };
        }

        public List<MapRatingInfo> GetMapRanking(MapRatings.Category category) =>
            MapRatings.GetMapRanking(category).Select(x => new MapRatingInfo
            {
                ResourceID = x.Map?.ResourceID ?? 0,
                Elo = x.Elo,
                EloStdev = x.EloStdev,
                Percentile = x.Percentile,
                Rank = x.Rank,
            }).ToList();

        public bool IsPlanetWarsMatchMakerRunning => server.PlanetWarsMatchMaker != null;

        public PwPhase? PlanetWarsPhase => server.PlanetWarsMatchMaker?.Phase;

        /// <summary>
        /// Safe to load and hand over: AddAttackOption copies scalars out of the planet into an
        /// AttackOption and keeps no reference, so nothing outlives this context.
        /// </summary>
        public void AddPlanetWarsAttackOption(int planetID, int attackerFactionId)
        {
            if (server.PlanetWarsMatchMaker == null) return;
            using (var db = new ZkDataContext())
            {
                var planet = db.Planets.Find(planetID);
                if (planet != null) server.PlanetWarsMatchMaker.AddAttackOption(planet, attackerFactionId);
            }
        }

        public PwMatchCommand GeneratePlanetWarsLobbyCommand(string playerName, string playerFaction) =>
            server.PlanetWarsMatchMaker?.GenerateLobbyCommand(playerName, playerFaction);

        public LobbyBattleStats GetBattleStats() {
            var stats = new LobbyBattleStats();
            foreach (var b in server.Battles.Values) {
                if (b == null || !b.IsInGame) continue;
                stats.BattlesRunning++;
                stats.UsersFighting += b.NonSpectatorCount + b.SpectatorCount;
            }
            return stats;
        }

        public bool IsUserInAnyBattle(string userName) =>
            server.Battles.Any(x => x.Value != null && x.Value.GetAllUserNames().Contains(userName));

        public Task ConnectPlayerToBattle(string userName, int battleID) {
            var user = server.ConnectedUsers.Get(userName);
            if (user == null) return Task.FromResult(0);
            return user.Process(new RequestConnectSpring() { BattleID = battleID });
        }
    }
}
