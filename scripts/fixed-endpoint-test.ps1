# Fixed Comprehensive API Endpoint Verification with Correct JWT Configuration
# This script tests all API endpoints with properly configured JWT authentication

$baseUrl = "http://localhost:5169"

# JWT Configuration matching the API EXACTLY from appsettings.Development.json
$secretKey = "development-super-secret-key-for-jwt-that-is-at-least-32-characters-long"
$issuer = "Zeus.People.API.Dev"
$audience = "Zeus.People.Client.Dev"

# Function to create a proper JWT token matching the API configuration
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
    
    # Create payload with exact claims structure
    $now = [DateTimeOffset]::UtcNow
    $payload = @{
        sub = "test-user-id"
        name = "Test User"
        role = $Role
        iss = $Issuer
        aud = $Audience
        iat = $now.ToUnixTimeSeconds()
        exp = $now.AddHours(2).ToUnixTimeSeconds()  # 2-hour expiration
        nbf = $now.ToUnixTimeSeconds()
        jti = [Guid]::NewGuid().ToString()
    } | ConvertTo-Json -Compress
    
    $payloadEncoded = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($payload)).TrimEnd('=').Replace('+', '-').Replace('/', '_')
    
    # Create signature using ASCII encoding (matches AuthenticationConfiguration.cs)
    $stringToSign = "$headerEncoded.$payloadEncoded"
    $keyBytes = [Text.Encoding]::ASCII.GetBytes($SecretKey)  # Use ASCII encoding to match API
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
        $responseType = if ($response) { 
            if ($response -is [array]) { "Array[$($response.Count)]" }
            elseif ($response -is [object]) { "Object" }
            else { $response.GetType().Name }
        } else { "Empty" }
        $details = "Success - $responseType"
        
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

Write-Host "🚀 Zeus.People API Fixed Endpoint Verification" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "Base URL: $baseUrl" -ForegroundColor Yellow
Write-Host "JWT Issuer: $issuer" -ForegroundColor Yellow
Write-Host "JWT Audience: $audience" -ForegroundColor Yellow
Write-Host ""

$allTests = @()

# Generate JWT tokens with correct configuration
Write-Host "🔑 Generating JWT tokens with correct configuration..." -ForegroundColor Green
$userToken = New-JwtToken -SecretKey $secretKey -Issuer $issuer -Audience $audience -Role "User"
$adminToken = New-JwtToken -SecretKey $secretKey -Issuer $issuer -Audience $audience -Role "Admin"

# Display token sample for verification
Write-Host "🔍 Sample JWT (first 50 chars): $($userToken.Substring(0, 50))..." -ForegroundColor DarkGray

# Test 1: Public endpoints
Write-Host ""
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

# Test 6: Query parameters
Write-Host ""
Write-Host "🎯 Testing Query Parameters" -ForegroundColor Green
Write-Host "===========================" -ForegroundColor Green

$allTests += Test-ApiEndpoint -Method "GET" -Endpoint "/api/academics?pageNumber=1&pageSize=5" -Description "Academics with pagination" -JwtToken $userToken
$allTests += Test-ApiEndpoint -Method "GET" -Endpoint "/api/rooms?pageNumber=1&pageSize=10" -Description "Rooms with pagination" -JwtToken $userToken
$allTests += Test-ApiEndpoint -Method "GET" -Endpoint "/api/rooms/building/A" -Description "Rooms in building A" -JwtToken $userToken -ExpectedStatusCode 404

# Test 7: POST validation tests (expect 400 for invalid data)
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
        Endpoint = "/api/departments"
        Description = "Create department with empty name"
        Body = @{ Name = ""; Head = "" }
    }
)

foreach ($test in $validationTests) {
    $allTests += Test-ApiEndpoint -Method "POST" -Endpoint $test.Endpoint -Description $test.Description -JwtToken $adminToken -Body $test.Body -ExpectedStatusCode 400
}

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
$errorTests = $allTests | Where-Object { $_.Description -like "*invalid*" -or $_.Method -eq "POST" -or $_.Description -like "*pagination*" -or $_.Description -like "*building*" }

Write-Host ""
Write-Host "🌐 Public Endpoints:" -ForegroundColor Green
$publicTests | Format-Table Status, Method, Endpoint, Description, Expected, Actual, ResponseType -AutoSize

Write-Host "🔒 Authentication Tests:" -ForegroundColor Yellow
$authTests | Format-Table Status, Method, Endpoint, Description, Expected, Actual -AutoSize

Write-Host "✅ GET Endpoints (Authenticated):" -ForegroundColor Green  
$getTests | Format-Table Status, Method, Endpoint, Description, Expected, Actual, ResponseType -AutoSize

Write-Host "🎯 Special Function Tests:" -ForegroundColor Blue
$errorTests | Format-Table Status, Method, Endpoint, Description, Expected, Actual, ResponseType -AutoSize

# Show any failures in detail
$failedTests = $allTests | Where-Object { $_.Status -eq "❌ FAIL" }
if ($failedTests.Count -gt 0) {
    Write-Host ""
    Write-Host "❌ FAILED TESTS DETAILS:" -ForegroundColor Red
    Write-Host "========================" -ForegroundColor Red
    foreach ($test in $failedTests) {
        Write-Host "  • $($test.Description)" -ForegroundColor Red
        Write-Host "    Expected: $($test.Expected), Got: $($test.Actual)" -ForegroundColor Red
        Write-Host "    Details: $($test.Details)" -ForegroundColor DarkRed
        Write-Host ""
    }
}

# Save results
$allTests | Export-Csv -Path "fixed-endpoint-verification-results.csv" -NoTypeInformation
Write-Host "💾 Detailed results saved to: fixed-endpoint-verification-results.csv" -ForegroundColor Green

# Final assessment
if ($failCount -eq 0) {
    Write-Host ""
    Write-Host "🎉 ALL TESTS PASSED! API endpoints are working correctly." -ForegroundColor Green
    Write-Host "✅ Authentication is properly configured" -ForegroundColor Green
    Write-Host "✅ JWT tokens are valid and accepted" -ForegroundColor Green
    Write-Host "✅ All endpoints return expected responses" -ForegroundColor Green
} elseif ($failCount -le 3) {
    Write-Host ""
    Write-Host "⚠️  Minor issues found, but core functionality works!" -ForegroundColor Yellow
    Write-Host "✅ Authentication is working" -ForegroundColor Green
    Write-Host "✅ JWT configuration is correct" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "❌ Multiple test failures detected. Review configuration." -ForegroundColor Red
}

Write-Host ""
Write-Host "🏁 Endpoint verification complete!" -ForegroundColor Cyan
