# Zeus.People API Authentication Test
# Test script to confirm JWT authentication works

$baseUrl = "http://localhost:5169"

# JWT Configuration (from appsettings.Development.json)
$secretKey = "development-super-secret-key-for-jwt-that-is-at-least-32-characters-long"
$issuer = "Zeus.People.API.Dev"
$audience = "Zeus.People.Client.Dev"

Write-Host "🔐 ZEUS.PEOPLE API AUTHENTICATION TEST" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green
Write-Host ""

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

# Function to test API endpoint
function Test-AuthenticatedEndpoint {
    param(
        [string]$Endpoint,
        [string]$Token = $null,
        [string]$Description
    )
    
    try {
        $headers = @{}
        if ($Token) {
            $headers["Authorization"] = "Bearer $Token"
        }
        
        $response = Invoke-RestMethod -Uri "$baseUrl$Endpoint" -Headers $headers -Method GET -TimeoutSec 10
        return [PSCustomObject]@{
            Status      = "✅ SUCCESS"
            Description = $Description
            StatusCode  = 200
            HasData     = $response -ne $null
        }
    }
    catch {
        $statusCode = if ($_.Exception.Response) { 
            $_.Exception.Response.StatusCode.value__ 
        }
        else { 
            0 
        }
        
        return [PSCustomObject]@{
            Status      = if ($statusCode -eq 401) { "🔒 UNAUTHORIZED" } else { "❌ ERROR" }
            Description = $Description
            StatusCode  = $statusCode
            HasData     = $false
        }
    }
}

# Test 1: API Health Check (no auth required)
Write-Host "1. Testing Health Endpoint (No Authentication Required)" -ForegroundColor Cyan
$healthResult = Test-AuthenticatedEndpoint -Endpoint "/health" -Description "Health check endpoint"
Write-Host "   $($healthResult.Status) - $($healthResult.Description) [Status: $($healthResult.StatusCode)]"
Write-Host ""

# Test 2: Protected endpoint without token
Write-Host "2. Testing Protected Endpoint WITHOUT Token" -ForegroundColor Cyan
$noTokenResult = Test-AuthenticatedEndpoint -Endpoint "/api/academics" -Description "Academics endpoint without token"
Write-Host "   $($noTokenResult.Status) - $($noTokenResult.Description) [Status: $($noTokenResult.StatusCode)]"
Write-Host ""

# Test 3: Protected endpoint with invalid token
Write-Host "3. Testing Protected Endpoint WITH INVALID Token" -ForegroundColor Cyan
$invalidTokenResult = Test-AuthenticatedEndpoint -Endpoint "/api/academics" -Token "invalid.jwt.token" -Description "Academics endpoint with invalid token"
Write-Host "   $($invalidTokenResult.Status) - $($invalidTokenResult.Description) [Status: $($invalidTokenResult.StatusCode)]"
Write-Host ""

# Test 4: Generate valid JWT token
Write-Host "4. Generating Valid JWT Token" -ForegroundColor Cyan
$validToken = New-TestJwtToken -Role "User"
Write-Host "   ✅ Token generated successfully"
Write-Host "   Token preview: $($validToken.Substring(0, 50))..."
Write-Host ""

# Test 5: Protected endpoint with valid token
Write-Host "5. Testing Protected Endpoint WITH VALID Token" -ForegroundColor Cyan
$validTokenResult = Test-AuthenticatedEndpoint -Endpoint "/api/academics" -Token $validToken -Description "Academics endpoint with valid token"
Write-Host "   $($validTokenResult.Status) - $($validTokenResult.Description) [Status: $($validTokenResult.StatusCode)]"
Write-Host ""

# Test 6: Multiple endpoints with valid token
Write-Host "6. Testing Multiple Endpoints WITH VALID Token" -ForegroundColor Cyan
$endpoints = @(
    "/api/departments",
    "/api/rooms",
    "/api/academics?pageNumber=1&pageSize=10"
)

foreach ($endpoint in $endpoints) {
    $result = Test-AuthenticatedEndpoint -Endpoint $endpoint -Token $validToken -Description "Testing $endpoint"
    Write-Host "   $($result.Status) - $($result.Description) [Status: $($result.StatusCode)]"
}
Write-Host ""

# Authentication Summary
Write-Host "🏆 AUTHENTICATION TEST SUMMARY" -ForegroundColor Yellow
Write-Host "==============================" -ForegroundColor Yellow

$allResults = @($healthResult, $noTokenResult, $invalidTokenResult, $validTokenResult)
$successCount = ($allResults | Where-Object { $_.Status -eq "✅ SUCCESS" }).Count
$unauthorizedCount = ($allResults | Where-Object { $_.Status -eq "🔒 UNAUTHORIZED" }).Count

Write-Host ""
Write-Host "Health Check (No Auth): " -NoNewline
Write-Host "$($healthResult.Status)" -ForegroundColor $(if ($healthResult.Status -eq "✅ SUCCESS") { "Green" } else { "Red" })

Write-Host "No Token (Should be 401): " -NoNewline  
Write-Host "$($noTokenResult.Status)" -ForegroundColor $(if ($noTokenResult.StatusCode -eq 401) { "Green" } else { "Red" })

Write-Host "Invalid Token (Should be 401): " -NoNewline
Write-Host "$($invalidTokenResult.Status)" -ForegroundColor $(if ($invalidTokenResult.StatusCode -eq 401) { "Green" } else { "Red" })

Write-Host "Valid Token (Should be 200): " -NoNewline
Write-Host "$($validTokenResult.Status)" -ForegroundColor $(if ($validTokenResult.Status -eq "✅ SUCCESS") { "Green" } else { "Red" })

Write-Host ""
if ($healthResult.Status -eq "✅ SUCCESS" -and $noTokenResult.StatusCode -eq 401 -and $invalidTokenResult.StatusCode -eq 401 -and $validTokenResult.Status -eq "✅ SUCCESS") {
    Write-Host "🎉 AUTHENTICATION WORKING CORRECTLY!" -ForegroundColor Green
    Write-Host "✅ Health endpoint accessible without authentication" -ForegroundColor Green
    Write-Host "✅ Protected endpoints reject requests without tokens (401)" -ForegroundColor Green
    Write-Host "✅ Protected endpoints reject invalid tokens (401)" -ForegroundColor Green
    Write-Host "✅ Protected endpoints accept valid JWT tokens (200)" -ForegroundColor Green
}
else {
    Write-Host "⚠️ AUTHENTICATION ISSUES DETECTED" -ForegroundColor Red
    Write-Host "Please check API configuration and ensure it's running on $baseUrl" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "💡 Note: Make sure the Zeus.People API is running on $baseUrl before running this test" -ForegroundColor Cyan
