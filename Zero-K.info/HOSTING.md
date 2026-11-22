# Hosting notes for Zero-K.info

## Why this file exists

`Global.StartApplication` starts the multiplayer lobby server inside the IIS worker
process: `ZkServerRunner.Run()` brings up `ZkLobbyServer` (player connections, chat,
matchmaking, battle hosting), alongside the award calculator, the forum post index,
the Steam depot generator and the autoregistrator.

That means **anything that recycles the app pool disconnects every player**. IIS
recycles on an idle timeout, on a periodic schedule, on memory limits, on a config
change, and on every deploy.

Separating the lobby server into its own process is the real fix and is a large piece
of work - the website reaches into the server's live in-memory state from 116 call
sites. Until that lands, the settings below remove every cause of a restart **except a
deploy**, which is the bulk of the pain for a few minutes of configuration.

## Required server configuration

These live in `applicationHost.config`, not in this repository, so they have to be
applied on the host and re-checked after an IIS upgrade.

    # never recycle on a timer
    appcmd set apppool /apppool.name:"<pool>" /recycling.periodicRestart.time:00:00:00

    # never recycle on a request/memory threshold
    appcmd set apppool /apppool.name:"<pool>" /recycling.periodicRestart.requests:0
    appcmd set apppool /apppool.name:"<pool>" /recycling.periodicRestart.privateMemory:0
    appcmd set apppool /apppool.name:"<pool>" /recycling.periodicRestart.memory:0

    # never shut down when idle
    appcmd set apppool /apppool.name:"<pool>" /processModel.idleTimeout:00:00:00

    # keep the worker process running and start it with IIS
    appcmd set apppool /apppool.name:"<pool>" /startMode:AlwaysRunning

    # warm the app immediately after a start, rather than on first request
    appcmd set app /app.name:"<site>/" /preloadEnabled:true

`preloadEnabled` works together with the `<applicationInitialization>` block in
`Web.config`, which is in source control. The IIS feature "Application Initialization"
must be installed for that block to do anything.

### Check the current values

    appcmd list apppool "<pool>" /text:*

Look at `recycling.periodicRestart`, `processModel.idleTimeout` and `startMode`.

## What this does not fix

- **Deploys still disconnect players.** The GitHub Actions workflow publishes on push
  to `stable`, which restarts the application. Until the lobby server is a separate
  process, releasing the website is a multiplayer outage. Consider deploying at a
  quiet hour, and announce it in-lobby first.
- **An unhandled exception** that kills the worker process still takes multiplayer
  with it.
- **`Application_End` calls `Global.StopApplication()`**, which calls
  `ZkServerRunner.Stop()`. IIS gives a shutting-down worker a limited window
  (`shutdownTimeLimit`, 90s by default), so a slow disconnect of many clients can be
  cut short. Raising it slightly is reasonable:

      appcmd set apppool /apppool.name:"<pool>" /processModel.shutdownTimeLimit:00:02:00

## Compile-checking on Linux

    ./tools/build-website.sh

Builds this project with msbuild under mono in Docker, in about twenty seconds, with no
.NET Framework and no Visual Studio. It is what every modernization change in this
repository has been checked with, and it is worth running before opening a pull request -
the Windows build in CI runs on a self-hosted runner that a fork cannot claim.

Two things it cannot do, which is why the Windows build still matters:

- **Razor views are not compiled.** mono ships no `aspnet_compiler.exe` (MSB6004), so a
  mistake in a `.cshtml` file passes this check untouched.
- **It does not run anything.** Running the site needs Windows, IIS Express and a database.

It works on a copy of the tracked files, so the repository never collects root-owned
`obj/` directories from the container, and it applies one workaround to that copy:
`ZkData.MissionUpdater.UpdateMission` uses `ZipFile.Open`, which trips a
`System.IO.Compression` facade version conflict under mono. That reproduces on unmodified
`master`, so it is an artefact of the container rather than of any change.

## Service endpoints that deployed clients depend on

Three endpoints serve the desktop clients. They look interchangeable and are not, so
before touching any of them:

- **`/ContentService`** - `Controllers/ContentServiceController.cs`, JSON over POST.
  This is the current path. `PlasmaShared.ContentServiceClient` posts here, and
  `GlobalConst.GetContentService()` is what `ZeroKLobby` and `ZkLobbyServer` call.
- **`ContentService.svc`** - WCF, and marked `[Obsolete]` in its own source. Every
  operation forwards to `GlobalConst.GetContentService()`, which means an HTTP round
  trip out of the worker process and back into `/ContentService` on the same site.
  Nothing in this repository calls it; it exists for clients that were deployed
  before the JSON endpoint. Deciding whether it can go needs production access-log
  evidence, not a grep.
- **`MissionService.svc`** - WCF, and live. `MissionEditor` in this repository builds
  a `ChannelFactory<IMissionService>` against `GlobalConst.BaseSiteUrl +
  "/MissionService.svc"`. Removing it breaks the mission editor.

## Phase 2, measured

The modernization plan describes Phase 2 as "remove dead ends - 3 WebForms pages,
LINQ-to-SQL remnant, WCF". Two of those three are not what they sound like.

