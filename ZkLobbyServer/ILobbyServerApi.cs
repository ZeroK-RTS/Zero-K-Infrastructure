using System;
﻿using System.Collections.Generic;
using System.Threading.Tasks;
using LobbyClient;
using ZeroKWeb;
using PlasmaShared;
using Ratings;
using ZkData;

namespace ZkLobbyServer
{
    /// <summary>
    /// Everything the website asks of the lobby server.
    ///
    /// The lobby server currently runs inside the IIS worker process, so any app-pool restart
    /// disconnects every player (see Zero-K.info/HOSTING.md). Moving it to its own process is
    /// blocked on the website reaching directly into the server's live in-memory state.
    ///
    /// **Every member here compiles outside the lobby server**, and that is now enforced rather
    /// than intended: this file is linked into the .NET 9 port (ZeroKWeb.Core and friends), which
    /// has no reference to this project, so a member naming a server-side type breaks that build.
    ///
    /// The six that did name one - ForceJoinBattle(Battle), GetPlanetBattles, AddBattle,
    /// RemoveBattle, PlanetWarsPhase and InProcess - moved to
    /// <see cref="ILobbyServerApiInProcess"/>. They are the remaining Phase 1 work, and they are
    /// now separated by a compiler rather than by a comment.
    ///
    /// Adding a member here that needs Battle, ServerBattle, PwPhase or ZkLobbyServer will fail
    /// the port's build. That is the point: it means the call has not been designed yet, and it
    /// belongs in the other interface until it has been.
    /// </summary>
    public interface ILobbyServerApi
    {
        // ---- commands -------------------------------------------------------------------
        // Fire-and-forget from the website's point of view. These port to a remote call directly.

        [ChangesLobbyState("posts a message in a channel")]
        Task GhostChanSay(string channelName, string text, bool isEmote = true, bool isRing = false);
        [ChangesLobbyState("sends a private message")]
        Task GhostPm(string name, string text);
        [ChangesLobbyState("posts a message")]
        Task GhostSay(Say say, int? battleID = null);
        [ChangesLobbyState("disconnects a player")]
        Task KickFromServer(string kickerName, string kickeeName, string reason);
        [ChangesLobbyState("moves a player into a battle")]
        Task ForceJoinBattle(string playerName, string battleHost);
        [ChangesLobbyState("sets a channel topic")]
        Task SetTopic(string channel, string topic, string author);
        [ChangesLobbyState("joins a PlanetWars planet")]
        Task RequestJoinPlanet(string name, int planetId, string attackerFaction);
        [ChangesLobbyState("makes a player's own game client act")]
        Task SendSiteToLobbyCommand(string user, SiteToLobbyCommand command);
        [ChangesLobbyState("changes the engine every player gets")]
        Task SetEngine(string engine);
        [ChangesLobbyState("changes the game version every player gets")]
        Task SetGame(string game);
        [ChangesLobbyState("makes the server reload its map list")]
        Task OnServerMapsChanged();

        /// <summary>Pushes an account's changed data to connected clients.</summary>
        [ChangesLobbyState("pushes an account to every connected client")]
        Task PublishAccountUpdate(int accountID);

        /// <summary>Pushes an account's changed profile to connected clients.</summary>
        [ChangesLobbyState("pushes a profile to every connected client")]
        Task PublishUserProfileUpdate(int accountID);

        /// <summary>
        /// The server writes the AbuseReport in its own context. It used to take the caller's,
        /// which read as sharing a transaction - but the one call site opens a context purely to
        /// validate the account id and has nothing else pending, so nothing was ever shared.
        /// </summary>
        [ChangesLobbyState("writes an AbuseReport row")]
        Task ReportUser(int reporterAccountID, int reportedAccountID, string report);

        // ---- queries --------------------------------------------------------------------

        [ReadsLobbyState]
        bool IsLobbyConnected(string user);
        [ReadsLobbyState]
        int GetDiscordUserCount();

        /// <summary>Number of clients currently connected to the lobby server.</summary>
        [ReadsLobbyState]
        int ConnectedUserCount { get; }



        // ---- lobby content lists --------------------------------------------------------
        // Served to the game client and refreshed when the website edits the underlying rows.

        [ReadsLobbyState]
        NewsList GetCurrentNewsList();
        [ReadsLobbyState]
        LadderList GetCurrentLadderList();
        [ReadsLobbyState]
        ForumList GetCurrentForumList(int? accountID);
        [ChangesLobbyState("makes the server rebuild its news list")]
        void OnNewsChanged();

        // ---- channels -------------------------------------------------------------------

