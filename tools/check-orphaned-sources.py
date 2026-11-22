#!/usr/bin/env python3
"""Every tracked C# file should be compiled by some project.

    ./tools/check-orphaned-sources.py

A .cs file that no project compiles is invisible in the worst way: it reads like live code, it
answers a search, and editing it changes nothing. This repository has produced three of them by
three different routes, all found by accident:

  - Shared/ZkData - a whole .NET 3.5 LINQ-to-SQL project left on disk when it left the solution,
    whose assembly name collided with the live one and which briefly won a link in ZkData.Core;
  - GalaxyDesigner/Diagram.cs, Node.cs, Vector.cs - copies of code that lives in PlasmaShared's
    Diagrams namespace, which is what GalaxyDesigner actually builds against;
  - ZeroKLobby/AutoJoinManager.cs and PlayerListItem.cs - left behind when the originals moved
    into MicroLobby/ and the project was updated to point at the new paths.

The last pair is the shape worth naming: **a move that updates the project file but does not delete
the source leaves a file that looks identical to the one being edited.** Nothing complains.

## What counts as compiled

An SDK-style project globs its own directory unless EnableDefaultCompileItems is false; every
project may also name files explicitly, including linked ones from elsewhere, with wildcards. Both
are followed, through .csproj and .props alike, because port-sources.props is how half this
repository's production code reaches .NET 9.

## Saying a file is deliberately not in a project

Some are, legitimately - a script may compile one ad hoc. Put the reason in the file:

    // not-in-a-project: compiled by tools/razor-v3-check/check.sh under mono

and it stops being reported. The marker is in the file rather than in a list here, so it is in front
of whoever next opens it.
"""
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(".").resolve()
COMPILE = re.compile(r'<Compile\s+(?:Include|Update)\s*=\s*"([^"]+)"', re.IGNORECASE)
SDK = re.compile(r'<Project[^>]*Sdk\s*=\s*"', re.IGNORECASE)
DEFAULTS_OFF = re.compile(r'<EnableDefaultCompileItems>\s*false', re.IGNORECASE)
EXEMPT = re.compile(r'//\s*not-in-a-project:\s*(\S.*)')


def tracked():
    out = subprocess.run(["git", "ls-files"], capture_output=True, text=True, check=True).stdout
    return [line for line in out.splitlines() if line]


def expand(base, raw, sources):
    """One Compile Include, MSBuild-ish globs and all, against the tracked file list."""
    candidate = (base / raw.replace("\\", "/"))
    try:
        rel = candidate.resolve().relative_to(ROOT).as_posix()
    except ValueError:
        return set()          # points outside the repository; not ours to judge
    if "*" not in rel:
        return {rel} & sources
    pattern = re.escape(rel).replace(r"\*\*/", "(.*/)?").replace(r"\*\*", ".*").replace(r"\*", "[^/]*")
    matcher = re.compile("^" + pattern + "$")
    return {f for f in sources if matcher.match(f)}


def main():
    files = tracked()
    sources = {f for f in files
               if f.endswith(".cs")
               and "/obj/" not in f and "/bin/" not in f}

# A check that looks at nothing passes. These scripts find their subjects through
# git ls-files, so a directory rename, a project move or a glob that stops matching
# leaves them scanning an empty list and reporting success - which is how a 314-item
# guard in tools/build-website.sh ran as a 0-item guard, green, for one commit.
    if not sources:
        print("found no tracked C# files - the check would pass by seeing nothing", file=sys.stderr)
        return 2

    covered = set()
    for project in [f for f in files if f.endswith((".csproj", ".props"))]:
        text = Path(project).read_text(encoding="utf-8-sig", errors="replace")
        base = Path(project).parent
        if project.endswith(".csproj") and SDK.search(text) and not DEFAULTS_OFF.search(text):
            prefix = "" if base.as_posix() in (".", "") else base.as_posix() + "/"
            covered |= {f for f in sources if f.startswith(prefix)}
        for match in COMPILE.finditer(text):
            covered |= expand(base, match.group(1), sources)

    orphans, waived = [], []
    for path in sorted(sources - covered):
        reason = EXEMPT.search(Path(path).read_text(encoding="utf-8-sig", errors="replace"))
        (waived if reason else orphans).append((path, reason.group(1) if reason else None))

    for path, _ in orphans:
        print("%s: compiled by no project" % path)

    if orphans:
        print("\n%d file(s) no project compiles." % len(orphans))
        print("Add them to a project, delete them, or say in the file why not:")
        print("    // not-in-a-project: <reason>")
        return 1

    print("every tracked C# file is compiled by some project (%d of them)" % len(sources))
    if waived:
        print("\n%d deliberately outside a project:" % len(waived))
        for path, reason in waived:
            print("  %s  %s" % (path, reason))
    return 0


if __name__ == "__main__":
    sys.exit(main())
