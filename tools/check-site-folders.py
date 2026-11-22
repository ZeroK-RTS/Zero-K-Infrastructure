#!/usr/bin/env python3
"""The site's published directories are each named in one place.

    ./tools/check-site-folders.py

`GlobalConst.ResourceFolder` and `GlobalConst.AvatarFolder` are the directories published files
live in and the URL segments they are served under. They exist because each name used to be
written out by hand in several files, and some of them spelled it differently:

    PlasmaServer.StoreMetadata   MapPath("~/Resources")          the writer
    MetaDataCache                Path.Combine(.., "resources")   the lobby server's disk read
    MissionUpdater               SiteDiskPath + @"\\resources\\"   and it CREATED that directory
    Fixer                        SiteDiskPath + @"\\Resources"

    HtmlHelperExtensions, Unlock  /img/avatars/{code}.png         what the SITE emits
    ZeroKLobby                    img/Avatars/{id}.png            what the game client asked for
    SteamDepotGenerator           Path.Combine(.., "img", "Avatars")

On NTFS those are one directory, so the spread was invisible for years. Off Windows they are
separate, and nothing reports it: the lobby server's disk lookup simply misses and falls back to
HTTP, a mission upload writes into a directory the site does not serve, and avatars 404 in the
lobby because PhysicalFileProvider is case-sensitive where IIS was not.

**A test cannot catch this.** By the time code runs, a literal has already become a path; what has
to be checked is that nobody wrote the name down again. So this reads the source.

Exempt: the constant's own declaration, and prose - comments, XML docs and this file - because
naming the directory while explaining it is the point.
"""
import pathlib
import re
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
DECLARATION = "Shared/PlasmaShared/GlobalConst.Portable.cs"

# A path or URL segment, not the word. "~/Resources", "\resources\", "/Resources/", "resources"
# as an argument to Path.Combine - but never db.Resources, the DbSet, which is the same word
# doing an entirely different job and appears everywhere.
PATTERNS = [
    re.compile(r'"~/[Rr]esources'),
    re.compile(r'"/[Rr]esources/'),
    re.compile(r'@?"\\+[Rr]esources'),
    re.compile(r'Path\.Combine\([^)]*"[Rr]esources"'),
    # img/avatars, which the site emits in lower case and two consumers asked for in upper.
    # NOT Path.Combine(configs, "Avatars"): that is the GAME's own directory inside a Steam
    # depot, named by its Lua, and it is right to be capitalised - so the patterns below only
    # match the folder when it is qualified by img or by a URL.
    re.compile(r'"/img/[Aa]vatars'),
    re.compile(r'"~/img/[Aa]vatars'),
    re.compile(r'"img",\s*"[Aa]vatars"'),
    re.compile(r'"[Aa]vatars/\{'),
]

COMMENT = re.compile(r'^\s*(///|//|\*|/\*)')


def tracked_sources():
    out = subprocess.run(["git", "-C", str(ROOT), "ls-files", "*.cs", "*.cshtml"],
                         capture_output=True, text=True, check=True).stdout
    return [line for line in out.splitlines() if line]


# A check that looks at nothing passes. These scripts find their subjects through
# git ls-files, so a directory rename, a project move or a glob that stops matching
# leaves them scanning an empty list and reporting success - which is how a 314-item
# guard in tools/build-website.sh ran as a 0-item guard, green, for one commit.
def main():
    scanned = tracked_sources()
    if not scanned:
        print("found no tracked C# or Razor files - the check would pass by seeing nothing",
              file=sys.stderr)
        return 2

    offenders = []
    for name in scanned:
        if name == DECLARATION or name.startswith("tools/"):
            continue
        path = ROOT / name
        try:
            text = path.read_text(encoding="utf-8-sig", errors="replace")
        except OSError:
            continue
        for number, line in enumerate(text.splitlines(), 1):
            if COMMENT.match(line):
                continue
            if any(pattern.search(line) for pattern in PATTERNS):
                offenders.append("%s:%d: %s" % (name, number, line.strip()))

    if offenders:
        print("A published directory is named outside its GlobalConst constant:")
        print()
        for line in offenders:
            print("  " + line)
        print()
        print("Use GlobalConst.ResourceFolder or GlobalConst.AvatarFolder. On Windows a second")
        print("spelling is the same directory;")
        print("off Windows it is a different one, and nothing fails - the file is just never found.")
        return 1

    print("each published directory is named once, in its GlobalConst constant (%d files read)"
          % len(scanned))
    return 0


if __name__ == "__main__":
    sys.exit(main())
