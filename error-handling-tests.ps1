# Zeus.People API Error Handling Tests
# Comprehensive testing of error handling with invalid inputs

$baseUrl = "http://localhost:5169"

# JWT Configuration for authenticated requests
$secretKey = "development-super-secret-key-for-jwt-that-is-at-least-32-characters-long"
$issuer = "Zeus.People.API.Dev"
$audience = "Zeus.People.Client.Dev"

# Function to create JWT token for testing
function New-TestJwtToken {
    param([string]$Role = "User")
    
    $header = @{ alg = "HS256"; typ = "JWT" } | ConvertTo-Json -Compress
    $headerEncoded = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($header)).TrimEnd('=').Replace('+', '-').Replace('/', '_')
    
    $now = [DateTimeOffset]::UtcNow
    $payload = @{
        sub  = "test-user-id"
        name = "Test User"
        role = $Role
        iss  = $issuer
        aud  = $audience
        iat  = $now.ToUnixTimeSeconds()
        exp  = $now.AddHours(2).ToUnixTimeSeconds()
        nbf  = $now.ToUnixTimeSeconds()
        jti  = [Guid]::NewGuid().ToString()
    } | ConvertTo-Json -Compress
    
    $payloadEncoded = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($payload)).TrimEnd('=').Replace('+', '-').Replace('/', '_')
    
    $stringToSign = "$headerEncoded.$payloadEncoded"
    $keyBytes = [Text.Encoding]::ASCII.GetBytes($secretKey)
    $hmac = New-Object System.Security.Cryptography.HMACSHA256
    $hmac.Key = $keyBytes
    $signatureBytes = $hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($stringToSign))
    $signatureEncoded = [Convert]::ToBase64String($signatureBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
    
    return "$headerEncoded.$payloadEncoded.$signatureEncoded"
}

# Helper function to test error scenarios
function Test-ErrorScenario {
    param(
        [string]$Method,
        [string]$Endpoint,
        [string]$Description,
        [object]$Body = $null,
        [hashtable]$Headers = @{},
        [int]$ExpectedStatusCode,
        [string]$ExpectedErrorPattern = $null
    )
    
    try {
        $uri = "$baseUrl$Endpoint"
        $params = @{
            Uri        = $uri
            Method     = $Method
            Headers    = $Headers
            TimeoutSec = 10
        }
        
        if ($Body) {
            $params.Body = if ($Body -is [string]) { $Body } else { ($Body | ConvertTo-Json -Depth 10) }
            $params.ContentType = "application/json"
        }
        
        $response = Invoke-RestMethod @params -ErrorAction Stop
        $actualStatusCode = 200
        $responseContent = $response | ConvertTo-Json -Depth 3
        $errorMessage = "Unexpected success - expected error $ExpectedStatusCode"
        
    }
    catch {
        $actualStatusCode = if ($_.Exception.Response) { 
            $_.Exception.Response.StatusCode.value__ 
        }
        else { 
            0 
        }
        
        $responseContent = if ($_.Exception.Response) {
            try {
                $stream = $_.Exception.Response.GetResponseStream()
                $reader = New-Object System.IO.StreamReader($stream)
                $reader.ReadToEnd()
            }
            catch {
                $_.Exception.Message
            }
        }
        else {
            $_.Exception.Message
        }
        
        $errorMessage = $responseContent
    }
    
    $status = if ($actualStatusCode -eq $ExpectedStatusCode) { "✅ PASS" } else { "❌ FAIL" }
    
    # Check if error message matches expected pattern
    if ($ExpectedErrorPattern -and $responseContent -and $status -eq "✅ PASS") {
        if ($responseContent -notmatch $ExpectedErrorPattern) {
            $status = "⚠️ PARTIAL"
            $errorMessage += " (Error format doesn't match expected pattern)"
        }
    }
    
    return [PSCustomObject]@{
        Method       = $Method
        Endpoint     = $Endpoint
        Description  = $Description
        Status       = $status
        Expected     = $ExpectedStatusCode
        Actual       = $actualStatusCode
        ErrorDetails = $errorMessage.Substring(0, [Math]::Min(200, $errorMessage.Length))
    }
}

