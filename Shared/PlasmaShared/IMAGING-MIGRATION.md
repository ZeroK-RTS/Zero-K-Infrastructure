# Replacing System.Drawing imaging for .NET 9

This is the second blocker in the Phase 3 port, after EF6 (`ZkData/EFCORE-MIGRATION.md`).

## First, the correction

`System.Drawing` is not one thing, and an earlier note in `Zero-K.info/HOSTING.md` was too
broad about it:

- **`System.Drawing.Primitives`** - `Point`, `Size`, `Rectangle`, `Color`, their `F`
  variants, and `ColorTranslator`. Part of the .NET 9 shared framework, fully
  cross-platform, needs no package reference. **Not a blocker.**
- **`System.Drawing.Common`** - `Bitmap`, `Image`, `Graphics`, `ImageCodecInfo`, `Encoder`,
  `InterpolationMode`, `Pen`, `Brush`, `Font`. Since .NET 7 this throws
  `PlatformNotSupportedException` anywhere but Windows. **This is the blocker.**

Verified rather than assumed: `Shared/PlasmaShared/Diagrams/Vector.cs` uses
`System.Drawing.Point` and is linked into `Tests.Portable`, where it compiles and its tests
run on .NET 9 - see `Tests.Portable/VectorTests.cs`.

That halves the apparent size of this job. Nineteen files reference `System.Drawing`; only
**thirteen** touch imaging. One more, `Shared/PlasmaShared/MetaDataCache.cs`, imported it
without using it at all and has been cleaned up.

A caution for anyone repeating this measurement: grepping for type names misses
`ColorTranslator`, which `Zero-K.info/ForumParser/Tags/ColorTag.cs` uses to parse BBCode
colour tags. It is in Primitives, so that file is fine - but it does not look like a
`System.Drawing` user until the compiler says so.

## The actual surface

| Uses | File | What it does |
|---|---|---|
| 21 | `Shared/PlasmaShared/Utils.cs` | `GetResized`, `GetResizedWithCache`, `SaveJpeg`, `ToBytes` - the shared resize and JPEG-encode helpers everything else calls |
| 13 | `Shared/PlasmaShared/UnitSyncLib/UnitSync.cs` | wraps the native unitsync library: `GetHeightMap`, `GetMetalMap`, `GetMinimap`, `FixAspectRatio` |
| 13 | `Shared/PlasmaShared/Diagrams/Node.cs` | diagram rendering |
| 10 | `Zero-K.info/Controllers/PlanetwarsController.cs` | planet icon upload and resize |
| 9 | `Shared/PlasmaShared/ResizedImageCache.cs` | cache of resized images, keyed on `Image` |
| 8 | `Zero-K.info/Controllers/ClansController.cs` | clan avatar upload |
| 8 | `Zero-K.info/AppCode/PlasmaServer.cs` | map minimap / heightmap storage |
| 2 | `ZkData/Ef/PlanetStructure.cs`, `Zero-K.info/Controllers/NewsController.cs`, `Zero-K.info/Controllers/LobbyNewsController.cs`, `Shared/PlasmaShared/UnitSyncLib/Map.cs`, `Shared/PlasmaShared/Diagrams/Diagram.cs` | image load / save |
| 1 | `AutoRegistrator/SteamDepotGenerator.cs` | |

Re-measure with:

    grep -rlE '\b(Bitmap|Graphics|ImageCodecInfo|Encoder|InterpolationMode|ImageFormat)\b' --include=*.cs .

## Shape of the work

`Utils.cs` is the choke point. `GetResized`, `SaveJpeg` and `ToBytes` are what the
controllers and `PlasmaServer` call; port those four methods and most call sites follow
without knowing which library is underneath. That argues for an interface over the four
operations rather than a mechanical type-for-type swap.

**ImageSharp** (`SixLabors.ImageSharp`) is the reasonable target: managed, cross-platform,
supports netstandard2.0 so it can be referenced from the current .NET Framework build
*before* the port, which means the swap can be done and shipped incrementally rather than
in one jump with everything else. SkiaSharp is faster for heavy work but carries native
binaries per platform, which is a worse fit for a job whose point is portability. Note
ImageSharp's licence changed at v3 - v2.1.x is Apache-2.0, v3+ is Six Labors Split, which
is free for open source but wants reading first.

