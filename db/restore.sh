#!/usr/bin/env bash
# Loads a dump into the local SQL Server container.
#
#   ./db/restore.sh                     # uses the single file in db/dumps/
#   ./db/restore.sh db/dumps/zk.bak     # or name one
#
# Handles SQL Server native backups (.bak) and script dumps (.sql), plus .gz/.zip of
# either. The target database is dropped and recreated, so this is not incremental.
set -euo pipefail

DB="${DB_NAME:-zero-k_local}"
PASS="${MSSQL_SA_PASSWORD:-ZkLocal!Dev2026}"
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SQLCMD=(docker exec zk-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$PASS" -C -b)

docker inspect zk-db >/dev/null 2>&1 || {
    echo "The zk-db container is not running. Start it with:" >&2
    echo "    docker compose -f db/docker-compose.yml up -d && ./db/wait-for-db.sh" >&2
    exit 1
}

# ---- pick the dump -----------------------------------------------------------------
if [ $# -ge 1 ]; then
    DUMP="$1"
else
    mapfile -t found < <(find "$HERE/dumps" -maxdepth 1 -type f \
        \( -name '*.bak' -o -name '*.sql' -o -name '*.gz' -o -name '*.zip' \) | sort)
    case ${#found[@]} in
        0) echo "No dump found in db/dumps/. See db/dumps/README.md." >&2; exit 1 ;;
        1) DUMP="${found[0]}" ;;
        *) echo "Several dumps in db/dumps/ - name the one you want:" >&2
           printf '    %s\n' "${found[@]}" >&2; exit 1 ;;
    esac
fi
[ -f "$DUMP" ] || { echo "No such file: $DUMP" >&2; exit 1; }

# ---- decompress if needed ----------------------------------------------------------
BASENAME="$(basename "$DUMP")"
case "$BASENAME" in
    *.gz)  echo "Decompressing $BASENAME"; gunzip -kf "$DUMP"; DUMP="${DUMP%.gz}"; BASENAME="$(basename "$DUMP")" ;;
    *.zip) echo "Decompressing $BASENAME"; unzip -o -j "$DUMP" -d "$HERE/dumps" >/dev/null
           DUMP="$(find "$HERE/dumps" -maxdepth 1 -newer "$HERE/dumps" -name '*.bak' -o -name '*.sql' | head -1)"
           BASENAME="$(basename "$DUMP")" ;;
esac

echo "Restoring $BASENAME into [$DB]"

# SQL Server reads the file as the in-container mssql user, not as you. A dump written
# with restrictive permissions is unreadable to it even though the directory is mounted,
# so make it world-readable, and if that is not possible (someone else owns it) copy it
# into the container instead.
IN_CONTAINER="/dumps/$BASENAME"
COPIED_IN=""
chmod a+r "$DUMP" 2>/dev/null || true
if ! docker exec zk-db test -r "$IN_CONTAINER" 2>/dev/null; then
    echo "  container cannot read the file directly, copying it in"
    IN_CONTAINER="/var/opt/mssql/$BASENAME"
    docker cp "$DUMP" "zk-db:$IN_CONTAINER"
    docker exec -u root zk-db chown mssql:root "$IN_CONTAINER"
    COPIED_IN="$IN_CONTAINER"
fi
cleanup() { [ -n "$COPIED_IN" ] && docker exec zk-db rm -f "$COPIED_IN" 2>/dev/null || true; }
trap cleanup EXIT

case "$BASENAME" in
  *.bak)
    # The backup's logical file names are whatever the source server called them, so ask
    # the backup itself rather than guessing.
    echo "  reading the backup's file list"
    FILELIST=$("${SQLCMD[@]}" -h -1 -W -s"|" -Q \
        "SET NOCOUNT ON; RESTORE FILELISTONLY FROM DISK='$IN_CONTAINER'")
    MOVES=""
    while IFS='|' read -r logical physical type rest; do
        [ -z "${logical// }" ] && continue
        case "$logical" in ---*|"LogicalName"*) continue ;; esac
        if [ "${type// }" = "L" ]; then ext="_log.ldf"; else ext=".mdf"; fi
        MOVES="$MOVES, MOVE '${logical// }' TO '/var/opt/mssql/data/${DB}${ext}'"
    done <<< "$FILELIST"
    [ -n "$MOVES" ] || { echo "Could not read the backup file list" >&2; exit 1; }

    "${SQLCMD[@]}" -Q "
        IF DB_ID('$DB') IS NOT NULL
        BEGIN
            ALTER DATABASE [$DB] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
            DROP DATABASE [$DB];
        END
        RESTORE DATABASE [$DB] FROM DISK='$IN_CONTAINER' WITH REPLACE${MOVES};
        ALTER DATABASE [$DB] SET MULTI_USER;"
    ;;
  *.sql)
    "${SQLCMD[@]}" -Q "
        IF DB_ID('$DB') IS NOT NULL
        BEGIN
            ALTER DATABASE [$DB] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
            DROP DATABASE [$DB];
        END
        CREATE DATABASE [$DB];"
    "${SQLCMD[@]}" -d "$DB" -i "$IN_CONTAINER"
    ;;
  *)
    echo "Don't know how to restore $BASENAME (expected .bak or .sql)" >&2; exit 1 ;;
esac

echo
echo "Done. What landed:"
"${SQLCMD[@]}" -d "$DB" -Q "
    SET NOCOUNT ON;
    SELECT TOP 20 t.name AS [table], SUM(p.rows) AS [rows]
    FROM sys.tables t JOIN sys.partitions p ON t.object_id = p.object_id AND p.index_id IN (0,1)
    GROUP BY t.name ORDER BY SUM(p.rows) DESC;"
echo
echo "Point the application at it with:"
echo "    export ZK_CONNECTION_STRING=\"\$(./db/connection-string.sh)\""
