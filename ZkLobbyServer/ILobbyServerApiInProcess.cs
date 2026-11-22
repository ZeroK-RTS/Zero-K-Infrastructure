using System.Collections.Generic;
using System.Threading.Tasks;
using LobbyClient;
using ZeroKWeb;
using ZkData;

namespace ZkLobbyServer
{
    /// <summary>
    /// The half of the website's lobby-server surface that cannot cross a process boundary.
    ///
    /// Every member here names a type that exists only inside the running server - Battle,
    /// ServerBattle, or the server itself - so none of them survives being called from another
    /// process, and none of them compiles in the .NET 9 port at all. That is what
    /// separates this interface from <see cref="ILobbyServerApi"/>, and the separation is now
    /// checked by a build rather than asserted in a comment.
    ///
    /// **Nothing in Zero-K.info calls any of it any more.** Every member was replaced by one on
    /// <see cref="ILobbyServerApi"/> that carries data instead of a live object:
    ///
    ///   ForceJoinBattle(string, Battle)  -> ForceJoinTourneyBattle(string, int)
    ///   AddBattle(ServerBattle)          -> CreateTourneyBattle(TourneyPrototypeInfo)
    ///   RemoveBattle(Battle)             -> RemoveTourneyBattle(int)
    ///   GetPlanetBattles(Planet)         -> GetPlanetBattles(string) / GetPlanetWarsBattles()
    ///   PlanetWarsPhase                  -> moved up unchanged; only its enum's FILE moved
    ///   InProcess                        -> the escape hatch, unused
    ///
    /// They stay declared because the lobby server implements and uses them itself. What changed
    /// is who calls them, and that is the thing Phase 1 was about: the website's coupling to the
    /// server's live object graph, not the graph's existence.
    ///
    /// Both greps should print nothing, and CI runs them:
    ///
    ///     grep -rn "LobbyApi.InProcess" Zero-K.info/
    ///     grep -rn "LobbyApi.GetPlanetBattles([^\"]" Zero-K.info/
    /// </summary>
    public interface ILobbyServerApiInProcess : ILobbyServerApi
    {
        Task ForceJoinBattle(string player, Battle bat);

        /// <summary>
        /// Returns live <see cref="Battle"/> objects. Superseded for the website by
        /// <see cref="ILobbyServerApi.GetPlanetBattles(string)"/>, which returns
        /// <see cref="PlanetBattleInfo"/>; kept because the server uses it.
        /// </summary>
        List<Battle> GetPlanetBattles(Planet planet);

        Task AddBattle(ServerBattle battle);

        Task RemoveBattle(Battle battle);

        /// <summary>
        /// The server itself, for the calls that still need its live object graph: the battle
        /// dictionary, connected users, the PlanetWars matchmaker, channel and list managers, and
        /// the lobby session tokens used for website single sign-on.
        ///
        /// Null when the lobby server is not running.
        /// </summary>
        ZkLobbyServer InProcess { get; }
    }
}
