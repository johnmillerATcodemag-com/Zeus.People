# Zeus.People API Endpoint Verification Script
# This script tests all API endpoints to verify they return expected responses

$baseUrl = "http://localhost:5169"
$testResults = @()

# Helper function to make API requests
function Test-Endpoint {
    param(
        [string]$Method,
        [string]$Endpoint,
        [string]$Description,
        [hashtable]$Headers = @{},
        [object]$Body = $null,
        [int]$ExpectedStatusCode = 200
    )
    
    try {
        $uri = "$baseUrl$Endpoint"
        $params = @{
            Uri     = $uri
            Method  = $Method
            Headers = $Headers
        }
        
        if ($Body) {
            $params.Body = ($Body | ConvertTo-Json -Depth 10)
            $params.ContentType = "application/json"
        }
        
        $response = Invoke-RestMethod @params -ErrorAction Stop
        
        $result = [PSCustomObject]@{
            Endpoint     = $Endpoint
            Method       = $Method
            Description  = $Description
            Status       = "✅ PASS"
            StatusCode   = 200
            ResponseType = $response.GetType().Name
            Details      = "Success"
        }
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        $status = if ($statusCode -eq $ExpectedStatusCode) { "✅ PASS" } else { "❌ FAIL" }
        
        $result = [PSCustomObject]@{
            Endpoint     = $Endpoint
            Method       = $Method
            Description  = $Description
            Status       = $status
            StatusCode   = $statusCode
            ResponseType = "Error"
            Details      = $_.Exception.Message
        }
    }
    
    return $result
}

Write-Host "🚀 Starting Zeus.People API Endpoint Verification" -ForegroundColor Cyan
Write-Host "Base URL: $baseUrl" -ForegroundColor Yellow
Write-Host ""

# Test 1: Health Check
Write-Host "Testing Health Endpoint..." -ForegroundColor Green
$testResults += Test-Endpoint -Method "GET" -Endpoint "/health" -Description "Health check endpoint"

# Test 2: Swagger Documentation
Write-Host "Testing Swagger Documentation..." -ForegroundColor Green
$testResults += Test-Endpoint -Method "GET" -Endpoint "/swagger/v1/swagger.json" -Description "OpenAPI specification"

# Test 3: Authentication Required Endpoints (should return 401)
Write-Host "Testing Authentication Requirements..." -ForegroundColor Green

$authRequiredEndpoints = @(
    @{ Method = "GET"; Endpoint = "/api/academics"; Description = "Get all academics (no auth)" },
    @{ Method = "GET"; Endpoint = "/api/departments"; Description = "Get all departments (no auth)" },
    @{ Method = "GET"; Endpoint = "/api/rooms"; Description = "Get all rooms (no auth)" },
    @{ Method = "GET"; Endpoint = "/api/extensions"; Description = "Get all extensions (no auth)" },
    @{ Method = "GET"; Endpoint = "/api/reports/dashboard"; Description = "Get dashboard (no auth)" },
    @{ Method = "GET"; Endpoint = "/api/reports/academics/stats"; Description = "Get academic stats (no auth)" }
)

foreach ($endpoint in $authRequiredEndpoints) {
    $testResults += Test-Endpoint -Method $endpoint.Method -Endpoint $endpoint.Endpoint -Description $endpoint.Description -ExpectedStatusCode 401
}

# Test 4: Generate Mock JWT Token for authenticated tests
Write-Host "Generating mock JWT token for authenticated tests..." -ForegroundColor Green

# Simple mock JWT token (not cryptographically valid, but for structure testing)
$mockJwtHeader = @{
    "alg" = "HS256"
    "typ" = "JWT"
} | ConvertTo-Json -Compress | ForEach-Object { [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($_)) }

$mockJwtPayload = @{
    "sub"  = "test-user"
    "name" = "Test User"
    "role" = "Admin"
    "exp"  = ([DateTimeOffset]::UtcNow.AddHours(1)).ToUnixTimeSeconds()
} | ConvertTo-Json -Compress | ForEach-Object { [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($_)) }

$mockJwtSignature = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("mock-signature"))
$mockJwt = "$mockJwtHeader.$mockJwtPayload.$mockJwtSignature"

$authHeaders = @{
    "Authorization" = "Bearer $mockJwt"
}

# Test 5: Authenticated GET endpoints
Write-Host "Testing Authenticated GET Endpoints..." -ForegroundColor Green

$authenticatedGetEndpoints = @(
    @{ Endpoint = "/api/academics"; Description = "Get all academics (with auth)" },
    @{ Endpoint = "/api/departments"; Description = "Get all departments (with auth)" },
    @{ Endpoint = "/api/rooms"; Description = "Get all rooms (with auth)" },
    @{ Endpoint = "/api/extensions"; Description = "Get all extensions (with auth)" },
    @{ Endpoint = "/api/reports/dashboard"; Description = "Get dashboard (with auth)" },
    @{ Endpoint = "/api/reports/academics/stats"; Description = "Get academic stats (with auth)" },
    @{ Endpoint = "/api/reports/departments/utilization"; Description = "Get department utilization" },
    @{ Endpoint = "/api/reports/rooms/occupancy"; Description = "Get room occupancy" },
    @{ Endpoint = "/api/reports/extensions/usage"; Description = "Get extension usage" },
    @{ Endpoint = "/api/rooms/available"; Description = "Get available rooms" },
    @{ Endpoint = "/api/departments/statistics"; Description = "Get department statistics" }
)

