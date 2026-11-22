#!/usr/bin/env bash
# Blocks until the local SQL Server container answers, then reports what is in it.
set -euo pipefail
PASS="${MSSQL_SA_PASSWORD:-ZkLocal!Dev2026}"
TRIES="${TRIES:-40}"

for i in $(seq 1 "$TRIES"); do
    if docker exec zk-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$PASS" -C -b \
         -Q "SET NOCOUNT ON; SELECT 1" >/dev/null 2>&1; then
        echo "SQL Server is up (after ${i}0s at most)"
        docker exec zk-db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$PASS" -C -h -1 \
            -Q "SET NOCOUNT ON; SELECT name FROM sys.databases WHERE database_id > 4"
        exit 0
    fi
    sleep 5
done
echo "SQL Server did not become ready in time. Try: docker logs zk-db" >&2
exit 1
