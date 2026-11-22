// Deliberately NOT in Properties/: ZkLobbyServer.Core globs this project but excludes
// Properties\**, so an assembly attribute placed there reaches the .NET Framework assembly and
// silently misses the .NET 9 one - which is what happened first, and looked like
// InternalsVisibleTo simply not working.
//
// tools/battle-result-probe calls BattleResultHandler.SaveSpringBattle, the code that turns a
// finished game into rows. Everything around it in SubmitSpringBattleResult needs a live
// ZkLobbyServer; the write itself needs only a context and a database.

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("BattleResultProbe")]