The awkward one is `UnitSync.cs`. It receives raw pixel buffers from a native library and
wraps them in `Bitmap`. That is not a library swap, it is a rewrite of the interop
marshalling, and it cannot be tested without unitsync and real map files.

## A defect found while separating the arithmetic

`Utils.ToBytes` is what `AutoRegistrator` uploads every new map's minimap, metal map and
height map through. It does two things wrong, both preserved for now in
`ImageSizing.LegacyToBytesSize` and pinned by tests:

1. **It applies the aspect ratio twice.** `UnitSync.GetMinimap` already calls
   `FixAspectRatio`, which turns unitsync's square minimap into the map's real proportions.
   `ToBytes` then computes the ratio of *that already-correct image* and applies it again,
   so a 2:1 minimap is uploaded as 4:1. Square images are unaffected, which is why it has
   survived.
2. **It ignores its `size` argument.** `AutoRegistrator` passes `ImageSize = 256`,
   commented "max size of minimap to be sent to server". Nothing downscales; a 1024-wide
   minimap is uploaded at 1024.

`ImageSizing.BoundedByLongestSide` is what it was presumably meant to do, and is tested but
deliberately **not wired up**: switching changes the images uploaded for every new map, and
whether to re-process the existing ones is a decision, not a refactor.

## Decisions taken, 2026-09-21

Two of the three open imaging questions were decided; the third stands.

### `Images.Processor` is ImageSharp — **decided, done**

One line in `Imaging/Images.cs`. Every upload through the seam - clan avatars and
backgrounds, news and lobby-news thumbnails, structure icons - is now encoded by ImageSharp
rather than System.Drawing.

**This changes bytes on disk for new images.** Both implementations ask for bicubic; different
libraries with different kernels do not produce identical pixels, so images uploaded from now
on differ slightly from those already stored. **Nothing stored is touched.**

Done now rather than when the port forced it: `System.Drawing.Common` is Windows-only on
.NET 9, so the line had to move before the port completes, and moving it while there is slack
means the difference lands somewhere it can be looked at. One line reverts it.

`Tests.Portable` now pins the choice - a revert, deliberate or by a bad merge, would otherwise
change what the site writes to disk with nothing to say so. A second test asserts the test
assembly has no `System.Drawing.Common` reference, which is only true because `Images.cs` names
an implementation that does not need one; while it named `SystemDrawingImageProcessor` that
file could not be linked into a .NET 9 project at all.

### Structure icons stay bicubic — **decided, accepted**

`PlanetStructure.GenerateResized` asked for `HighQualityBilinear`; the seam asks for
`HighQualityBicubic`, so icons regenerated since the seam landed differ slightly from earlier
ones. Accepted rather than reverted: the alternative is widening `IImageProcessor.SaveResized`
with a resampler argument for one call site, and these icons are small and regenerate
routinely.

### `ToBytes` — **decided 2026-09-25: fixed for new maps, stored images left alone**

`ToBytes` now uses `BoundedByLongestSide`, so a newly registered map gets undistorted images
bounded by the 256 that `AutoRegistrator` has been asking for all along. Images already stored
are not touched.

**The objection recorded here was wrong, and that is what unblocked it.** It said the two
generations would sit side by side "with no way to tell them apart". They are trivially
distinguishable: unitsync's minimap is square, `FixAspectRatio` stretches it to the map's true
ratio **R**, and the old rule then applied R a second time - so a legacy image has aspect **R
squared** where a correct one has **R**, and the database knows R. That is
`ImageSizing.LooksLikeLegacyToBytes`, with tests, and

    ZK_CONNECTION_STRING=... dotnet run --project ZkData.Core -- minimaps /path/to/Resources

reports how many stored images are of each kind, with examples and the size each should have
been. It only reads.

**The backfill exists too**, as the re-stretching option:

    ZK_CONNECTION_STRING=... dotnet run --project ZkData.Core \
        -- backfill-minimaps <dir> [--apply] [--touch] [--limit=N] [--only=name,name]