Write-Host "🚨 ZEUS.PEOPLE API ERROR HANDLING TESTS" -ForegroundColor Red
Write-Host "=======================================" -ForegroundColor Red
Write-Host ""
Write-Host "Testing comprehensive error handling with invalid inputs..." -ForegroundColor Yellow
Write-Host ""

$errorTests = @()
$validToken = New-TestJwtToken
$authHeaders = @{ "Authorization" = "Bearer $validToken" }

# Test 1: Authentication Errors
Write-Host "🔒 AUTHENTICATION ERROR TESTS" -ForegroundColor Cyan
Write-Host "==============================" -ForegroundColor Cyan

$errorTests += Test-ErrorScenario -Method "GET" -Endpoint "/api/academics" -Description "No authorization header" -ExpectedStatusCode 401
$errorTests += Test-ErrorScenario -Method "GET" -Endpoint "/api/academics" -Headers @{"Authorization" = "Bearer invalid-token" } -Description "Invalid JWT token" -ExpectedStatusCode 401
$errorTests += Test-ErrorScenario -Method "GET" -Endpoint "/api/academics" -Headers @{"Authorization" = "Basic invalid" } -Description "Wrong auth scheme" -ExpectedStatusCode 401
$errorTests += Test-ErrorScenario -Method "GET" -Endpoint "/api/academics" -Headers @{"Authorization" = "Bearer " } -Description "Empty JWT token" -ExpectedStatusCode 401

# Test 2: Invalid HTTP Methods
Write-Host ""
Write-Host "🚫 HTTP METHOD ERROR TESTS" -ForegroundColor Cyan
Write-Host "===========================" -ForegroundColor Cyan

$errorTests += Test-ErrorScenario -Method "PATCH" -Endpoint "/api/academics" -Headers $authHeaders -Description "Unsupported HTTP method" -ExpectedStatusCode 405
$errorTests += Test-ErrorScenario -Method "DELETE" -Endpoint "/api/academics" -Headers $authHeaders -Description "DELETE on collection" -ExpectedStatusCode 405

# Test 3: Invalid Content-Type
Write-Host ""
Write-Host "📝 CONTENT-TYPE ERROR TESTS" -ForegroundColor Cyan
Write-Host "============================" -ForegroundColor Cyan

$invalidJsonBody = "{ invalid json structure"
$errorTests += Test-ErrorScenario -Method "POST" -Endpoint "/api/academics" -Headers $authHeaders -Body $invalidJsonBody -Description "Invalid JSON format" -ExpectedStatusCode 400

$xmlBody = "<?xml version='1.0'?><data>test</data>"
$xmlHeaders = $authHeaders.Clone()
$xmlHeaders["Content-Type"] = "application/xml"
$errorTests += Test-ErrorScenario -Method "POST" -Endpoint "/api/academics" -Headers $xmlHeaders -Body $xmlBody -Description "Unsupported content type" -ExpectedStatusCode 415

# Test 4: Academic Model Validation Errors
Write-Host ""
Write-Host "👨‍🎓 ACADEMIC VALIDATION ERROR TESTS" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan

# Empty academic data
$errorTests += Test-ErrorScenario -Method "POST" -Endpoint "/api/academics" -Headers $authHeaders -Body @{} -Description "Empty academic object" -ExpectedStatusCode 400

# Missing required fields
$errorTests += Test-ErrorScenario -Method "POST" -Endpoint "/api/academics" -Headers $authHeaders -Body @{ EmpName = "John Doe" } -Description "Missing EmpNr" -ExpectedStatusCode 400
$errorTests += Test-ErrorScenario -Method "POST" -Endpoint "/api/academics" -Headers $authHeaders -Body @{ EmpNr = "12345" } -Description "Missing EmpName" -ExpectedStatusCode 400

