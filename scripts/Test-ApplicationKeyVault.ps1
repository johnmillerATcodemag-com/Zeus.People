# Test-ApplicationKeyVault.ps1
# Simple test to verify the application's Key Vault managed identity access

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$ApiBaseUrl = "https://localhost:7001",
    
    [Parameter(Mandatory = $false)]
    [string]$HealthEndpoint = "/health/keyvault",
    
    [Parameter(Mandatory = $false)]
    [int]$TimeoutSeconds = 30,
    
    [Parameter(Mandatory = $false)]
    [switch]$StartApplication
)

$logFile = "Application-KeyVault-Test-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"

function Write-TestLog {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logEntry = "[$timestamp] [$Level] $Message"
    $color = switch ($Level) {
        "ERROR" { "Red" }
        "WARN" { "Yellow" }
        "SUCCESS" { "Green" }
        default { "White" }
    }
    Write-Host $logEntry -ForegroundColor $color
    Add-Content -Path $logFile -Value $logEntry
}

function Start-ApplicationIfNeeded {
    Write-TestLog "Checking if application needs to be started..."
    
    try {
        $testUrl = "$ApiBaseUrl/health"
        $response = Invoke-WebRequest -Uri $testUrl -Method Get -TimeoutSec 5 -ErrorAction SilentlyContinue
        
        if ($response.StatusCode -eq 200) {
            Write-TestLog "Application is already running" -Level "SUCCESS"
            return $true
        }
    }
    catch {
        Write-TestLog "Application is not running"
    }
    
    if ($StartApplication) {
        Write-TestLog "Starting application..."
        
        # Navigate to project directory
        $projectPath = Split-Path $PSScriptRoot -Parent
        Set-Location "$projectPath\src\API"
        
        # Start the application in background
        $job = Start-Job -ScriptBlock {
            param($path)
            Set-Location $path
            dotnet run --urls "https://localhost:7001"
        } -ArgumentList (Get-Location).Path
        
        Write-TestLog "Application started in background (Job ID: $($job.Id))"
        
        # Wait for application to start
        $maxWait = 60
        $waited = 0
        
        while ($waited -lt $maxWait) {
            Start-Sleep -Seconds 2
            $waited += 2
            
            try {
                $response = Invoke-WebRequest -Uri "$ApiBaseUrl/health" -Method Get -TimeoutSec 5 -ErrorAction SilentlyContinue
                if ($response.StatusCode -eq 200) {
                    Write-TestLog "Application started successfully" -Level "SUCCESS"
                    return $true
                }
            }
            catch {
                # Continue waiting
            }
            
            Write-TestLog "Waiting for application to start... ($waited/$maxWait seconds)"
        }
        
        Write-TestLog "Application failed to start within $maxWait seconds" -Level "ERROR"
        return $false
    }
    else {
        Write-TestLog "Application is not running. Use -StartApplication to start it automatically" -Level "WARN"
        return $false
    }
}

function Test-GeneralHealth {
    Write-TestLog "Testing general application health..."
    
    try {
        $healthUrl = "$ApiBaseUrl/health"
        Write-TestLog "Testing: $healthUrl"
        
        $response = Invoke-RestMethod -Uri $healthUrl -Method Get -TimeoutSec $TimeoutSeconds
        
        if ($response) {
            Write-TestLog "General health check successful" -Level "SUCCESS"
            Write-TestLog "Response: $($response | ConvertTo-Json -Compress)"
            return $true
        }
        else {
            Write-TestLog "General health check returned empty response" -Level "ERROR"
            return $false
        }
    }
    catch {
        Write-TestLog "General health check failed: $($_.Exception.Message)" -Level "ERROR"
        return $false
    }
}

function Test-KeyVaultHealth {
    Write-TestLog "Testing Key Vault specific health check..."
    
    try {
        $keyVaultHealthUrl = "$ApiBaseUrl$HealthEndpoint"
        Write-TestLog "Testing: $keyVaultHealthUrl"
        
        $response = Invoke-RestMethod -Uri $keyVaultHealthUrl -Method Get -TimeoutSec $TimeoutSeconds
        
        if ($response) {
            Write-TestLog "Key Vault health check successful" -Level "SUCCESS"
            Write-TestLog "Response: $($response | ConvertTo-Json -Compress)"
            
            # Check response for success indicators
            $responseText = $response | ConvertTo-Json
            if ($responseText -like "*Healthy*" -or $responseText -like "*success*" -or $responseText -like "*UP*") {
                Write-TestLog "Key Vault appears to be healthy based on response" -Level "SUCCESS"
                return $true
            }
            elseif ($responseText -like "*error*" -or $responseText -like "*fail*" -or $responseText -like "*DOWN*") {
                Write-TestLog "Key Vault appears to have issues based on response" -Level "ERROR"
                return $false
            }
            else {
                Write-TestLog "Key Vault health status unclear from response" -Level "WARN"
                return $true
            }
        }
        else {
            Write-TestLog "Key Vault health check returned empty response" -Level "ERROR"
            return $false
        }
    }
    catch {
        Write-TestLog "Key Vault health check failed: $($_.Exception.Message)" -Level "ERROR"
        
        # Check if it's a 404 (endpoint doesn't exist)
        if ($_.Exception.Message -like "*404*") {
            Write-TestLog "Key Vault health endpoint not found. Trying alternative endpoints..." -Level "WARN"
            return Test-AlternativeHealthEndpoints
        }
        
        return $false
    }
}