**Running it for real, in the order that keeps it reversible.** The directory is the site's
`Resources` folder, and these are the only copies of those images - the originals are inside map
archives:

    -- minimaps <dir>                          how many are legacy, and examples
    -- backfill-minimaps <dir>                 dry run: what would change, nothing written
    -- backfill-minimaps <dir> --apply --limit=1
                                               one map. Look at it on the site before going on.
    -- backfill-minimaps <dir> --apply --limit=20
                                               a batch, then look again
    -- backfill-minimaps <dir> --apply         the rest
    -- backfill-minimaps <dir> --apply --touch the same, and make clients re-sync

`--only=name` targets named maps, for redoing one that came out wrong. Every overwritten file is
copied to `<name>.legacy` first, so any step is undone with `mv`; a second run corrects nothing it
has already corrected.

It corrects the minimap, metal map and height map of every legacy resource and regenerates the
thumbnail from the corrected minimap - the thumbnail being the case where the defect hides best,
since its dimensions were always right and only its content was distorted.

Three things about it are deliberate:

- **It writes nothing without `--apply`**, and copies every file it overwrites to `<name>.legacy`
  first, so a run is undone with `mv`. These are the only copies of images whose originals live in
  map archives that would need re-scanning with unitsync to reproduce.
- **It is idempotent**, because `LooksLikeLegacyToBytes` stops recognising an image once it has
  been corrected. That is asserted as a property over a range of ratios, not as an example.
- **`--touch` is opt-in.** It moves `LastChange` so clients re-sync rather than keeping the image
  they already have; it is off by default because it is a database write and a first run is
  usually a rehearsal.

**The sequence above has been rehearsed end to end**, against 26 non-square maps whose images were
generated at the legacy shape (aspect R squared) from the fixture's own `MapSizeRatio` values. It
has still never been run on the production `Resources` folder - that needs someone with access to
the box - but every step of the procedure, including the rollback, has now been executed rather
than only written down:

| step | what happened |
|---|---|
| `minimaps <dir>` | `legacy (squashed) 26`, `current 0` |
| `backfill-minimaps <dir>` | `corrected 78` (26 maps x 3 kinds), nothing written |
| `--apply --limit=1` | `maps touched 1`; 3 files rewritten, 4 `.legacy` copies made including the thumbnail |
| the corrected minimap | `1024x455 -> 1024x682`, mean RGB unchanged: the picture was stretched, not replaced |
| the thumbnail | stayed `96x64` and its content changed, being regenerated from the corrected minimap |
| the next map | untouched, no `.legacy` - `--limit` really stops |
| `--apply` | `corrected 75, already right 3`: it resumed where the limited run stopped |
| `--apply` a third time | `corrected 0, already right 78` - idempotent |
| `for f in *.legacy; do mv "$f" "${f%.legacy}"; done` | 104 files restored, and the report reads `legacy 26, current 0` again: the exact starting state |

**The rehearsal found one defect, in the report rather than the backfill.** `minimaps` printed its
"expected about WxH" from its own copy of the arithmetic, which was the landscape case written out
- keep the width, derive the height. For a portrait map the backfill keeps the *height* and
widens, so a 455x1024 image was reported as "expected about 455x682" when the backfill would make
it 682x1024. The operator reading that line before deciding to `--apply` was told the image would
get shorter when it was about to get wider. It calls `ImageSizing.CorrectedFromLegacy` now, the
same function the backfill uses, so there is no second copy to drift. Neither file was wrong to
read; the two only disagreed when run.

**What it cannot do is recover detail.** The old rule resized the short axis away, and no stretch
brings it back: a corrected 2:1 minimap is geometrically right and softer than one registered
today. The correction keeps the long axis at its stored resolution for that reason - throwing away
the axis that survived would lose more.

### The lossless option: re-registration

Going back to the archive does recover the detail, because unitsync renders the images again.
`UnitSyncer.Scan` takes a list of resources to register **again** although the database already has
them, and `AutoRegistrator.ReregisterResource(name)` downloads the archive first - a map registered
long ago is usually not on the machine, and Scan only sees archives it can find. A SuperAdmin POST
to `/Admin/ReregisterResource` drives it.

