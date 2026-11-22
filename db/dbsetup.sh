#!/usr/bin/env bash
# Builds and runs db/DbSetup, the EF6 migration runner, under mono in Docker.
#
#   ./db/dbsetup.sh status                                       # applied vs pending
#   ./db/dbsetup.sh latest                                       # migrate up to newest
#   ./db/dbsetup.sh to 202404060935038_AddPopularMapsFraction    # migrate up or DOWN
#   ./db/dbsetup.sh list                                         # recent migrations
#
# Works on a copy of the tracked files, so the repository never collects root-owned
# obj/ and packages/ directories from the container.
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO="$(cd "$HERE/.." && pwd)"
WORK="${ZK_BUILD_DIR:-$HOME/.cache/zk-dbsetup}"
CACHE="${NUGET_CACHE:-$HOME/.cache/zk-mono-nuget}"
CS="${ZK_CONNECTION_STRING:-$("$HERE/connection-string.sh")}"
mkdir -p "$CACHE"

# Refresh the copy only when something tracked has changed since the last build.
if [ ! -f "$WORK/db/DbSetup/bin/DbSetup.exe" ] || [ -n "$(find "$REPO/db/DbSetup" "$REPO/ZkData" -newer "$WORK/db/DbSetup/bin/DbSetup.exe" -name '*.cs' -print -quit 2>/dev/null)" ]; then
    echo "building DbSetup..."
    [ -d "$WORK" ] && docker run --rm -v "$(dirname "$WORK")":/w alpine rm -rf "/w/$(basename "$WORK")"
    mkdir -p "$WORK"
    git -C "$REPO" ls-files -z | tar -C "$REPO" --null -T - -cf - | tar -C "$WORK" -xf -

    # One UpdateMission overload uses ZipFile.Open, which trips a System.IO.Compression
    # facade version conflict under mono. Stubbed in the copy only - DbSetup never calls it.
    python3 - "$WORK" <<'PYEOF'
import sys, os, re
p = os.path.join(sys.argv[1], "ZkData/MissionService/MissionUpdater.cs")
s = open(p, encoding="utf-8-sig").read()
pos = 0
while True:
    m = re.compile(r"public void UpdateMission\([^)]*\)\s*\{").search(s, pos)
    if not m:
        break
    j, d, k = m.end() - 1, 0, m.end() - 1
    while k < len(s):
        if s[k] == "{": d += 1
        elif s[k] == "}":
            d -= 1
            if d == 0: break
        k += 1
    head = s[m.start():j]
    body = '{ throw new System.NotImplementedException("stubbed for mono build"); }'
    s = s[:m.start()] + head + body + s[k+1:]
    pos = m.start() + len(head) + len(body)
open(p, "w", encoding="utf-8").write(s)
PYEOF
    docker run --rm -v "$WORK":/src -w /src -v "$CACHE":/root/.nuget mono:6.12 bash -c '
        nuget restore Zero-K.sln >/dev/null 2>&1 || nuget restore Zero-K.sln
        msbuild db/DbSetup/DbSetup.csproj /p:Configuration=Debug /p:Platform=x64 \
            /p:DebugType=none /v:minimal /nologo' | grep -E ': error|-> ' || true
fi

docker run --rm --network host -v "$WORK":/src -w /src/db/DbSetup/bin \
    -e ZK_CONNECTION_STRING="$CS" mono:6.12 mono DbSetup.exe "$@"