# Invalid field formats
$errorTests += Test-ErrorScenario -Method "POST" -Endpoint "/api/academics" -Headers $authHeaders -Body @{ EmpNr = ""; EmpName = ""; Rank = "" } -Description "Empty string values" -ExpectedStatusCode 400
$errorTests += Test-ErrorScenario -Method "POST" -Endpoint "/api/academics" -Headers $authHeaders -Body @{ EmpNr = "EMP001"; EmpName = "John Doe"; Rank = "Professor" } -Description "Invalid EmpNr format" -ExpectedStatusCode 400
$errorTests += Test-ErrorScenario -Method "POST" -Endpoint "/api/academics" -Headers $authHeaders -Body @{ EmpNr = "123456789012345678901"; EmpName = "John Doe"; Rank = "Professor" } -Description "EmpNr too long" -ExpectedStatusCode 400

# Invalid data types
$errorTests += Test-ErrorScenario -Method "POST" -Endpoint "/api/academics" -Headers $authHeaders -Body @{ EmpNr = 12345; EmpName = "John Doe"; Rank = "Professor" } -Description "EmpNr as number instead of string" -ExpectedStatusCode 400

# Test 5: Department Validation Errors
Write-Host ""
Write-Host "🏢 DEPARTMENT VALIDATION ERROR TESTS" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan

$errorTests += Test-ErrorScenario -Method "POST" -Endpoint "/api/departments" -Headers $authHeaders -Body @{} -Description "Empty department object" -ExpectedStatusCode 400
$errorTests += Test-ErrorScenario -Method "POST" -Endpoint "/api/departments" -Headers $authHeaders -Body @{ Name = "" } -Description "Empty department name" -ExpectedStatusCode 400
$errorTests += Test-ErrorScenario -Method "POST" -Endpoint "/api/departments" -Headers $authHeaders -Body @{ Name = $null } -Description "Null department name" -ExpectedStatusCode 400

# Test 6: Room Validation Errors
Write-Host ""
Write-Host "🏠 ROOM VALIDATION ERROR TESTS" -ForegroundColor Cyan
Write-Host "===============================" -ForegroundColor Cyan

$errorTests += Test-ErrorScenario -Method "POST" -Endpoint "/api/rooms" -Headers $authHeaders -Body @{} -Description "Empty room object" -ExpectedStatusCode 400
$errorTests += Test-ErrorScenario -Method "POST" -Endpoint "/api/rooms" -Headers $authHeaders -Body @{ Number = ""; Building = ""; Floor = -1 } -Description "Invalid room data" -ExpectedStatusCode 400

# Test 7: Invalid Resource IDs
Write-Host ""
Write-Host "🔍 INVALID RESOURCE ID TESTS" -ForegroundColor Cyan
Write-Host "============================" -ForegroundColor Cyan

$errorTests += Test-ErrorScenario -Method "GET" -Endpoint "/api/academics/0" -Headers $authHeaders -Description "Academic ID zero" -ExpectedStatusCode 404
$errorTests += Test-ErrorScenario -Method "GET" -Endpoint "/api/academics/-1" -Headers $authHeaders -Description "Negative academic ID" -ExpectedStatusCode 400
$errorTests += Test-ErrorScenario -Method "GET" -Endpoint "/api/academics/abc" -Headers $authHeaders -Description "Non-numeric academic ID" -ExpectedStatusCode 400
$errorTests += Test-ErrorScenario -Method "GET" -Endpoint "/api/academics/999999999" -Headers $authHeaders -Description "Very large academic ID" -ExpectedStatusCode 404

$errorTests += Test-ErrorScenario -Method "GET" -Endpoint "/api/departments/invalid-id" -Headers $authHeaders -Description "Invalid department ID format" -ExpectedStatusCode 400
$errorTests += Test-ErrorScenario -Method "GET" -Endpoint "/api/rooms/non-existent" -Headers $authHeaders -Description "Non-existent room ID" -ExpectedStatusCode 400

