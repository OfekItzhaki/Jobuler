@echo off
REM Run this script once to set up the local dev environment.
REM It will ask for your PostgreSQL postgres password.

SET PSQL="C:\Program Files\PostgreSQL\18\bin\psql.exe"
SET PGPASSWORD=

echo === Step 1: Setting up database ===
%PSQL% -U postgres -f infra\scripts\setup-local-db.sql

echo === Step 2: Running migrations ===
%PSQL% -U jobuler -d jobuler -f infra\migrations\000_extensions.sql
%PSQL% -U jobuler -d jobuler -f infra\migrations\001_core_identity.sql
%PSQL% -U jobuler -d jobuler -f infra\migrations\002_operational_data.sql
%PSQL% -U jobuler -d jobuler -f infra\migrations\003_tasks_and_constraints.sql
%PSQL% -U jobuler -d jobuler -f infra\migrations\004_scheduling.sql
%PSQL% -U jobuler -d jobuler -f infra\migrations\005_logs_and_exports.sql
%PSQL% -U jobuler -d jobuler -f infra\migrations\006_notifications.sql

echo === Step 3: Loading seed data ===
%PSQL% -U jobuler -d jobuler -f infra\scripts\seed.sql

echo === Done! ===
echo Now start the API: cd apps\api ^&^& dotnet run --project Jobuler.Api
echo And the frontend: cd apps\web ^&^& npm run dev
