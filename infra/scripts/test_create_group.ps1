$r = Invoke-WebRequest -Uri "http://localhost:5000/auth/login" -Method POST -ContentType "application/json" -Body '{"email":"admin@demo.local","password":"Demo1234!"}' -UseBasicParsing
$token = ($r.Content | ConvertFrom-Json).accessToken
$h = @{Authorization="Bearer $token"; "Content-Type"="application/json"}

try {
    $r2 = Invoke-WebRequest -Uri "http://localhost:5000/spaces/e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9/groups" -Method POST -Headers $h -Body '{"name":"TestGroup123","description":null}' -UseBasicParsing
    Write-Host "OK: $($r2.StatusCode) - $($r2.Content)"
} catch {
    $stream = $_.Exception.Response.GetResponseStream()
    $reader = New-Object System.IO.StreamReader($stream)
    $body = $reader.ReadToEnd()
    Write-Host "ERROR $($_.Exception.Response.StatusCode.value__): $body"
}
