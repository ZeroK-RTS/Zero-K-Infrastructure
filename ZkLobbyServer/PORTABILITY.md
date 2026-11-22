# What stands between ZkLobbyServer and .NET 9

Surveyed 2026-09-27. **This is a floor, not a ceiling** — see the caveat at the end before quoting
any of it as "all that is left".

## Why survey it at all

The lobby server is the last large thing on .NET Framework 4.8 that the port needs. It already runs
as its own process (`ZkLobbyServer.Standalone`) and as its own container, but under **mono** — so
the stack still carries a runtime nobody ships new versions of. Moving it to .NET 9 removes mono
from the picture entirely.

The plan estimated this as part of a 10–16 week phase and assumed the hard part would be the code.
Measured, the picture is better than that.

## What the code itself uses

108 files, about 15,200 lines. Imports of the namespaces that usually block a port:

| namespace | files |
|---|---|
| `System.Web` | 0 |
| `System.ServiceModel` (WCF) | 0 |
| `System.Drawing` | 0 |
| `System.Configuration` | 0 |
| `System.Runtime.Remoting` | 0 |
| `System.Data.Entity` (EF6) | **6** |

**EF6 is the only Framework-coupled namespace in the lobby server**, and it is the one the port has
already answered: `ZkData.Core` reproduces the schema, reads and writes the data, and produces
identical WHR ratings, all enforced in CI.

## What a link probe says

Compiling `ZkLobbyServer` + `LobbyClient` + `PlasmaDownloader` + `PlasmaShared` as one `net9.0`
library against `ZkData.Core` reduces to **11 distinct errors**, none of them in the lobby server's
own logic:

| what | where | the answer |
|---|---|---|
| `Mono.Unix` | `PlasmaDownloader/EngineDownload.cs`, `PlasmaShared/SelfUpdater.cs` | `File.SetUnixFileMode`, which .NET 9 has and Framework did not |
| `System.Configuration.SettingChangingEventArgs` | `PlasmaShared/Settings.cs` | the `System.Configuration.ConfigurationManager` package |
| `Neo.IronLua` | `PlasmaShared/UnitSyncLib/ArchiveCache.cs` | a package reference the probe lacked |
| `Discord.Rpc` | `ZkLobbyServer/ChatRelay.cs` | a Discord.Net sub-package |
| `MonoTorrent.Common`, `Torrent` | `PlasmaDownloader/Torrents/*` | **the repository has its own `Shared/MonoTorrent` project**; the probe wrongly used the NuGet package, whose namespaces moved in 2.x |
| `System.Data.Linq` | `ZkLobbyServer/MatchMaker/MatchMaker.MapVetoer.cs` | a stale `using` from the deleted LINQ-to-SQL project — removed |

So: **no blocker was found in ZkLobbyServer's own logic.** What the probe hit was its dependency
chain and a handful of package references.

## The caveat that matters

**A type-resolution error suppresses method-body binding.** While the probe cannot resolve
`SpringPaths` or `ITransport`, the compiler never gets far enough to complain about a
Framework-only *member* — `AppDomain.CurrentDomain.SetupInformation`, a `WebClient` overload that
does not exist any more, a `BinaryFormatter`. Those errors appear only once the type errors are
gone.

This has already caught this project out once, in the Razor view port, where a count of "compiling
views" was measured against a project that had a declaration error in it and was therefore binding
nothing.

**So the honest reading is:** the survey rules out the *structural* blockers — no WCF, no
`System.Web`, no `System.Drawing`, no remoting — and identifies the dependency work. It does not
yet establish that the bodies compile. The next person should resolve the eleven above and re-run
before claiming a number.

## Step 1, measured: PlasmaShared already compiles

Surveyed the next day, because "PlasmaShared first" deserved a number rather than an ordering.

**All 83 of its source files compile on `net9.0`, with zero errors.** Not a subset, not with files
excluded — the whole tree, given the package references the existing project already declares.
38 of them were being linked into `ZkData.Core` individually and 10 more into `Tests.Portable`, so
roughly half was already known to work; the other half had simply never been tried.

Two packages restore but do not mean what the green build suggests, and they are the whole of the
remaining work in this tree:

| package | what the compiler says | what Linux would do |
|---|---|---|
| `Mono.Posix` | resolves, with **NU1701**: restored as `.NETFramework 4.8` against a `net9.0` project | one real user, and it is **not** in this tree — see below |
| `System.Drawing.Common` | resolves and compiles | Windows-only since .NET 6. Five files construct GDI objects: `ResizedImageCache.cs`, `Utils.cs`, `UnitSyncLib/UnitSync.cs`, `Imaging/GdiPixelBridge.cs`, `Imaging/SystemDrawingImageProcessor.cs` |

**Compiling is not running, and this is the case that shows why.** A build with 0 errors says
nothing about `System.Drawing.Common` throwing `PlatformNotSupportedException` on the first call off
Windows. The site already hit this and answered it — `Images.Processor` selects ImageSharp, and
`SystemDrawingImageProcessor` is kept deliberately as the GDI implementation to compare against, not
as something the port uses. The remaining GDI users are unitsync and the diagram code, neither of
which the website needs.

### Mono.Unix is one method, in the other tree

The first survey said two files used `Mono.Unix` and put both in the same sentence. Only one of them
uses it:

- `PlasmaShared/SelfUpdater.cs` had `using Mono.Unix.Native;` and **nothing from it** — a stale
  import, removed. (The file itself is very much alive: `SelfChecker` is what `ZeroKLobby` and
  `ChobbyLauncher` call to find out whether there is an update.)
- `PlasmaDownloader/EngineDownload.cs` is the real user, in one method:

      private static void FixPermissions(string targetDir)
      {
          if (Environment.OSVersion.Platform == PlatformID.Unix)
              Syscall.chmod(tpath, FilePermissions.S_IRWXU | ... );
      }

  making a downloaded engine executable. .NET 7 added `File.SetUnixFileMode`, which does exactly
  this.

**It cannot be swapped yet**, and the reason is the port's own discipline rather than difficulty:
`PlasmaDownloader` still targets 4.8, where `File.SetUnixFileMode` does not exist. Changing it now
means either breaking the Framework build or introducing an `#if` into a file that currently needs
none. It belongs with the move of that project, not before it.

### The Diagrams code is live, and the duplicate was somewhere else

`PlasmaShared/Diagrams` looked at first like dead weight dragging `System.Drawing` into the portable
subset: `GalaxyDesigner` has its own `Diagram.cs` and `Node.cs`, so the shared copies seemed
shadowed. **That reading was backwards.** Those two files are in no project at all — orphans on disk
since 2011 — and `GalaxyDesigner/MainWindow.xaml.cs` does `using Diagrams;` and builds against the
shared ones. The orphans are deleted; `PlasmaShared/Diagrams` stays.

It is also the least urgent of the three `System.Drawing` users: its only consumer is a WPF desktop
tool that will not be running on Linux whatever happens to the port.

### Step 1 is closed: none of the System.Drawing users is in the server's path

The question was what `UnitSync`, `ResizedImageCache` and `Diagrams` should do off Windows. Asked
the other way round — *who calls them* — it stops being a question:

| | called by | in the lobby server's path? |
|---|---|---|
| `Diagrams` | `GalaxyDesigner`, a WPF tool | no |
| `ResizedImageCache`, and the `GetResized*` helpers in `Utils.cs` | `ZeroKLobby` and `ChobbyLauncher` UI chrome, `AutoRegistrator`, and PlasmaShared's own GDI image processor | no |
| `UnitSync` | `new UnitSync(` appears in **nine** places: AutoRegistrator, ChobbyLauncher, MissionEditor, ZeroKLobby, `UnitsyncResourcePresenceChecker`, `MissionUpdater`. **`ZkLobbyServer` is not one of them** | no |

`ZkLobbyServer` does use `ZkData.UnitSyncLib` — but only `Map` and `Mod`, which are plain data
classes that happen to live in the same namespace as the native wrapper. Worth stating because the
namespace makes it look otherwise: `PlasmaShared/UnitSyncLib/*.cs` declares `namespace
ZkData.UnitSyncLib`, so a `using` in the server reads as if it pulls the whole thing in.

`ZkData.Core` already demonstrates the shape: it links twelve unitsync **data** files and neither
`UnitSync.cs` nor `MissionUpdater.cs`.

