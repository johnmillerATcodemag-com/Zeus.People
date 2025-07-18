# Comprehensive API Endpoint Verification with Proper JWT
# This script tests all API endpoints with proper JWT authentication

$baseUrl = "http://localhost:5169"

# JWT Configuration matching the API
$secretKey = "test-secret-key-that-is-at-least-32-characters-long"
$issuer = "Zeus.People.API"
$audience = "Zeus.People.Client"

# Function to create a proper JWT token
function New-JwtToken {
    param(
        [string]$SecretKey,
        [string]$Issuer,
        [string]$Audience,
        [string]$Role = "User"
    )
    
    # Create header
    $header = @{
        alg = "HS256"
        typ = "JWT"
    } | ConvertTo-Json -Compress
    
    $headerEncoded = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($header)).TrimEnd('=').Replace('+', '-').Replace('/', '_')
    
    # Create payload
    $now = [DateTimeOffset]::UtcNow
    $payload = @{
        sub = "test-user-id"
        name = "Test User"
        role = $Role
        iss = $Issuer
        aud = $Audience
        iat = $now.ToUnixTimeSeconds()
        exp = $now.AddHours(1).ToUnixTimeSeconds()
        nbf = $now.ToUnixTimeSeconds()
        jti = [Guid]::NewGuid().ToString()
    } | ConvertTo-Json -Compress
    
    $payloadEncoded = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($payload)).TrimEnd('=').Replace('+', '-').Replace('/', '_')
    
    # Create signature
    $stringToSign = "$headerEncoded.$payloadEncoded"
    $keyBytes = [Text.Encoding]::UTF8.GetBytes($SecretKey)
    $hmac = New-Object System.Security.Cryptography.HMACSHA256
    $hmac.Key = $keyBytes
    $signatureBytes = $hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($stringToSign))
    $signatureEncoded = [Convert]::ToBase64String($signatureBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
    
    return "$headerEncoded.$payloadEncoded.$signatureEncoded"
}

# Helper function to test endpoints
function Test-ApiEndpoint {
    param(
        [string]$Method,
        [string]$Endpoint,
        [string]$Description,
        [string]$JwtToken = $null,
        [object]$Body = $null,
        [int]$ExpectedStatusCode = 200,
        [string]$ContentType = "application/json"
    )
    
    try {
        $uri = "$baseUrl$Endpoint"
        $headers = @{}
        
        if ($JwtToken) {
            $headers["Authorization"] = "Bearer $JwtToken"
        }
        
        $params = @{
            Uri = $uri
            Method = $Method
            Headers = $headers
            TimeoutSec = 30
        }
        
        if ($Body) {
            $params.Body = ($Body | ConvertTo-Json -Depth 10)
            $params.ContentType = $ContentType
        }
        
        $response = Invoke-RestMethod @params -ErrorAction Stop
        $actualStatusCode = 200
        $responseType = if ($response) { $response.GetType().Name } else { "Empty" }
        $details = "Success"
        
    } catch {
        $actualStatusCode = if ($_.Exception.Response) { $_.Exception.Response.StatusCode.value__ } else { 0 }
        $responseType = "Error"
        $details = $_.Exception.Message -replace "Response status code does not indicate success: \d+ \([^)]+\)\.", ""
    }
    
    $status = if ($actualStatusCode -eq $ExpectedStatusCode) { "✅ PASS" } else { "❌ FAIL" }
    
    return [PSCustomObject]@{
        Method = $Method
        Endpoint = $Endpoint
        Description = $Description
        Status = $status
        Expected = $ExpectedStatusCode
        Actual = $actualStatusCode
        ResponseType = $responseType
        Details = $details.Trim()
    }
}

Write-Host "🚀 Zeus.People API Comprehensive Endpoint Verification" -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan
Write-Host "Base URL: $baseUrl" -ForegroundColor Yellow
Write-Host ""

$allTests = @()

# Generate JWT tokens
Write-Host "🔑 Generating JWT tokens..." -ForegroundColor Green
$userToken = New-JwtToken -SecretKey $secretKey -Issuer $issuer -Audience $audience -Role "User"
$adminToken = New-JwtToken -SecretKey $secretKey -Issuer $issuer -Audience $audience -Role "Admin"

