# Error Handling Fix Verification
# Tests the three specific issues identified from comprehensive error handling tests

# API Base URL
$baseUrl = "https://localhost:7176"

# Track test results
$testResults = @()

# Function to test error scenario
function Test-ErrorScenarioFix {
    param(
        [string]$testName,
        [string]$method,
        [string]$url,
        [hashtable]$headers = @{},
        [string]$body = $null,
        [int]$expectedStatusCode,
        [string]$description
    )
    
    try {
        $requestParams = @{
            Uri                  = $url
            Method               = $method
            Headers              = $headers
            TimeoutSec           = 10
            SkipCertificateCheck = $true
        }
        
        if ($body) {
            $requestParams.Body = $body
        }
        
        $response = Invoke-WebRequest @requestParams -ErrorAction Stop
        $actualStatus = $response.StatusCode
        $success = $actualStatus -eq $expectedStatusCode
        
        Write-Host "✓ $testName`: Status $actualStatus (Expected: $expectedStatusCode) - $(if($success){'PASS'}else{'FAIL'})" -ForegroundColor $(if ($success) { 'Green' }else { 'Red' })
        
        return @{
            TestName    = $testName
            Description = $description
            Expected    = $expectedStatusCode
            Actual      = $actualStatus
            Success     = $success
            Error       = $null
        }
    }
    catch {
        $actualStatus = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
        $success = $actualStatus -eq $expectedStatusCode
        $errorMsg = if ($actualStatus -eq 0) { "Timeout/Connection Error" } else { $_.Exception.Message }
        
        Write-Host "✓ $testName`: Status $actualStatus (Expected: $expectedStatusCode) - $(if($success){'PASS'}else{'FAIL'}) - $errorMsg" -ForegroundColor $(if ($success) { 'Green' }else { 'Red' })
        
        return @{
            TestName    = $testName
            Description = $description
            Expected    = $expectedStatusCode
            Actual      = $actualStatus
            Success     = $success
            Error       = $errorMsg
        }
    }
}

# Function to create JWT token for API testing
function New-TestJwtToken {
    $header = @{
        alg = "HS256"
        typ = "JWT"
    } | ConvertTo-Json -Compress

    $payload = @{
        sub = "test-user"
        iss = "Zeus.People.API.Dev"
        aud = "Zeus.People.API.Dev"
        exp = [DateTimeOffset]::UtcNow.AddHours(1).ToUnixTimeSeconds()
        iat = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
    } | ConvertTo-Json -Compress

    $headerBase64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($header)).TrimEnd('=').Replace('+', '-').Replace('/', '_')
    $payloadBase64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($payload)).TrimEnd('=').Replace('+', '-').Replace('/', '_')
    
    $secretKey = "your-super-secret-key-that-is-at-least-32-characters-long-for-development"
    $signature = [System.Security.Cryptography.HMACSHA256]::new([Text.Encoding]::UTF8.GetBytes($secretKey))
    $signatureBytes = $signature.ComputeHash([Text.Encoding]::UTF8.GetBytes("$headerBase64.$payloadBase64"))
    $signatureBase64 = [Convert]::ToBase64String($signatureBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
    
    return "$headerBase64.$payloadBase64.$signatureBase64"
}

Write-Host "`n=== ERROR HANDLING FIX VERIFICATION ===" -ForegroundColor Cyan
Write-Host "Testing the three specific issues identified from comprehensive error handling tests" -ForegroundColor Gray

# Create authentication headers
$jwtToken = New-TestJwtToken
$authHeaders = @{
    "Authorization" = "Bearer $jwtToken"
    "Content-Type"  = "application/json"
}

Write-Host "`nTesting Issue #1: Zero Page Parameters (Currently cause timeouts)" -ForegroundColor Yellow

# Issue 1: Zero page parameters should return 400, not timeout
$testResults += Test-ErrorScenarioFix -testName "Zero Page Number" -method "GET" `
    -url "$baseUrl/api/academics?pageNumber=0&pageSize=10" -headers $authHeaders `
    -expectedStatusCode 400 -description "Page number zero should return 400 Bad Request, not timeout"