**So PlasmaShared does not need to be split or rewritten for the port.** It needs to be linked
selectively, which is what the port has done since `ZkData.Core` — 38 of its 84 files, chosen. The
Windows-only code keeps compiling for the Framework applications that actually want it.

The `Mono.Unix` question is one method in `PlasmaDownloader`, and it waits for that project.

## What is actually left

1. ~~`Shared/PlasmaShared`~~ — answered: it compiles, and its Windows-only parts are not on the
   server's path.
2. ~~`Shared/LobbyClient` and `Shared/MonoTorrent`~~ — both compile.
3. ~~`Shared/PlasmaDownloader`~~ — compiles; its `Mono.Unix` call is the one runtime item.
4. ~~`ZkLobbyServer` itself~~ — **`ZkLobbyServer.Core` exists and builds**, in one assembly, and
   CI builds it on every pull request.

## ZkLobbyServer.Core

`ZkLobbyServer.Core/ZkLobbyServer.Core.csproj` compiles the whole chain on `net9.0` — PlasmaShared,
LobbyClient, MonoTorrent, PlasmaDownloader and ZkLobbyServer, plus everything `ZkData.Core` links —
as a **single assembly**, linking production sources rather than copying them. 1.8 MB of output
containing `ZkLobbyServer`, `ServerBattle`, `MatchMaker`, `LoginChecker`, `TasClient`,
`ZkDataContext` and `BEncodedDictionary`, which is how the build is checked to have compiled
something rather than nothing.

It does **not** reference `ZkData.Core`, and the header says why at length: referencing it splits
`GlobalConst` and `Utils`, whose portable halves live there, and the resulting errors name members
that were present all along.

Two of `ZkData.Core`'s own files are excluded, because they are stand-ins its tooling wants and the
server does not: `GlobalConstMode.cs` hard-codes `Mode` and `BaseSiteUrl` to a dev value, and
`Ef6Compat/ImagesDefault.cs` is a minimal `Images`. The real PlasmaShared versions compile on .NET 9
and carry what the server reads.

### Two BCL collisions, which only compiling could find

Both are PlasmaShared back-filling something the Framework lacks and .NET 9 has:

- **`DistinctBy`** — `System.Linq` grew it in .NET 6. Two equally applicable extension methods are an
  *ambiguity error*, not a silent winner. Moved to `Utils.Polyfills.cs`, which the Framework projects
  compile and this one excludes; the semantics are identical, which is what makes the swap safe.
- **`Stream.ReadExactly`** — .NET 7 grew it, returning `void` where PlasmaShared's extension returns
  `bool`. An instance method beats an extension, so the call sites stopped compiling. Renamed to
  `TryReadExactly`, which is also what it does.

Neither was visible from reading. Both appeared the first time the chain was compiled as one
assembly.

### What this does not mean

~~It compiles. It has not been run~~ - **it has now been run.** See "It runs" below.


### Steps 2 and 3, measured: both compile

`Shared/LobbyClient` (23 files) and `Shared/MonoTorrent` (35 files) each compile on `net9.0` with
**zero errors**, alongside PlasmaShared. Checked by looking for `TasClient`, `Battle` and
`BEncodedDictionary` in the produced assemblies, because a glob that matches nothing also produces
zero errors.

### Step 4: one stale using, then one real API

`ZkLobbyServer/ChatRelay.cs` had `using Discord.Rpc;` and nothing from it — Discord.Net dropped that
package at 3.x, and the import was the only thing keeping the tree from resolving. Removed.

With it gone the compiler finally binds method bodies, and **one error is left that is not an
artifact of the probe**:

    ZkServerTraceListener.cs: 'DatabaseFacade' does not contain a definition for 'ExecuteSqlCommand'

That is EF6's API; EF Core spells it `ExecuteSqlRaw`. **Fixed**, and it needed no new machinery:
`ExecuteSqlCommandCompat` already existed in both halves of `DbCompat`, put there for
`ResourceLinkProvider`. The call site moved to it, which is the whole change.

### What is left after that: one namespace, four call sites

    ForumListManager.cs: The name 'EntityState' does not exist in the current context

