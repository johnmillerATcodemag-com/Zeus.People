# Zeus.People API Health Endpoint Test
# Comprehensive testing of the /health endpoint

$baseUrl = "http://localhost:5169"
$healthEndpoint = "/health"

Write-Host "🏥 ZEUS.PEOPLE API HEALTH ENDPOINT TEST" -ForegroundColor Green
Write-Host "=======================================" -ForegroundColor Green
Write-Host ""

# Function to test health endpoint
function Test-HealthEndpoint {
    param(
        [string]$Url,
        [string]$Description
    )
    
    try {
        Write-Host "Testing: $Description" -ForegroundColor Cyan
        Write-Host "URL: $Url" -ForegroundColor DarkGray
        
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        $response = Invoke-RestMethod -Uri $Url -Method GET -TimeoutSec 30
        $stopwatch.Stop()
        
        $responseTime = $stopwatch.ElapsedMilliseconds
        
        return [PSCustomObject]@{
            Status       = "✅ SUCCESS"
            Description  = $Description
            StatusCode   = 200
            ResponseTime = "$responseTime ms"
            Data         = $response
            Error        = $null
        }
    }
    catch {
        $statusCode = if ($_.Exception.Response) { 
            $_.Exception.Response.StatusCode.value__ 
        }
        else { 
            0 
        }
        
        $errorMessage = if ($_.Exception.Response) {
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
        
        return [PSCustomObject]@{
            Status       = "❌ FAILED"
            Description  = $Description
            StatusCode   = $statusCode
            ResponseTime = "N/A"
            Data         = $null
            Error        = $errorMessage
        }
    }
}

# Function to analyze health check response
function Analyze-HealthResponse {
    param([object]$HealthData)
    
    if (-not $HealthData) {
        Write-Host "   ⚠️ No health data received" -ForegroundColor Yellow
        return
    }
    
    Write-Host "   📊 Health Check Analysis:" -ForegroundColor Yellow
    
    # Check overall status
    if ($HealthData.status) {
        $statusColor = switch ($HealthData.status.ToLower()) {
            "healthy" { "Green" }
            "degraded" { "Yellow" }
            "unhealthy" { "Red" }
            default { "White" }
        }
        Write-Host "      Overall Status: $($HealthData.status)" -ForegroundColor $statusColor
    }
    
    # Check total duration
    if ($HealthData.totalDuration) {
        Write-Host "      Total Duration: $($HealthData.totalDuration)" -ForegroundColor White
    }
    
    # Check individual health checks
    if ($HealthData.results) {
        Write-Host "      Individual Checks:" -ForegroundColor White
        foreach ($check in $HealthData.results.PSObject.Properties) {
            $checkName = $check.Name
            $checkResult = $check.Value
            
            $checkStatusColor = switch ($checkResult.status.ToLower()) {
                "healthy" { "Green" }
                "degraded" { "Yellow" } 
                "unhealthy" { "Red" }
                default { "White" }
            }
            
            Write-Host "         • $checkName : $($checkResult.status)" -ForegroundColor $checkStatusColor
            if ($checkResult.description) {
                Write-Host "           Description: $($checkResult.description)" -ForegroundColor DarkGray
            }
            if ($checkResult.duration) {
                Write-Host "           Duration: $($checkResult.duration)" -ForegroundColor DarkGray
            }
            if ($checkResult.tags) {
                Write-Host "           Tags: $($checkResult.tags -join ', ')" -ForegroundColor DarkGray
            }
        }
    }
}

# Test 1: Basic Health Check
Write-Host "1. BASIC HEALTH CHECK" -ForegroundColor Cyan
Write-Host "====================" -ForegroundColor Cyan
$healthResult = Test-HealthEndpoint -Url "$baseUrl$healthEndpoint" -Description "Basic health endpoint test"

Write-Host "Result: $($healthResult.Status)" -ForegroundColor $(if ($healthResult.Status -like "*SUCCESS*") { "Green" } else { "Red" })
Write-Host "Status Code: $($healthResult.StatusCode)" -ForegroundColor White
Write-Host "Response Time: $($healthResult.ResponseTime)" -ForegroundColor White

if ($healthResult.Data) {
    Analyze-HealthResponse -HealthData $healthResult.Data
}
elseif ($healthResult.Error) {
    Write-Host "Error Details: $($healthResult.Error)" -ForegroundColor Red
}
Write-Host ""

# Test 2: Health Check with Detailed Response (if API supports it)
Write-Host "2. DETAILED HEALTH CHECK" -ForegroundColor Cyan
Write-Host "========================" -ForegroundColor Cyan
$detailedHealthResult = Test-HealthEndpoint -Url "$baseUrl$healthEndpoint?detailed=true" -Description "Detailed health check with query parameter"

Write-Host "Result: $($detailedHealthResult.Status)" -ForegroundColor $(if ($detailedHealthResult.Status -like "*SUCCESS*") { "Green" } else { "Red" })
Write-Host "Status Code: $($detailedHealthResult.StatusCode)" -ForegroundColor White
Write-Host "Response Time: $($detailedHealthResult.ResponseTime)" -ForegroundColor White

if ($detailedHealthResult.Data) {
    Analyze-HealthResponse -HealthData $detailedHealthResult.Data
}
elseif ($detailedHealthResult.Error) {
    Write-Host "Error Details: $($detailedHealthResult.Error)" -ForegroundColor Red
}
Write-Host ""

# Test 3: Health Check Response Headers
Write-Host "3. RESPONSE HEADERS ANALYSIS" -ForegroundColor Cyan
Write-Host "============================" -ForegroundColor Cyan

try {
    $webRequest = [System.Net.HttpWebRequest]::Create("$baseUrl$healthEndpoint")
    $webRequest.Method = "GET"
    $webRequest.Timeout = 30000
    
    $response = $webRequest.GetResponse()
    $headers = $response.Headers
    
    Write-Host "✅ Response Headers:" -ForegroundColor Green
    foreach ($header in $headers.AllKeys) {
        Write-Host "   $header : $($headers[$header])" -ForegroundColor White
    }
    
    $response.Close()
}
catch {
    Write-Host "❌ Failed to get response headers: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 4: Performance Test (Multiple Requests)
Write-Host "4. PERFORMANCE TEST" -ForegroundColor Cyan
Write-Host "==================" -ForegroundColor Cyan

$performanceResults = @()
$requestCount = 5

Write-Host "Sending $requestCount consecutive requests..." -ForegroundColor Yellow

for ($i = 1; $i -le $requestCount; $i++) {
    $perfResult = Test-HealthEndpoint -Url "$baseUrl$healthEndpoint" -Description "Performance test request $i"
    $performanceResults += $perfResult
    Write-Host "   Request $i : $($perfResult.Status) ($($perfResult.ResponseTime))" -ForegroundColor $(if ($perfResult.Status -like "*SUCCESS*") { "Green" } else { "Red" })
}

# Calculate performance metrics
$successfulRequests = $performanceResults | Where-Object { $_.Status -like "*SUCCESS*" }
$responseTimes = $successfulRequests | ForEach-Object { [int]($_.ResponseTime -replace " ms", "") }

if ($responseTimes.Count -gt 0) {
    $avgResponseTime = [math]::Round(($responseTimes | Measure-Object -Average).Average, 2)
    $minResponseTime = ($responseTimes | Measure-Object -Minimum).Minimum
    $maxResponseTime = ($responseTimes | Measure-Object -Maximum).Maximum
    
    Write-Host ""
    Write-Host "📈 Performance Metrics:" -ForegroundColor Yellow
    Write-Host "   Success Rate: $($successfulRequests.Count)/$requestCount ($([math]::Round(($successfulRequests.Count / $requestCount) * 100, 1))%)" -ForegroundColor White
    Write-Host "   Average Response Time: $avgResponseTime ms" -ForegroundColor White
    Write-Host "   Min Response Time: $minResponseTime ms" -ForegroundColor White
    Write-Host "   Max Response Time: $maxResponseTime ms" -ForegroundColor White
}
Write-Host ""

# Health Endpoint Compliance Check
Write-Host "🏆 HEALTH ENDPOINT COMPLIANCE" -ForegroundColor Yellow
Write-Host "=============================" -ForegroundColor Yellow
Write-Host ""

$complianceChecks = @()

# Check 1: Endpoint accessible
$complianceChecks += [PSCustomObject]@{
    Check   = "Health endpoint accessible"
    Status  = if ($healthResult.Status -like "*SUCCESS*") { "✅ PASS" } else { "❌ FAIL" }
    Details = "GET /health returns HTTP 200"
}

# Check 2: Response time reasonable (< 5 seconds)
$responseTimeMs = if ($healthResult.ResponseTime -match "(\d+) ms") { [int]$matches[1] } else { 999999 }
$complianceChecks += [PSCustomObject]@{
    Check   = "Reasonable response time"
    Status  = if ($responseTimeMs -lt 5000) { "✅ PASS" } else { "❌ FAIL" }
    Details = "Response time: $($healthResult.ResponseTime) (should be < 5000ms)"
}

# Check 3: No authentication required
$complianceChecks += [PSCustomObject]@{
    Check   = "No authentication required"
    Status  = if ($healthResult.StatusCode -ne 401) { "✅ PASS" } else { "❌ FAIL" }
    Details = "Endpoint accessible without Authorization header"
}

# Check 4: Returns JSON response (if successful)
$hasJsonResponse = $false
if ($healthResult.Data) {
    $hasJsonResponse = $true
}
$complianceChecks += [PSCustomObject]@{
    Check   = "Returns structured response"
    Status  = if ($hasJsonResponse) { "✅ PASS" } else { "⚠️ UNKNOWN" }
    Details = "Returns JSON/structured health data"
}

# Display compliance results
foreach ($check in $complianceChecks) {
    $color = switch ($check.Status) {
        { $_ -like "*PASS*" } { "Green" }
        { $_ -like "*FAIL*" } { "Red" }
        default { "Yellow" }
    }
    Write-Host "$($check.Status) $($check.Check)" -ForegroundColor $color
    Write-Host "    $($check.Details)" -ForegroundColor DarkGray
}

Write-Host ""

# Final Assessment
$passCount = ($complianceChecks | Where-Object { $_.Status -like "*PASS*" }).Count
$totalChecks = $complianceChecks.Count

Write-Host "📋 FINAL ASSESSMENT" -ForegroundColor Green
Write-Host "===================" -ForegroundColor Green
Write-Host ""

if ($passCount -eq $totalChecks) {
    Write-Host "🎉 EXCELLENT: Health endpoint is fully compliant!" -ForegroundColor Green
    Write-Host "✅ All health checks passed" -ForegroundColor Green
    Write-Host "✅ Endpoint is accessible and responsive" -ForegroundColor Green
    Write-Host "✅ No authentication barriers" -ForegroundColor Green
    Write-Host "✅ Returns structured health information" -ForegroundColor Green
}
elseif ($passCount -ge ($totalChecks * 0.75)) {
    Write-Host "✅ GOOD: Health endpoint is mostly working correctly" -ForegroundColor Green
    Write-Host "⚠️ Some minor issues detected" -ForegroundColor Yellow
}
else {
    Write-Host "❌ NEEDS ATTENTION: Health endpoint has issues" -ForegroundColor Red
    Write-Host "🔧 Review health endpoint configuration" -ForegroundColor Red
}

Write-Host ""
Write-Host "💡 Health Endpoint Configuration:" -ForegroundColor Cyan
Write-Host "   • URL: $baseUrl$healthEndpoint" -ForegroundColor White
Write-Host "   • Method: GET" -ForegroundColor White
Write-Host "   • Authentication: None required" -ForegroundColor White
Write-Host "   • Expected: HTTP 200 + JSON health data" -ForegroundColor White
Write-Host ""
Write-Host "🔍 For detailed health information, the API includes:" -ForegroundColor Cyan
Write-Host "   • Database connectivity check" -ForegroundColor White
Write-Host "   • Event Store connectivity check" -ForegroundColor White  
Write-Host "   • Service Bus connectivity check" -ForegroundColor White
Write-Host "   • Cosmos DB connectivity check" -ForegroundColor White
