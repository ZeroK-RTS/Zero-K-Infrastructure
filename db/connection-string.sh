#!/usr/bin/env bash
# Prints the connection string for the local container, for ZK_CONNECTION_STRING.
DB="${DB_NAME:-zero-k_local}"
PASS="${MSSQL_SA_PASSWORD:-ZkLocal!Dev2026}"
echo "Data Source=127.0.0.1,14330;Initial Catalog=${DB};User ID=sa;Password=${PASS};MultipleActiveResultSets=true;TrustServerCertificate=true;Min Pool Size=5;Max Pool Size=2000"