`ForumListManager` imports `System.Data.Entity` for `EntityState.Added` / `Deleted` / `Unchanged`.
EF Core has the same enum with the same members, in `Microsoft.EntityFrameworkCore`.

**Fixed, and the answer was already half-built.** `entityEntry` is not EF's entry type — it is
`ZkDataContext.EntityEntry`, a wrapper the port already maintains in both twins so that
`IEntityAfterChange` implementations compile unchanged. Its `State` property was the only thing
leaking EF's namespace to a caller.

So the comparison moved into the wrapper, where each twin already imports its own EF:

    public bool IsAdded     => State == EntityState.Added;
    public bool IsDeleted   => State == EntityState.Deleted;
    public bool IsUnchanged => State == EntityState.Unchanged;

`ForumListManager` asks `entityEntry.IsAdded` and no longer imports `System.Data.Entity` at all.
This is `DbCompat`'s philosophy applied to a type instead of a method: a *type* cannot move behind
one name, so the **comparison** moves to where the type is already unambiguous.

### Where that leaves the chain

Nothing in `ZkLobbyServer` or its dependencies now fails to compile on `net9.0` for a reason that
belongs to the source. Every error the probe still reports — 270 of them — is one of five classes
that only duplicate types produce:

    CS0121  ambiguous between 'Utils.EscapePath(string)' and 'Utils.EscapePath(string)'
    CS0019  Operator '==' cannot be applied to 'ResourceType' and 'ResourceType'
    CS1503 / CS0266 / CS0029  conversions between a type and itself

— because the probe globs PlasmaShared *and* references `ZkData.Core`, which contains it too.

**That is not the same as "it compiles".** A single-assembly probe would settle it, and this survey
has not produced one that is clean, for the reasons in the trap above. What can be said is narrower
and still worth having: after four one-line changes — three stale `using`s and one compat call —
no source-level blocker is known.

Everything else the probe reports is the duplicate-type artifact described above: roughly 150
errors of the form *"Operator '==' cannot be applied to operands of type 'ResourceType' and
'ResourceType'"*, which is one `ResourceType` from the globbed sources and one from the referenced
`ZkData.Core`. They vanish when the chain is one assembly.

### The trap a ZkLobbyServer.Core will hit

**`GlobalConst` and `Utils` are partial classes split across `.cs` and `.Portable.cs`, and
`ZkData.Core` links only the portable halves.** A probe that *references* `ZkData.Core` and also
compiles the non-portable halves ends up with two different `GlobalConst` types in two assemblies —
the source one wins, and it is missing every member that lives in the portable half.

That produced about 150 errors of the form *"'GlobalConst' does not contain a definition for
`NightwatchName`"*, none of them real: `NightwatchName` is in `GlobalConst.Portable.cs` and
`HashLobbyPassword` is in `Utils.Enumerable.cs`, both perfectly portable and both already linked.

So a real `ZkLobbyServer.Core` must put the whole chain in **one** assembly — linking what
`ZkData.Core` links rather than referencing it — or it will spend a long time chasing errors that
are entirely its own doing. Both of this survey's wrong turns were of that kind.

## Suggested order

1. ~~`Shared/PlasmaShared` first~~ — **done**: it compiles, and the runtime questions turned out
   not to be on the server's path. See "Step 1 is closed" above.
2. `Shared/LobbyClient` and `Shared/MonoTorrent` next.
3. `Shared/PlasmaDownloader`, which needs the `Mono.Unix` replacement.
4. `ZkLobbyServer` last, against `ZkData.Core` rather than `ZkData`.

Each of those can be a linked-source probe project of its own, the way `ZkData.Core` and
`ZeroKWeb.Core` were — production files compiling in both stacks unmodified, so nothing forks.

## It runs

`ZkLobbyServer.Standalone.Core` is the .NET 9 host: two linked files (`Program.cs` and
`StandalonePlanetwarsEventCreator.cs`, the same sources the Framework `ZkLobbyServer.Standalone`
compiles) against `ZkLobbyServer.Core`. It starts, and `tools/lobby-core-start.sh` keeps it that
way - a CI step in `test_database.yml` runs the process against a real database and connects to
the port players use. Twenty seconds, so it costs nothing to keep honest.

### What running found that compiling could not