# Test 8: Invalid Query Parameters
Write-Host ""
Write-Host "🔎 QUERY PARAMETER ERROR TESTS" -ForegroundColor Cyan
Write-Host "==============================" -ForegroundColor Cyan

$errorTests += Test-ErrorScenario -Method "GET" -Endpoint "/api/academics?pageNumber=0" -Headers $authHeaders -Description "Zero page number" -ExpectedStatusCode 400
$errorTests += Test-ErrorScenario -Method "GET" -Endpoint "/api/academics?pageNumber=-1" -Headers $authHeaders -Description "Negative page number" -ExpectedStatusCode 400
$errorTests += Test-ErrorScenario -Method "GET" -Endpoint "/api/academics?pageSize=0" -Headers $authHeaders -Description "Zero page size" -ExpectedStatusCode 400
$errorTests += Test-ErrorScenario -Method "GET" -Endpoint "/api/academics?pageSize=1001" -Headers $authHeaders -Description "Page size too large" -ExpectedStatusCode 400
$errorTests += Test-ErrorScenario -Method "GET" -Endpoint "/api/academics?pageNumber=abc" -Headers $authHeaders -Description "Non-numeric page number" -ExpectedStatusCode 400

# Test 9: SQL Injection Attempts
Write-Host ""
Write-Host "💉 SQL INJECTION PROTECTION TESTS" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

$sqlInjectionPayloads = @(
    "1'; DROP TABLE Academics; --",
    "1' OR '1'='1",
    "'; DELETE FROM Academics WHERE 1=1; --",
    "1 UNION SELECT * FROM Users --"
)

foreach ($payload in $sqlInjectionPayloads) {
    $errorTests += Test-ErrorScenario -Method "GET" -Endpoint "/api/academics/$payload" -Headers $authHeaders -Description "SQL injection attempt: $($payload.Substring(0, 20))..." -ExpectedStatusCode 400
}

# Test 10: Large Payload Tests
Write-Host ""
Write-Host "📦 LARGE PAYLOAD ERROR TESTS" -ForegroundColor Cyan
Write-Host "============================" -ForegroundColor Cyan

$largeString = "A" * 10000
$largePayload = @{ 
    EmpNr   = $largeString
    EmpName = $largeString
    Rank    = $largeString 
}
$errorTests += Test-ErrorScenario -Method "POST" -Endpoint "/api/academics" -Headers $authHeaders -Body $largePayload -Description "Extremely large payload" -ExpectedStatusCode 400

# Test 11: Invalid Routes
Write-Host ""
Write-Host "🚫 INVALID ROUTE TESTS" -ForegroundColor Cyan
Write-Host "======================" -ForegroundColor Cyan

$errorTests += Test-ErrorScenario -Method "GET" -Endpoint "/api/nonexistent" -Headers $authHeaders -Description "Non-existent endpoint" -ExpectedStatusCode 404
$errorTests += Test-ErrorScenario -Method "GET" -Endpoint "/api/academics/extra/path" -Headers $authHeaders -Description "Invalid route structure" -ExpectedStatusCode 404

# Display Results
Write-Host ""
Write-Host "📊 ERROR HANDLING TEST RESULTS" -ForegroundColor Red
Write-Host "==============================" -ForegroundColor Red

$passCount = ($errorTests | Where-Object { $_.Status -eq "✅ PASS" }).Count
$failCount = ($errorTests | Where-Object { $_.Status -eq "❌ FAIL" }).Count
$partialCount = ($errorTests | Where-Object { $_.Status -eq "⚠️ PARTIAL" }).Count
$totalCount = $errorTests.Count

Write-Host ""
Write-Host "Total Error Tests: $totalCount" -ForegroundColor White
Write-Host "Passed: $passCount" -ForegroundColor Green
Write-Host "Failed: $failCount" -ForegroundColor Red
Write-Host "Partial: $partialCount" -ForegroundColor Yellow
Write-Host "Success Rate: $([math]::Round(($passCount / $totalCount) * 100, 2))%" -ForegroundColor Cyan
Write-Host ""