**One map per call, deliberately.** Every part of this - unitsync, the archive download, the upload
through RegisterResource - is something no test in this repository can exercise: there is no
unitsync here and no map archives. So the unit of work is the one a person can check the result of
before doing the next, rather than a loop that is either fine or has quietly rewritten a library.

**It is therefore compiled, not verified**, and that distinction is the point. What is known: the
website, AutoRegistrator and the .NET 9 port all build with it, the ordinary scan is unchanged
because the force list defaults to null, and `RegisterResource` already rewrites stored images when
the same md5 arrives again, which is the path it depends on. What is not known is how it behaves
against a real archive, and the only way to find out is to run it on one map and look.

Suggested order for whoever does: `minimaps` to list the affected maps, re-register **one**, check
its images and its thumbnail on the site, then decide between doing the rest that way and running
the in-place `backfill-minimaps` for the ones not worth a download.

### Doing it on demand, one map at a time, as people open them

`LegacyMinimapUpdater` (2026-10-02) is re-registration driven by traffic rather than by an
operator. `MapsController.GetMapDetailData` - the one place both `Detail` and `DetailName` come
through - hands the map's name to a queue and returns; a worker then checks whether that map is
still legacy and, if it is, re-registers it. Two workers at most.

This is what makes the lossless option affordable at all. Variant B costs an archive download and
a unitsync scan per map, which is why the manual procedure above is "do one and look". Spread over
the maps people actually open, in the order they open them, that cost stops being a project and
becomes background noise - and the maps nobody looks at never cost anything.

**A job checks at the moment it starts that the map has not already been corrected**, so it is
safe to mix with everything else here: a map corrected by `backfill-minimaps`, by a manual
`/Admin/ReregisterResource`, by an earlier run of the web process, or by the other worker while
this key was queued, is skipped. Three things make that hold.

1. The check is the first thing the job does, and the job runs when a worker **starts** rather
   than when the name was queued - so it reads current state, not state from when somebody opened
   the page. `BoundedWorkQueueTests.Work_sees_the_state_at_start_not_at_enqueue` pins it, and
   fails if the queue is made to run work eagerly.
2. It measures the stored minimap and reads `MapSizeRatio` on the spot. Nothing is cached.
3. A corrected image stops being recognised as legacy -
   `ImageSizingTests.Correcting_a_legacy_image_gives_it_the_maps_own_ratio`, over seven ratios in
   both orientations. Without that this would re-register the same map on every process start.

For re-registration the third point holds because unitsync's images go back through the same
`ToBytes` the 2026-09-25 fix corrected, so what returns carries the map's ratio **R** and not **R
squared**. That step is reasoned from the fix rather than measured, because measuring it needs
unitsync and a map archive - the first real run is where it gets confirmed, which is the same
caveat the manual procedure carries.

**Two limits worth knowing.** The queue's seen-set and the lock inside `ReregisterResource` are
both per-process, so a second web process can re-register a map this one is already doing; the
result is the same either way, so the cost is duplicated work rather than a wrong image. And the
whole feature is inert on .NET 9 - `LegacyMinimapUpdaterCore` is a deliberate no-op, because
re-registration needs unitsync and the ported site has none.

Note what the defect actually did, since it was never a cosmetic rounding: for a 2:1 map the
stored minimap, metal map and height map were squashed to 4:1, and the thumbnail
`PlasmaServer` derives from them was then stretched back to 2:1 - correct dimensions, distorted
content. In the local fixture 26 of 68 resources are non-square.

## 2026-09-25: the last two call sites, and 34 of 34 controllers

**Every controller in Zero-K.info now compiles on .NET 9.** The two that were left were the
two blocked here, and they needed the seam to grow by two operations.

**`SaveResizedJpeg(bytes, size, path, quality)`.** `PlasmaServer` writes map thumbnails at
JPEG quality **100** and the galaxy render at **85**, both chosen deliberately; ImageSharp's
default is 75. Porting those call sites through the quality-less `SaveResized` would have
compiled, passed every test, and quietly made both worse - so quality is on the interface, and
a test asserts that quality 100 produces a bigger file than quality 10. Breaking it (dropping
the encoder argument) fails that test, checked.