`TcpTransportServerListener.Bind` P/Invoked `SetHandleInformation` out of `kernel32.dll`. Off
Windows that throws `DllNotFoundException` - and `Bind` catches it, retries 120 times at one
second each, and gives up. **The server then comes up with its API listening and no player port
at all.** Nothing looked wrong: the process was alive, the API answered, and the message was one
line in a log two minutes of retries deep.

The call clears `HANDLE_FLAG_INHERIT` so a spawned Spring server does not inherit the listening
socket. On Unix .NET already opens sockets `FD_CLOEXEC`, so there is nothing to clear and nothing
needed in its place; the call is now guarded by `RuntimeInformation.IsOSPlatform(OSPlatform
.Windows)`. The Framework build never hit this because mono maps `kernel32` for it.

This is the whole argument for the check being a *start*, not a build. Restoring the bug fails
four of its six assertions; a process-liveness check or an API ping passes with the bug in place.

### The runtime questions the survey listed

**`Mono.Posix` is answered, and it was a defect.** `EngineDownload.FixPermissions` makes the
downloaded `spring` binary executable with `Syscall.chmod`. On .NET 9 that throws - *"Unable to
load shared library 'MonoPosixHelper'"*, because MonoPosixHelper is a mono runtime component -
and the call sits inside `catch (Exception) { Trace.TraceWarning(...) }`. So the download reports
success and leaves the engine **non-executable**: the game would simply never start, with a
warning as the only trace.

Three things kept it hidden. The method runs only under `PlatformID.Unix`, so no Windows build
executes it; the Framework build runs under mono, which provides MonoPosixHelper; and the catch
turns the failure into a warning. The same shape as the `kernel32` defect above - compiles
everywhere, throws on the platform it was written for, swallowed into a broken state.

Replaced with a compat twin, the idiom the rest of the port uses: `UnixPermissions.cs` keeps
`Syscall.chmod` for the Framework build and `UnixPermissionsCore.cs` uses `File.SetUnixFileMode`
(.NET 7+, no package), same name so `EngineDownload` calls it unchanged in both stacks.
`ZkLobbyServer.Core` no longer references Mono.Posix at all, and **that removal is the guard**: a
`using Mono.Unix` added to anything the .NET 9 server compiles now fails the build.

**`System.Drawing.Common` is not answered.** It is Windows-only on .NET 9, and
`ZkLobbyServer.Core` still references it - `UnitSync.cs`, `Map.cs`, `ResizedImageCache.cs` and the
GDI+ bridge all use it. Nothing on the path the probe walks touches it, which is why the server
starts; what touches it is map imagery, on the same path as the engine.

### Two things to know before running it by hand

- `tools/lobby-config.sh` defaults the API port to **8200**, which is `GlobalConst.LobbyServerPort`
  in Local mode - the API then takes the port the player listener is about to ask for. Pass a
  different one: `./tools/lobby-config.sh set 8300`.
- Left set, those MiscVars make every later host-harness run find a lobby server configured and
  none running. `lobby-core-start.sh` clears them on the way out; by hand, `clear` yourself.

## A client connects, registers, logs in, chats and opens a battle

~~No player has connected through it~~ - one does now, every time the check runs.
`tools/lobby-client-probe` drives **LobbyClient's `TasClient`**, the same type the real lobby uses,
through a connect, a `Register`, a `Login`, a `JoinChannel`, a `Say` and an `OpenBattle`. Not a
reimplementation of the protocol: if the protocol changes, the probe follows it.

`OpenBattle` is the boundary. It constructs a `ServerBattle`, stores it and announces it to every
connected user - and starts no Spring process, which is why it is reachable without the engine.
Everything up to here now runs on .NET 9; everything past it is untested.

**Every assertion in the probe names which thing answered it**, and that is not fussiness. The
server auto-joins an account's default channels at login, so an unfiltered `ChannelJoined` handler
is satisfied before the explicit join is even sent - which made a *correctly refused* join to the
moderator channel report `ok joined #zkadmin` for an account with `AdminLevel 0`. That read as an
authorization hole for a while. It was not one; the probe was reading someone else's answer.

### What that found: registration linked its rows by an accident of EF6

The first client to try it got no answer at all. Server-side:

```
error processing line Register {"Name":"LobbyProbe",...}
System.InvalidOperationException: The value of 'AccountUserID.AccountID' is unknown when
attempting to save changes. This is because the property is also part of a foreign key for
which the principal entity in the relationship is not known.
   at ZkLobbyServer.LoginChecker.DoRegister(...) LoginChecker.cs:line 246
```

`DoRegister` builds the `Account`, calls `LogIP` and `LogUserID`, and **then** adds the account.
Both helpers do `new AccountUserID { AccountID = acc.AccountID, ... }` - and at that moment
`acc.AccountID` is `0`, because nothing has saved it. The navigation property is never set, so on
paper nothing connects the dependent row to the account.

It works under EF6 because **EF6 leaves an unsaved store-generated int key at `0`**, so the Added
account's key is also `0` and relationship fixup matches the two. The rows are related by which
sentinel value EF6 happened to pick. EF Core uses negative temporary keys, `0` matches nothing,
and `SaveChanges` throws.

The fix is one line in each helper - add the row to `acc.AccountIPs` / `acc.AccountUserIDs`, which
is how both stacks are actually meant to be told. `Tests.Database/RegistrationFixupTests` measures
both halves under EF6: that the foreign-key-only form works there (with an assertion on
`new Account().AccountID == 0`, so the explanation fails if the mechanism ever changes), and that
the navigation form works there too - the fix cannot break the stack still serving players.

This is the third defect the port has found by *running* something it had only ever compiled, and
the second that no amount of reading would have produced.

## The Spring process starts, on .NET 9, on Linux

~~The Spring process the server spawns~~ - `tools/dedicated-server-check.sh` calls
`DedicatedServer.HostGame`, the same method `ZkLobbyServer` calls when a battle starts, and a real
`spring-dedicated` comes up:

```
   ok    the port resolved the dedicated server: .../engine/linux64/105.1.1-2457-g8095d30/spring-dedicated
   ok    ScriptGenerator produced a host script (1081 chars)
   ok    DedicatedServer launched the process
   ok    UDP 8452 is held - a player could connect to this game
```

Nothing here is reimplemented: `ScriptGenerator` writes the script, `SpringPaths` resolves the
binary and sets `SPRING_DATADIR`, and `Process.Start` launches it, exactly as in production.

**It costs an engine and a 333KB map, and no game download at all.** A dedicated server *relays*
the game rather than simulating it, but it still resolves and hashes both archives - so a game is
required, just not a real one. A six-line `modinfo.lua` satisfies it, which is what keeps this out
of multi-gigabyte territory. Not wired into CI: it fetches 42MB and runs an engine, and the
runners are ephemeral so nothing would be cached.

### Two things it got wrong before it got them right

Both are in the code as comments, because both are easy to repeat. `DedicatedServerStarted` fires
when the **process** starts, not when the game port is bound - checking immediately reported
"nothing is listening" while the engine's own log said `[GameServer] Server started on port 8452`.
And the check bound the wildcard address rather than the loopback one the script names; a wildcard
bind can succeed alongside a specific one, so it answers a different question than the one asked.

### A player joins, and the ported code is told

~~No player has joined a game.~~ `spring-headless` connects to the hosted game, plays it, and the
whole chain back to the port runs:

```
   ok    a Spring dedicated server is running, started by the ported code
   ok    a real player joined, and the ported code was told: PlayerJoined(probe)
```

Every link in that is the port's: the engine reports over UDP to `LobbyClient.Talker`, whose
listener thread parses the autohost packet, and `DedicatedServer` raises `PlayerJoined`. That is
the mechanism by which the lobby server learns anything at all about a running game.

It still needs no game content. A headless client joins the six-line `Probe Game` and goes ingame,
which was the surprise - the assumption had been that a *client* must simulate and therefore needs
real units.

**One trap, and it looks exactly like a broken autohost path.** If the client's `MyPasswd` does not
match the player's `ScriptPassword` in the host script, the engine answers *"server requested quit
or rejected connection"*, the client exits before joining, and nothing ever reaches `Talker` - so
the symptom is silence in the event stream rather than an error about a password.

### The end of a game, and a fifth defect