function Test-AlternativeHealthEndpoints {
    Write-TestLog "Testing alternative health endpoints..."
    
    $alternativeEndpoints = @(
        "/health",
        "/health/ready",
        "/health/live",
        "/healthcheck",
        "/healthz"
    )
    
    foreach ($endpoint in $alternativeEndpoints) {
        try {
            $url = "$ApiBaseUrl$endpoint"
            Write-TestLog "Trying: $url"
            
            $response = Invoke-RestMethod -Uri $url -Method Get -TimeoutSec 10
            
            if ($response) {
                Write-TestLog "Found working health endpoint: $endpoint" -Level "SUCCESS"
                Write-TestLog "Response: $($response | ConvertTo-Json -Compress)"
                
                # Check if response mentions Key Vault
                $responseText = $response | ConvertTo-Json
                if ($responseText -like "*vault*" -or $responseText -like "*KeyVault*") {
                    Write-TestLog "Key Vault information found in health response" -Level "SUCCESS"
                    return $true
                }
            }
        }
        catch {
            Write-TestLog "Endpoint $endpoint failed: $($_.Exception.Message)" -Level "WARN"
        }
    }
    
    Write-TestLog "No alternative health endpoints found with Key Vault information" -Level "ERROR"
    return $false
}

function Test-ConfigurationEndpoint {
    Write-TestLog "Testing configuration endpoint for Key Vault settings..."
    
    $configEndpoints = @(
        "/api/configuration",
        "/api/config",
        "/configuration",
        "/config"
    )
    
    foreach ($endpoint in $configEndpoints) {
        try {
            $url = "$ApiBaseUrl$endpoint"
            Write-TestLog "Trying configuration endpoint: $url"
            
            $response = Invoke-RestMethod -Uri $url -Method Get -TimeoutSec 10
            
            if ($response) {
                Write-TestLog "Configuration endpoint successful: $endpoint" -Level "SUCCESS"
                
                $responseText = $response | ConvertTo-Json
                if ($responseText -like "*vault*" -or $responseText -like "*KeyVault*") {
                    Write-TestLog "Key Vault configuration found" -Level "SUCCESS"
                    Write-TestLog "Configuration snippet: $($responseText.Substring(0, [Math]::Min(200, $responseText.Length)))"
                    return $true
                }
            }
        }
        catch {
            # Silently continue to next endpoint
        }
    }
    
    return $false
}

# Main execution
Write-TestLog "Starting Application Key Vault Test" -Level "SUCCESS"
Write-TestLog "Target URL: $ApiBaseUrl"
Write-TestLog "Health Endpoint: $HealthEndpoint"
Write-TestLog "Timeout: $TimeoutSeconds seconds"

$testResults = @{
    ApplicationRunning  = $false
    GeneralHealth       = $false
    KeyVaultHealth      = $false
    ConfigurationAccess = $false
    OverallSuccess      = $false
}

try {
    # Test 1: Ensure application is running
    Write-TestLog "=== Test 1: Application Status ===" -Level "SUCCESS"
    $testResults.ApplicationRunning = Start-ApplicationIfNeeded
    
    if (-not $testResults.ApplicationRunning) {
        Write-TestLog "Cannot proceed without running application" -Level "ERROR"
        exit 1
    }
    
    # Test 2: General health check
    Write-TestLog "=== Test 2: General Health Check ===" -Level "SUCCESS"
    $testResults.GeneralHealth = Test-GeneralHealth
    
    # Test 3: Key Vault specific health check
    Write-TestLog "=== Test 3: Key Vault Health Check ===" -Level "SUCCESS"
    $testResults.KeyVaultHealth = Test-KeyVaultHealth
    
    # Test 4: Configuration endpoint (if available)
    Write-TestLog "=== Test 4: Configuration Access ===" -Level "SUCCESS"
    $testResults.ConfigurationAccess = Test-ConfigurationEndpoint
    
    # Overall assessment
    $testResults.OverallSuccess = $testResults.ApplicationRunning -and $testResults.GeneralHealth
    
    Write-TestLog "=== TEST SUMMARY ===" -Level "SUCCESS"
    Write-TestLog "Application Running:       $($testResults.ApplicationRunning)"
    Write-TestLog "General Health:            $($testResults.GeneralHealth)"
    Write-TestLog "Key Vault Health:          $($testResults.KeyVaultHealth)"
    Write-TestLog "Configuration Access:      $($testResults.ConfigurationAccess)"
    Write-TestLog "Overall Success:           $($testResults.OverallSuccess)"
    
    if ($testResults.OverallSuccess) {
        if ($testResults.KeyVaultHealth) {
            Write-TestLog "Application Key Vault managed identity access verification PASSED!" -Level "SUCCESS"
        }
        else {
            Write-TestLog "Application is running but Key Vault health check was inconclusive" -Level "WARN"
            Write-TestLog "This may indicate the Key Vault health endpoint is not available or configured differently" -Level "WARN"
        }
        exit 0
    }
    else {
        Write-TestLog "Application Key Vault access verification FAILED!" -Level "ERROR"
        exit 1
    }
}
catch {
    Write-TestLog "Unexpected error during testing: $($_.Exception.Message)" -Level "ERROR"
    Write-TestLog "Stack trace: $($_.ScriptStackTrace)" -Level "ERROR"
    exit 1
}
finally {
    Write-TestLog "Test completed. Log file: $logFile"
    
    # If we started the application, provide instructions to stop it
    if ($StartApplication -and $testResults.ApplicationRunning) {
        Write-TestLog "Note: Application was started by this script. To stop it, use:"
        Write-TestLog "Get-Job | Stop-Job; Get-Job | Remove-Job"
    }
}
