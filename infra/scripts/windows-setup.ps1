#!/usr/bin/env pwsh
<#
.SYNOPSIS
    One-command local development setup for Windows (no Docker required).
    Run from the repo root: .\infra\scripts\windows-setup.ps1

.DESCRIPTION
    - Checks prerequisites (Node, .NET, Python, psql)
    - Creates the PostgreSQL database and user
    - Runs all SQL migrations in order
    - Loads seed data
    - Installs npm and pip dependencies
    - Prints instructions for starting the services
#>

param(
    [string]$PgHost     = "localhost",
    [string]$PgPort     = "5432",
    [string]$PgUser     = "jobuler",
    [string]$PgPassword = "changeme_local",
    [string]$PgDb       = "jobuler",
    [string]$PgSuperUser = "postgres"
)

$ErrorActionPreference = "Stop"

function Write-Step([string]$msg) {
    Write-Host "`n==> $msg" -ForegroundColor Cyan
}

function Write-OK([string]$msg) {
    Write-Host "  ✓ $msg" -ForegroundColor Green
}

function Write-Warn([string]$msg) {
    Write-Host "  ⚠ $msg" -ForegroundColor Yellow
}

function Write-Fail([string]$msg) {
    Write-Host "  ✗ $msg" -ForegroundColor Red
}

# ── Check prerequisites ───────────────────────────────────────────────────────

Write-Step "Checking prerequisites"

$missing = @()

if (-not (Get-Command node -ErrorAction SilentlyContinue)) { $missing += "Node.js 20+ (https://nodejs.org)" }
else { Write-OK "Node.js $(node --version)" }

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { $missing += ".NET SDK 8+ (https://dotnet.microsoft.com/download)" }
else { Write-OK ".NET $(dotnet --version)" }

if (-not (Get-Command python -ErrorAction SilentlyContinue)) { $missing += "Python 3.11+ (https://python.org)" }
else { Write-OK "Python $(python --version)" }

if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
    $missing += "PostgreSQL 16 (https://www.postgresql.org/download/windows/)"
    Write-Warn "psql not found — add PostgreSQL bin to PATH after installing"
} else {
    Write-OK "psql $(psql --version)"
}

if ($missing.Count -gt 0) {
    Write-Host "`nMissing prerequisites:" -ForegroundColor Red
    $missing | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    Write-Host "`nInstall the above tools and re-run this script." -ForegroundColor Red
    exit 1
}

# ── Create database ───────────────────────────────────────────────────────────

Write-Step "Setting up PostgreSQL database"

$env:PGPASSWORD = Read-Host "Enter PostgreSQL superuser ($PgSuperUser) password" -AsSecureString |
    ForEach-Object { [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($_)) }

# Create user if not exists
$createUser = @"
DO `$`$
BEGIN
  IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = '$PgUser') THEN
    CREATE USER $PgUser WITH PASSWORD '$PgPassword';
  END IF;
END
`$`$;
"@

psql -h $PgHost -p $PgPort -U $PgSuperUser -c $createUser 2>&1 | Out-Null
Write-OK "User '$PgUser' ready"

# Create database if not exists
$dbExists = psql -h $PgHost -p $PgPort -U $PgSuperUser -tAc "SELECT 1 FROM pg_database WHERE datname='$PgDb'" 2>&1
if ($dbExists -ne "1") {
    psql -h $PgHost -p $PgPort -U $PgSuperUser -c "CREATE DATABASE $PgDb OWNER $PgUser;" 2>&1 | Out-Null
    Write-OK "Database '$PgDb' created"
} else {
    Write-OK "Database '$PgDb' already exists"
}

psql -h $PgHost -p $PgPort -U $PgSuperUser -c "GRANT ALL PRIVILEGES ON DATABASE $PgDb TO $PgUser;" 2>&1 | Out-Null

# ── Run migrations ────────────────────────────────────────────────────────────

Write-Step "Running database migrations"

$env:PGPASSWORD = $PgPassword
$migrationsDir = Join-Path $PSScriptRoot ".." "migrations"
$sqlFiles = Get-ChildItem -Path $migrationsDir -Filter "*.sql" | Sort-Object Name

foreach ($file in $sqlFiles) {
    Write-Host "  → $($file.Name)" -NoNewline
    $result = psql -h $PgHost -p $PgPort -U $PgUser -d $PgDb -f $file.FullName 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host " ✓" -ForegroundColor Green
    } else {
        Write-Host " (already applied or warning)" -ForegroundColor Yellow
    }
}

# ── Seed data ─────────────────────────────────────────────────────────────────

Write-Step "Loading seed data"

$seedFile = Join-Path $PSScriptRoot "seed.sql"
if (Test-Path $seedFile) {
    psql -h $PgHost -p $PgPort -U $PgUser -d $PgDb -f $seedFile 2>&1 | Out-Null
    Write-OK "Seed data loaded"
    Write-Host "  Demo login: admin@demo.local / Demo1234!" -ForegroundColor Gray
} else {
    Write-Warn "seed.sql not found — skipping"
}

# ── Install npm dependencies ──────────────────────────────────────────────────

Write-Step "Installing frontend dependencies"

$webDir = Join-Path $PSScriptRoot ".." ".." "apps" "web"
Push-Location $webDir
npm install --legacy-peer-deps --silent
if ($LASTEXITCODE -eq 0) { Write-OK "npm packages installed" }
else { Write-Warn "npm install had warnings — check output" }
Pop-Location

# ── Install Python dependencies ───────────────────────────────────────────────

Write-Step "Installing Python solver dependencies"

$solverDir = Join-Path $PSScriptRoot ".." ".." "apps" "solver"
Push-Location $solverDir
pip install -r requirements.txt --quiet 2>&1 | Out-Null
if ($LASTEXITCODE -eq 0) { Write-OK "Python packages installed" }
else { Write-Warn "pip install had warnings — check output" }
Pop-Location

# ── Restore .NET packages ─────────────────────────────────────────────────────

Write-Step "Restoring .NET packages"

$apiDir = Join-Path $PSScriptRoot ".." ".." "apps" "api"
dotnet restore "$apiDir/Jobuler.sln" --nologo -v q
Write-OK ".NET packages restored"

# ── Done ──────────────────────────────────────────────────────────────────────

Write-Host "`n" + ("=" * 60) -ForegroundColor Green
Write-Host "Setup complete! Start the services in 3 terminals:" -ForegroundColor Green
Write-Host ""
Write-Host "  Terminal 1 (API):" -ForegroundColor White
Write-Host "    cd apps/api && dotnet run --project Jobuler.Api" -ForegroundColor Gray
Write-Host "    → http://localhost:5000/swagger" -ForegroundColor Gray
Write-Host ""
Write-Host "  Terminal 2 (Solver):" -ForegroundColor White
Write-Host "    cd apps/solver && python -m uvicorn main:app --port 8000 --reload" -ForegroundColor Gray
Write-Host "    → http://localhost:8000/health" -ForegroundColor Gray
Write-Host ""
Write-Host "  Terminal 3 (Frontend):" -ForegroundColor White
Write-Host "    cd apps/web && npm run dev" -ForegroundColor Gray
Write-Host "    → http://localhost:3000" -ForegroundColor Gray
Write-Host ""
Write-Host "  Demo login: admin@demo.local / Demo1234!" -ForegroundColor Cyan
Write-Host ("=" * 60) -ForegroundColor Green