**The WebForms pages were not in the live site.** There are no `.aspx`, `.ashx` or `.asmx`
files under `Zero-K.info` at all. The ones the plan counted were in `PlanetWars.old/` and
`PlanetWars/` - two trees last touched in **2010**, carrying their own `PlanetWars2.sln`,
in neither `Zero-K.sln` nor referenced by any tracked file outside themselves. 470 files,
6 MB, now deleted; git keeps them if anyone wants them back. The only WebForms left in the
live site were two `using System.Web.UI` lines that use nothing, already marked as dead
usings by the .NET 9 port.

**Five more trees were dead the same way, and are gone too (2026-09-24).** `PlanetWars.old`
was found by reading the plan's own list; these were found by asking which projects in the
repository no solution contains, and then which of those anything actually uses. 256 files,
about 31,600 lines:

| tree | files | last real change | what referenced it |
|---|---|---|---|
| `ModelBase` (with `ModToXml`, `UnitImporter`) | 108 | 2015 | one `.gitignore` line |
| `LuaAdmin` (with `LuaSharp`, `LuaWrap`, `LuaManagerLib`) | 102 | 2012 | nothing |
| `ModStats` | 27 | 2012 | nothing |
| `NightWatch` | 14 | 2016 | a commented-out `using` in `Fixer`, and a 2012 publish manifest |
| `SpringAccountReader` | 3 | 2011 | nothing |

Two strays went with them: `MissionEditor2.csproj`, a second project file sitting beside the
`MissionEditor.csproj` the solution actually builds and referenced by nothing, and
`Zero-K.info/asp.net.Publish.xml`, a Visual Studio 2010 FTP publish history listing files as
they stood in 2012. That last one carried a production FTP URL and user name - no password,
`savePWD` was false - and nothing reads it.

**The test was "what uses it", not "how old is it".** Ages here are misleading on their own:
every one of these trees has a 2022 or 2023 commit from a repository-wide change, which is why
they look alive in a log. `NightWatch` is the one worth naming, because it had the most signs of
former life and none of them were uses - a `using` behind a `//`, and a filename in a manifest
nothing reads.

**The LINQ-to-SQL remnant is vocabulary, not a dependency.** `InsertOnSubmit`,
`DeleteOnSubmit` and friends appear 185 times across 44 files - and they are extension
methods over EF6, defined in `ZkData/DbExtensions.cs`, with a twin in `ZkData.Core` for
the port. Nothing links LINQ to SQL. Renaming 185 call sites would change no behaviour and
risk a typo in each one; it is cosmetic debt and is left alone deliberately.

**WCF is the real item**, and it splits in two:

- `MissionService.svc` - **there is now a JSON endpoint beside it**, `/MissionService`,
  built the same way `/ContentService` is: request and response classes dispatched by name
  through `CommandJsonSerializer`. It does not reimplement anything - `MissionService.svc.cs`
  stays the one implementation and the JSON layer is an envelope over it, because two copies
  of an operation that deletes other people's missions is not a thing to have while both
  endpoints are live.

  The contract is unchanged: `Mission` is `[DataContract]` and Json.NET honours that, so the
  same fields cross as under WCF. One deliberate difference - WCF turned an
  `ApplicationException` into a fault the channel rethrew, and there is no such machinery
  here, so the message comes back in an `Error` field.

  **`MissionEditor` now uses `/MissionService`.** `MissionServiceJsonClient` implements the
  same `IMissionService` the WCF channel did, so nothing calling it changed; it converts the
  `Error` field back into the exception those callers are written around, and keeps the
  one-hour timeout the WCF binding had, because a mission upload is a whole game archive and
  HttpClient defaults to 100 seconds.

  The `.svc` is still hosted, for editors already installed - ClickOnce updates them when a
  user next launches the editor, not when the site deploys, so removing it earlier breaks
  publishing for anyone who has not opened it since.

  **It now says who is still calling it.** Every WCF operation writes a line to `LogEntries`
  (14 days, visible at `Admin/TraceLogs`), and `SendMission` includes the caller's editor
  version:

      MissionService.svc (deprecated WCF endpoint): SendMission by someone, mission editor 1.2.3.4

  So the question "has everyone updated?" is answerable from the site's own data:

  ```sql
  -- anyone still on the WCF endpoint in the last fortnight?
  SELECT Message FROM LogEntries
   WHERE Message LIKE 'MissionService.svc (deprecated%' ORDER BY Time DESC;

  -- which editor versions have published at all, either way
  SELECT MissionEditorVersion, COUNT(*), MAX(ModifiedTime)
    FROM Missions GROUP BY MissionEditorVersion ORDER BY 3 DESC;
  ```

  **Do not read an empty result as "nobody calls it".** That query returns nothing just as
  readily when the deployed build has no instrumentation in it, or the trace listener is not
  attached, or the site has not been restarted since the change - and those lead to the
  opposite decision. Deleting the endpoint on that reading breaks publishing for every mission
  editor that has not updated, and the log looks identical either way.

  So a build that carries the reporting says so when it starts, and there is a command that
  refuses to conclude without it:

      ZK_CONNECTION_STRING=... dotnet run --project ZkData.Core -- legacy-callers [--days=N]

  Three answers, and only the middle one is permission to delete anything:

      MissionService.svc: CANNOT TELL. No "legacy WCF watch active" in the last 14 days, so
      nothing says an instrumented build was running. Silence here is not evidence - deploy
      and wait.

      ContentService.svc: no calls since 2026-09-17 07:48 UTC, when a watched build last
      started. That is the evidence the retirement needs.

      MissionService.svc: STILL CALLED - 2 report line(s), most recently 2026-09-26 07:48 UTC.
      Mission editor versions seen: 1.2.3, 1.4.0.

  It reports the two endpoints separately, because they are retired separately. When it says
  "no calls since" for `MissionService.svc`, that file, `MissionService.svc.cs`'s
  `MissionService` class and `IMissionService`'s WCF attributes can go in one commit - the
  operations live in `MissionServiceLogic`, which the JSON endpoint uses and which stays.

  This is the difference between this endpoint and `ContentService.svc`: that one serves
  clients the repository cannot see, so it needs production access logs; this one reports
  itself.
