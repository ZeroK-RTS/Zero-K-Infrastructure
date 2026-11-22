#!/usr/bin/env bash
# The website and the lobby server, as two processes, against one database.
#
#     ./tools/stack.sh            bring both up, check they are talking, tear down
#     ./tools/stack.sh --serve    bring both up and leave them running
#
# **This is what Phase 1 was for.** The website used to start the lobby server inside its own
# IIS worker, so every deploy disconnected every player. Here they are separate containers:
# the ported site on .NET 9, the lobby server on mono, sharing the database and speaking over
# the HTTP+JSON transport in ZkLobbyServer/Api.
#
# The switch is one MiscVar. LobbyApiUrl set means the website talks to a server somewhere
# else; unset means it starts one of its own, which is what the live site still does.
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."

SITE_PORT=5200
# A second site, started for one check: the same image with the harness endpoints left off.
LOCKED_PORT=5201
# Where this run records the views it rendered - see the note by the harness container below.
VIEWS_DIR="$(mktemp -d)"
# Named under the pattern .gitignore ALREADY carries (rendered-views.tmp*) rather than adding
# one. It outlives the run that writes it on purpose, despite the prefix - tools/rendered-views.sh
# reads it later and does not start a stack of its own.
STACK_VIEWS=rendered-views.tmp-stack
API_PORT=8300
SECRET="local-dev-secret-not-a-real-one"
DB_NAME="${DB_NAME:-zk_test}"

DB_NAME="$DB_NAME" ./db/require-db.sh
CS="${ZK_CONNECTION_STRING:-$(DB_NAME="$DB_NAME" ./db/connection-string.sh)}"

echo "configuring the switch: LobbyApiUrl -> http://127.0.0.1:$API_PORT/"
./tools/lobby-config.sh set "$API_PORT"

echo "building both images..."
./tools/lobby-image.sh zk-lobby
docker build -q -t zk-site . >/dev/null

docker rm -f zk-lobby-up zk-site-up zk-site-locked >/dev/null 2>&1 || true
harness_log="$(mktemp)"
# Served by the site container as /Resources/*, which is where the lobby server looks for map
# metadata when it cannot read the website's disk - and out of process it never can.
RESOURCES="$(mktemp -d)"
probe_log="$(mktemp)"
control_log="$(mktemp)"
# GlobalConst.LobbyServerPort in Local mode - the port players connect to, not the API port.
PLAYER_PORT=8200
PROBE_PASS="${ZK_PROBE_PASS:-probe-pass-not-a-real-one}"
MAP=test_map_1
MAP_OPTION=stackprobeopt
# In the fixture like $MAP, and deliberately left out of $RESOURCES.
UNSERVED_MAP=test_map_2
cleanup() {
    docker rm -f zk-lobby-up zk-site-up zk-site-locked >/dev/null 2>&1 || true
    docker run --rm --network host -e ZK_CONNECTION_STRING="$CS" \
        --entrypoint dotnet zk-site bin/ZeroKWeb.Host.dll --remove-planetwars-round >/dev/null 2>&1 || true
    rm -f "$harness_log"
    rm -rf "$RESOURCES"
    rm -rf "$VIEWS_DIR"
    rm -f "$probe_log" "$control_log"
    [ "${1:-}" = "keep-config" ] || ./tools/lobby-config.sh clear
}
trap 'cleanup' EXIT

# Before the lobby server, not after. PlanetWarsMatchMaker reads the default galaxy and the
# faction list in its CONSTRUCTOR and gives up on both if there is no galaxy yet, and MiscVar
# caches per process - so a round seeded after it starts is a round it never hears about, and
# the symptom is nothing rather than an error.
echo "publishing one map's metadata from the site..."
./tools/stack-metadata/make-map-metadata.py "$MAP" "$MAP_OPTION" \
    "$RESOURCES/$MAP.metadata.xml.gz" | sed 's/^/   /'

echo "seeding a PlanetWars round..."
docker run --rm --network host -e ZK_CONNECTION_STRING="$CS" \
    --entrypoint dotnet zk-site bin/ZeroKWeb.Host.dll --seed-planetwars-round | sed 's/^/   /'