# Test 1: Public endpoints
Write-Host "📋 Testing Public Endpoints" -ForegroundColor Green
Write-Host "===========================" -ForegroundColor Green

$allTests += Test-ApiEndpoint -Method "GET" -Endpoint "/swagger/v1/swagger.json" -Description "OpenAPI specification"

# Test 2: Health check (expect 503 due to missing dependencies)
$allTests += Test-ApiEndpoint -Method "GET" -Endpoint "/health" -Description "Health check" -ExpectedStatusCode 503

# Test 3: Authentication required (401 without token)
Write-Host ""
Write-Host "🔒 Testing Authentication Requirements" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green

$protectedEndpoints = @(
    "/api/academics",
    "/api/departments", 
    "/api/rooms",
    "/api/extensions",
    "/api/reports/dashboard"
)

foreach ($endpoint in $protectedEndpoints) {
    $allTests += Test-ApiEndpoint -Method "GET" -Endpoint $endpoint -Description "$(($endpoint -split '/')[-1]) without auth" -ExpectedStatusCode 401
}

# Test 4: Authenticated GET requests
Write-Host ""
Write-Host "✅ Testing Authenticated GET Endpoints" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Green

$getEndpoints = @(
    @{ Endpoint = "/api/academics"; Description = "Get all academics" },
    @{ Endpoint = "/api/departments"; Description = "Get all departments" },
    @{ Endpoint = "/api/rooms"; Description = "Get all rooms" },
    @{ Endpoint = "/api/extensions"; Description = "Get all extensions" },
    @{ Endpoint = "/api/reports/dashboard"; Description = "Get dashboard data" },
    @{ Endpoint = "/api/reports/academics/stats"; Description = "Get academic statistics" },
    @{ Endpoint = "/api/departments/statistics"; Description = "Get department statistics" },
    @{ Endpoint = "/api/rooms/available"; Description = "Get available rooms" }
)

foreach ($test in $getEndpoints) {
    $allTests += Test-ApiEndpoint -Method "GET" -Endpoint $test.Endpoint -Description $test.Description -JwtToken $userToken
}

# Test 5: Invalid resource IDs (404 errors)
Write-Host ""
Write-Host "🔍 Testing Invalid Resource IDs" -ForegroundColor Green
Write-Host "===============================" -ForegroundColor Green

$invalidIdTests = @(
    "/api/academics/99999",
    "/api/departments/99999", 
    "/api/rooms/99999",
    "/api/extensions/99999"
)

foreach ($endpoint in $invalidIdTests) {
    $resource = ($endpoint -split '/')[-2]
    $allTests += Test-ApiEndpoint -Method "GET" -Endpoint $endpoint -Description "Get $resource with invalid ID" -JwtToken $userToken -ExpectedStatusCode 404
}

# Test 6: Invalid POST data (400 validation errors)
Write-Host ""
Write-Host "❌ Testing Validation Errors" -ForegroundColor Green
Write-Host "===========================" -ForegroundColor Green

$validationTests = @(
    @{
        Endpoint = "/api/academics"
        Description = "Create academic with empty data"
        Body = @{ EmpNr = ""; EmpName = ""; Rank = "" }
    },
    @{
        Endpoint = "/api/academics"
        Description = "Create academic with invalid EmpNr format"
        Body = @{ EmpNr = "EMP001"; EmpName = "John Doe"; Rank = "Professor" }
    },
    @{
        Endpoint = "/api/departments"
        Description = "Create department with empty name"
        Body = @{ Name = ""; Head = "" }
    }
)

foreach ($test in $validationTests) {
    $allTests += Test-ApiEndpoint -Method "POST" -Endpoint $test.Endpoint -Description $test.Description -JwtToken $adminToken -Body $test.Body -ExpectedStatusCode 400
}

# Test 7: Content-Type validation
Write-Host ""
Write-Host "📝 Testing Content-Type Validation" -ForegroundColor Green
Write-Host "==================================" -ForegroundColor Green

$allTests += Test-ApiEndpoint -Method "POST" -Endpoint "/api/academics" -Description "POST with wrong content-type" -JwtToken $adminToken -Body "invalid" -ExpectedStatusCode 400 -ContentType "text/plain"

