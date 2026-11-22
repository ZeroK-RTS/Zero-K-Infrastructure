#!/usr/bin/env bash
# Builds the zk-lobby image: ZkLobbyServer.Standalone compiled under mono, in a mono runtime.
#
#     ./tools/lobby-image.sh
#
# Shared by tools/lobby-container.sh and tools/stack.sh, for the same reason tools/view-set.sh
# is shared - two implementations of the same build drift, and the one that drifts is the one
# nobody runs.
#
# It builds through tools/build-website.sh rather than a Dockerfile of its own, so there is a
# single implementation of "compile this .NET Framework project under mono", including the
# MissionUpdater stub that build has to apply.
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."

IMAGE="${1:-zk-lobby}"
BUILD="${ZK_BUILD_DIR:-$HOME/.cache/zk-website-build}"
CONTEXT="${TMPDIR:-/tmp}/zk-lobby-context"

./tools/build-website.sh ZkLobbyServer.Standalone/ZkLobbyServer.Standalone.csproj >/dev/null

OUT="$BUILD/ZkLobbyServer.Standalone/bin/x64/Debug/net48"
[ -f "$OUT/ZkLobbyServer.Standalone.exe" ] || { echo "no build output at $OUT" >&2; exit 2; }

rm -rf "$CONTEXT" && mkdir -p "$CONTEXT/app"
cp -r "$OUT/." "$CONTEXT/app/"

# LoginChecker reads this to place logins by country, from whatever directory it is given as a
# site path. The website passes its own root, where the file lives; a container has to carry it.
cp Zero-K.info/GeoLite2-Country.mmdb "$CONTEXT/app/"

cat > "$CONTEXT/Dockerfile" <<'DOCKER'
# The lobby server, as its own process, on Linux. mono because ZkLobbyServer is .NET Framework
# 4.8 and EF6 - porting it is not Phase 4's job, running it off Windows is.
FROM mono:6.12
WORKDIR /app
COPY app/ ./
# Configuration is MiscVars in the shared database; the connection string is the one thing that
# has to arrive from outside, and GlobalConst already reads ZK_CONNECTION_STRING.
ENTRYPOINT ["mono", "ZkLobbyServer.Standalone.exe"]
DOCKER

docker build -q -t "$IMAGE" "$CONTEXT" >/dev/null
