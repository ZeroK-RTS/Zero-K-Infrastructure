#!/usr/bin/env bash
# Loads the committed test fixture into a database that already has the schema.
#
#   DB_NAME=zk_test ./db/load-fixture.sh
#
# The fixture is anonymised and tiny (see db/fixture/fixture.sql); it is meant for
# automated tests, not for looking at the site with real content.
set -euo pipefail
DB="${DB_NAME:-zero-k_local}"
PASS="${MSSQL_SA_PASSWORD:-ZkLocal!Dev2026}"
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

docker inspect zk-db >/dev/null 2>&1 || {
    echo "zk-db is not running: docker compose -f db/docker-compose.yml up -d" >&2; exit 1; }

docker exec -i zk-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$PASS" -C -I -d "$DB" -b \
    < "$HERE/fixture/fixture.sql" >/dev/null

# Hand-written, and separate from the generated fixture on purpose - see its header. The site's
# front page is a forum page, so a database with no categories cannot serve one.
docker exec -i zk-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$PASS" -C -I -d "$DB" -b \
    < "$HERE/fixture/forum-seed.sql" >/dev/null

docker exec zk-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$PASS" -C -I -d "$DB" -h -1 -W -Q \
  "SET NOCOUNT ON;
   SELECT 'Accounts '            + CAST(COUNT(*) AS varchar) FROM Accounts UNION ALL
   SELECT 'Resources '           + CAST(COUNT(*) AS varchar) FROM Resources UNION ALL
   SELECT 'SpringBattles '       + CAST(COUNT(*) AS varchar) FROM SpringBattles UNION ALL
   SELECT 'SpringBattlePlayers ' + CAST(COUNT(*) AS varchar) FROM SpringBattlePlayers UNION ALL
   SELECT 'AccountRatings '      + CAST(COUNT(*) AS varchar) FROM AccountRatings UNION ALL
   SELECT 'ForumCategories '     + CAST(COUNT(*) AS varchar) FROM ForumCategories;"
