#!/bin/sh
set -eu

psql --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" \
    --set=admin_user="$POSTGRES_USER" \
    --set=app_user="$POSTGRES_APP_USER" \
    --set=app_password="$POSTGRES_APP_PASSWORD" \
    --set=db_name="$POSTGRES_DB" <<'SQL'
SELECT format('CREATE ROLE %I LOGIN PASSWORD %L', :'app_user', :'app_password')
WHERE NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = :'app_user') \gexec
GRANT CONNECT ON DATABASE :"db_name" TO :"app_user";
GRANT USAGE ON SCHEMA public TO :"app_user";
ALTER DEFAULT PRIVILEGES FOR ROLE :"admin_user" IN SCHEMA public
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO :"app_user";
ALTER DEFAULT PRIVILEGES FOR ROLE :"admin_user" IN SCHEMA public
GRANT USAGE, SELECT ON SEQUENCES TO :"app_user";
SQL