        [ChangesLobbyState("creates a channel")]
        void AddClanChannel(int clanID);

        /// <summary>
        /// An authorization question, so the server answers it from its OWN copy of the account.
        /// It used to take the caller's entity, which meant the website supplying the AdminLevel
        /// and DevLevel that the answer turns on.
        /// </summary>
        [ReadsLobbyState]
        bool CanJoinChannel(int accountID, string channel);

        // ---- login throttling -----------------------------------------------------------
        // Shared between the lobby and the website so a brute force cannot dodge one by using
        // the other.

        [ReadsLobbyState]
        bool VerifyIp(string ip);
        [ChangesLobbyState("records a failed login against an IP")]
        void LogIpFailure(string ip);

        // ---- ratings ---------------------------------------------------------------------
        // Almost everything the site asks about ratings is answered from the AccountRatings
        // table without asking the server at all - see RatingSystems.CreateRatingSystems. What
        // is here is the part that is not in any table: the WHR pass's own working state, and
        // two commands that tell the process running that pass to do something.

        /// <summary>Recompute every rating system now. The admin button.</summary>
        [ChangesLobbyState("recomputes every rating")]
        void ForceRatingsUpdate();

        /// <summary>Throw away the PlanetWars ratings and start them again.</summary>
        [ChangesLobbyState("throws away the PlanetWars ratings")]
        void ResetPlanetwarsRatings();

        /// <summary>Day by day rating for one player, for the chart. Empty when nothing computed it.</summary>
        [ReadsLobbyState]
        Dictionary<DateTime, float> GetPlayerRatingHistory(RatingCategory category, int accountID);

        /// <summary>
        /// The WHR internals for one player at one moment, which WhrController publishes.
        /// A DTO rather than the PlayerDay itself: that holds the player's whole game graph,
        /// and the caller reads two floats off it.
        /// </summary>
        [ReadsLobbyState]
        InternalRatingInfo GetInternalRating(RatingCategory category, int accountID, DateTime time);

        /// <summary>
        /// The map ranking, as ids and numbers. The website joins them to its own Resources.
        /// </summary>
        [ReadsLobbyState]
        List<MapRatingInfo> GetMapRanking(MapRatings.Category category);

        // ---- planetwars matchmaker ------------------------------------------------------

        /// <summary>False when PlanetWars is offline; callers must handle that.</summary>
        [ReadsLobbyState]
        bool IsPlanetWarsMatchMakerRunning { get; }


        [ChangesLobbyState("adds an attack option to the current turn")]
        void AddPlanetWarsAttackOption(int planetID, int attackerFactionId);

        /// <summary>
        /// Which half of the turn the matchmaker is in; null when PlanetWars is offline.
        ///
        /// This was on the in-process interface only because PwPhase lived in a lobby-server
        /// file. Moving that enum to PlanetWarsApi.cs, which the port links, is the whole change.
        /// </summary>
        [ReadsLobbyState]
        PwPhase? PlanetWarsPhase { get; }

        /// <summary>
        /// The PlanetWars battles on one map. Callers pass <c>planet.Resource.InternalName</c>.
        ///
        /// It takes a map name rather than the <c>Planet</c> it used to, because the server did
        /// nothing with the entity but read that one string off it - through a navigation
        /// property, on an entity the WEBSITE's DbContext had loaded. In one process that is a
        /// lazy load; in two it is not possible at all.
        /// </summary>
        [ReadsLobbyState]
        List<PlanetBattleInfo> GetPlanetBattles(string mapName);

        /// <summary>
        /// Every PlanetWars battle on the server, in one call.
        ///
        /// For the galaxy map, which asks the question once per planet. That loop is
        /// O(planets x battles) in this process already; as a remote call it would have been one
        /// round trip per planet.
        /// </summary>
        [ReadsLobbyState]
        List<PlanetBattleInfo> GetPlanetWarsBattles();

        /// <summary>Per-viewer, so attack options render with the right flags. Null when offline.</summary>
        [ReadsLobbyState]
        PwMatchCommand GeneratePlanetWarsLobbyCommand(string playerName, string playerFaction);

        // ---- battles --------------------------------------------------------------------

        /// <summary>Aggregate counts for the front page, so callers do not walk the battle list.</summary>
        [ReadsLobbyState]
        LobbyBattleStats GetBattleStats();

        /// <summary>Used to warn before renaming someone who is mid-battle.</summary>
        [ReadsLobbyState]
        bool IsUserInAnyBattle(string userName);