**`ComposeJpeg(background, overlays, quality)`.** The galaxy map is a background with a planet
icon drawn at each planet's position, which is compositing and did not fit a seam built around
resize-and-save. Overlays carry a `System.Drawing.Rectangle` and are stretched into it, which
is what `Graphics.DrawImage(image, x, y, w, h)` did.

It returns **bytes rather than writing a file**, and that is what let the call site cross:
`Index` needs the dimensions as well as the image, and both are now had without naming an
imaging type. It also makes the operation testable - the tests decode the result and assert
which colour won at a point inside the overlay and at a point outside it, compared loosely
because JPEG is lossy. Drawing every overlay at (0,0) instead of its rectangle fails two of
them, checked.

**The arithmetic went to `ImageSizing.PlanetIconPlacement` first**, with four tests, including
one that pins the *truncation*: the width truncates before the height is derived from it, and
the halving truncates again. Computing in doubles and rounding once would be defensible and
would move every planet by a pixel, which is not what a port is for.

**`PlasmaServer`'s thumbnail sizing was already extracted** - `ImageSizing.ScaledToFit` was
written from exactly those lines and had been waiting, tested, since step 0.

**One branch was not ported, deliberately.** `GenerateGalaxyImage`'s `antiAliasingFactor`
composed at a multiple and resized down. It was unreachable: the only caller takes the
defaults, and the parameter was pinned to 1 by a FIXME saying the Bitmap path had issues. It
now throws if anything passes something else, rather than being silently ignored or ported
untested - supersampling is a change to what the galaxy looks like, not a port.

**Two more things were found stranded in the GDI+ half of `Utils.cs`**, which is now a
familiar shape: `SafeDelete` (eleven lines that swallow an exception, and the last thing
keeping `PlasmaServer` off the port) and, for `ContentServiceImplementation`,
`EngineDownload.VersionNumberComparer`, split into a portable half.

**Still not done, and unchanged:** `UnitSync.cs` - the interop rewrite - and `ToBytes`, whose
aspect-ratio and size defects are a backfill decision rather than an imaging one. `ZeroKLobby`
keeps System.Drawing either way. `ResizedImageCache` is keyed on an `Image` and is only used
by paths that have not moved.

## Suggested order

0. **Done:** the target-size arithmetic is separated into
   `Shared/PlasmaShared/Imaging/ImageSizing.cs`, linked into `Tests.Portable` and covered
   by 14 tests. It is pure, uses only `System.Drawing.Primitives`, and therefore already
   runs on .NET 9. Sizing is where a change silently distorts an image, so it is worth
   holding still before anything underneath it moves.
1. **Done:** `Imaging/IImageProcessor.cs` is the seam - `Measure`, `Save`, `SaveResized`,
   in terms of encoded bytes and `System.Drawing.Size`. Nothing in it names `Bitmap`,
   `Image` or `Graphics`, so it compiles on .NET 9; it is linked into `Tests.Portable` and
   exercised there, which is what stops someone quietly adding an `Image` to it.
   `SystemDrawingImageProcessor` implements it as a thin wrapper that does exactly what
   each call site did before, so no pixels changed.

   Migrated to it: clan avatar and background upload (both actions in `ClansController`),
   the news thumbnail, the lobby news thumbnail. Five call sites; `Images.Processor` is the
   one place an ImageSharp implementation gets substituted.

   Not migrated, and why:
   - `PlanetwarsController.SaveJpeg` renders a galaxy image in memory with `Graphics` and
     saves the `Bitmap` it already holds. Nothing crosses a byte boundary, so the seam does
     not fit until the renderer itself is ported.
   - `AutoRegistrator` / `UnitSync` receive `Bitmap` objects from a native library. That is
     the interop rewrite described above, not a call-site change.
   - `GetResized` / `GetResizedWithCache` in `ZeroKLobby`: a WinForms client that is not
     part of this port and keeps System.Drawing either way.
