# Comprehensive feature test script
param([string]$BaseUrl = "http://localhost:5000")

$psql = "C:\Program Files\PostgreSQL\18\bin\psql.exe"
$env:PGPASSWORD = "Akame157157"
$spaceId = "e5f6a7b8-c9d0-4e1f-2a3b-c4d5e6f7a8b9"
$groupId = "a3b4c5d6-e7f8-4a9b-0c1d-e2f3a4b5c6d7"

$pass = 0; $fail = 0

function Test-Endpoint {
    param([string]$Name, [string]$Method, [string]$Url, [hashtable]$Headers, [string]$Body = $null, [int]$ExpectedStatus = 200)
    try {
        $params = @{ Uri = $Url; Method = $Method; Headers = $Headers; UseBasicParsing = $true }
        if ($Body) { $params.Body = $Body; $params.ContentType = "application/json" }
        $r = Invoke-WebRequest @params
        if ($r.StatusCode -eq $ExpectedStatus -or ($ExpectedStatus -eq 200 -and $r.StatusCode -lt 300)) {
            Write-Host "  [PASS] $Name ($($r.StatusCode))" -ForegroundColor Green
            $script:pass++
            return $r.Content
        } else {
            Write-Host "  [FAIL] $Name - Expected $ExpectedStatus got $($r.StatusCode)" -ForegroundColor Red
            $script:fail++
        }
    } catch {
        $code = $_.Exception.Response.StatusCode.value__
        if ($code -eq $ExpectedStatus) {
            Write-Host "  [PASS] $Name ($code - expected)" -ForegroundColor Green
            $script:pass++
            return $null
        }
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $errBody = $reader.ReadToEnd()
        Write-Host "  [FAIL] $Name - $code : $errBody" -ForegroundColor Red
        $script:fail++
    }
    return $null
}

# ── Login ─────────────────────────────────────────────────────────────────────
Write-Host "`n=== AUTH ===" -ForegroundColor Cyan
$loginResult = Test-Endpoint "Login" "POST" "$BaseUrl/auth/login" @{} '{"email":"admin@demo.local","password":"Demo1234!"}'
if (-not $loginResult) { Write-Host "Cannot continue without auth token"; exit 1 }
$token = ($loginResult | ConvertFrom-Json).accessToken
$h = @{Authorization = "Bearer $token"}

# ── Spaces ────────────────────────────────────────────────────────────────────
Write-Host "`n=== SPACES ===" -ForegroundColor Cyan
Test-Endpoint "Get my spaces" "GET" "$BaseUrl/spaces" $h | Out-Null

# ── Groups ────────────────────────────────────────────────────────────────────
Write-Host "`n=== GROUPS ===" -ForegroundColor Cyan
Test-Endpoint "List groups" "GET" "$BaseUrl/spaces/$spaceId/groups" $h | Out-Null
Test-Endpoint "Get group members" "GET" "$BaseUrl/spaces/$spaceId/groups/$groupId/members" $h | Out-Null
Test-Endpoint "Get group schedule" "GET" "$BaseUrl/spaces/$spaceId/groups/$groupId/schedule" $h | Out-Null

# Create a group with unique name
$uniqueName = "TestGroup-$(Get-Date -Format 'HHmmss')"
$newGroupResult = Test-Endpoint "Create group" "POST" "$BaseUrl/spaces/$spaceId/groups" $h "{`"name`":`"$uniqueName`",`"description`":null}" 201
if ($newGroupResult) {
    $newGroupId = ($newGroupResult | ConvertFrom-Json).id
    Write-Host "    Created group: $newGroupId" -ForegroundColor DarkGray
    # Delete it
    Test-Endpoint "Delete group (soft)" "DELETE" "$BaseUrl/spaces/$spaceId/groups/$newGroupId" $h $null 204 | Out-Null
}

