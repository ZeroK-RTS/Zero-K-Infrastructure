#!/usr/bin/env bash
# Builds the ported site's container and checks it serves a page.
#
#     ./tools/site-container.sh            build, run, check, stop
#     ./tools/site-container.sh --serve    build and leave it running on http://127.0.0.1:5200
#
# It needs the database db/docker-compose.yml starts, and reaches it on the host's loopback -
# the same way tools/dotnet.sh does - so the container runs with --network host.
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."

IMAGE=zk-site
NAME=zk-site-check
PORT=5200

case "${ZK_CONNECTION_STRING:-}" in
    ""|*"127.0.0.1,14330"*|*"localhost,14330"*) DB_NAME="${DB_NAME:-zk_test}" ./db/require-db.sh ;;
esac
CS="${ZK_CONNECTION_STRING:-$(DB_NAME="${DB_NAME:-zk_test}" ./db/connection-string.sh)}"

echo "building $IMAGE - the first build compiles 123 views and takes a few minutes"
docker build -q -t "$IMAGE" . >/dev/null

docker rm -f "$NAME" >/dev/null 2>&1 || true

if [ "${1:-}" = "--serve" ]; then
    echo "serving on http://127.0.0.1:$PORT - ctrl-c to stop"
    exec docker run --rm --name "$NAME" --network host \
        -e ZK_CONNECTION_STRING="$CS" \
        -e ZK_HOST_URLS="http://0.0.0.0:$PORT" \
        "$IMAGE"
fi

docker run -d --rm --name "$NAME" --network host \
    -e ZK_CONNECTION_STRING="$CS" \
    -e ZK_HOST_URLS="http://0.0.0.0:$PORT" \
    "$IMAGE" >/dev/null
trap 'docker rm -f "$NAME" >/dev/null 2>&1 || true' EXIT

# Kestrel is listening within a second or two of the process starting; the first request also
# pays for the first EF Core query, so this waits rather than assuming.
for _ in $(seq 1 60); do
    if curl -fsS -o /dev/null "http://127.0.0.1:$PORT/Home/NotLoggedIn" 2>/dev/null; then ready=1; break; fi
    sleep 1
done

failures=0
check() {
    if [ "$1" = "0" ]; then echo "   ok    $2"; else echo "   FAIL  $2"; failures=$((failures + 1)); fi
}

if [ "${ready:-}" != "1" ]; then
    echo "   FAIL  the container never answered - last 30 lines:"
    docker logs "$NAME" 2>&1 | tail -30
    exit 1
fi

echo
echo "the ported site, in a container:"

body=$(curl -fsS "http://127.0.0.1:$PORT/Home/NotLoggedIn")

# The asset build. The image carries bundles built by esbuild, so the page asks for ONE script
# rather than the eleven the build tree serves - and that file has to actually be there.
scripts=$(printf '%s' "$body" | grep -oE '<script src="[^"]+"' | wc -l)
check "$([ "$scripts" = "1" ] && echo 0 || echo 1)" "the page asks for one script bundle, not eleven ($scripts)"
bundle=$(curl -sS -o /dev/null -w '%{http_code}' "http://127.0.0.1:$PORT/bundles/main.js" 2>/dev/null)
check "$([ "$bundle" = "200" ] && echo 0 || echo 1)" "and the built bundle is served ($bundle)"
css=$(curl -sS -o /dev/null -w '%{http_code}' "http://127.0.0.1:$PORT/bundles/maincss.css" 2>/dev/null)
check "$([ "$css" = "200" ] && echo 0 || echo 1)" "so is the built stylesheet ($css)"

case "$body" in *"<html"*) check 0 "it serves a whole HTML document" ;; *) check 1 "it serves a whole HTML document" ;; esac
case "$body" in *"@"*) check 1 "no unprocessed Razor markers survived" ;; *) check 0 "no unprocessed Razor markers survived" ;; esac

# A page that reads the database, so this is not just Razor rendering static text.
forum=$(curl -sS -o /dev/null -w '%{http_code}' "http://127.0.0.1:$PORT/Forum")
check "$([ "$forum" = "200" ] && echo 0 || echo 1)" "a database-backed page answered ($forum)"

# Static content, which is why the image carries img/ at all.
logo=$(curl -sS -o /dev/null -w '%{http_code}' "http://127.0.0.1:$PORT/img/zk_logo.png" 2>/dev/null)
check "$([ "$logo" = "200" ] && echo 0 || echo 1)" "static content is served from img/ ($logo)"

# ...and the source tree is not in the image at all, which is what actually protects it here.
#
# This replaced an HTTP probe for /Web.config, which was worthless: ASP.NET Core's static file
# middleware refuses unknown content types, so that URL answers 404 whether the whole source
# tree is being served or nothing is. A positive control - serving the entire web root on
# purpose - passed, which is how the check was found to be measuring nothing.
#
# What the image contains can be checked, and discriminates by construction.
contents=$(docker run --rm --entrypoint sh "$IMAGE" -c 'ls /app/Zero-K.info | sort | tr "\n" " "')
case "$contents" in
    "Scripts Styles img "|"Scripts Styles img") check 0 "the image carries the three asset directories and no source" ;;
    *) check 1 "the image carries only assets - found: $contents" ;;
esac

echo
[ "$failures" = "0" ] && echo "all checks passed" || echo "$failures check(s) failed"
exit "$failures"
