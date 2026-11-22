#!/usr/bin/env python3
r"""The eleven ported views differ from their originals in the ways they are recorded to.

    ./tools/check-diverged-views.py            fail if a difference changed
    ./tools/check-diverged-views.py --update   re-record them

PortedViews/ holds a second copy of eleven views. port-views.props compiles the copy and
excludes the original, so the .NET 9 site serves PortedViews/Shared/TopMenu.cshtml and the
MVC 5 site serves Zero-K.info/Views/Shared/TopMenu.cshtml. They diverge for one reason, and
the recorded diffs are the evidence: a child action has no equivalent in ASP.NET Core, so

    Html.RenderAction("ChatNotification", "Lobby")

becomes

    @await Component.InvokeAsync("LobbyChatNotification")

Two copies of a file drift. tools/view-port-report.sh --check already fails when the SET
changes - a ported view with no original, an original that was not excluded - but nothing
compared the contents, so an edit to the original that was not mirrored into the copy would
have left the MVC 5 site serving the new markup and the port serving the old, with every
check here green. The ported views are small and the differences are a handful of lines, so
the baseline is reviewable: it says what every divergence IS, which is worth having written
down whatever it catches.

A difference that changes is not necessarily wrong - mirroring an edit into both copies
changes nothing, and changing one on purpose is a normal thing to do. It just has to be
looked at, and then recorded with --update.
"""
import difflib
import io
import os
import pathlib
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
BASELINE = ROOT / "tools" / "diverged-views.txt"
ORIGINALS = "Zero-K.info/Views"


def read(path):
    """Line endings are not the subject here: the originals are CRLF and some copies are not."""
    return io.open(path, encoding="utf-8-sig", errors="replace").read().splitlines()


def differences():
    listed = subprocess.run(["git", "-C", str(ROOT), "ls-files", "PortedViews/*.cshtml"],
                            capture_output=True, text=True, check=True).stdout
    ported = sorted(name for name in listed.splitlines() if name)

    missing, chunks = [], []
    for name in ported:
        relative = name[len("PortedViews/"):]
        original = ROOT / ORIGINALS / relative
        if not original.exists():
            missing.append(relative)
            continue

        diff = list(difflib.unified_diff(read(original), read(ROOT / name),
                                         fromfile=ORIGINALS + "/" + relative,
                                         tofile=name, lineterm="", n=2))
        # An identical pair is a divergence that no longer exists, which is worth recording as
        # such rather than silently omitting: the copy could then simply be deleted.
        chunks.append("\n".join(diff) if diff else
                      "=== %s and %s are identical" % (ORIGINALS + "/" + relative, name))

    return ported, missing, "\n\n".join(chunks) + "\n"


def main():
    ported, missing, text = differences()

    if not ported:
        print("found no views under PortedViews/ - the check would pass by seeing nothing",
              file=sys.stderr)
        return 2

    if missing:
        print("%d ported view(s) have no original to differ from:" % len(missing))
        for name in missing:
            print("  " + name)
        print("")
        print("Either the original was deleted and the copy should be too, or it moved.")
        return 1

    if "--update" in sys.argv:
        io.open(BASELINE, "w", encoding="utf-8", newline="\n").write(text)
        print("recorded %d divergence(s) in %s" % (len(ported), BASELINE.relative_to(ROOT)))
        return 0

    if not BASELINE.exists():
        print("missing %s - run with --update" % BASELINE.relative_to(ROOT))
        return 2

    recorded = io.open(BASELINE, encoding="utf-8").read()
    if recorded == text:
        print("%d ported view(s) differ from their originals exactly as recorded" % len(ported))
        return 0

    print("a ported view and its original differ in a way that is not recorded:")
    print("")
    for line in difflib.unified_diff(recorded.splitlines(), text.splitlines(),
                                     fromfile="recorded", tofile="now", lineterm="", n=1):
        print("  " + line)
    print("")
    print("If the edit belongs in BOTH copies, make it in both - the .NET 9 site serves the one")
    print("under PortedViews/ and the MVC 5 site serves the other. Then re-record with --update.")
    return 1


if __name__ == "__main__":
    sys.exit(main())