2. **Done.** `Imaging/ImageSharpImageProcessor.cs` is what `Images.Processor` returns, and has
   been since 2026-09-21. It is linked into `Tests.Portable` and exercised on **.NET 9, on
   Linux**, where System.Drawing.Common cannot run at all - real bytes, real files.

   (This section said "not wired up" for four days after it was. The decision above was the
   true one; this is why the two halves of a document should not both describe state.)

   Version, since it is less obvious than it looks: **ImageSharp 2.1.13**. The 2.x line is
   Apache-2.0 and supports .NET Framework 4.8, so it lives in the codebase *before* the
   port rather than after - which is what makes an incremental switch possible at all. 3.x
   and later need .NET 6+ and carry the Six Labors Split Licence. Older 2.1.x patch levels
   have published high-severity advisories (2.1.9 does, and so does 3.1.6); 2.1.13 is
   clean. Verified by restoring each on both target frameworks.
3. `ResizedImageCache` - its key is an `Image`, so it changes with whatever type replaces
   it.
4. **Half done - see below.** `UnitSync.cs`: the arithmetic is out and tested, the Bitmaps remain.

## 2026-09-25: unitsync, as far as it goes without a decision

**What moved:** the arithmetic. `PixelBuffers` unpacks unitsync's raw buffers - 16-bit RGB565 for
the minimap, 8-bit greyscale for the height and metal maps - into tightly packed RGB24, and
`ImageSizing.MinimapAspectCorrection` is `FixAspectRatio`'s sizing. Both are pure, both are tested
on .NET 9 on Linux, and `UnitSync.cs` now calls them instead of doing it inline.

**What did not move:** `UnitSync` still returns `Bitmap`, because `Map` holds `Image` fields,
`Utils.ToBytes` takes an `Image`, and `AutoRegistrator` passes them along. `GdiPixelBridge` is the
one remaining piece of System.Drawing in that path, and it is where the next cut goes.

**The interesting part is how it was verified**, because "cannot be tested without unitsync and
real map files" was true of the old code and is no longer true of this part. Two things could not
be checked on Linux and both are now checked on the **Windows CI job**, which gained its first
Windows-only reason to exist:

- **5-6-5 to 8-8-8 scaling.** GDI+ unpacked the minimap by its own rule, and the plausible
  candidates - `x * 255 / 31` and bit replication `(x << 3) | (x >> 2)` - agree at 0 and 31 and
  differ in between, which is a shade on every pixel of every minimap. `Tests/UnitSyncPixelTests`
  compares the two implementations for **all 65,536 values**.
- **Channel order and row padding.** `Format24bppRgb` is BGR in memory despite the name, and GDI+
  pads every row to a four-byte boundary. Both mistakes are invisible on the height and metal
  maps, which are grey - so they are asserted on coloured pixels and at a width (3) where the
  padding bites.

**Copying rather than wrapping.** The old `new Bitmap(size, size, stride, format, pointer)` handed
GDI+ a pointer into unitsync's own memory and kept it. The bytes are copied out now, so nothing
downstream holds a buffer unitsync may free.

**What finishing requires is a decision, not more porting.** `Map.Minimap` would become encoded
bytes, `Utils.ToBytes` would move onto the imaging seam, and `AutoRegistrator`'s three call sites
would follow. That is mechanical - but `ToBytes` is where the aspect-ratio and size defects live,
and porting it means either preserving them deliberately in the new code or fixing them, which is
the backfill question below and is not the port's to answer.


Only step 1 is verifiable in this repository today. Steps 2-4 need either a test
deployment or the native unitsync library.

## The lobby server, measured 2026-09-28

`ZkLobbyServer.Core` still references `System.Drawing.Common`. The website's half of this job is
done - `Images.Processor` is ImageSharp - but the lobby server compiles a different set of files,
and this is what it would take to get it off the package.

**Nothing the CI check walks touches it.** The server starts, registers an account, logs in, joins
a channel and opens a battle on .NET 9 without a single GDI+ call, which is why none of this has
surfaced as a failure. What touches it is map imagery, on the same path as the engine.

### How big it is, and why that number is a floor

Removing the `PackageReference` and building gives **84 errors across 7 files**:

| errors | file |
|---|---|
| 24 | `Utils.cs` - the shared resize/JPEG helpers |
| 16 | `ResizedImageCache.cs` |
| 14 | `UnitSyncLib/UnitSync.cs` |
| 14 | `Diagrams/Node.cs` |
| 12 | `UnitSyncLib/Map.cs` |
| 2 | `Imaging/GdiPixelBridge.cs` |
| 2 | `Diagrams/Diagram.cs` |

