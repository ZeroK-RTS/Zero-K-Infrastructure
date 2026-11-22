#!/usr/bin/env bash
# Compile-checks the website on Linux, with no .NET Framework and no Visual Studio.
#
#   ./tools/build-website.sh                              # Zero-K.info alone
#   ./tools/build-website.sh tools/mono-buildable.proj     # every project mono can build
#   ./tools/build-website.sh Fixer/Fixer.csproj            # one project
#
# Builds with msbuild under mono in Docker. It is the only way to know whether a change
# compiles without a Windows machine, and it is what every modernization change in this
# repository has been checked with. The argument is any msbuild project; CI passes
# tools/mono-buildable.proj, which names the sixteen of the solution's twenty projects
# that mono can build - Zero-K.sln itself cannot be used, because the other four are WPF.
#
# Works on a copy of the tracked files, so the repository never collects root-owned obj/
# and packages/ directories from the container.
#
# What it CANNOT do, and why the Windows build still matters:
#   * Razor views are not compiled. mono ships no aspnet_compiler.exe (MSB6004), so a
#     mistake in a .cshtml file gets through this check untouched.
#   * It does not run anything. Running the site needs Windows, IIS Express and a database.
#   * It does not publish, so on its own it cannot catch a project item that names a file
#     which is not there. The guard below does that part, because the Windows build does.
set -euo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
WORK="${ZK_BUILD_DIR:-$HOME/.cache/zk-website-build}"
CACHE="${NUGET_CACHE:-$HOME/.cache/zk-mono-nuget}"
PROJECT="${1:-Zero-K.info/asp.net.csproj}"
mkdir -p "$CACHE"

# The container writes obj/ as root, so a plain rm -rf fails with permission denied.
if [ -d "$WORK" ]; then
    docker run --rm -v "$(dirname "$WORK")":/w alpine rm -rf "/w/$(basename "$WORK")"
fi
mkdir -p "$WORK"
git -C "$REPO" ls-files -z | tar -C "$REPO" --null -T - -cf - | tar -C "$WORK" -xf -

# Every file the projects name is actually there.
#
# msbuild compiles what it is given and says nothing about a <Content> item whose file is
# missing; the Windows build only fails later, in the publish step that copies them:
#
#     Microsoft.Web.Publishing.targets(3074,5): error : Copying file Views\...\X.cshtml failed
#
# Deleting a dead view and forgetting its csproj line passed here and failed there. This runs
# against the tracked copy, which is what CI checks out, so a file that is untracked but
# present on this machine cannot hide the same mistake.
#
# When the argument is tools/mono-buildable.proj it checks each project that file lists, not
# the .proj itself - which names no files of its own, so checking it alone would check nothing
# and pass. That is not hypothetical: moving CI onto the .proj did exactly that for one commit,
# and took a 314-item guard down to 0 without failing.
python3 - "$WORK" "$PROJECT" <<'GUARDEOF'
import os, re, sys

work, project = sys.argv[1], sys.argv[2]

ITEM = re.compile(r'<(Content|None|Compile|EmbeddedResource)\s+Include="([^"*$]+)"')
BUILDABLE = re.compile(r'<Buildable\s+Include="([^"]+)"')
SDK = re.compile(r'<Project[^>]*Sdk\s*=\s*"', re.IGNORECASE)


def read(path):
    return open(path, encoding='utf-8-sig').read()


# Resolved one component at a time and compared without case, because NTFS is without case and
# these projects were written on it: <Content Include="Img\zk_logo.png_org" /> names
# img/zk_logo.png_org and is correct. A case-sensitive check reports files Windows finds
# perfectly well. Component by component rather than against one set of every file under the
# project, because an Include may reach outside it with ..\, and a flat set cannot express that.
listings = {}


def exists(base, relative):
    current = base
    for part in relative.replace('\\', os.sep).split(os.sep):
        if part in ('', '.'):
            continue
        if part == '..':
            current = os.path.dirname(current)
            continue
        if current not in listings:
            try:
                listings[current] = os.listdir(current)
            except OSError:
                listings[current] = []
        found = next((e for e in listings[current] if e.lower() == part.lower()), None)
        if found is None:
            return False
        current = os.path.join(current, found)
    return True


# A .proj that lists projects is checked through to them; anything else is checked as itself.
source = read(os.path.join(work, project))
listed = BUILDABLE.findall(source)
if listed:
    root = os.path.dirname(project)
    projects = [os.path.normpath(os.path.join(root, name.replace('\\', os.sep))) for name in listed]
else:
    projects = [project]

missing, checked, expected = [], 0, False
for name in projects:
    base = os.path.dirname(os.path.join(work, name))
    text = read(os.path.join(work, name))
    # An SDK-style project globs its own sources and names no items, so zero here is correct
    # for it - ZkLobbyServer.Standalone and Tests are both like that. An old-style project
    # listing nothing means the file was not read the way this guard thinks it was.
    expected = expected or not SDK.search(text)
    for item, include in ITEM.findall(text):
        checked += 1
        if not exists(base, include):
            missing.append('%s: %s: %s' % (name, item, include))

if expected and not checked:
    print('%s: the item guard checked nothing, and at least one of these projects lists its'
          ' files explicitly, so it should have checked something.' % project)
    sys.exit(1)

if missing:
    print('%d item(s) across %d project(s) name a file that is not in the repository:'
          % (len(missing), len(projects)))
    for line in missing:
        print('  ' + line)
    print('')
    print('The Windows build fails on these in its publish step. Remove the item or restore the file.')
    sys.exit(1)

print('item guard: %d item(s) across %d project(s), all present' % (checked, len(projects)))
GUARDEOF

# One workaround, applied to the copy only. ZkData.MissionUpdater.UpdateMission uses
# ZipFile.Open, which trips a System.IO.Compression facade version conflict under mono -
# reproducible on unmodified master, so it is an artefact of the container rather than of
# any change. Both overloads are stubbed; their signatures are kept so callers still
# resolve.
python3 - "$WORK" <<'PYEOF'
import sys, os, re
path = os.path.join(sys.argv[1], "ZkData/MissionService/MissionUpdater.cs")
source = open(path, encoding="utf-8-sig").read()
position, stubbed = 0, 0
while True:
    match = re.compile(r"public void UpdateMission\([^)]*\)\s*\{").search(source, position)
    if not match:
        break
    start, depth, index = match.end() - 1, 0, match.end() - 1
    while index < len(source):
        if source[index] == "{":
            depth += 1
        elif source[index] == "}":
            depth -= 1
            if depth == 0:
                break
        index += 1
    head = source[match.start():start]
    body = '{ throw new System.NotImplementedException("stubbed for the mono build"); }'
    source = source[:match.start()] + head + body + source[index + 1:]
    position = match.start() + len(head) + len(body)
    stubbed += 1
open(path, "w", encoding="utf-8").write(source)
print("stubbed %d UpdateMission overload(s)" % stubbed)
PYEOF

# Platform=x64 is required: asp.net.csproj defines only Debug|x64 and Release|x64, and
# without it msbuild fails with "the OutputPath property is not set".
# DebugType=none because mono cannot write Windows PDBs.
docker run --rm -v "$WORK":/src -w /src -v "$CACHE":/root/.nuget mono:6.12 bash -c "
    nuget restore Zero-K.sln >/dev/null 2>&1 || nuget restore Zero-K.sln
    msbuild '$PROJECT' /p:Configuration=Debug /p:Platform=x64 /p:DebugType=none /v:minimal /nologo
"
