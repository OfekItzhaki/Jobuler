#!/usr/bin/env pwsh
# generate-api-client.ps1
# Checks that the API is reachable, then regenerates the typed TypeScript client.

$apiHealthUrl = "http://localhost:5000/health"
$webDir = Join-Path $PSScriptRoot "..\..\apps\web"

Write-Host "Checking API health at $apiHealthUrl ..."

try {
    $response = Invoke-WebRequest -Uri $apiHealthUrl -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
    if ($response.StatusCode -ne 200) {
        throw "Non-200 status: $($response.StatusCode)"
    }
} catch {
    Write-Host ""
    Write-Host "ERROR: The API does not appear to be running at http://localhost:5000." -ForegroundColor Red
    Write-Host ""
    Write-Host "Start it first:" -ForegroundColor Yellow
    Write-Host "  cd apps/api/Jobuler.Api"
    Write-Host "  dotnet run"
    Write-Host ""
    Write-Host "Then re-run this script." -ForegroundColor Yellow
    exit 1
}

Write-Host "API is up. Generating TypeScript client..." -ForegroundColor Green

Push-Location $webDir
try {
    npm run generate:api
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Client generation failed." -ForegroundColor Red
        exit $LASTEXITCODE
    }
} finally {
    Pop-Location
}

Write-Host ""
Write-Host "Done. Generated file: apps/web/lib/api/generated/client.ts" -ForegroundColor Green