- `ContentService.svc` - obsolete and uncalled from this repository: every caller here goes
  through `IContentServiceClient` to `/ContentService`, and inside the website `Global.cs`
  overrides that to run the implementation in-process. It is hosted purely for clients
  deployed before the JSON endpoint existed.

  This used to say it **could not be retired from the evidence available here**, because
  the decision needed production access logs. It does not any more: **all 14 operations
  now report their callers**, by user agent and address, plus the login and API version
  where the operation carries one. The same `LogEntries` that answers the question for
  `MissionService.svc` answers it for this one.

  **It counts rather than logs, and the difference is the point.**
  `ZkServerTraceListener` turns every trace into its own `ZkDataContext` and one insert.
  Publishing a mission is rare, so `MissionService.svc` can afford a line per call;
  `DownloadFile` and `GetResourceData` are whatever a fleet of outdated lobbies asks for,
  and a line per call would put a database write on a path that has none today. So the
  first call from each operation and user agent reports immediately - no line means no
  caller, which is what the retirement turns on - and after that one line an hour carries
  the count. `Zero-K.info/AppCode/LegacyCallReporter.cs` holds that, clock-injected, with
  ten tests in `Tests.Portable` and both failure directions covered: reporting per call,
  and losing calls so a busy endpoint looks idle.

  What it still needs is **time**, not evidence. Nothing can be concluded until the
  instrumented build has been deployed and watched.

**Porting note.** Server-side WCF hosting has no in-box successor on .NET 9. The two
`.svc` endpoints are therefore a hard constraint on the port: either host them with
CoreWCF, or re-expose their operations as JSON endpoints and ship updated clients
first. This is the reason the modernization plan's "remove WCF" step could not be
carried out as written.

## Linux and containers: what is done and what is still blocked

The modernization plan puts Linux, containers and CI in Phase 4, after the .NET 9 port.
Most of it genuinely is blocked on that port - .NET Framework 4.8 does not run on Linux,
so the website cannot be containerized before it is ported. Two pieces were not blocked
and are done:

**The case-collision is fixed.** `AutoRegistrator` (the project) and `Autoregistrator`
(its resources) were two directories differing only in case. Windows sees one directory
and Linux sees two, and the website referenced `..\Autoregistrator\Autoregistrator.csproj`,
which on Linux matched neither. Everything is now `AutoRegistrator`, matching the solution
file, `Fixer.csproj` and the project's own `AssemblyName`. A Linux build no longer needs
symlinks to get past it.

**CI runs the .NET 9 tests.** `.github/workflows/test_portable.yml` runs
`Tests.Portable` on a stock Linux runner: no Windows, no MSBuild, no database, well under
a minute. `test_pullrequest.yml` still does the full Framework build on the self-hosted
Windows runner, and still has to.

**Both processes run together, against one database, and talk (2026-09-26).**

    ./tools/stack.sh            bring both up, check they are talking, tear down
    ./tools/stack.sh --serve    bring both up and leave them running

The website is the .NET 9 port on Kestrel; the lobby server is `ZkLobbyServer.Standalone` under
mono; the switch is the `LobbyApiUrl` MiscVar. **`Global.LobbyApi` now builds a
`RemoteLobbyServerApi` from it** rather than answering null - the note that used to sit there
said a switch could not safely be flipped until the shared statics were answered, and they were.
Unset, it is still null, which is what the fixture leaves it as, so CI is unchanged.

The port links the **client** half of the transport only, never `LobbyApiHost`: linking that
would put an HttpListener in the website. `DefaultPrefix` moved to `LobbyApiConfiguration` for
the same reason - a client asking for the default should not drag a listener behind it.

The check that matters is the negative one. `HomeController.Index` asks the lobby server for
`ConnectedUserCount`, so the front page answers 200 with the server up and **500 with it
stopped**; a 200 on its own would prove the page rendered, not that it ever asked. The first
version of this script checked `/Battles`, which never calls the lobby server and answers 200
with nothing running at all.

**What has still not happened is a player connecting and a game being watched.**

**The lobby server runs, as its own process, on Linux (2026-09-25).**

    ./tools/lobby-container.sh            build it, start it, check it comes up
    ./tools/lobby-container.sh --serve    leave it running

This is the thing Phase 1 was for, and the line above it in this document - "what remains is
running it" - was true until now. `ZkLobbyServer.Standalone` runs under mono in a container,
reads its configuration from the MiscVars in the shared database, initialises the rating
systems from it, starts the server and answers on its API port. Not ported to .NET 9, which is
not what Phase 4 asks: `ZkLobbyServer` is .NET Framework 4.8 and EF6, and running it off
Windows is a different question from porting it.

