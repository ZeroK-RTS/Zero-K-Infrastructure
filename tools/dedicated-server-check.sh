#!/usr/bin/env bash
# Does the ported code start a real Spring game server?
#
#     ./tools/dedicated-server-check.sh
#
# tools/lobby-core-start.sh takes the lobby server as far as OpenBattle, which announces a battle
# without starting anything. This is the step after: DedicatedServer.HostGame - the method
# ZkLobbyServer calls when a battle actually starts - generating a script, resolving the binary
# through SpringPaths, and launching spring-dedicated.
#
# What it needs, and why so little: an engine (~42MB) and a map (~333KB), both from the projects'
# own distribution via tools/fetch-engine.sh, plus a six-line game archive written here. A Spring
# DEDICATED server relays the game rather than simulating it, but it still resolves and hashes both
# archives, so a game is required - just not a real one. That is what keeps this out of
# multi-gigabyte territory.
#
# Skipped, not failed, when no engine is present: CI has none.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."

VERSION="${ZK_ENGINE_VERSION:-105.1.1-2457-g8095d30}"
MAP="${ZK_PROBE_MAP:-Bluescreen fields v2}"
GAME="Probe Game 1.0"
PORT="${ZK_PROBE_GAME_PORT:-8452}"

# Skipping is right on a laptop with no engine, no curl, or no database: this is the only check
# that drives a real engine, and demanding one would stop anyone running the rest. It is wrong in
# CI, where all three are provided and a skip means the only coverage the game chain has quietly
# stopped running while the job stayed green. ZK_REQUIRE_FULL_RUN=1, which the workflow sets,
# turns every skip here into a failure. Unset it and the tolerant behaviour comes back.
skip() {
    if [ "${ZK_REQUIRE_FULL_RUN:-}" = "1" ]; then
        echo "$1" >&2
        echo "ZK_REQUIRE_FULL_RUN=1: this run was meant to be complete, so a skip is a failure." >&2
        exit 1
    fi
    echo "skipped: $1"
    exit 0
}

if ! command -v curl >/dev/null || ! command -v unzip >/dev/null; then
    skip "curl and unzip are needed to fetch an engine"
fi

ENGINE="$(./tools/fetch-engine.sh)" || skip "no engine could be fetched"

# SpringPaths wants <writable>/engine/<platform>/<version>/, with `spring` and the done.txt marker
# EngineDownload writes. Built here rather than in the cache so the cache stays a plain unpack.
WRITABLE="${ZK_SPRING_WRITABLE:-$HOME/.cache/zk-spring}"
TARGET="$WRITABLE/engine/linux64/$VERSION"
if [ ! -f "$TARGET/done.txt" ]; then
    echo "laying out the engine at $TARGET" >&2
    mkdir -p "$TARGET"
    cp -r "$ENGINE/." "$TARGET/"
    # The archive ships its binaries 0664 - see Tests.Portable/UnixPermissionsTests. Production
    # fixes that with EngineDownload.FixPermissions; here the layout step does it.
    chmod +x "$TARGET/spring" "$TARGET/spring-dedicated" "$TARGET/spring-headless" 2>/dev/null || true
    echo "Engine download completed" > "$TARGET/done.txt"
fi

# The minimal game. A dedicated server resolves and hashes it but never simulates with it.
mkdir -p "$TARGET/games/probegame.sdd"
cat > "$TARGET/games/probegame.sdd/modinfo.lua" <<'LUA'
return {
	name        = "Probe Game",
	shortname   = "PROBE",
	game        = "Probe Game",
	version     = "1.0",
	mutator     = "official",
	description = "Minimal game archive: a dedicated server needs one to resolve, not to play.",
	modtype     = 1,
}
LUA
mkdir -p "$TARGET/maps"
cp -n "$ENGINE/maps/"*.sd7 "$TARGET/maps/" 2>/dev/null || true

echo "the ported code, hosting a game:"
./tools/dotnet.sh build tools/dedicated-probe -v q --nologo >/dev/null
# RW: the engine makes a cache and a demos directory, and will not start without them.
# ZK_BATTLE_CONTEXT_OUT: where to leave the finished battle for tools/battle-result-probe, so the
# storing end can be fed a context a real engine produced rather than one a test wrote. It lands in
# the mounted directory because that is what both sides can see.
ZK_DOTNET_MOUNT="$WRITABLE" ZK_DOTNET_MOUNT_RW=1 ZK_BATTLE_CONTEXT_OUT=/mnt/extra/battle-context.json \
    ./tools/dotnet.sh run --project tools/dedicated-probe --no-build -- \
    /mnt/extra "$VERSION" "$MAP" "$GAME" "$PORT"

echo
echo "and that context, stored:"
if [ -n "${ZK_CONNECTION_STRING:-}" ] || [ -f db/connection-string.sh ]; then
    export ZK_CONNECTION_STRING="${ZK_CONNECTION_STRING:-$(DB_NAME="${DB_NAME:-zk_test}" ./db/connection-string.sh)}"
    ./tools/dotnet.sh build tools/battle-result-probe -v q --nologo >/dev/null
    ZK_DOTNET_MOUNT="$WRITABLE" ZK_BATTLE_CONTEXT_IN=/mnt/extra/battle-context.json \
        ./tools/dotnet.sh run --project tools/battle-result-probe --no-build
else
    skip "no database to store it in"
fi
