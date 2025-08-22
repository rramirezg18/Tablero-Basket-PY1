#!/usr/bin/env bash
set -euo pipefail

echo "Esperando a SQL Server en db:1433 ..."
for i in $(seq 1 60); do
  /opt/mssql-tools/bin/sqlcmd -S db -U sa -P "$MSSQL_SA_PASSWORD" -Q "SELECT 1" -l 3 && break
  echo "  intentando conectar ($i/60)..."
  sleep 2
done

echo "Ejecutando init.sql ..."
/opt/mssql-tools/bin/sqlcmd -S db -U sa -P "$MSSQL_SA_PASSWORD" -i /docker-entrypoint-initdb.d/init.sql
echo "init.sql OK"