It builds through `tools/build-website.sh` rather than a Dockerfile of its own, so there is one
implementation of "compile this .NET Framework project under mono".

**Running it found something nothing else would have.** `LoginChecker` opens the MaxMind GeoIP
database from whatever directory it is handed as a site path, while the server is being
constructed - which is *after* a full WHR pass. The website never hit it, because it passes
`MapPath("~")` and the file sits in that directory. The standalone defaults its site path to
the working directory, so the first real run died two minutes in with a
`FileNotFoundException` from inside MaxMind, naming a path nobody had chosen. It now refuses at
once, naming the file and where the repository keeps it, and the container carries it.

**The ported site runs in a Linux container (2026-09-25).**

    ./tools/site-container.sh            build it, run it, check it answers
    ./tools/site-container.sh --serve    leave it running on http://127.0.0.1:5200

`Dockerfile` builds `ZeroKWeb.Host` on `dotnet/sdk:9.0` and runs it on `dotnet/aspnet:9.0` -
no IIS, no .NET Framework, no mono, no System.Drawing anywhere. Every controller and all 123
views are compiled into the image by the Razor SDK. It needs the database
`db/docker-compose.yml` starts, which it reaches over the host's loopback the same way
`tools/dotnet.sh` does.

**What it does not prove** is that this can replace the live site. The entry point is the
harness host; the lobby server is not in it, the two WCF endpoints have no home here, and the
unitsync and MonoTorrent tripwires throw rather than work.

**Only `img/`, `Scripts/` and `Styles/` are in the image**, because the Host's web root is the
site SOURCE tree - serving it wholesale would publish `Web.config` and every `.cs` file. The
check that the image carries no source replaced an HTTP probe for `/Web.config` that turned out
to measure nothing: ASP.NET Core's static file middleware refuses unknown content types, so that
URL answers 404 whether the whole tree is served or none of it is. Serving the entire web root
on purpose - a positive control - passed, which is how the check was found to be worthless.

`ZK_HOST_URLS` sets the bind address, and is read **only when serving**: honouring it during the
harness checks would move the server without moving what they ask for.


What is still blocked, in the order it has to be unblocked:

1. **EF6.** `ZkDataContext` and 117 migrations. EF Core is a rewrite of the data layer,
   not a retarget, and everything else waits behind it. Measured in
   `ZkData/EFCORE-MIGRATION.md`, along with the two blockers already cleared.
2. ~~**`System.Drawing.Common`**~~ - **done for the website** (2026-09-25); what is left is
   `ZeroKLobby`, which is WinForms and not part of this port. The original note follows.
   **`System.Drawing.Common`** - the imaging types only. `Bitmap`, `Graphics`, `Image`
   and friends throw `PlatformNotSupportedException` off Windows since .NET 7. The
   geometry types (`Point`, `Size`, `Rectangle`, `Color`) are in
   `System.Drawing.Primitives`, are part of the .NET 9 shared framework and are **not** a
   blocker - an earlier version of this note said otherwise. 13 files are affected, with
   `Shared/PlasmaShared/Utils.cs` the choke point. Measured in
   `Shared/PlasmaShared/IMAGING-MIGRATION.md`.
3. **Server-side WCF.** The two `.svc` endpoints above. CoreWCF, or replace them and ship
   updated clients first.
4. ~~**A mono build still needs one workaround**, so it is not CI-able as a Linux check.~~
   **Stale on both counts.** The workaround exists - `tools/build-website.sh` applies it to
   its copy of the tree, because `ZkData.MissionUpdater.UpdateMission` uses `ZipFile.Open`
   and trips a `System.IO.Compression` facade version conflict under mono 6.12 - and that
   script has been running as a Linux compile check of the website in `test_database.yml`
   for some time. The note simply outlived the work.

## Engine upload: the attributes now apply (2026-09-26)

`EnginesController.UploadEngine` carried `[Auth(Role = SuperAdmin)]`, `[HttpPost]` and
`[ValidateAntiForgeryToken]` on a **private** method that `Index` called. MVC only runs filters
for actions it invokes, so all three were inert: the effective policy was the class-level
`[Auth(Moderator)]`, any verb, no token. The form made it worse by having no `method="post"` -
it submitted as GET, with `@Html.AntiForgeryToken()` sitting unread in the query string.

It is a real action now, so the filters apply. **The role is deliberately left at Moderator**
rather than raised to the SuperAdmin the old attribute named: whoever uploads engines today keeps
doing so, and what changes is that a link can no longer make a moderator's browser do it.

Checked against the running port, signed in as a moderator:

    GET  /Engines/UploadEngine -> 405
    POST without a token       -> 400
    GET  /Engines              -> 200

`uploadName` is also rejected when it contains a path separator or `..`, because it becomes both
a directory and a file name. Rejected rather than stripped - an engine name is a version like
`104.0.1-287-gf7b0fcc`, so a separator in it is a mistake or an attempt, and quietly rewriting it
would put the engine somewhere nobody asked for.

**Not done:** the upload still downloads a caller-supplied URL and runs `7za.exe` over it. That is
unchanged behaviour and a moderator-level capability by design; narrowing it further is a separate
decision.

## Secrets

