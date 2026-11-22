# Tests.Portable

Tests that run on **.NET 9**, on Linux, with `dotnet test`. This is the Phase 3 gate: the
plan puts the .NET 9 port at 10-16 weeks and says to build tests first, because the port
will churn code that nothing currently checks.

## Running them

No .NET SDK is needed on the machine - only Docker:

```sh
docker run --rm -v "$PWD":/src -w /src mcr.microsoft.com/dotnet/sdk:9.0 \
    dotnet test Tests.Portable/Tests.Portable.csproj
```

63 tests, about 300 ms.

CI runs exactly this on every pull request - `.github/workflows/test_portable.yml`, on a
stock Linux runner, no Windows and no database.

## Why it links sources instead of referencing projects

`ZkData` and `PlasmaShared` target .NET Framework 4.8 and cannot be loaded by a .NET 9
test host. So this project has **no project references**. It lists the production `.cs`
files as linked `Compile` items instead.

That buys two things from one mechanism:

1. The tests exercise the real production code, not a copy.
2. **Every file in the link list is proven to compile on .NET 9.** The list is the running
   inventory of what the port has already cleared. It only grows by a file actually
   compiling and its tests actually passing.

What it does not buy: runtime behaviour differences between Framework and .NET 9 in code
that compiles on both. Those need the real port.

## Deliberately not in `Zero-K.sln`

The solution is built by msbuild on Windows against .NET Framework. Adding a `net9.0`
project to it would make a solution build depend on the .NET 9 SDK being installed on
that box. This project is built and run on its own, by the command above, and by CI once
Phase 4 sets that up.

## What is covered so far

The Whole History Rating core - `ZkData/Ef/WHR/{Game,Player,PlayerDay}.cs`, 734 lines of
Bayesian rating math. It was picked first because it is pure logic, it decides every
player's displayed rating, and a port that shifts it would do so silently.

- `RatingScaleTests` - Elo / natural-rating / gamma conversions, including the defining
  property that a 400 Elo lead is ten-to-one in gamma.
- `RatingDayTests` - day-number conversion, epoch anchoring, and the fact that a
  `DateTimeKind.Unspecified` value is read as **local time**, not UTC.
- `WholeHistoryRatingTests` - the update itself: winners rise, an even record stays level,
  uncertainty falls with games played, team weight is shared, no NaN or infinity over a
  100-game streak. Properties, not pinned floats, because exact values legitimately depend
  on iteration order.
- `RatingConstantsTests` - the shape of the drift constants.
- `GalaxyMapGeometryTests` - the PlanetWars galaxy map link layout (Phase 5), which used
  to be inline in three Razor views and emitted as JavaScript, so nothing could check it.
- `VectorTests` - polar-coordinate maths from the diagram renderer, linked both to test it
  and to demonstrate that `System.Drawing.Point` is fine on .NET 9.
- `ImageSizingTests` - target sizes for image scaling, including a characterisation of the
  double-applied aspect ratio in `Utils.ToBytes`. See
  `Shared/PlasmaShared/IMAGING-MIGRATION.md`.
- `ImageProcessorContractTests` - implements `IImageProcessor` and drives it here, which is
  the evidence that the imaging seam is free of System.Drawing.Common. If an `Image` or
  `Bitmap` appears on that interface, this stops compiling.
- `ImageSharpProcessorTests` - the ImageSharp implementation of that seam, decoding and
  re-encoding real images on .NET 9. This suite is the only place in the repository where
  imaging runs off Windows at all.

These were checked against a deliberate mutation: changing the Elo scale constant from
`ln(10)/400` to `ln(10)/200` fails three of them.

## Port blockers found while assembling the link list

Each of these is why a file is *not* in the list, and each is real work for Phase 3:

| Blocker | Where | Note |
|---|---|---|
| WCF (`System.ServiceModel`) | `GlobalConst.cs`, via `IContentServiceClient` | Server-side WCF has no in-box .NET 9 successor. A constants file is unportable because it also holds a service factory. |
| `System.Drawing.Common` | `Utils.cs` and 12 others | The **imaging** types only - `Bitmap`, `Graphics`, `Image` - which are Windows-only on .NET 9. The geometry types (`Point`, `Size`, `Rectangle`) are in `System.Drawing.Primitives` and work fine; `Linked/Vector.cs` proves it. See `Shared/PlasmaShared/IMAGING-MIGRATION.md`. |
| Entity Framework 6 | `RatingSystems.cs`, `WholeHistoryRating.cs` | EF6 does not run on .NET 9; EF Core is a rewrite of the data layer, not a retarget. |

Three small splits were made to get the WHR core linkable, each keeping the public API
identical and the Framework build green:

- `ZkData/Ef/WHR/RatingSystems.Dates.cs` - the two pure day-conversion helpers, out of an
  EF-coupled class.
- `Shared/PlasmaShared/GlobalConst.Portable.cs` and `ModeType.cs` - the rating constants and
  the mode enum, out of the file holding the service factory.
- `Shared/PlasmaShared/Utils.Enumerable.cs` - the `ForEach` extension, out of the file that
  pulls in `System.Drawing`.

`GlobalConstMode.cs` is the one piece of this project that is not production code: it
supplies `GlobalConst.Mode` as `Local`, because production resolves it from the
environment in a file that cannot compile here. It is stated explicitly rather than
inherited, and `RatingConstantsTests` pins what follows from it.

## The old Tests project

`Tests/` still exists and still targets `net48`. Its five tests mostly reach the network -
engine list download, whois, a currency rate, an external IP lookup - so they are
integration probes rather than unit tests, and they cannot run here. Leave them until the
port reaches the projects they cover.
