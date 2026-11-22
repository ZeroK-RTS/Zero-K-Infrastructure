#!/usr/bin/env bash
# Loads the BCP native-format dump (db/dumps/*.bcp) into the local container.
#
# Read db/dumps/README.txt first - this script automates it, with one addition the
# instructions do not mention: the dump comes from the LIVE database, which is behind
# this repository's migrations, so the schema has to be rolled back before the data fits.
# See db/README.md.
#
#   ./db/load-bcp.sh            # the tables needed for rating work
#   ./db/load-bcp.sh --all      # those plus StructureTypes and AccountBattleAwards
set -euo pipefail

DB="${DB_NAME:-zero-k_local}"
PASS="${MSSQL_SA_PASSWORD:-ZkLocal!Dev2026}"
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# The order matters: foreign keys are not checked during a bcp load, but the rating code
# reads these tables together and a partial load is worse than none.
TABLES=(Accounts Resources ResourceContentFiles SpringBattles SpringBattlePlayers SpringBattleBots AccountRatings)
[ "${1:-}" = "--all" ] && TABLES+=(StructureTypes AccountBattleAwards)

sql() { docker exec zk-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$PASS" -C -I -d "$DB" -b "$@"; }

docker inspect zk-db >/dev/null 2>&1 || {
    echo "zk-db is not running: docker compose -f db/docker-compose.yml up -d" >&2; exit 1; }

echo "Emptying target tables (reverse order, so dependents go first)"
for (( i=${#TABLES[@]}-1 ; i>=0 ; i-- )); do
    sql -Q "IF OBJECT_ID('dbo.${TABLES[i]}') IS NOT NULL DELETE FROM [dbo].[${TABLES[i]}];" >/dev/null
done

for t in "${TABLES[@]}"; do
    f="$HERE/dumps/$t.bcp"
    if [ ! -f "$f" ]; then echo "  skipping $t (no $t.bcp)"; continue; fi
    printf '%-24s %8s MB  ' "$t" "$(( $(stat -c%s "$f") / 1048576 ))"
    # -n native, -E keep the source identity values, -u trust the container's self-signed
    # certificate, -b batch so a big table does not become one transaction.
    out=$(docker exec zk-db /opt/mssql-tools18/bin/bcp "dbo.$t" in "/dumps/$t.bcp" \
            -q -n -E -b 10000 -S localhost -U sa -P "$PASS" -d "$DB" -u 2>&1 || true)
    if grep -q 'rows copied' <<<"$out"; then
        echo "$(grep -oE '[0-9]+ rows copied' <<<"$out" | head -1)"
    else
        echo "FAILED"
        sed 's/^/      /' <<<"$out" | grep -E 'Error|Msg' | head -3
    fi
done

echo
echo "Row counts:"
sql -h -1 -W -Q "SET NOCOUNT ON;
    SELECT t.name + ' ' + CAST(SUM(p.rows) AS varchar)
    FROM sys.tables t JOIN sys.partitions p ON t.object_id = p.object_id AND p.index_id IN (0,1)
    WHERE SUM(p.rows) > 0 GROUP BY t.name HAVING SUM(p.rows) > 0 ORDER BY SUM(p.rows) DESC;" 2>/dev/null \
  || sql -h -1 -W -Q "SET NOCOUNT ON;
    SELECT t.name, SUM(p.rows) FROM sys.tables t
    JOIN sys.partitions p ON t.object_id = p.object_id AND p.index_id IN (0,1)
    GROUP BY t.name ORDER BY SUM(p.rows) DESC;"
