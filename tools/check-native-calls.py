#!/usr/bin/env python3
"""Every native library the .NET 9 lobby server P/Invokes, against a reviewed baseline.

    python3 tools/check-native-calls.py

A P/Invoke compiles everywhere and only fails when it runs. That is not a theory here:
TcpTransportServerListener.Bind called SetHandleInformation out of kernel32, which throws off
Windows - and Bind caught the exception, retried 120 times and gave up, so the server came up
with its API listening and NO player port at all. Nothing in any build said a word.

So this is a baseline check, not a cleverness check. It resolves what ZkLobbyServer.Core actually
compiles - the same Compile items MSBuild sees, not a hardcoded list of directories - collects
every [DllImport] library name in those files, and compares the set to tools/native-calls.txt,
which carries a one-line verdict for each. It fails when the set changes IN EITHER DIRECTION: a
new native dependency needs a verdict, and one that disappears means a verdict is now stale.

It deliberately does not try to decide whether a call site is *guarded*. A first attempt did, and
it was the same mistake as the EF6 zero-key scan in ZkData/EFCORE-MIGRATION.md - a heuristic that
could not reliably catch its own motivating case. Naming the library is exact; judging the guard
is a person's job, done once, written in the baseline.
"""
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(".").resolve()
PROJECT = Path("ZkLobbyServer.Core/ZkLobbyServer.Core.csproj")
BASELINE = Path("tools/native-calls.txt")

COMPILE = re.compile(r'<Compile\s+(?:Include|Update)\s*=\s*"([^"]+)"', re.IGNORECASE)
REMOVE = re.compile(r'<Compile\s+Remove\s*=\s*"([^"]+)"', re.IGNORECASE)
# The library is the first argument, quoted or a const identifier: [DllImport("kernel32.dll")]
DLLIMPORT = re.compile(r'\[\s*DllImport(?:Attribute)?\s*\(\s*([^,)\s]+)', re.IGNORECASE)


def tracked():
    out = subprocess.run(["git", "ls-files", "-z"], capture_output=True, text=True, check=True).stdout
    return [f for f in out.split("\0") if f]


def expand(base, raw, sources):
    candidate = base / raw.replace("\\", "/")
    try:
        rel = candidate.resolve().relative_to(ROOT).as_posix()
    except ValueError:
        return set()
    if "*" not in rel:
        return {rel} & sources
    pattern = re.escape(rel).replace(r"\*\*/", "(.*/)?").replace(r"\*\*", ".*").replace(r"\*", "[^/]*")
    matcher = re.compile("^" + pattern + "$")
    return {f for f in sources if matcher.match(f)}


def resolve(const, text):
    """A DllImport argument is either a literal or a const; resolve the const in its own file."""
    if const.startswith('"'):
        return const.strip('"')
    match = re.search(r'\bconst\s+string\s+' + re.escape(const.split(".")[-1]) + r'\s*=\s*"([^"]*)"', text)
    return match.group(1) if match else const + " (unresolved)"


def main():
    if not PROJECT.exists():
        print("%s is gone - this check names one project and that project is not there" % PROJECT)
        return 1

    sources = {f for f in tracked() if f.endswith(".cs") and "/obj/" not in f and "/bin/" not in f}
    text = PROJECT.read_text(encoding="utf-8-sig", errors="replace")
    base = PROJECT.parent

    compiled = set()
    for match in COMPILE.finditer(text):
        compiled |= expand(base, match.group(1), sources)
    for match in REMOVE.finditer(text):
        compiled -= expand(base, match.group(1), sources)

    if not compiled:
        print("resolved no source files for %s - the check would pass by seeing nothing" % PROJECT)
        return 1

    found = {}
    for path in sorted(compiled):
        body = Path(path).read_text(encoding="utf-8-sig", errors="replace")
        for match in DLLIMPORT.finditer(body):
            found.setdefault(resolve(match.group(1), body), set()).add(path)

    expected = {}
    for line in BASELINE.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if line and not line.startswith("#"):
            name, _, verdict = line.partition("  ")
            expected[name.strip()] = verdict.strip()

    added = sorted(set(found) - set(expected))
    gone = sorted(set(expected) - set(found))

    for name in added:
        print("NEW native dependency: %s" % name)
        for path in sorted(found[name]):
            print("    %s" % path)
    for name in gone:
        print("GONE, so its verdict is stale: %s  (%s)" % (name, expected[name]))

    if added or gone:
        print("\n%s compiles a different set of native calls than %s records." % (PROJECT, BASELINE))
        print("Decide what each one does off Windows, then write the verdict in the baseline.")
        print("A P/Invoke that throws on Linux is invisible to every build in this repository.")
        return 1

    print("%d native dependencies in %s, all with a recorded verdict:" % (len(found), PROJECT.name))
    for name in sorted(found):
        print("  %-16s %s" % (name, expected[name]))
    return 0


if __name__ == "__main__":
    sys.exit(main())