**That table is a lower bound, not the answer, and the reason is worth knowing before anyone
repeats the measurement.** `Imaging/SystemDrawingImageProcessor.cs` is in the same compilation -
`-getItem:Compile` lists it - and it uses `Image`, `Bitmap`, `Graphics` and `InterpolationMode`
throughout. It contributes **zero** of the 84. Compiled on its own against net9 without the
package it produces **46 errors**.

The cause is Roslyn declaration-error masking, the same trap the EF Core port hit: `Utils.cs` line
22 is `using Encoder = System.Drawing.Imaging.Encoder;`, an alias that fails to bind. A failed
declaration stops method-body binding **across the whole compilation**, and every GDI+ use in
`SystemDrawingImageProcessor` is inside a method body. So one build reports the files whose
*declarations* break and hides every file whose *bodies* do.

To get the real set, fix the declaration errors first and rebuild, repeatedly, or compile
suspected files in isolation:

    <Compile Include="the/one/file.cs" />   with EnableDefaultItems=false

(and set `EnableDefaultEmbeddedResourceItems=false` too, or the build fails on a `**/*.resx` glob
before it ever reaches the compiler - which looks exactly like "no errors").

### What it would take

`Utils.cs`, `ResizedImageCache.cs` and `GdiPixelBridge.cs` are library-swap work: the seam already
exists, and these are the callers that have not moved through it. `Diagrams/*` draws the galaxy
map and is website code that the lobby server only compiles because `ZkLobbyServer.Core` globs all
of PlasmaShared - **excluding it is probably cheaper than porting it**, and would want checking
against what actually references it rather than against the file's name.

`UnitSync.cs` and `Map.cs` are the awkward ones this document already flagged: they receive raw
pixel buffers from the native library and wrap them in `Bitmap`. That is interop marshalling, not
a library swap, and it cannot be tested without unitsync and real map files. `tools/native-calls.txt`
records that the `unitsync` name resolves correctly on Linux - measured - but no entry point of the
135 has ever been called.

### Narrowed, 2026-09-28: 84 errors across 7 files down to 28 across 3

Most of that surface was code the lobby server compiles but cannot reach. Decided by what
references each file, not by what it is called:

| removed from `ZkLobbyServer.Core` | why |
|---|---|
| `Diagrams/Node.cs`, `Diagrams/Diagram.cs` | `GalaxyDesigner`, a Windows-only WPF app, is their only consumer anywhere in the repository. `port-sources.props` already links `Vector.cs` alone for the same reason |
| `Utils.Imaging.cs` (new) | `GetResized`, `GetResizedWithCache`, `SaveJpeg`, `ToBytes`, split out of `Utils.cs` the way `Utils.Enumerable.cs` and `Utils.Polyfills.cs` already were. Every caller is ZeroKLobby, ChobbyLauncher or AutoRegistrator - all Framework, all Windows |
| `ResizedImageCache.cs` | reached only through `Utils.GetResizedWithCache` |
| `Imaging/SystemDrawingImageProcessor.cs` | nothing anywhere instantiates it; `Images.Processor` names the ImageSharp one. Kept in PlasmaShared so that choice stays one line to revert |

What is left is exactly what this document always said was the hard part, and nothing else:

| errors | file |
|---|---|
| 14 | `UnitSyncLib/UnitSync.cs` |
| 12 | `UnitSyncLib/Map.cs` |
| 2 | `Imaging/GdiPixelBridge.cs` |

All three are the native-buffer-to-`Bitmap` interop. That is not a library swap and it cannot be
tested without unitsync and real map files, so the package reference stays until it can be.

**A warning about measuring this, earned twice in one afternoon.** Do not delete the package with
`sed -i '/System.Drawing.Common/d'`: this file now contains that string inside an XML comment, and
removing that line leaves malformed XML. MSBuild then fails with `MSB4067` at project-load time,
compiles nothing, and reports **zero** compile errors - which reads exactly like success. Remove
the `<PackageReference>` element specifically, and check the exit code rather than a grep of the
output. The same mistake in a different costume as the `**/*.resx` one above.

