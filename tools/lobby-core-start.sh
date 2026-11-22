#!/usr/bin/env bash
# Does the .NET 9 lobby server actually START, and does it open the port players connect to?
#
#     ./tools/lobby-core-start.sh
#
# ZkLobbyServer.Core proves the chain compiles. That is not the same claim. This runs the program
# against a real database and checks three things a build cannot see:
#
#   1. it refuses when no standalone server is configured - the website would start its own
#   2. it refuses when configured but handed no site directory, naming the GeoIP file it wants
#   3. configured and handed the site directory, it comes up and LISTENS ON BOTH PORTS
#
# (3) is here because of a defect that only running found. TcpTransportServerListener.Bind
# P/Invoked SetHandleInformation out of kernel32; off Windows that throws DllNotFoundException,
# Bind caught it, retried 120 times and gave up - and the server came up with its API listening
# and no player port at all. Nothing was wrong in any log at a glance. Checking that the process
# is alive, or that the API answers, would both have passed. Only the player port shows it.
#
# It costs a couple of minutes: a Whole History Rating pass runs before the listener opens.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."

DB_NAME="${DB_NAME:-zk_test}"
export ZK_CONNECTION_STRING="${ZK_CONNECTION_STRING:-$(DB_NAME="$DB_NAME" ./db/connection-string.sh)}"

# 8300 because 8200 is GlobalConst.LobbyServerPort, the port the player listener is about to ask
# for. This script worked around that locally and left the shared default alone, so stack.sh and
# lobby-container.sh went on colliding; the default is 8300 now and this is simply agreeing.
API_PORT=8300
PLAYER_PORT=8200
LOG="$(mktemp)"
PROBE_USER="${ZK_PROBE_USER:-LobbyProbe}"
PROBE_PASS="${ZK_PROBE_PASS:-probe-pass-not-a-real-one}"
CONTAINER=zk-lobby-core
BOOT_TIMEOUT="${ZK_LOBBY_BOOT_TIMEOUT:-360}"
# How long a port is given to appear AFTER the server says it is up. Seconds, not minutes: this
# is the gap between two listeners in the same process, not a boot.
PORT_TIMEOUT="${ZK_LOBBY_PORT_TIMEOUT:-30}"

cleanup() { docker rm -f "$CONTAINER" >/dev/null 2>&1 || true; ./tools/lobby-config.sh clear >/dev/null 2>&1 || true; rm -f "$LOG"; }
trap cleanup EXIT

failures=0
check() { if [ "$1" = "0" ]; then echo "   ok    $2"; else echo "   FAIL  $2"; failures=$((failures+1)); fi; }

echo "the .NET 9 lobby server, starting:"

./tools/lobby-config.sh clear >/dev/null 2>&1 || true
out="$(./tools/dotnet.sh run --project ZkLobbyServer.Standalone.Core 2>&1 || true)"
grep -q "LobbyApiUrl MiscVar is not set" <<<"$out" \
    && check 0 "refuses when no standalone server is configured" \
    || check 1 "refuses when no standalone server is configured"

./tools/lobby-config.sh set "$API_PORT" >/dev/null 2>&1
out="$(./tools/dotnet.sh run --project ZkLobbyServer.Standalone.Core 2>&1 || true)"
grep -q "GeoLite2-Country.mmdb" <<<"$out" \
    && check 0 "gets past configuration and asks for the GeoIP database" \
    || check 1 "gets past configuration and asks for the GeoIP database"

# The real start. Built first, so the build's own minutes do not eat the boot timeout.
./tools/dotnet.sh build ZkLobbyServer.Standalone.Core -v q --nologo >/dev/null
./tools/dotnet.sh build tools/lobby-client-probe -v q --nologo >/dev/null
docker rm -f "$CONTAINER" >/dev/null 2>&1 || true
ZK_DOTNET_NAME="$CONTAINER" ./tools/dotnet.sh run --project ZkLobbyServer.Standalone.Core --no-build -- /repo/Zero-K.info >"$LOG" 2>&1 &

listening=1
for _ in $(seq 1 "$BOOT_TIMEOUT"); do
    if grep -q "Listening at port $PLAYER_PORT" "$LOG" 2>/dev/null; then listening=0; break; fi
    if ! docker inspect "$CONTAINER" >/dev/null 2>&1 && [ -s "$LOG" ] && ! grep -q Listening "$LOG"; then break; fi
    sleep 1
done
check "$listening" "starts, and says it is listening on the player port"

# Opening a TCP connection rather than reading a socket table: it is the claim that matters
# ("a client can reach it"), and it does not need iproute2 to be installed on the runner.
connects() { timeout 5 bash -c "exec 3<>/dev/tcp/127.0.0.1/$1" 2>/dev/null; }

# Given a bounded chance rather than asked once, and the API port is why. The loop above waits
# for "Listening at port $PLAYER_PORT", which is the PLAYER listener saying it is up - the API
# port is bound by a different server in the same process and that log line promises nothing
# about it. Both were then tested in the same instant, which made this a race the runner could
# lose, and on 2026-10-03 it did: the player port connected, the API port was refused, and the
# twelve checks after it all passed against the server that had just been reported unreachable.
#
# Still a real connection, still a real failure if nothing ever binds - it just stops being a
# question about which listener got there first.
reachable() {
    for _ in $(seq 1 "$PORT_TIMEOUT"); do
        connects "$1" && return 0
        sleep 1
    done
    return 1
}

reachable "$PLAYER_PORT" \
    && check 0 "a client can connect to the player port ($PLAYER_PORT)" \
    || check 1 "a client can connect to the player port ($PLAYER_PORT)"
reachable "$API_PORT" \
    && check 0 "a client can connect to the API port ($API_PORT)" \
    || check 1 "a client can connect to the API port ($API_PORT)"
# The handshake. Opening a port is not serving anybody: this drives LobbyClient's TasClient - the
# same type the real lobby uses - through a register, a login, a channel join and a message. The
# register writes an Account through EF Core, the join reads one back through it, and the message
# comes back from the server, which is the first thing here that shows traffic in both directions.
if [ "$listening" = "0" ]; then
    # errexit off around the pipeline: pipefail makes a failing probe abort the script before
    # check() can report it, which is how a real failure once printed no verdict at all.
    set +e
    ./tools/dotnet.sh run --project tools/lobby-client-probe --no-build -- \
        127.0.0.1 "$PLAYER_PORT" "$PROBE_USER" "$PROBE_PASS" 2>&1 | sed 's/^/  /'
    probe_status="${PIPESTATUS[0]}"
    set -e
    check "$probe_status" "a client registers, logs in, chats, and opens a battle"
else
    check 1 "a client registers, logs in, chats, and opens a battle (skipped: never listened)"
fi

grep -qiE 'kernel32|DllNotFoundException' "$LOG" \
    && check 1 "no Windows-only P/Invoke on the way up" \
    || check 0 "no Windows-only P/Invoke on the way up"

if [ "$listening" != "0" ]; then echo; echo "--- last 30 lines ---"; tail -30 "$LOG"; fi

echo
if [ "$failures" -ne 0 ]; then echo "$failures check(s) failed"; exit 1; fi
echo "the lobby server runs on .NET 9, on Linux, up to the point a game would start"