# ── Alerts ────────────────────────────────────────────────────────────────────
Write-Host "`n=== GROUP ALERTS ===" -ForegroundColor Cyan
Test-Endpoint "Get alerts" "GET" "$BaseUrl/spaces/$spaceId/groups/$groupId/alerts" $h | Out-Null
$alertResult = Test-Endpoint "Create alert" "POST" "$BaseUrl/spaces/$spaceId/groups/$groupId/alerts" $h '{"title":"Test Alert","body":"This is a test","severity":"info"}' 201
if ($alertResult) {
    $alertId = ($alertResult | ConvertFrom-Json).id
    Test-Endpoint "Delete alert" "DELETE" "$BaseUrl/spaces/$spaceId/groups/$groupId/alerts/$alertId" $h $null 204 | Out-Null
}

# ── Messages ──────────────────────────────────────────────────────────────────
Write-Host "`n=== GROUP MESSAGES ===" -ForegroundColor Cyan
Test-Endpoint "Get messages" "GET" "$BaseUrl/spaces/$spaceId/groups/$groupId/messages" $h | Out-Null

# ── Tasks ─────────────────────────────────────────────────────────────────────
Write-Host "`n=== TASKS ===" -ForegroundColor Cyan
Test-Endpoint "Get task types" "GET" "$BaseUrl/spaces/$spaceId/task-types" $h | Out-Null
Test-Endpoint "Get task slots" "GET" "$BaseUrl/spaces/$spaceId/task-slots" $h | Out-Null

# ── Constraints ───────────────────────────────────────────────────────────────
Write-Host "`n=== CONSTRAINTS ===" -ForegroundColor Cyan
Test-Endpoint "Get constraints" "GET" "$BaseUrl/spaces/$spaceId/constraints" $h | Out-Null
$constraintResult = Test-Endpoint "Create constraint" "POST" "$BaseUrl/spaces/$spaceId/constraints" $h '{"scopeType":"Group","scopeId":null,"severity":"Soft","ruleType":"min_rest_hours","rulePayloadJson":"{\"hours\":8}","effectiveFrom":null,"effectiveUntil":null}' 201
if ($constraintResult) {
    $constraintId = ($constraintResult | ConvertFrom-Json).id
    Write-Host "    Created constraint: $constraintId" -ForegroundColor DarkGray
}

# ── People ────────────────────────────────────────────────────────────────────
Write-Host "`n=== PEOPLE ===" -ForegroundColor Cyan
Test-Endpoint "Get people" "GET" "$BaseUrl/spaces/$spaceId/people" $h | Out-Null

# ── Notifications ─────────────────────────────────────────────────────────────
Write-Host "`n=== NOTIFICATIONS ===" -ForegroundColor Cyan
Test-Endpoint "Get notifications" "GET" "$BaseUrl/spaces/$spaceId/notifications" $h | Out-Null

# ── Schedule versions ─────────────────────────────────────────────────────────
Write-Host "`n=== SCHEDULE ===" -ForegroundColor Cyan
Test-Endpoint "Get schedule versions" "GET" "$BaseUrl/spaces/$spaceId/schedule-versions" $h | Out-Null
Test-Endpoint "Get current schedule (no schedule yet - 404 expected)" "GET" "$BaseUrl/spaces/$spaceId/schedule-versions/current" $h $null 404 | Out-Null

# ── Auth flows ────────────────────────────────────────────────────────────────
Write-Host "`n=== AUTH FLOWS ===" -ForegroundColor Cyan
Test-Endpoint "Forgot password (always 200)" "POST" "$BaseUrl/auth/forgot-password" @{} '{"email":"nobody@example.com"}' 200 | Out-Null
Test-Endpoint "Register (duplicate email - 400 expected)" "POST" "$BaseUrl/auth/register" @{} '{"email":"admin@demo.local","displayName":"Test","password":"Test1234!"}' 400 | Out-Null

# ── Summary ───────────────────────────────────────────────────────────────────
Write-Host "`n$('='*50)" -ForegroundColor Cyan
Write-Host "RESULTS: $pass passed, $fail failed" -ForegroundColor $(if ($fail -eq 0) { "Green" } else { "Yellow" })
if ($fail -gt 0) { exit 1 }