`Shared/PlasmaShared/SECRETS.md` covers how secrets are handled here - `MiscVar` through
`ZkData/Secrets.cs` - and the one credential that is not, a GitHub token compiled into
`GlobalConst`. The short version: it cannot be fixed from this repository, and **deleting the line
is not a fix** - it leaves the token valid and published while removing the only thing prompting
anyone to rotate it.

## The two Web.config settings that turn off platform input filtering (2026-09-27)

    <httpRuntime targetFramework="4.5" requestPathInvalidCharacters="" requestValidationMode="2.0" ... />

Both have been on the security list since the first assessment and left alone twice, on the
assumption that restoring the defaults would reject legitimate map, replay and wiki names in route
segments. **That assumption had never been measured.** This is what measuring it found. Neither
setting is changed here - the measurement narrows what would have to be true, and one of the two
unknowns is now closed.

### Which routes actually carry free text

Six, from `Global.asax.cs`. What matters is not that the segment exists but whether the site ever
*generates* a URL with a risky value in it:

| route | what fills it | generated anywhere in this repository? |
|---|---|---|
| `Wiki/{node}` | `ForumThread.WikiKey` | **yes** - Forum/Thread, Forum/NewPost, the `[WikiKey]` forum tag, Autocomplete |
| `Missions/File/{name}` | a MissionID **or** a mission name | yes, and always with the numeric `MissionID` |
| `Missions/Img/{name}` | same | no link anywhere |
| `Replays/{name}` | `SpringBattle.ReplayFileName`, which is `Path.GetFileName` of the engine's demo file | no link anywhere |
| `Static/{name}` | a static page name | no link anywhere |
| `{controller}/{action}/{id}` | ids, numeric at every call site checked | n/a |

So the only segment the site builds from free text is the wiki key.

### `requestPathInvalidCharacters`: the wiki route is now provably safe

Its default is ``<>*%&:\?``, and it is checked against the **decoded** path, so percent-encoding a
name does not get it past the filter.

Wiki keys are validated with `Account.IsValidLobbyName`, whose charset is letters, digits,
underscore and brackets - none of the default invalid characters, and no slash.
`Tests.Portable/WikiKeyRouteSafetyTests.cs` asserts exactly that link, so widening the charset
fails a test rather than producing a 400 in production.

**One gap had to be closed to say that.** `ForumController.SubmitPost` validated the key when
creating a thread and **not** when editing one, so a key creation refuses could be set by editing
instead. Both paths run the check now.

### `requestValidationMode`: the reason for `2.0` is the seven actions that accept markup

`SubmitPost`, `Preview`, both `PostNews`, `ReportToAdminSubmit` and the two service endpoints carry
`[ValidateInput(false)]` because forum posts and news legitimately contain `<`. Under
`requestValidationMode="4.0"` that attribute does not help: validation runs at `BeginRequest`,
before any action filter. `2.0` makes it work, which is almost certainly why it is set.

All seven are POST-only. The modern equivalent of what this setting is buying is **deferred**
validation - drop the attribute entirely and `httpRuntime` inherits `targetFramework="4.5"`, under
which `[ValidateInput(false)]` and `[AllowHtml]` apply as written while everything else stays
validated.

**That last paragraph is reasoning, not a measurement, and nothing here can raise it to one.**
Request validation is .NET Framework behaviour in IIS; mono does not exercise it and the .NET 9
port does not have it at all. It needs trying on a Windows staging site.

### What production data would settle the rest

- **`Replays/{name}`** - whether any `SpringBattles.ReplayFileName` contains one of ``<>*%&:\?``.
  The committed fixture stores that column as NULL for every row, so it cannot be asked here.
- **Wiki keys that predate the fix above** - the edit path never validated, so a key already in the
  database may contain anything. `SELECT WikiKey FROM ForumThreads WHERE WikiKey IS NOT NULL` and
  check it against the same charset.

Both are one query each against the live database, and both have to come back empty before
`requestPathInvalidCharacters` is worth touching.

## What IIS does for this site that the .NET 9 host does not (2026-10-02)

`Web.config` is not only build configuration: several of its settings are IIS *behaviour*, and a
cutover that moves to Kestrel loses them unless something else provides them. Every line of both
`system.web` and `system.webServer` was walked for this; most are reproduced in
`ZeroKWeb.Host/Program.cs` now, and the ones that are not are listed here because they need a
decision from whoever deploys rather than a commit.

### Reproduced in the host

| Web.config | what the host does |
|---|---|
| `<forms timeout="2880">` | `ExpireTimeSpan = TimeSpan.FromMinutes(2880)`, asserted by the sign-in check |
| `<globalization culture="en-US" uiCulture="en">` | culture pinned at startup, asserted through its *effect* on a formatted number and date |
| `<httpRuntime maxRequestLength>` and `<requestLimits maxAllowedContentLength>` | `MaxRequestBodySize` and `MultipartBodyLengthLimit` at 500,000,000 - without them Kestrel refuses anything over 30MB |
| `<staticContent>` mimeMap for `.sd7`, `.sdz` | a `FileExtensionContentTypeProvider` with those two extensions; without it an unknown extension is **404**, not "served untyped" |
| `<customErrors mode="RemoteOnly">` | the host logs to `Trace` and rethrows, so Kestrel answers an empty 500. A check asserts the response carries no exception text - there is no `RemoteOnly` here, so **nothing but that check stops `UseDeveloperExceptionPage` leaking stack traces in production** |