# Test 8: Specific endpoint functionality tests
Write-Host ""
Write-Host "🎯 Testing Specific Endpoint Features" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green

# Test query parameters
$allTests += Test-ApiEndpoint -Method "GET" -Endpoint "/api/academics?pageNumber=1&pageSize=5" -Description "Academics with pagination" -JwtToken $userToken
$allTests += Test-ApiEndpoint -Method "GET" -Endpoint "/api/rooms?pageNumber=1&pageSize=10" -Description "Rooms with pagination" -JwtToken $userToken

# Test building filter
$allTests += Test-ApiEndpoint -Method "GET" -Endpoint "/api/rooms/building/A" -Description "Rooms in building A" -JwtToken $userToken -ExpectedStatusCode 404

# Display Results
Write-Host ""
Write-Host "📊 TEST RESULTS SUMMARY" -ForegroundColor Cyan
Write-Host "======================" -ForegroundColor Cyan

$passCount = ($allTests | Where-Object { $_.Status -eq "✅ PASS" }).Count
$failCount = ($allTests | Where-Object { $_.Status -eq "❌ FAIL" }).Count
$totalCount = $allTests.Count

Write-Host ""
Write-Host "Total Tests: $totalCount" -ForegroundColor White
Write-Host "Passed: $passCount" -ForegroundColor Green  
Write-Host "Failed: $failCount" -ForegroundColor Red
Write-Host "Success Rate: $([math]::Round(($passCount / $totalCount) * 100, 2))%" -ForegroundColor Yellow

Write-Host ""
Write-Host "DETAILED RESULTS:" -ForegroundColor Cyan
Write-Host "=================" -ForegroundColor Cyan

# Group results by category
$publicTests = $allTests | Where-Object { $_.Endpoint -like "*swagger*" -or $_.Endpoint -eq "/health" }
$authTests = $allTests | Where-Object { $_.Description -like "*without auth*" }
$getTests = $allTests | Where-Object { $_.Method -eq "GET" -and $_.Description -notlike "*without auth*" -and $_.Description -notlike "*invalid*" -and $_.Endpoint -notlike "*swagger*" -and $_.Endpoint -ne "/health" }
$errorTests = $allTests | Where-Object { $_.Description -like "*invalid*" -or $_.Method -eq "POST" }

Write-Host ""
Write-Host "🌐 Public Endpoints:" -ForegroundColor Green
$publicTests | Format-Table Status, Method, Endpoint, Description, Expected, Actual -AutoSize

Write-Host "🔒 Authentication Tests:" -ForegroundColor Yellow
$authTests | Format-Table Status, Method, Endpoint, Description, Expected, Actual -AutoSize

Write-Host "✅ GET Endpoints (Authenticated):" -ForegroundColor Green  
$getTests | Format-Table Status, Method, Endpoint, Description, Expected, Actual -AutoSize

Write-Host "❌ Error Handling Tests:" -ForegroundColor Red
$errorTests | Format-Table Status, Method, Endpoint, Description, Expected, Actual -AutoSize

# Save results
$allTests | Export-Csv -Path "endpoint-verification-results.csv" -NoTypeInformation
Write-Host ""
Write-Host "💾 Detailed results saved to: endpoint-verification-results.csv" -ForegroundColor Green

# Final assessment
if ($failCount -eq 0) {
    Write-Host ""
    Write-Host "🎉 ALL TESTS PASSED! API endpoints are working correctly." -ForegroundColor Green
    Write-Host "✅ Authentication is properly configured" -ForegroundColor Green
    Write-Host "✅ Validation is working as expected" -ForegroundColor Green
    Write-Host "✅ Error handling is functioning correctly" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "⚠️  Some tests failed. Review the failed endpoints above." -ForegroundColor Yellow
    $failedTests = $allTests | Where-Object { $_.Status -eq "❌ FAIL" }
    Write-Host ""
    Write-Host "Failed tests summary:" -ForegroundColor Red
    foreach ($test in $failedTests) {
        Write-Host "  • $($test.Description): Expected $($test.Expected), got $($test.Actual)" -ForegroundColor Red
    }
}