echo "starting the lobby server..."
# No --rm: a container that removes itself takes its log with it the instant it exits, and
# this one exits precisely when something is worth reading. The cleanup trap below force-
# removes it either way.
# ZK_BASE_SITE_URL, because the mode's BaseSiteUrl is localhost:44301 - the Local mode's address
# for a site that is not there. Without it the lobby server would ask the wrong host for metadata.
docker run -d --name zk-lobby-up --network host -e ZK_CONNECTION_STRING="$CS" \
    -e ZK_BASE_SITE_URL="http://127.0.0.1:$SITE_PORT" zk-lobby >/dev/null
for _ in $(seq 1 240); do
    docker logs zk-lobby-up 2>&1 | grep -q "lobby server running" && { lobby=1; break; }
    docker inspect -f '{{.State.Running}}' zk-lobby-up 2>/dev/null | grep -q true || break
    sleep 1
done

# Why, while the container is still there to ask. The cleanup trap removes it, so a caller that
# waits until the end - CI did - finds nothing to read, and the failure is a bare "the lobby
# server is up" FAIL. It takes this server about ninety seconds of WHR before it says anything
# interesting, which made a port collision read like a timeout for as long as nobody looked.
if [ "${lobby:-}" != "1" ]; then
    echo
    # Errors first, then the tail. This server logs one line per WHR iteration at startup -
    # hundreds of them - so a plain tail shows nothing but "Running WHR iteration N" while the
    # exception that killed it scrolled past long before.
    echo "the lobby server did not come up. Anything it called an error:"
    docker logs zk-lobby-up 2>&1 | grep -iE "error|exception|refusing|fatal|at [A-Z]" \
        | tail -25 | sed 's/^/   /' || true
    echo
    echo "and its last 10 lines:"
    docker logs zk-lobby-up 2>&1 | tail -10 | sed 's/^/   /' || true
    echo
fi

echo "starting the website..."
# The resources directory is mounted rather than baked in: it is deployment content that
# AutoRegistrator writes, so a checkout has none and the image ships none.
# ZK_HARNESS_ENDPOINTS because the lobby probe asks /Harness/Whoami to prove that single
# sign-on left the player signed in. Those endpoints are OFF by default when serving - see the
# control below, which starts the same image without this line and shows that they are.
docker run -d --rm --name zk-site-up --network host \
    -v "$RESOURCES":/app/Zero-K.info/Resources:ro \
    -e ZK_HARNESS_ENDPOINTS=1 \
    -e ZK_CONNECTION_STRING="$CS" -e ZK_HOST_URLS="http://0.0.0.0:$SITE_PORT" zk-site >/dev/null
for _ in $(seq 1 120); do
    curl -fsS -o /dev/null "http://127.0.0.1:$SITE_PORT/Home/NotLoggedIn" 2>/dev/null && { site=1; break; }
    sleep 1
done

failures=0
check() { if [ "$1" = "0" ]; then echo "   ok    $2"; else echo "   FAIL  $2"; failures=$((failures + 1)); fi; }

echo
echo "two processes, one database:"
check "$([ "${lobby:-}" = "1" ] && echo 0 || echo 1)" "the lobby server is up and listening on $API_PORT"
check "$([ "${site:-}" = "1" ] && echo 0 || echo 1)" "the website is up and serving on $SITE_PORT"

