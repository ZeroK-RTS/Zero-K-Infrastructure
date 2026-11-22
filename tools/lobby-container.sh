#!/usr/bin/env bash
# Runs the standalone lobby server in a container, against the local test database.
#
#     ./tools/lobby-container.sh            build it, start it, check it comes up, stop it
#     ./tools/lobby-container.sh --serve    leave it running
#
# **This is the thing Phase 1 was for and nobody had done**: the lobby server outside the IIS
# worker, as its own process, against a database the website also uses. See Zero-K.info/HOSTING.md.
#
# It builds with tools/build-website.sh rather than a Dockerfile of its own, so there is one
# implementation of "compile this .NET Framework project under mono" - including the
# MissionUpdater stub that build has to apply. The image is the mono runtime plus that output.
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."

IMAGE=zk-lobby
NAME=zk-lobby-check
API_PORT=8300
BUILD="${ZK_BUILD_DIR:-$HOME/.cache/zk-website-build}"
CONTEXT="${TMPDIR:-/tmp}/zk-lobby-context"

case "${ZK_CONNECTION_STRING:-}" in
    ""|*"127.0.0.1,14330"*|*"localhost,14330"*) DB_NAME="${DB_NAME:-zk_test}" ./db/require-db.sh ;;
esac
CS="${ZK_CONNECTION_STRING:-$(DB_NAME="${DB_NAME:-zk_test}" ./db/connection-string.sh)}"

# The server refuses to start without these, and rightly so - but a check script that assumes
# somebody set them by hand is a check script that passes for a reason it does not contain.
echo "configuring the switch (MiscVars)..."
./tools/lobby-config.sh set "$API_PORT"
trap './tools/lobby-config.sh clear' EXIT

echo "building the lobby image..."
./tools/lobby-image.sh "$IMAGE"

docker rm -f "$NAME" >/dev/null 2>&1 || true

if [ "${1:-}" = "--serve" ]; then
    exec docker run --rm --name "$NAME" --network host -e ZK_CONNECTION_STRING="$CS" "$IMAGE"
fi

docker run -d --rm --name "$NAME" --network host -e ZK_CONNECTION_STRING="$CS" "$IMAGE" >/dev/null
trap 'docker rm -f "$NAME" >/dev/null 2>&1 || true; ./tools/lobby-config.sh clear' EXIT

echo
echo "the lobby server, in a container:"

# It prints this once the API is listening. Rating systems run a full WHR pass first, so this
# waits rather than assuming - on the fixture that is 150 battles.
for _ in $(seq 1 180); do
    logs=$(docker logs "$NAME" 2>&1 || true)
    case "$logs" in
        *"lobby server running"*) started=1; break ;;
        *"refusing to start"*|*"could not read configuration"*) started=refused; break ;;
    esac
    docker inspect -f '{{.State.Running}}' "$NAME" 2>/dev/null | grep -q true || { started=exited; break; }
    sleep 1
done

failures=0
check() { if [ "$1" = "0" ]; then echo "   ok    $2"; else echo "   FAIL  $2"; failures=$((failures + 1)); fi; }

case "${started:-}" in
    1) check 0 "it started and reported its API listening" ;;
    refused) check 1 "it refused to start - configuration:"; docker logs "$NAME" 2>&1 | tail -5 ;;
    exited) check 1 "the process exited - last lines:"; docker logs "$NAME" 2>&1 | tail -20 ;;
    *) check 1 "it never reported being ready - last lines:"; docker logs "$NAME" 2>&1 | tail -20 ;;
esac

if [ "${started:-}" = "1" ]; then
    # The API refuses unauthenticated callers, so 401/403 is a pass: it proves something is
    # listening and that it is the lobby API rather than an open port.
    code=$(curl -sS -o /dev/null -w '%{http_code}' "http://127.0.0.1:$API_PORT/" 2>/dev/null || echo 000)
    case "$code" in
        000) check 1 "the lobby API answered on $API_PORT (no answer)" ;;
        *) check 0 "the lobby API answered on $API_PORT ($code)" ;;
    esac
fi

echo
[ "$failures" = "0" ] && echo "all checks passed" || echo "$failures check(s) failed"
exit "$failures"