foreach ($endpoint in $authenticatedGetEndpoints) {
    $testResults += Test-Endpoint -Method "GET" -Endpoint $endpoint.Endpoint -Description $endpoint.Description -Headers $authHeaders
}

# Test 6: POST endpoints with invalid data (should return 400)
Write-Host "Testing POST Endpoints with Invalid Data..." -ForegroundColor Green

$invalidPostTests = @(
    @{ 
        Endpoint       = "/api/academics"
        Description    = "Create academic with invalid data"
        Body           = @{ EmpNr = ""; EmpName = ""; Rank = "" }
        ExpectedStatus = 400
    },
    @{ 
        Endpoint       = "/api/departments"
        Description    = "Create department with invalid data"
        Body           = @{ Name = ""; Head = "" }
        ExpectedStatus = 400
    },
    @{ 
        Endpoint       = "/api/rooms"
        Description    = "Create room with invalid data"
        Body           = @{ RoomNumber = ""; Building = "" }
        ExpectedStatus = 400
    },
    @{ 
        Endpoint       = "/api/extensions"
        Description    = "Create extension with invalid data"
        Body           = @{ ExtensionNumber = ""; AccessLevel = "" }
        ExpectedStatus = 400
    }
)

foreach ($test in $invalidPostTests) {
    $testResults += Test-Endpoint -Method "POST" -Endpoint $test.Endpoint -Description $test.Description -Headers $authHeaders -Body $test.Body -ExpectedStatusCode $test.ExpectedStatus
}

# Test 7: GET endpoints with invalid IDs (should return 404)
Write-Host "Testing GET Endpoints with Invalid IDs..." -ForegroundColor Green

$invalidIdTests = @(
    @{ Endpoint = "/api/academics/99999"; Description = "Get academic with invalid ID" },
    @{ Endpoint = "/api/departments/99999"; Description = "Get department with invalid ID" },
    @{ Endpoint = "/api/rooms/99999"; Description = "Get room with invalid ID" },
    @{ Endpoint = "/api/extensions/99999"; Description = "Get extension with invalid ID" }
)

foreach ($test in $invalidIdTests) {
    $testResults += Test-Endpoint -Method "GET" -Endpoint $test.Endpoint -Description $test.Description -Headers $authHeaders -ExpectedStatusCode 404
}

# Test 8: Content-Type validation
Write-Host "Testing Content-Type Validation..." -ForegroundColor Green

try {
    $invalidContentTypeHeaders = $authHeaders.Clone()
    $response = Invoke-RestMethod -Uri "$baseUrl/api/academics" -Method POST -Headers $invalidContentTypeHeaders -Body "invalid-json" -ContentType "text/plain" -ErrorAction Stop
}
catch {
    $testResults += [PSCustomObject]@{
        Endpoint     = "/api/academics"
        Method       = "POST"
        Description  = "POST with invalid content-type"
        Status       = if ($_.Exception.Response.StatusCode.value__ -eq 400) { "✅ PASS" } else { "❌ FAIL" }
        StatusCode   = $_.Exception.Response.StatusCode.value__
        ResponseType = "Error"
        Details      = "Content-type validation working"
    }
}

# Display Results
Write-Host ""
Write-Host "📊 Test Results Summary" -ForegroundColor Cyan
Write-Host "======================" -ForegroundColor Cyan

$passCount = ($testResults | Where-Object { $_.Status -eq "✅ PASS" }).Count
$failCount = ($testResults | Where-Object { $_.Status -eq "❌ FAIL" }).Count
$totalCount = $testResults.Count

Write-Host ""
Write-Host "Total Tests: $totalCount" -ForegroundColor White
Write-Host "Passed: $passCount" -ForegroundColor Green
Write-Host "Failed: $failCount" -ForegroundColor Red
Write-Host "Success Rate: $([math]::Round(($passCount / $totalCount) * 100, 2))%" -ForegroundColor Yellow

Write-Host ""
Write-Host "Detailed Results:" -ForegroundColor Cyan
Write-Host "=================" -ForegroundColor Cyan

$testResults | Format-Table -Property Status, Method, Endpoint, Description, StatusCode, Details -AutoSize

# Save results to file
$testResults | Export-Csv -Path "api-test-results.csv" -NoTypeInformation
Write-Host ""
Write-Host "💾 Results saved to: api-test-results.csv" -ForegroundColor Green

if ($failCount -eq 0) {
    Write-Host ""
    Write-Host "🎉 All tests passed! API endpoints are working correctly." -ForegroundColor Green
}
else {
    Write-Host ""
    Write-Host "⚠️  Some tests failed. Please review the failed endpoints." -ForegroundColor Yellow
}
