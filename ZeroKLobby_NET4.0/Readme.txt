This project is based on Zero-K Infrastructure (of project ZeroKLobby, PlasmaDownloader, LobbyClient, Benchmarker,MonoTorrent,and PlasmaShared) of 16 February 2015

Suggested method of updating the project is to simply copy-paste the respective ZK project into the folder of same name:
1) Benchmarker -> ZeroKLobby_NET4.0\Benchmarker
2) LobbyClient -> ZeroKLobby_NET4.0\LobbyClient 
3) PlasmaShared -> ZeroKLobby_NET4.0\PlasmaShared
4) MonoTorrent -> ZeroKLobby_NET4.0\MonoTorrent
- But must avoid undo-ing any Await/Task/Async or any refactoring done by previous commit with message "zklnet4"

ZeroKLobby can also be updated using copy-paste :
5) ZeroKLobby -> ZeroKLobby_NET4.0
- But must avoid overwriting: package.config, app.config and ZeroKLobby_NET4.0.csproj. Any new file for ZeroKLobby_NET4.0 should be included manually from within IDE.

This project is targeted for Window XP compatibility (NET Framework 4.0 Client).