$testResults += Test-ErrorScenarioFix -testName "Zero Page Size" -method "GET" `
    -url "$baseUrl/api/academics?pageNumber=1&pageSize=0" -headers $authHeaders `
    -expectedStatusCode 400 -description "Page size zero should return 400 Bad Request, not timeout"

Write-Host "`nTesting Issue #2: Alphanumeric IDs (Currently return 404)" -ForegroundColor Yellow

# Issue 2: Alphanumeric IDs should return 400, not 404
$testResults += Test-ErrorScenarioFix -testName "Alphanumeric Academic ID" -method "GET" `
    -url "$baseUrl/api/academics/abc" -headers $authHeaders `
    -expectedStatusCode 400 -description "Invalid GUID format should return 400 Bad Request, not 404 Not Found"

$testResults += Test-ErrorScenarioFix -testName "Alphanumeric Department ID" -method "GET" `
    -url "$baseUrl/api/departments/xyz123" -headers $authHeaders `
    -expectedStatusCode 400 -description "Invalid GUID format should return 400 Bad Request, not 404 Not Found"

Write-Host "`nTesting Issue #3: Content-Type Mismatches (Currently return 400)" -ForegroundColor Yellow

# Issue 3: Content-type mismatches should return 415, not 400
$xmlHeaders = @{
    "Authorization" = "Bearer $jwtToken"
    "Content-Type"  = "application/xml"
}

$testResults += Test-ErrorScenarioFix -testName "XML Content-Type" -method "POST" `
    -url "$baseUrl/api/academics" -headers $xmlHeaders -body "<xml>test</xml>" `
    -expectedStatusCode 415 -description "Unsupported media type should return 415 Unsupported Media Type, not 400"

$textHeaders = @{
    "Authorization" = "Bearer $jwtToken"
    "Content-Type"  = "text/plain"
}

$testResults += Test-ErrorScenarioFix -testName "Text Content-Type" -method "POST" `
    -url "$baseUrl/api/departments" -headers $textHeaders -body "plain text" `
    -expectedStatusCode 415 -description "Unsupported media type should return 415 Unsupported Media Type, not 400"

# Summary
Write-Host "`n=== FIX VERIFICATION SUMMARY ===" -ForegroundColor Cyan

$totalTests = $testResults.Count
$passedTests = ($testResults | Where-Object { $_.Success }).Count
$failedTests = $totalTests - $passedTests

Write-Host "Total Tests: $totalTests" -ForegroundColor White
Write-Host "Passed: $passedTests" -ForegroundColor Green
Write-Host "Failed: $failedTests" -ForegroundColor Red
Write-Host "Success Rate: $([math]::Round(($passedTests / $totalTests) * 100, 2))%" -ForegroundColor $(if ($passedTests -eq $totalTests) { 'Green' }else { 'Yellow' })

if ($failedTests -gt 0) {
    Write-Host "`nFAILED TESTS:" -ForegroundColor Red
    $testResults | Where-Object { -not $_.Success } | ForEach-Object {
        Write-Host "  • $($_.TestName): Expected $($_.Expected), Got $($_.Actual) - $($_.Description)" -ForegroundColor Red
    }
    
    Write-Host "`nFIXES NEEDED:" -ForegroundColor Yellow
    Write-Host "1. Add query parameter validation for pageNumber and pageSize (prevent zero values)" -ForegroundColor Gray
    Write-Host "2. Add route parameter validation for GUID format validation" -ForegroundColor Gray
    Write-Host "3. Add content-type validation middleware to return 415 for unsupported media types" -ForegroundColor Gray
}
else {
    Write-Host "`n🎉 ALL FIXES WORKING CORRECTLY!" -ForegroundColor Green
}

# Export results
$csvFile = "fix-verification-results.csv"
$testResults | Export-Csv -Path $csvFile -NoTypeInformation
Write-Host "`nDetailed results exported to: $csvFile" -ForegroundColor Gray
