# Run all pending migrations against the local PostgreSQL instance.
# Usage: .\infra\scripts\migrate.ps1
# Requires PostgreSQL 18 installed at C:\Program Files\PostgreSQL\18\

param(
    [string]$PgHost     = "localhost",
    [string]$PgPort     = "5432",
    [string]$PgDb       = "jobuler",
    [string]$PgUser     = "postgres",
    [string]$PgPassword = "Akame157157"
)

$psql = "C:\Program Files\PostgreSQL\18\bin\psql.exe"
$migrationsDir = Join-Path $PSScriptRoot "..\migrations"

$env:PGPASSWORD = $PgPassword

Write-Host "Running migrations from $migrationsDir"
Write-Host "Connecting to ${PgHost}:${PgPort}/${PgDb} as $PgUser`n"

# Ensure tracking table exists
& $psql -h $PgHost -p $PgPort -U $PgUser -d $PgDb -c @"
CREATE TABLE IF NOT EXISTS schema_migrations (
    version    TEXT        PRIMARY KEY,
    applied_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
"@ | Out-Null

$applied = 0
$skipped = 0

Get-ChildItem "$migrationsDir\*.sql" | Sort-Object Name | ForEach-Object {
    $version = $_.Name
    $filePath = $_.FullName

    $count = & $psql -h $PgHost -p $PgPort -U $PgUser -d $PgDb -tAc `
        "SELECT COUNT(*) FROM schema_migrations WHERE version = '$version';"

    if ([int]$count -gt 0) {
        Write-Host "  ✓ $version (already applied, skipping)"
        $skipped++
    } else {
        Write-Host "  → $version"
        & $psql -h $PgHost -p $PgPort -U $PgUser -d $PgDb -f $filePath
        & $psql -h $PgHost -p $PgPort -U $PgUser -d $PgDb -c `
            "INSERT INTO schema_migrations (version) VALUES ('$version') ON CONFLICT DO NOTHING;" | Out-Null
        $applied++
    }
}

Write-Host "`nMigrations complete. Applied: $applied, Skipped: $skipped"
