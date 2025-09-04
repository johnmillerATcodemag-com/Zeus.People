# Zeus.People API Health Endpoint Test Results
# Comprehensive health endpoint verification

Write-Host "🏥 ZEUS.PEOPLE API HEALTH ENDPOINT VERIFICATION" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host ""

# Test with a simple curl command to get raw response
try {
    Write-Host "📡 Testing Health Endpoint Response..." -ForegroundColor Cyan
    
    $response = Invoke-WebRequest -Uri "http://localhost:5169/health" -Method GET -TimeoutSec 30
    $statusCode = $response.StatusCode
    $content = $response.Content
    
    Write-Host "✅ Health endpoint is accessible!" -ForegroundColor Green
    Write-Host "   Status Code: $statusCode" -ForegroundColor White
    Write-Host "   Content Type: $($response.Headers['Content-Type'])" -ForegroundColor White
    Write-Host "   Response Length: $($content.Length) characters" -ForegroundColor White
    Write-Host ""
    
    Write-Host "📄 Raw Response Content:" -ForegroundColor Yellow
    Write-Host $content -ForegroundColor White
    Write-Host ""
    
    # Parse as JSON if possible
    try {
        $healthData = $content | ConvertFrom-Json
        Write-Host "📊 Parsed Health Data:" -ForegroundColor Yellow
        
        if ($healthData.status) {
            $statusColor = switch ($healthData.status.ToLower()) {
                "healthy" { "Green" }
                "degraded" { "Yellow" }
                "unhealthy" { "Red" }
                default { "White" }
            }
            Write-Host "   Overall Status: $($healthData.status)" -ForegroundColor $statusColor
        }
        
        if ($healthData.totalDuration) {
            Write-Host "   Total Duration: $($healthData.totalDuration)" -ForegroundColor White
        }
        
        if ($healthData.results) {
            Write-Host "   Individual Health Checks:" -ForegroundColor White
            foreach ($prop in $healthData.results.PSObject.Properties) {
                $checkName = $prop.Name
                $checkData = $prop.Value
                
                $checkColor = switch ($checkData.status.ToLower()) {
                    "healthy" { "Green" }
                    "degraded" { "Yellow" }
                    "unhealthy" { "Red" }
                    default { "White" }
                }
                
                Write-Host "      • $checkName" -ForegroundColor $checkColor
                Write-Host "        Status: $($checkData.status)" -ForegroundColor $checkColor
                if ($checkData.description) {
                    Write-Host "        Description: $($checkData.description)" -ForegroundColor DarkGray
                }
                if ($checkData.duration) {
                    Write-Host "        Duration: $($checkData.duration)" -ForegroundColor DarkGray
                }
                if ($checkData.tags) {
                    Write-Host "        Tags: $($checkData.tags -join ', ')" -ForegroundColor DarkGray
                }
            }
        }
    }
    catch {
        Write-Host "⚠️ Response is not valid JSON" -ForegroundColor Yellow
    }
}
catch {
    $statusCode = if ($_.Exception.Response) { 
        $_.Exception.Response.StatusCode.value__ 
    }
    else { 
        0 
    }
    
    Write-Host "❌ Health endpoint test failed" -ForegroundColor Red
    Write-Host "   Status Code: $statusCode" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "🔍 HEALTH ENDPOINT ANALYSIS" -ForegroundColor Cyan
Write-Host "===========================" -ForegroundColor Cyan
Write-Host ""

# Check what we can determine about the health endpoint
Write-Host "📋 Health Endpoint Characteristics:" -ForegroundColor Yellow
Write-Host "   • URL: http://localhost:5169/health" -ForegroundColor White
Write-Host "   • Method: GET" -ForegroundColor White
Write-Host "   • Authentication: None required" -ForegroundColor White
Write-Host "   • Response Format: Plain text or JSON" -ForegroundColor White
Write-Host ""

Write-Host "💡 Expected Behavior in Development:" -ForegroundColor Yellow
Write-Host "   • Health endpoint should be accessible" -ForegroundColor White
Write-Host "   • May return 503 if external dependencies are unavailable" -ForegroundColor White
Write-Host "   • Should provide detailed health check information" -ForegroundColor White
Write-Host "   • Individual checks may fail in dev environment" -ForegroundColor White
Write-Host ""

Write-Host "🏗️ Health Checks Configured:" -ForegroundColor Yellow
Write-Host "   • Database (SQL Server) - Should work with LocalDB" -ForegroundColor White
Write-Host "   • Event Store (SQL Server) - Should work with LocalDB" -ForegroundColor White
Write-Host "   • Service Bus - May fail without Azure Service Bus" -ForegroundColor White
Write-Host "   • Cosmos DB - May fail without Cosmos DB Emulator" -ForegroundColor White
Write-Host ""

Write-Host "✅ HEALTH ENDPOINT VERIFICATION COMPLETE" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