### Not reproduced, and they are deployment's problem

Measured against the running host, not assumed:

- **`www` is not redirected to the apex.** The `Remove WWW` rewrite rule issues a permanent
  redirect; the host answers `200` to both `Host: zero-k.info` and `Host: www.zero-k.info`. Cookies
  are issued per host, so a visitor who lands on `www` is signed in separately from one who lands
  on the apex. **This belongs in the reverse proxy or ingress in front of Kestrel.**
- **The wiki's pretty URLs are not rewritten.** `RewriteUserFriendlyURL1` turns
  `/mediawiki/Foo` into `/mediawiki/index.php?title=Foo`; the host answers `404`. This matters less
  than it looks, because the **site itself only ever emits the `index.php?title=` form** - checked,
  every `mediawiki` URL in the C# and the views is the long one. What breaks is inbound links and
  bookmarks. It also only arises at all if mediawiki is still co-hosted: the PHP wiki is a separate
  application and this host does not serve it under any URL.
- **`<applicationInitialization>` has no equivalent and may not need one.** It exists because the
  lobby server runs inside the IIS worker process, so an app-pool start is what brings multiplayer
  back - which is the entire problem Phase 1 removes. Once the lobby server is its own process,
  warming the website is an ordinary concern rather than a multiplayer outage.

`<modules runAllManagedModulesForAllRequests>`, `<handlers><remove name="UrlRoutingHandler"/>`,
`<compilation>` and `<pages>` are System.Web plumbing with no meaning off that stack.
`<pages validateRequest="false">` is reproduced by absence: ASP.NET Core has no request validation
to turn off. `requestPathInvalidCharacters` and `requestValidationMode` have their own section
above.

## The harness endpoints, and why they are off by default (2026-10-03)

`HarnessController` lives in `ZeroKWeb.Host`, which is both the thing that runs the checks and
the thing that serves the site. So whatever that project is deployed as would serve the harness's
own endpoints, under guessable paths and with no authentication in front of them:

| | |
|---|---|
| `/Harness/Whoami` | names the signed-in account, its AccountID and its AdminLevel |
| `/Harness/Throw` | makes the site answer 500 on demand |
| `/Harness/Upload` | accepts a file |
| `/Harness/Ipn` | reads a raw request body |
| `/Harness/Token` | hands out an anti-forgery token |
| `/Harness/Slow` | sleeps for as long as it is asked to, up to five seconds |
| `/Harness/Login`, `/Harness/Logout` | a second sign-in form, without the real one's CSRF handling |

None of them is a way in - `Login` does the same password check `Home/Logon` does - but several
are free reconnaissance and one is a cheap way to tie up connections.

They are therefore **off whenever this host is serving**, and answer `404` rather than `403`,
because a `403` tells you the endpoint is there. Two things turn them on:

- **this process running its own checks** - the harness is the caller, in the same process;
- **`ZK_HARNESS_ENDPOINTS=1`**, which `tools/stack.sh` sets on the site container because the
  lobby probe asks `/Harness/Whoami` to prove single sign-on left the player signed in.

`tools/stack.sh` also runs the control: the same image, the same command, one environment
variable fewer, on a second port. `/Harness/Whoami` answers 404 there and `/Home/NotLoggedIn`
still answers 200, so it is the endpoints that are gone and not the container that is broken.

**If this project is deployed, deploy it without that variable.** The default is already right;
this is written down so that setting it "to make the checks pass" in production is a decision
somebody has to make on purpose.

## /Contributions/Ipn, the one endpoint that must stay anonymous (2026-10-04)

PayPal posts payment notifications to `/Contributions/Ipn`. It is the caller, so it carries no
cookie, no session and no anti-forgery token, and it cannot be made to - which means the endpoint
is open to anyone who can reach the site, by necessity rather than by oversight. Whatever a
deployment puts in front of the site must leave it reachable from the public internet, or
donations stop being recorded.

What stands in for authentication is the **postback**: the received bytes are sent back to
`https://www.paypal.com/cgi-bin/webscr` with `cmd=_notify-validate`, and PayPal answers
`VERIFIED` or not. **That now happens before anything is written.** It used to happen after, and
since the sender chooses every field, one unauthenticated request was enough to put a
contribution of any size under any name on the public `/Contributions` page, set `HasKudos` on
whichever account the item code named, mail a working redeem code from the team address to an
address of the sender's choosing, and announce it in the public `zk` lobby channel - all before
PayPal said it had sent nothing. Three outcomes now, in `PayPalInterface.ImportIpnPayment`:

| PayPal says | recorded | granted |
|---|---|---|
| it did not send this | no - the parsed fields go into the alert so a real payment is still recoverable by hand | no |
| nothing (unreachable) | yes, with `Comment = "VERIFICATION UNAVAILABLE"` and an alert | no |
| `VERIFIED` | yes | yes |

A deployment therefore needs **outbound HTTPS to paypal.com**. Without it every notification lands
in the middle row: the money is still recorded and nobody is paid out in kudos until someone
looks. That is the deliberate choice - a network fault must not lose a real donation - but it is
silent unless the alerts are going somewhere, which on a host with no lobby server means the trace
log (see the error-log section below).

`ZeroKWeb.Host`'s own `CheckIpnVerification` holds this in place, including the positive control:
a notification PayPal confirms still has to be recorded, or "nothing was written" would pass for
a seeding fault as readily as for the fix.