if [ "${lobby:-}" = "1" ] && [ "${site:-}" = "1" ]; then
    # HomeController.Index asks the lobby server for ConnectedUserCount and the battle stats, so
    # the front page is the one that cannot render without it.
    code=$(curl -sS -o /dev/null -w '%{http_code}' "http://127.0.0.1:$SITE_PORT/" 2>/dev/null)
    check "$([ "$code" = "200" ] && echo 0 || echo 1)" "the front page, which asks the lobby server, answered ($code)"

    # The checks that need a lobby server, run by the site's own harness.
    #
    # The image's ENTRYPOINT is the harness binary and its CMD is --serve; with the CMD dropped it
    # runs the checks instead of serving. That is the whole trick: these are the SAME checks
    # tools/run-host.sh runs, and the only difference is that LobbyApiUrl is set here, so the ones
    # that skip themselves without a lobby server - Planetwars/PwMatchMaker.cshtml is the first -
    # run for real. It binds 127.0.0.1:5199 for its own requests, which is not $SITE_PORT, so the
    # container already serving above is left alone.
    echo "   ...  running the site's checks with a lobby server attached"
    # ZK_RENDERED_VIEWS, into a mounted directory, because this run reaches views the one
    # tools/rendered-views.sh measures cannot. That script runs the harness with NO lobby server -
    # deliberately, the site has to work without one - so a view whose ACTION asks Global.LobbyApi
    # before choosing a view is invisible to it and counts as "never rendered". Measured, two are:
    # Planetwars/PwMatchMaker and Tourney/TourneyIndex. Writing the list here lets that script say
    # so instead of the reader having to know.
    rm -rf "$VIEWS_DIR"; mkdir -p "$VIEWS_DIR"; chmod 777 "$VIEWS_DIR"
    if docker run --rm --network host -e ZK_CONNECTION_STRING="$CS" \
            -v "$VIEWS_DIR":/out -e ZK_RENDERED_VIEWS=/out/views.tmp \
            --entrypoint dotnet zk-site bin/ZeroKWeb.Host.dll > "$harness_log" 2>&1; then
        harness=0
    else
        harness=1
    fi
    # A hand-kept list of the lines worth showing, which means a check added later is run here and
    # invisible until its line is added - /Tourney was, for one commit. "rendered against a real
    # lobby server" is the shared wording, so a new lobby-dependent check that uses it shows up on
    # its own; the rest are named because they do not.
    grep -E "against a real lobby server|options loop|faction beside|Join form|needs a lobby server|needs a seeded round" \
        "$harness_log" | sed 's/^/     /' || true
    check "$harness" "the site's own checks pass with a lobby server attached"

    # Kept at a stable path for tools/rendered-views.sh, which annotates its "never rendered"
    # list from it. Removed by the cleanup trap otherwise, so a stale file cannot outlive the run
    # that produced it and quietly make the next measurement look better than it is.
    if [ -s "$VIEWS_DIR/views.tmp" ]; then
        sort -u "$VIEWS_DIR/views.tmp" > "$STACK_VIEWS"
        echo "   ...  $(wc -l < "$STACK_VIEWS") views rendered here, recorded in $STACK_VIEWS"
    fi
    [ "$harness" = "0" ] || grep -E "^\s+(FAIL|note)" "$harness_log" | head -20

    # THE THING THE TWO HALVES CANNOT DO SEPARATELY: the lobby server reading a file that only
    # the website has.
    #
    # ServerBattle asks for the map's metadata every time a battle opens. That used to be a local
    # file read, which worked only because the lobby server ran inside the website's IIS worker
    # and shared its disk. Here they are different containers, so the only way is the URL the
    # website already publishes - and !listmapoptions is where the answer becomes visible: it
    # says "this map has no map options" when the lookup came back empty, which is exactly what a
    # player saw before this worked.
    code=$(curl -sS -o /dev/null -w '%{http_code}' \
        "http://127.0.0.1:$SITE_PORT/Resources/$MAP.metadata.xml.gz" 2>/dev/null)
    check "$([ "$code" = "200" ] && echo 0 || echo 1)" "the site serves the map's metadata ($code)"

    # THE CONTROL for the harness endpoints, and the only place it can be run: the same image,
    # the same command, one environment variable fewer. HarnessController ships in this project,
    # so whatever gets deployed serves it - /Harness/Whoami names the signed-in account and its
    # admin level, /Harness/Throw answers 500 on demand, /Harness/Slow sleeps on request. The
    # container above has them because the lobby probe needs them; a deployment would not.
    #
    # A second container on a second port rather than a restart of the first, which everything
    # after this still depends on.
    docker run -d --rm --name zk-site-locked --network host \
        -e ZK_CONNECTION_STRING="$CS" -e ZK_HOST_URLS="http://0.0.0.0:$LOCKED_PORT" zk-site >/dev/null
    locked_up=
    for _ in $(seq 1 120); do
        curl -fsS -o /dev/null "http://127.0.0.1:$LOCKED_PORT/Home/NotLoggedIn" 2>/dev/null \
            && { locked_up=1; break; }
        sleep 1
    done
    if [ "${locked_up:-}" = "1" ]; then
        locked=$(curl -sS -o /dev/null -w '%{http_code}' \
            "http://127.0.0.1:$LOCKED_PORT/Harness/Whoami" 2>/dev/null)
        page=$(curl -sS -o /dev/null -w '%{http_code}' \
            "http://127.0.0.1:$LOCKED_PORT/Home/NotLoggedIn" 2>/dev/null)
        check "$([ "$locked" = "404" ] && echo 0 || echo 1)" \
            "without ZK_HARNESS_ENDPOINTS the harness endpoints are gone ($locked)"
        check "$([ "$page" = "200" ] && echo 0 || echo 1)" \
            "and the site itself still serves, so it is the endpoints and not the container ($page)"
    else
        check 1 "the locked-down site container never came up"
    fi
    docker rm -f zk-site-locked >/dev/null 2>&1 || true

    # ZK_SITE_URL turns on the probe's single sign-on check, which needs BOTH halves at once and
    # is the reason it lives in the probe rather than in this script: the lobby server issues the
    # token in memory and drops every token for the account when the client disconnects, so the
    # only process that can hand a live one to the website is the one still holding the session.
    if ZK_SITE_URL="http://127.0.0.1:$SITE_PORT" ./tools/dotnet.sh run --project tools/lobby-client-probe -- \
            127.0.0.1 "$PLAYER_PORT" MetaProbe "$PROBE_PASS" "$MAP" "$MAP_OPTION" > "$probe_log" 2>&1; then
        meta=0
    else
        meta=1
    fi
    grep -E "listmapoptions|opened a battle|session|signs nobody|token out of the URL|by cookie alone|session cookie|as a COOKIE" \
        "$probe_log" | sed 's/^/     /' || true
    check "$meta" "a battle in the lobby server has the map options only the site could have given it"
    [ "$meta" = "0" ] || tail -20 "$probe_log"

    # THE CONTROL for the check above. A second map, from the same fixture, that the site is NOT
    # publishing metadata for - same lobby server, same command, same code path, one file
    # different. Without it "the options were there" could mean the server had them all along.
    #
    # A second map rather than a second lobby server: the fetch is cached per resource name, so
    # a name nothing has fetched is a cold path, and starting another container would cost the
    # ninety seconds of WHR this server needs before it says anything.
    if ./tools/dotnet.sh run --project tools/lobby-client-probe -- \
            127.0.0.1 "$PLAYER_PORT" MetaProbe2 "$PROBE_PASS" "$UNSERVED_MAP" "this map has no map options" \
            > "$control_log" 2>&1; then
        control=0
    else
        control=1
    fi
    grep -E "listmapoptions" "$control_log" | sed 's/^/     /' || true
    check "$control" "and a map the site publishes nothing for comes back empty, as a player would see it"
    [ "$control" = "0" ] || tail -20 "$control_log"

    # THE CONTROL, and the only reason the check above means anything: stop the lobby server and
    # the same page must stop working. Without this, a 200 proves the page rendered - not that it
    # ever asked. An earlier version of this script checked /Battles, which does not call the
    # lobby server at all and would have answered 200 with nothing running.
    #
    # The site is restarted because GetCurrentLobbyStats is MemCached for two minutes, so the
    # second request would otherwise be answered from the first one's result.
    echo "   ...  stopping the lobby server to check the page really depends on it"
    docker rm -f zk-lobby-up >/dev/null 2>&1 || true
    docker restart zk-site-up >/dev/null
    for _ in $(seq 1 120); do
        curl -sS -o /dev/null "http://127.0.0.1:$SITE_PORT/Home/NotLoggedIn" 2>/dev/null && break
        sleep 1
    done

    without=$(curl -sS -o /dev/null -w '%{http_code}' "http://127.0.0.1:$SITE_PORT/" 2>/dev/null)
    check "$([ "$without" != "200" ] && echo 0 || echo 1)" \
        "and with the lobby server stopped it does NOT ($without) - so the two really were talking"
fi

if [ "${1:-}" = "--serve" ]; then
    trap 'cleanup keep-config' EXIT
    echo
    echo "left running: site http://127.0.0.1:$SITE_PORT, lobby API $API_PORT"
    echo "ctrl-c to stop; the MiscVar switch stays set until you run this again without --serve"
    docker attach zk-site-up
fi

echo
[ "$failures" = "0" ] && echo "all checks passed" || echo "$failures check(s) failed"
exit "$failures"
