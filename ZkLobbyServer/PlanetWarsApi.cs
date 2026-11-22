using System.Collections.Generic;

namespace ZeroKWeb
{
    /// <summary>
    /// Which half of the PlanetWars turn the matchmaker is in.
    ///
    /// This enum has always been two constants with no dependencies, but it lived in
    /// SpringieInterface/PlanetWarsMatchMakerState.cs, in the lobby server project, beside a
    /// class full of server-side types. That is the only reason `ILobbyServerApi.PlanetWarsPhase`
    /// could not be declared on the crossable interface - not the enum, its address.
    ///
    /// It is moved here, into a file the .NET 9 port links, so the phase can cross a process
    /// boundary. The namespace is deliberately unchanged: `Planet.cshtml` reads
    /// `ZeroKWeb.PwPhase.AttackCollect`, and it still does.
    /// </summary>
    public enum PwPhase
    {
        AttackCollect = 0,
        DefendCollect = 1
    }
}

namespace ZkLobbyServer
{
    /// <summary>
    /// A PlanetWars battle, as data rather than as a live <c>Battle</c>.
    ///
    /// This is the other half of what <see cref="ILobbyServerApiInProcess"/> had left, and unlike
    /// the tournament console its callers include two Razor views, so the shape had to satisfy
    /// `Planet.cshtml` and `Galaxy.cshtml` as well as five controller actions.
    ///
    /// Across all nine call sites the surface turned out to be two members: `IsInGame`, and the
    /// number of users. Nothing reads a name, a status, a rank or a map option. So this carries
    /// the count rather than the collection - shipping a dictionary of `UserBattleStatus` per
    /// battle to answer `.Count` would be payload nobody reads, and the galaxy map asks the
    /// question once per planet.
    ///
    /// `MapName` and `BattleID` are here because the calls need them, not the callers: the map
    /// name is what the server filters on, and the battle id is what
    /// <see cref="ILobbyServerApi.ConnectPlayerToBattle"/> takes.
    ///
    /// Why the live type could not travel: <c>LobbyClient.Battle</c> is not one of the protocol
    /// types the port links. It pulls in <c>ZkData.UnitSyncLib</c> and Json.NET, it is the
    /// client's own model of a battle, and the objects the server hands out are live
    /// <c>ServerBattle</c> instances upcast to it - so a caller in this process is reading a
    /// mutating object, and a caller in another one could not be given it at all.
    /// </summary>
    public class PlanetBattleInfo
    {
        public int BattleID;

        /// <summary>The map the battle is on, which is what a planet is matched by.</summary>
        public string MapName;

        public bool IsInGame;

        /// <summary>
        /// <c>Battle.Users.Count</c>. A count rather than the collection, because that is all
        /// nine call sites ever asked for.
        /// </summary>
        public int UserCount;
    }
}