## The site's own error log, and who writes it now (2026-10-03)

`Admin/TraceLogs` reads the `LogEntries` table. Nothing writes that table unless a
`ZkServerTraceListener` is on `Trace.Listeners`, and on .NET Framework exactly one thing puts it
there: `Global.StartApplication`, which the .NET 9 host does not call.

So the port traced into the void. `Program.cs` already called `Trace.TraceError` for an unhandled
request exception - deliberately, and with a comment saying it was the half that belongs to
serving a request - but the listener that turns a trace into a row was never installed. The site's
own error log was permanently empty, and a check asserting `Admin/TraceLogs` renders passed
against an empty page.

The host installs it now, and a check follows an exception all the way through: `/Harness/Throw`
-> `Trace.TraceError` -> `LogEntries` -> the `Admin/TraceLogs` page.

### The lobby server half, which is a decision rather than a bug

Before Phase 1 the lobby server ran inside the website's worker process, so the website's listener
caught **its** traces too. `ZkLobbyServer.Standalone` installs a `ConsoleTraceListener` instead, so
in the split architecture its traces go to the container's stdout and not to `LogEntries`.

That is a real change in where a moderator looks for lobby-server errors, and it is left alone
here on purpose. Every trace becomes its own `ZkDataContext` and one `LogEntries` insert - which is
why `LegacyCallReporter` reports counts rather than calls - and a lobby server is far chattier than
a website. Restoring the old behaviour is one line in `ZkLobbyServer.Standalone/Program.cs`;
whether a busy server should write a row per trace is a question for whoever runs it.

`docker logs` is where those traces are today.

## Related work in progress

`ILobbyServerApi` (see `ZkLobbyServer/ILobbyServerApi.cs`) is the seam introduced so the
website stops touching `ZkLobbyServer` directly. It is implemented in-process today and
changes no behaviour; its purpose was to make the coupling explicit and countable before
a transport is chosen.

**The website no longer reaches past it.** `Global.LobbyApi` is declared as
`ILobbyServerApi` rather than `ILobbyServerApiInProcess`, so the live battle list, the
live `Battle` objects and the server itself are not merely unused - they do not compile.
That is the check; there is no grep to run, and the earlier one here was wrong anyway.
It matched `LobbyApi.InProcess` and so missed `Global.LobbyApi?.InProcess?.SessionTokens`
in `Global.asax.cs`, which the narrowed type found immediately.

What this does **not** mean is that the server can move out today. Still in the way:

- ~~Deployment.~~ **Configurable.** Setting the `LobbyApiUrl` MiscVar is the entire switch:
  unset - every deployment that has not been changed on purpose - and the website starts the
  lobby server in its own process exactly as before. Set, and it starts none and talks to
  `LobbyApiUrl` with `LobbyApiSecret`. A URL without a secret **refuses to start** rather
  than falling back, because falling back means two lobby servers against one database.
  `ZkLobbyServer.Standalone` is the other end. It is also the only Exe/net48/SDK-style
  project in `Zero-K.sln`, which made it the first here to meet all three conditions of
  the SDK's RuntimeIdentifier inference and so **broke the Windows solution build** -
  restore and compile were choosing different RIDs. Its csproj now says `PlatformTarget`
  out loud and carries the explanation. Nothing in this fork could have caught it: the
  inference only fires on a Windows host, and `tools/build-website.sh` is mono on Linux
  building one project rather than the solution.
- ~~The reverse dependency.~~ **Moved.** The event feed's formatting is now
  `ZkData/ZkHtmlFormat.cs` and `ZkData/PlanetwarsEventFormatter.cs`, one implementation that
  both halves call. It used to be two near-verbatim copies - the website's and the port's -
  with the event creator as a third caller that only the website could satisfy. A standalone
  server writes PlanetWars events now.
- ~~No transport.~~ **Written.** `ZkLobbyServer/Api/` holds an HTTP+JSON host
  (`LobbyApiHost`) and client (`RemoteLobbyServerApi`); `Tests.Database` drives all 40
  members over real loopback HTTP and checks the arguments and results arrive intact.
  HTTP because both halves are .NET Framework 4.8 today, which rules out grpc-dotnet, and
  because HttpListener and HttpClient are unchanged on .NET 9 - the transport does not have
  to be rewritten by the port going on around it.

  **It is not wired up.** Nothing constructs a `RemoteLobbyServerApi`; `Global.LobbyApi` is
  still the in-process one. Choosing between them is deployment configuration, and it is
  not worth adding before the item below is answered.
- **The shared statics: mostly answered.** `RatingSystems.Init()` is split. A process that
  only READS ratings calls `RatingSystems.CreateRatingSystems()`, which creates the systems
  and computes nothing; each one then serves `GetPlayerRating` out of the `AccountRatings`
  table, which is the rating of record. That covers `Account.GetRating` (15 sites),
  `GetPlayerRating`, `GetTopPlayers` and the PlanetWars faction stats - the website reads
  ratings with no lobby server, and `ZeroKWeb.Host` runs that way so it stays true.

  The five that have no database behind them are now `ILobbyServerApi` members:
  `GetPlayerRatingHistory`, `GetInternalRating` and `GetMapRanking` as queries,
  `ForceRatingsUpdate` and `ResetPlanetwarsRatings` as commands. **The website no longer
  names `RatingSystems` or `MapRatings` for anything the database cannot answer.**

  The interface is 45 members, 45 crossable, and the transport test drives all of them.
