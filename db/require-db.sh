#!/usr/bin/env bash
# Fails fast, with something you can act on, when the local SQL Server is not usable.
#
#     ./db/require-db.sh                checks DB_NAME (default zk_test) exists
#     ./db/require-db.sh --server-only  checks only that SQL Server answers
#
# Every harness here needs a database, and without this the failure arrives as a
# Microsoft.Data.SqlClient stack trace forty frames deep that says "the server was not found
# or was not accessible" - true of a container that was never created, one that is stopped, one
# that is still starting, and a database that does not exist, which are four different fixes.
#
# The container really does die on its own: zk-db was found Exited (137) seventeen hours after
# it was last used, and the first sign of it was a harness dumping a stack trace in the middle
# of a check.
#
# Cheap on purpose: one docker inspect and one query, so it can go at the top of anything.
# Use ./db/wait-for-db.sh instead when you have just started the container and want to block.
set -euo pipefail

DB_NAME="${DB_NAME:-zk_test}"
SERVER_ONLY=""
[ "${1:-}" = "--server-only" ] && SERVER_ONLY=1
PASS="${MSSQL_SA_PASSWORD:-ZkLocal!Dev2026}"
CONTAINER="${ZK_DB_CONTAINER:-zk-db}"

fail() { echo "" >&2; echo "$1" >&2; echo "" >&2; exit 2; }

if ! docker info >/dev/null 2>&1; then
    fail "Docker is not responding, so the test database cannot be reached.
  Start Docker, then:  docker start $CONTAINER && ./db/wait-for-db.sh"
fi

if ! state=$(docker inspect -f '{{.State.Status}}' "$CONTAINER" 2>/dev/null); then
    fail "There is no '$CONTAINER' container, so there is no test database.
  Create it with:      docker compose -f db/docker-compose.yml up -d
  then the schema:     ./db/dbsetup.sh latest
  then the fixture:    ./db/load-fixture.sh
  See db/README.md."
fi

if [ "$state" != "running" ]; then
    code=$(docker inspect -f '{{.State.ExitCode}}' "$CONTAINER" 2>/dev/null || echo "?")
    extra=""
    [ "$code" = "137" ] && extra="
  Exit code 137 is SIGKILL. That is what a plain 'docker stop' leaves behind too, because
  SQL Server does not finish shutting down inside the ten-second grace period - so on its own
  it does not mean anything went wrong. If the container died while nothing stopped it, the
  OOM killer is the usual cause; SQL Server wants ~2GB."
    fail "The '$CONTAINER' container is '$state' (exit code $code), so the database is down.
  Start it with:       docker start $CONTAINER && ./db/wait-for-db.sh$extra"
fi

query() {
    docker exec "$CONTAINER" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$PASS" -C -b -h -1 \
        -Q "SET NOCOUNT ON; $1" 2>/dev/null
}

if ! query "SELECT 1" >/dev/null; then
    fail "The '$CONTAINER' container is running but SQL Server is not answering yet.
  It may still be starting:  ./db/wait-for-db.sh
  If that times out:         docker logs $CONTAINER"
fi

# --server-only for callers that CREATE their database: db/efcore-schema.sh drops and recreates
# zk_efcore through EnsureDeleted/EnsureCreated, so requiring it to exist first would fail every
# clean run, CI's included.
[ -n "$SERVER_ONLY" ] && exit 0

if ! query "SELECT name FROM sys.databases WHERE name = '$DB_NAME'" | grep -q "$DB_NAME"; then
    fail "SQL Server is up but the database '$DB_NAME' does not exist.
  Create the schema:   DB_NAME=$DB_NAME ./db/dbsetup.sh latest
  then the fixture:    DB_NAME=$DB_NAME ./db/load-fixture.sh"
fi