# Detailed results by category
$categories = @{
    "Authentication"   = $errorTests | Where-Object { $_.Description -like "*auth*" -or $_.Description -like "*token*" }
    "HTTP Methods"     = $errorTests | Where-Object { $_.Description -like "*method*" }
    "Content Type"     = $errorTests | Where-Object { $_.Description -like "*content*" -or $_.Description -like "*JSON*" }
    "Validation"       = $errorTests | Where-Object { $_.Description -like "*validation*" -or $_.Description -like "*empty*" -or $_.Description -like "*missing*" -or $_.Description -like "*invalid*format*" }
    "Resource IDs"     = $errorTests | Where-Object { $_.Description -like "*ID*" }
    "Query Parameters" = $errorTests | Where-Object { $_.Description -like "*page*" }
    "Security"         = $errorTests | Where-Object { $_.Description -like "*injection*" }
    "Other"            = $errorTests | Where-Object { $_.Description -like "*payload*" -or $_.Description -like "*route*" }
}

foreach ($category in $categories.Keys) {
    $categoryTests = $categories[$category]
    if ($categoryTests.Count -gt 0) {
        $categoryPass = ($categoryTests | Where-Object { $_.Status -eq "✅ PASS" }).Count
        $categoryTotal = $categoryTests.Count
        
        Write-Host "$category Tests ($categoryPass/$categoryTotal passed):" -ForegroundColor Cyan
        $categoryTests | Format-Table Status, Method, Endpoint, Description, Expected, Actual -AutoSize
        Write-Host ""
    }
}

# Failed tests details
$failedTests = $errorTests | Where-Object { $_.Status -ne "✅ PASS" }
if ($failedTests.Count -gt 0) {
    Write-Host "❌ FAILED/PARTIAL ERROR TESTS:" -ForegroundColor Red
    Write-Host "==============================" -ForegroundColor Red
    foreach ($test in $failedTests) {
        Write-Host "  • $($test.Description)" -ForegroundColor Red
        Write-Host "    Expected: $($test.Expected), Got: $($test.Actual)" -ForegroundColor Red
        Write-Host "    Details: $($test.ErrorDetails)" -ForegroundColor DarkRed
        Write-Host ""
    }
}

# Save results
$errorTests | Export-Csv -Path "error-handling-test-results.csv" -NoTypeInformation
Write-Host "💾 Error handling test results saved to: error-handling-test-results.csv" -ForegroundColor Green

# Summary assessment
Write-Host ""
Write-Host "🏆 ERROR HANDLING ASSESSMENT" -ForegroundColor Yellow
Write-Host "============================" -ForegroundColor Yellow

if ($failCount -eq 0) {
    Write-Host "🎉 EXCELLENT: All error handling tests passed!" -ForegroundColor Green
    Write-Host "✅ API properly handles all invalid input scenarios" -ForegroundColor Green
    Write-Host "✅ Security protections are working" -ForegroundColor Green
    Write-Host "✅ Validation is comprehensive" -ForegroundColor Green
}
elseif ($failCount -le 3) {
    Write-Host "✅ GOOD: Most error handling is working correctly" -ForegroundColor Green
    Write-Host "⚠️ Minor issues found that should be addressed" -ForegroundColor Yellow
}
else {
    Write-Host "❌ NEEDS IMPROVEMENT: Multiple error handling issues detected" -ForegroundColor Red
    Write-Host "🔧 Review API validation and error response configuration" -ForegroundColor Red
}

Write-Host ""
Write-Host "🔒 SECURITY NOTE: Pay special attention to:" -ForegroundColor Red
Write-Host "  • SQL injection protection" -ForegroundColor Yellow
Write-Host "  • Input validation completeness" -ForegroundColor Yellow  
Write-Host "  • Authentication bypass attempts" -ForegroundColor Yellow
Write-Host "  • Large payload handling" -ForegroundColor Yellow
