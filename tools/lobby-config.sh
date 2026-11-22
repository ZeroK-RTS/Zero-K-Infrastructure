#!/usr/bin/env bash
# The MiscVars that decide whether the lobby server runs as its own process.
#
#     ./tools/lobby-config.sh set [api-port]   point the website at a standalone server
#     ./tools/lobby-config.sh clear            put it back: the website starts its own again
#
# Shared by tools/lobby-container.sh and tools/stack.sh. It exists because the first version of
# lobby-container.sh assumed these were already set - they were, by hand, in one developer's
# database - so the script passed for a reason it did not contain. The same shape of mistake as
# the fixture having no forum categories.
#
# The secret is a local development value and deliberately obvious. A real deployment sets these
# in the real database; see Zero-K.info/HOSTING.md.
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/.."

ACTION="${1:-}"
API_PORT="${2:-8300}"
DB_NAME="${DB_NAME:-zk_test}"
PASS="$(grep -oP '(?<=MSSQL_SA_PASSWORD: ")[^"]+' db/docker-compose.yml)"

sql() { docker exec -i zk-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$PASS" -C -I -d "$DB_NAME" -b -h -1 -W -Q "$1" >/dev/null; }

KEYS="'LobbyApiUrl','LobbyApiSecret','LobbyApiListenPrefix','LobbyApiAllowInsecureTransport'"

case "$ACTION" in
    set)
        sql "set nocount on;
             delete from MiscVars where VarName in ($KEYS);
             insert into MiscVars (VarName, VarValue) values
               ('LobbyApiUrl', 'http://127.0.0.1:$API_PORT/'),
               ('LobbyApiSecret', 'local-dev-secret-not-a-real-one'),
               ('LobbyApiListenPrefix', 'http://+:$API_PORT/'),
               ('LobbyApiAllowInsecureTransport', 'true');"
        ;;
    clear)
        # Left set, every later run of the host harness finds a lobby server configured and none
        # running - a failure about the last script that ran, not about the site.
        sql "delete from MiscVars where VarName in ($KEYS);"
        ;;
    *)
        echo "usage: lobby-config.sh set|clear [api-port]" >&2
        exit 2
        ;;
esac