### The blocker is one function, measured 2026-09-28

The three remaining files were called "the awkward one... interop marshalling, not a library swap,
and it cannot be tested without unitsync and real map files". Both are now fetchable -
`tools/fetch-engine.sh` gets a 42MB engine and a 333KB map from the projects' own distribution -
so it was tested. On .NET 9 on Linux, against the real engine and a real map:

| step | result |
|---|---|
| `GetMapCount()` | 1 |
| `GetMapName(0)` | `Bluescreen fields v2` |
| `GetMinimap(name, 0)` | a real pointer |
| `Marshal.Copy` of the buffer | 2,097,152 bytes = 1024x1024x2, RGB565 |
| `PixelBuffers.Rgb565ToRgb24` | 3,145,728 bytes, first pixel rgb(0,121,214) |
| `GdiPixelBridge.FromRgb24` | **throws** `TypeInitializationException` |

**Everything except the last line already works without GDI+.** The interop binds and marshals,
the buffer arrives intact, and the 5-6-5 unpacking is `PixelBuffers`, which is managed, portable
and already pinned against GDI+ itself for all 65,536 values on the Windows job. What fails is
only the final step that turns a finished RGB24 byte array into an image object.

So this is not the rewrite the note above feared. `GdiPixelBridge.FromRgb24` needs an ImageSharp
twin, and the type flowing out of `UnitSync.GetSquareMinimap` and stored on `Map.Minimap` has to
stop being `System.Drawing.Bitmap`. That second half is the real work: `Map.Minimap` is consumed by
the Framework stack too, so it is a decision about what both stacks carry, not a swap.

`Tests.Portable/UnitSyncEngineTests` holds the measured half down. It stops before
`GdiPixelBridge` deliberately - not only because that would throw, but because
`ImagesProcessorChoiceTests` asserts this assembly has **no** System.Drawing.Common reference, and
linking that file would add one. `Marshal.Copy` does what `GdiPixelBridge.CopyFrom` does.

### Done, 2026-09-28: the compat twin, and System.Drawing.Common is gone from the port

`MapImage` is a compat twin in the idiom the rest of the port uses:

| file | stack | what it holds |
|---|---|---|
| `Imaging/MapImage.cs` | net48 | a `System.Drawing.Image`, built by `GdiPixelBridge` - so map registration produces exactly the bytes it did before |
| `Imaging/MapImageCore.cs` | net9 | unitsync's RGB24 pixels and a `Size`. ImageSharp is reached for only to resize or encode |

`UnitSync` and `Map` name `MapImage` and neither knows which half it got.
`Map.Minimap`/`Heightmap`/`Metalmap` change type, which is safe because all three are
`[NonSerialized]`, `[XmlIgnore]` and `[JsonIgnore]` - nothing ever persisted them.

`NativeBuffer.CopyFrom` was split out of `GdiPixelBridge`: it is `Marshal.Copy` and never needed
GDI+, so the .NET 9 build no longer links a file full of `Bitmap` to get at it. `GdiPixelBridge`
now has exactly one caller, the Framework half of the twin.

**The result is that no project in this repository references `System.Drawing.Common` any more** -
not `ZkLobbyServer.Core`, and not `ZkData.Core`, which carried it "COMPILE-TIME ONLY" because
`Map` declared `Bitmap` members. Verified by exit code and by the built assemblies, which contain
no reference to it. That removal is the guard: a `Bitmap` added to anything either project
compiles no longer resolves.

Proven end to end on .NET 9 on Linux, against the real engine and a real map, in
`Tests.Portable/UnitSyncEngineTests`:

    GetMinimap -> 2,097,152 bytes RGB565 -> Rgb565ToRgb24 -> MapImage -> ToBytes(256)
        -> 6,019 bytes of JPEG, header FF D8

The step that threw `TypeInitializationException` in the measurement above now completes.

**What is deliberately unchanged:** the Framework stack still uses GDI+, so the images
AutoRegistrator uploads are byte-for-byte what they were. The two halves ask for bicubic and will
not agree pixel-for-pixel if the .NET 9 path ever registers maps - the same caveat recorded when
`Images.Processor` moved to ImageSharp, and the same answer: nothing stored is touched.