        // ---- tournaments ----------------------------------------------------------------
        // TourneyController's whole surface, in terms of TourneyBattleInfo rather than the live
        // TourneyBattle objects. The controller now calls these: its eight LobbyApi.InProcess
        // uses are gone, and with them the website's last use of the escape hatch.

        /// <summary>Every tournament battle currently on the server.</summary>
        [ReadsLobbyState]
        List<TourneyBattleInfo> GetTourneyBattles();

        /// <summary>One of them, or null when no tournament battle has that id.</summary>
        [ReadsLobbyState]
        TourneyBattleInfo GetTourneyBattle(int battleID);

        /// <summary>
        /// Creates a tournament battle from a prototype and returns its id.
        /// The website sends names, not accounts: it has already resolved them.
        /// </summary>
        [ChangesLobbyState("creates a battle")]
        Task<int> CreateTourneyBattle(TourneyPrototypeInfo prototype);

        /// <summary>Removes one. False when it was not there.</summary>
        [ChangesLobbyState("removes a battle")]
        Task<bool> RemoveTourneyBattle(int battleID);

        /// <summary>
        /// Forces a player into a tournament battle, addressed by id.
        ///
        /// The general ForceJoinBattle takes a host NAME and finds the first battle with that
        /// founder, which is not the same question: two battles can share a founder, and the
        /// tournament console has the id in hand anyway. This is the "a battle id would do"
        /// note on the in-process overload, done.
        /// </summary>
        [ChangesLobbyState("moves a player into a battle")]
        Task ForceJoinTourneyBattle(string player, int battleID);

        // ---- single sign-on -------------------------------------------------------------

        /// <summary>
        /// Redeems a lobby session token for the account it was issued to, and invalidates it.
        /// Returns null when the token is unknown, already used, or null.
        ///
        /// This is how a player logged into the game client arrives at the website already
        /// signed in: ClientConnection puts the token in the server's table at login, the client
        /// puts it in a URL, and Global.asax redeems it here. Single use, by design - the old
        /// code called ConcurrentDictionary.TryRemove directly, and this keeps that.
        ///
        /// It was the last thing reaching through <see cref="ILobbyServerApiInProcess.InProcess"/>,
        /// and it was reaching for a raw dictionary. A remote implementation needs a real
        /// endpoint for this one rather than a wrapper, and it is worth noticing that the token
        /// is a bearer credential: whatever carries this call has to be as trusted as the table.
        /// </summary>
        [ChangesLobbyState("single use - redeeming invalidates the token")]
        int? RedeemSessionToken(string token);

        // ---- connected users ------------------------------------------------------------

        /// <summary>Tells a connected client to join a battle. No-op when the user is offline.</summary>
        [ChangesLobbyState("tells a client to join a battle")]
        Task ConnectPlayerToBattle(string userName, int battleID);

        // ---- nothing left ---------------------------------------------------------------
        //
        // This interface is now the website's whole lobby-server surface. ILobbyServerApiInProcess
        // still exists and the lobby server still implements it, but no caller in Zero-K.info
        // names any of its members - including InProcess itself.
        //
        // Nothing is bodged onto this interface to get there: every member above compiles in a
        // project with no reference to ZkLobbyServer, and the port's build is what enforces it.

    }

    /// <summary>
    /// This member changes something on the lobby server: a message is posted, a player is moved,
    /// a setting every player sees is replaced. <paramref name="what"/> says which, in a few words.
    ///
    /// tools/check-lobby-writes.py reads these. An action reachable by GET that calls one of them
    /// is a forgeable state change, and the site's other CSRF check cannot see it: that one
    /// matches the DATABASE call, and these effects are out in the other process.
    ///
    /// Defined HERE, beside the interface, because this file is the one linked into the .NET 9
    /// port - the attribute travels with the thing it annotates and there is no second place for
    /// the two to drift apart.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public sealed class ChangesLobbyStateAttribute : Attribute
    {
        public ChangesLobbyStateAttribute(string what) { What = what; }
        public string What { get; }
    }

    /// <summary>
    /// This member only answers a question. Asking it twice is the same as asking it once.
    ///
    /// It exists so that EVERY member has to be classified: a new one with neither attribute
    /// fails tools/check-lobby-writes.py until somebody has decided which it is. A default would
    /// make that decision silently, and the wrong default is the whole failure mode here.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public sealed class ReadsLobbyStateAttribute : Attribute
    {
    }

    /// <summary>Aggregate battle counts, so the website does not need the live battle list.</summary>
    public class LobbyBattleStats
    {
        public int BattlesRunning;
        public int UsersFighting;
    }
}