The host script uses `StartPosType=2` - `ScriptGenerator` hardcodes it for every non-mission game -
so Spring waits for each player to place a start position and ready up, and a headless client never
does. `DedicatedServer.ForceStart()` sends `/forcestart` through the Talker, which is exactly what
an autohost does when players do not ready up, and **the game then starts for real**:
`SERVER_STARTPLAYING` is the engine's, and the replay path and game id that reach the database are
ones the engine made.

`SERVER_GAMEOVER` is still **synthesised**, and the reason is not squeamishness. The game runs, but
nothing in it can ever end: Spring does not decide winners, the *game* does, in Lua - and "Probe
Game" is six lines of `modinfo.lua` with no gadgets and no units. Waited 75 seconds for a real one;
none comes, and none can. A real game archive would carry the victory condition, and is the
multi-gigabyte download this whole arrangement exists to avoid.

The packet is built in the layout `Talker` parses - the layout a real `PLAYER_JOINED` and a real
`SERVER_STARTPLAYING` both arrived in.

```
   ok    SERVER_STARTPLAYING parsed: BattleStarted, replay=probe.sdfz, engineBattleID=0123456789ABCDEF...
   ok    SERVER_GAMEOVER parsed: GameOver, duration=0s, winners=[probe]
   ok    the battle context is complete - this is what BattleResultHandler is handed
```

Sending `GAMEOVER` alone would have proved nothing, and that is worth stating: `DedicatedServer`
refuses to raise `GameOver` unless `Context.IngameStartTime` is set, which only
`SERVER_STARTPLAYING` does. A check built on `GAMEOVER` by itself would have passed by never being
reached. That prerequisite is now met by the engine rather than by a second synthesised packet.

**The defect.** `process.PriorityClass = ProcessPriorityClass.High` throws
`Win32Exception(13): Permission denied` on Linux, because an unprivileged process may only *lower*
its nice value. It sits inside `talker_SpringEvent`'s `catch`, so the throw took `BattleStarted`
with it: the engine started a game and **the lobby was never told**. `IngameStartTime` is assigned
before the throw, so `GameOver` would still have worked later - which is what makes it a partial
failure rather than an obvious one.

The same shape as `kernel32`, `MonoPosixHelper` and the EF6 zero key: compiles everywhere, throws
on Linux, swallowed by a `catch` into a state that looks like success. The priority is an
optimisation, so it is now guarded and logs a warning instead of taking an event with it.

## A finished battle becomes rows

~~Nothing has written a row.~~ `tools/battle-result-probe` hands a complete `SpringBattleContext`
to `BattleResultHandler.SaveSpringBattle` - the real writer - on .NET 9 against a real database,
and checks what landed by querying it back rather than by reading the object it was given:

```
   ok    the database assigned a SpringBattleID (151)
   ok    the founder resolved to an account by name
   ok    the map and the game resolved to Resources by name
   ok    both players were written (2)
   ok    every player row points at the battle
   ok    nothing was left behind: the fixture is untouched
```

It is a CI step, because storing needs a database and **no engine** - the mirror of
`dedicated-server-check.sh`, which needs an engine and no database. Splitting them is what makes
each runnable at all: tied together, neither would be.

Everything runs in a transaction that is always rolled back, and the last check proves it by
counting what the fixture holds afterwards.

**That "every player row points at the battle" line is the EF6 zero-key trap, executed.** The
sweep in `ZkData/EFCORE-MIGRATION.md` read `SaveSpringBattle` and concluded it was already correct
because it uses `sb.SpringBattlePlayers.Add(...)` rather than copying a key. Reading it was right;
this runs it.

`SaveSpringBattle` is `internal` rather than `private` so the probe calls exactly it. The
`InternalsVisibleTo` deliberately lives in `ZkLobbyServer/InternalsVisibleTo.cs` and **not** under
`Properties/`, because `ZkLobbyServer.Core` globs this project while excluding `Properties\**` - an
attribute placed there reaches the Framework assembly and silently misses the .NET 9 one, which
looked exactly like `InternalsVisibleTo` not working.

### The two ends join

~~The context the writer stores is built rather than captured.~~ The dedicated probe now writes its
finished `SpringBattleContext` out, and the storing probe reads it back:

```
   ok    the captured context deserialised
         captured from a real engine: founder=probe, map=Bluescreen fields v2, game=Probe Game 1.0
   ok    the captured battle was stored (155)
   ok    every player the engine reported was written (1 of 1)
   ok    the engine's own game id was stored
```

The founder, map and game are the ones that game was played with, and the fixture has never heard
of them - so the probe creates those rows inside the same rolled-back transaction. That is not a
workaround: a real deployment has them because the account registered and the registrar published
the map, and a battle cannot be stored without them. Creating them states the prerequisite out loud.

### The sixth defect, and the one that hid best

Feeding a real context in is what found it. `Account.AccountByName` is:

```csharp
db.Accounts.FirstOrDefault(x => x.Name == name)
    ?? db.Accounts.FirstOrDefault(x => x.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
```

The first query translates. **The fallback does not** - EF Core cannot translate `string.Equals`
with a `StringComparison`, and throws `InvalidOperationException`. Because it sits behind a `??`,
it only runs when the exact match finds nothing: so `AccountByName` threw for precisely the
**"no such account"** answer that every caller detects by testing the result for null.
`AccountVerify`, which signs players in, had the same line.

Every check written before this one used a name that already existed - registration, login, the
battle rows - so the working half was exercised again and again and the broken half never was. The
fix is `ToLower()`, which translates on both stacks. `tools/battle-result-probe` now asks about a
name that does not exist, first, before anything else.

### The rating pass, and why it is not in the probe

`SubmitSpringBattleResult` calls `RatingSystems.FillApplicableRatings` and
`RatingSystems.ProcessResult` after storing the battle. Neither takes a server, so both looked
reachable from `tools/battle-result-probe` - and they are, but the check they would support is
not worth having. **Tried, and recorded rather than shipped.**

`ProcessResult` hands the debriefing to `AttachResultReporting`, which invokes it only once the
rating system has processed that battle. The rating system reads the database **on its own
connection**, and the probe's battle exists only inside a transaction it always rolls back. So:

```
WHR: Read 150 battles from database.      <- the fixture's, not the probe's
```

The debriefing is registered against a `SpringBattleID` that will never be processed, and never
fires. Committing the battle instead would make the check work and the fixture dirty, which is a
worse trade for what it proves.

What it would have proved is also already covered, and more strongly: `db/compare-ratings.sh` runs
the whole WHR pipeline on **both** stacks and compares the ratings, and they are identical. The
lobby server runs a full pass at startup on .NET&nbsp;9 every time
`tools/lobby-core-start.sh` starts it. The rating *computation* is not in question; only the
hand-off after a battle is, and that hand-off cannot be observed from inside a rollback.

### What a live ZkLobbyServer costs, measured

Award calculation, replay upload and Planetwars sit past `server.GhostSay`, which
`SubmitSpringBattleResult` calls **unconditionally and before the rating block** - so a null-server
run stops there. The obvious next thought is to construct a real one. Its constructor says
otherwise:

```csharp
RatingSystems.Init();                                        // a full WHR pass
Downloader.GetResource(DownloadType.ENGINE, ...);            // downloads an engine
Downloader.PackageDownloader.DoMasterRefresh();              // fetches the package master list
SteamWebApi = new SteamWebApi(..., new Secrets().GetSteamWebApiKey());
```

Two network fetches and a Steam API key before it has done anything. Constructing one is not a
step a database probe can take; it *is* starting the server, which `tools/lobby-core-start.sh`
already does. Reaching those paths therefore needs the standalone server **and** a game that
finishes - which needs a real game archive. They are one problem, not two, and they are gated on
content rather than on the port.

### Still not done

**Nothing has finished a game by itself, and nothing has rated one end to end.** The engine's
`GAMEOVER` is synthesised because "Probe Game" has no victory condition; the rating hand-off cannot
be observed from inside a rollback, and is covered more strongly elsewhere; and the three paths
above need a running server plus real game content. Everything reachable without those has been
run.

`LogIP` is only half-covered: it skips private addresses, so a probe connecting from `127.0.0.1`
never reaches it. The EF6 test covers that half; nothing on .NET 9 does yet.

And it is still not in `Zero-K.sln`, which is correct - the Framework solution cannot build a
`net9.0` project.
