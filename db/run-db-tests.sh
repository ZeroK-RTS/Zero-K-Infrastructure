#!/usr/bin/env bash
# Builds and runs Tests.Database - the WHR pipeline tests - against the local container.
#
#   ./db/run-db-tests.sh              # all of them
#   ./db/run-db-tests.sh Predicted    # only tests whose name contains "Predicted"
#
# Expects the container up, a schema, and the fixture loaded:
#   docker compose -f db/docker-compose.yml up -d && ./db/wait-for-db.sh
#   DB_NAME=zk_test ./db/dbsetup.sh latest      # (ZK_CONNECTION_STRING form, see below)
#   DB_NAME=zk_test ./db/load-fixture.sh
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO="$(cd "$HERE/.." && pwd)"
WORK="${ZK_TEST_BUILD_DIR:-$HOME/.cache/zk-dbtests}"
CACHE="${NUGET_CACHE:-$HOME/.cache/zk-mono-nuget}"
DB="${DB_NAME:-zk_test}"
PASS="${MSSQL_SA_PASSWORD:-ZkLocal!Dev2026}"
CS="${ZK_CONNECTION_STRING:-Data Source=127.0.0.1,14330;Initial Catalog=${DB};User ID=sa;Password=${PASS};MultipleActiveResultSets=true;TrustServerCertificate=true}"
mkdir -p "$CACHE"

# Everything Tests.Database compiles, transitively, is watched - and the list is the whole point.
# ZkLobbyServer was missing once, because Tests.Database LINKS the lobby API transport out of it,
# and the omission was silent in the worst way: a positive control that deliberately broke the
# client still passed, because the stale binary did not contain the break.
#
# Shared/ was missing for the same reason and found the same way: PlasmaShared and LobbyClient are
# project references, so a change to GlobalConst or MetaDataCache rebuilt nothing and a control
# that reverted a fix in one of them passed against a binary that still had the fix in it.
if [ ! -f "$WORK/Tests.Database/bin/x64/Debug/net48/Tests.Database.exe" ] \
   || [ -n "$(find "$REPO/Tests.Database" "$REPO/ZkData" "$REPO/ZkLobbyServer" "$REPO/Shared" -newer "$WORK/Tests.Database/bin/x64/Debug/net48/Tests.Database.exe" -name '*.cs' -print -quit 2>/dev/null)" ]; then
    echo "building Tests.Database..."
    [ -d "$WORK" ] && docker run --rm -v "$(dirname "$WORK")":/w alpine rm -rf "/w/$(basename "$WORK")"
    mkdir -p "$WORK"
    git -C "$REPO" ls-files -z | tar -C "$REPO" --null -T - -cf - | tar -C "$WORK" -xf -

    # Same mono workaround as db/dbsetup.sh: one UpdateMission overload uses ZipFile.Open,
    # which trips a System.IO.Compression facade conflict. Nothing under test calls it.
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
        nuget restore Zero-K.sln >/dev/null 2>&1 || true
        msbuild Tests.Database/Tests.Database.csproj /t:Restore /v:quiet /nologo
        msbuild Tests.Database/Tests.Database.csproj /p:Configuration=Debug /p:Platform=x64 \
            /v:minimal /nologo' | grep -E ': error|-> ' || true
fi

EXE_DIR="$WORK/Tests.Database/bin/x64/Debug/net48"
[ -f "$EXE_DIR/Tests.Database.exe" ] || { echo "build produced no executable" >&2; exit 1; }

docker run --rm --network host -v "$WORK":/src -w "/src/Tests.Database/bin/x64/Debug/net48" \
    -e ZK_CONNECTION_STRING="$CS" mono:6.12 mono Tests.Database.exe "$@"
