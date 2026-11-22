using System.Collections.Generic;
using LobbyClient;

namespace ZkLobbyServer
{
    /// <summary>
    /// The tournament battles, as data rather than as live server objects.
    ///
    /// This is the last thing standing between <see cref="ILobbyServerApi"/> and an
    /// out-of-process lobby server. `TourneyController` is a tournament admin console over
    /// `TourneyBattle` instances - it lists them, creates them from a prototype, reads their
    /// debriefings, and the Razor view renders those objects directly - so every one of its
    /// eight `LobbyApi.InProcess` uses needs this shape before the server can move out.
    ///
    /// **Every member here is a primitive or a LobbyClient protocol type**, which is what makes
    /// it crossable: `UserBattleStatus` and `BattleDebriefing` already travel between the lobby
    /// server and the game client, so they travel to a website in another process too.
    ///
    /// The field NAMES are deliberately the ones `TourneyBattle` and `ServerBattle` already
    /// expose - BattleID, Title, FounderName, MaxPlayers, Users, Debriefings, Prototype and the
    /// two counts. `Views/Tourney/TourneyIndex.cshtml` reads exactly those, so a controller
    /// handing it this type instead of the live one needs no change to the view.
    ///
    /// That is now measured rather than assumed. When this type was added, the claim here was
    /// that nothing in the repository could compile a Razor view and so the match could not be
    /// checked. That was wrong: it is true of the mono build, which has no aspnet_compiler, but
    /// ZeroKWeb.Core compiles views with the Razor source generator, and it now links
    /// TourneyController. Building that view against this type produces two errors, both
    /// `MultiSelectFor` - an MVC 5 helper still in the unported half of HtmlHelperExtensions -
    /// and none from the nine members above. The view was previously blocked by CS0234 on the
    /// controller type itself, which masked everything behind it.
    /// </summary>
    public class TourneyBattleInfo
    {
        public int BattleID;
        public string Title;
        public string FounderName;
        public int MaxPlayers;
        public int SpectatorCount;
        public int NonSpectatorCount;

        /// <summary>Keyed by name, as <c>ServerBattle.Users</c> is, so `b.Users.Values` still reads.</summary>
        public Dictionary<string, UserBattleStatus> Users = new Dictionary<string, UserBattleStatus>();

        public List<BattleDebriefing> Debriefings = new List<BattleDebriefing>();

        public TourneyPrototypeInfo Prototype = new TourneyPrototypeInfo();
    }

    /// <summary>
    /// A tournament's setup, in and out. The website sends one to create a battle and reads one
    /// back to render it, so the same shape serves both directions.
    ///
    /// Mirrors <see cref="TourneyBattle.TourneyPrototype"/> field for field - that type lives
    /// inside the lobby server and cannot leave it.
    /// </summary>
    public class TourneyPrototypeInfo
    {
        public string Title;
        public string FounderName;
        public List<List<string>> TeamPlayers = new List<List<string>>();
        public Dictionary<string, string> ModOptions = new Dictionary<string, string>();
        public Dictionary<string, string> MapOptions = new Dictionary<string, string>();
    }
}