- ~~Six members pass EF entities.~~ **Done.** All 40 members now take primitives and
  protocol DTOs only; the six that took `Account`, `Clan`, `Planet` or the caller's
  `ZkDataContext` take ids, and the server loads what it needs from its own context.
  `ZkLobbyServer/seam-inventory.txt` is **generated** and checked on every pull request:

      dotnet run --project ZkData.Core -- seam --update

  It walks each member's types transitively, so a DTO holding an entity two levels down is
  caught too - which reading signatures does not do, and which is how the list came to say
  six when an earlier hand-written version of this section said two.
- ~~`RedeemSessionToken` is a bearer credential exchange.~~ **Handled, on the wire.** The
  transport refuses plaintext off loopback - the shared secret and the sign-on token both
  cross it - so it needs `https` unless both ends are on the machine. A private segment
  without a certificate is still deployable by setting the
  `LobbyApiAllowInsecureTransport` MiscVar, which has to be said rather than fallen into.
  The token itself now comes from a cryptographic generator rather than `Guid.NewGuid()`.

  **It expires**, 24 hours after issue by default, configurable with the
  `LobbySessionTokenLifetimeHours` MiscVar. The client is handed its token once, at login,
  and nothing refreshes it - so a client connected for longer than that finds the website no
  longer signs it in automatically. The user sees a login page, reconnecting to the lobby
  fixes it, and nothing is lost. That is the cost of bounding the credential.

  **One property is unchanged**: the game client passes the token to the website in a
  **query string** (`ZeroKLobby/BrowserInterop.cs`), which is where URLs end up in history,
  referer headers and proxy logs. Expiry bounds how long a leaked one is worth having;
  moving it out of the URL is a change to a shipped client and is still worth deciding on.
- **Shared statics the seam never modelled.** `Ratings.RatingSystems` and
  `Ratings.MapRatings` are filled only by `ZkLobbyServer.ZkLobbyServer`, and eight website
  files read them; `Global.AutoRegistrator`, `Global.SteamDepotGenerator` and
  `PlasmaShared.ContentService` are the same shape. None are API calls, so no count of
  `ILobbyServerApi` members finds them - they were found by requesting the pages on the
  .NET 9 port and getting 500s. The login rate limiter (`VerifyIp`/`LogIpFailure`) IS on
  the interface, but its state is the server's memory, so the port has none today.

The remaining `ILobbyServerApiInProcess` members are still implemented and still used -
by the lobby server itself, and by the `Fixer` tool through `Global.Server`. What changed
is that Zero-K.info is not one of their callers.

## The GET surface, surveyed 2026-09-29 - NOW A CHECK, and the numbers below are stale

**Read this section as history.** The survey it records was done by hand and the figures decayed
the moment anything changed - and plenty has. `/Replays/Download` is on its list of 500s and
answers 404 now; seven routes exist that did not; and `/Planetwars/Planet` turned out to throw the
first time an account that could play PlanetWars asked for it, which no hand survey had been in a
position to notice.

`CheckGetSurface` in `ZeroKWeb.Host/Program.cs` does it on every run instead. It finds the actions
by **reflection** over the linked controllers rather than from a list, so an action added tomorrow
is surveyed tomorrow, and it pins the ones that answer 500 against a list in that same file -
failing in **both** directions, because a 500 that stops is a fix somebody should record rather
than silently absorb.

The reasons below are still the right reasons; it is the counts that rot.


The host harness exercised **18 of 34 controllers**. The other sixteen compiled, their views
compiled, and no request had ever reached them - the same gap that hid every defect the lobby port
found by running things. It hid three more here, all in `AutocompleteController`, all now fixed and
checked.

Going a level deeper found nothing further. Of the site's **207 actions**, 86 are GET-able with no
required argument; requested anonymously they answer:

| | |
|---|---|
| 200 | 38 |
| 302 | 26 - `[Auth]`, redirected to sign in |
| 405 | 14 - POST-only, which is the CSRF work holding |
| 404 | 1 |
| **500** | **7** |

**None of the seven is a port defect**, and the reasons are worth recording so the next survey does
not re-derive them:

- `/Replays/Download`, `/Missions/File`, `/Forum/DeletePostPrompt`, `/Home/GetTooltip` want an
  argument the survey did not give them. The same call fails identically on MVC 5.
- `/News/Index` does `Model.First()` on an empty sequence. The fixture has **0 news rows**. Worth
  knowing separately that the RSS feed cannot render a site with no news at all, on either stack.
- `/Planetwars/Minimap` and `/Planetwars/Ladder` do `Galaxies.Single(g => g.IsDefault)` and the
  fixture has **0 galaxies**. Inserting one makes both answer 200.

`/Planetwars` itself needs more than a row: it reads `img/galaxies/<ImageName>` off disk, so it
wants a real background image as well. Not a defect either - just content the fixture does not
carry.

**Filling those gaps in the committed fixture would unlock the pages, and is deliberately not done
here.** `ZeroKWeb.Host` already creates its own galaxy for the Planetwars checks and keeps
`IsDefault` false on purpose, with a comment saying a second default galaxy would break an
unrelated page. Adding a default one to the fixture would walk into exactly that.
