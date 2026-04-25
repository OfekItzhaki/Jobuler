# Test endpoints and show actual error bodies
$psql = "C:\Program Files\PostgreSQL\18\bin\psql.exe"
$env:PGPASSWORD = "Akame157157"

# Login
$loginBody = '{"email":"admin@demo.local","password":"Demo1234!"}'
$r = Invoke-WebRequest -Uri "http://localhost:5000/auth/login" -Method POST -ContentType "application/json" -Body $loginBody -UseBasicParsing
$token = ($r.Content | ConvertFrom-Json).accessToken
$userId = ($r.Content | ConvertFrom-Json).userId
Write-Host "Logged in as userId: $userId"
$h = @{Authorization="Bearer $token"}

# Test task-slots
Write-Host "`n--- task-slots ---"
try {
    $r2 = Invoke-WebRequest -Uri "http://localhost:5000/spaces/e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9/task-slots" -Headers $h -UseBasicParsing
    Write-Host "OK: $($r2.StatusCode)"
} catch {
    $body = $_.ErrorDetails.Message
    Write-Host "ERROR $($_.Exception.Response.StatusCode): $body"
}

# Test alerts
Write-Host "`n--- alerts ---"
try {
    $r3 = Invoke-WebRequest -Uri "http://localhost:5000/spaces/e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9/groups/a3b4c5d6-e7f8-4a9b-0c1d-e2f3a4b5c6d7/alerts" -Headers $h -UseBasicParsing
    Write-Host "OK: $($r3.StatusCode)"
} catch {
    $body = $_.ErrorDetails.Message
    Write-Host "ERROR $($_.Exception.Response.StatusCode): $body"
}

# Test messages
Write-Host "`n--- messages ---"
try {
    $r4 = Invoke-WebRequest -Uri "http://localhost:5000/spaces/e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9/groups/a3b4c5d6-e7f8-4a9b-0c1d-e2f3a4b5c6d7/messages" -Headers $h -UseBasicParsing
    Write-Host "OK: $($r4.StatusCode)"
} catch {
    $body = $_.ErrorDetails.Message
    Write-Host "ERROR $($_.Exception.Response.StatusCode): $body"
}

# Check what permissions the user has
Write-Host "`n--- user permissions in DB ---"
& $psql -h localhost -p 5432 -U postgres -d jobuler -c "SELECT permission_key FROM space_permission_grants WHERE user_id = '$userId' AND space_id = 'e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9';"
