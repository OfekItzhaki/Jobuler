# migrate.ps1 — Run all pending SQL migrations in order against the local PostgreSQL instance.
# Already-applied migrations are skipped using the schema_migrations tracking table.
#
# Usage:
#   .\infra\scripts\migrate.ps1
#   .\infra\scripts\migrate.ps1 -Password yourpassword
#   .\infra\scripts\migrate.ps1 -User postgres -Password yourpassword -DB jobuler

param(
    [string]$PgHost   = "localhost",
    [int]   $Port     = 5432,
    [string]$DB       = "jobuler",
    [string]$User     = "postgres",
    [string]$Password = ""
)

# ── Find psql ─────────────────────────────────────────────────────────────────

$psql = "C:\Program Files\PostgreSQL\18\bin\psql.exe"

if (-not (Test-Path $psql)) {
    $found = Get-Command psql -ErrorAction SilentlyContinue
    if ($found) { $psql = $found.Source }
    else {
        Write-Error "psql.exe not found. Add C:\Program Files\PostgreSQL\18\bin to your PATH."
        exit 1
    }
}

# ── Password ──────────────────────────────────────────────────────────────────

if (-not $Password) {
    $securePass = Read-Host "Password for $User@$PgHost" -AsSecureString
    $Password = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePass))
}

$env:PGPASSWORD = $Password

# ── Helper: run a SQL query and return stdout ─────────────────────────────────

function Invoke-Psql([string]$sql) {
    & $psql -h $PgHost -p $Port -U $User -d $DB -tAc $sql 2>&1
}

function Invoke-PsqlFile([string]$filePath) {
    & $psql -h $PgHost -p $Port -U $User -d $DB -f $filePath 2>&1
}

# ── Ensure tracking table exists ──────────────────────────────────────────────

Write-Host "Connecting to ${PgHost}:${Port}/${DB} as ${User}" -ForegroundColor Cyan

$createTracking = @"
CREATE TABLE IF NOT EXISTS schema_migrations (
    version    TEXT        PRIMARY KEY,
    applied_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
"@

& $psql -h $PgHost -p $Port -U $User -d $DB -c $createTracking | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to connect to database or create tracking table."
    exit 1
}

# ── Run migrations ────────────────────────────────────────────────────────────

$migrationsDir = Join-Path $PSScriptRoot "..\migrations"
$files = Get-ChildItem -Path $migrationsDir -Filter "*.sql" | Sort-Object Name

Write-Host "Running migrations from $migrationsDir" -ForegroundColor Cyan
Write-Host ""

$applied = 0
$skipped = 0

foreach ($file in $files) {
    $version = $file.Name

    # Check if already applied
    $count = Invoke-Psql "SELECT COUNT(*) FROM schema_migrations WHERE version = '$version';"
    $count = ($count | Select-Object -Last 1).Trim()

    if ($count -eq "1") {
        Write-Host "  ✓ $version" -ForegroundColor DarkGray -NoNewline
        Write-Host " (already applied, skipping)" -ForegroundColor DarkGray
        $skipped++
        continue
    }

    Write-Host "  → $version" -ForegroundColor Yellow
    $output = Invoke-PsqlFile $file.FullName

    # Print any non-empty output lines (errors, notices)
    $output | Where-Object { $_ -match '\S' } | ForEach-Object { Write-Host "    $_" }

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Migration failed: $version"
        exit 1
    }

    # Record successful application
    Invoke-Psql "INSERT INTO schema_migrations (version) VALUES ('$version') ON CONFLICT DO NOTHING;" | Out-Null
    $applied++
}

Write-Host ""
Write-Host "Migrations complete." -ForegroundColor Green
Write-Host "  Applied : $applied" -ForegroundColor Green
Write-Host "  Skipped : $skipped" -ForegroundColor DarkGray
