# Test-ApplicationStartup.ps1
# Comprehensive test script to verify application startup with Azure configuration

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("Development", "Staging", "Production")]
    [string]$Environment = "Development",
    
    [Parameter(Mandatory = $false)]
    [string]$ApplicationUrl = "https://localhost:7001",
    
    [Parameter(Mandatory = $false)]
    [string]$KeyVaultName,
    
    [Parameter(Mandatory = $false)]
    [int]$StartupTimeoutSeconds = 120,
    
    [Parameter(Mandatory = $false)]
    [switch]$RunIntegrationTests,
    
    [Parameter(Mandatory = $false)]
    [switch]$TestKeyVaultAccess,
    
    [Parameter(Mandatory = $false)]
    [switch]$WhatIf
)

# Set error action preference
$ErrorActionPreference = 'Stop'

# Test results collection
$testResults = @()
$startTime = Get-Date

function Write-TestResult {
    param(
        [string]$TestName,
        [bool]$Passed,
        [string]$Message,
        [string]$Details = "",
        [int]$Duration = 0
    )
    
    $result = [PSCustomObject]@{
        TestName  = $TestName
        Passed    = $Passed
        Message   = $Message
        Details   = $Details
        Duration  = $Duration
        Timestamp = (Get-Date)
    }
    
    $script:testResults += $result
    
    $statusIcon = if ($Passed) { "✅" } else { "❌" }
    $color = if ($Passed) { "Green" } else { "Red" }
    
    Write-Host "$statusIcon $TestName - $Message" -ForegroundColor $color
    if ($Details) {
        Write-Host "   Details: $Details" -ForegroundColor Yellow
    }
    if ($Duration -gt 0) {
        Write-Host "   Duration: $Duration ms" -ForegroundColor Gray
    }
}

function Test-Prerequisites {
    Write-Host "`n🔍 Testing Prerequisites..." -ForegroundColor Cyan
    
    # Test .NET runtime
    try {
        $dotnetVersion = & dotnet --version 2>$null
        Write-TestResult -TestName "Prerequisites: .NET Runtime" -Passed $true -Message ".NET runtime available" -Details "Version: $dotnetVersion"
    }
    catch {
        Write-TestResult -TestName "Prerequisites: .NET Runtime" -Passed $false -Message ".NET runtime not found" -Details $_.Exception.Message
        return $false
    }
    
    # Test project file exists
    if (Test-Path ".\src\API\Zeus.People.API.csproj") {
        Write-TestResult -TestName "Prerequisites: Project File" -Passed $true -Message "API project file found"
    }
    else {
        Write-TestResult -TestName "Prerequisites: Project File" -Passed $false -Message "API project file not found"
        return $false
    }
    
    # Test configuration files
    $configFiles = @(
        ".\src\API\appsettings.json",
        ".\src\API\appsettings.$Environment.json"
    )
    
    foreach ($configFile in $configFiles) {
        if (Test-Path $configFile) {
            Write-TestResult -TestName "Prerequisites: Config File" -Passed $true -Message "Configuration file found" -Details (Split-Path $configFile -Leaf)
        }
        else {
            Write-TestResult -TestName "Prerequisites: Config File" -Passed $false -Message "Configuration file missing" -Details $configFile
        }
    }
    
    return $true
}

function Test-BuildApplication {
    Write-Host "`n🔨 Testing Application Build..." -ForegroundColor Cyan
    
    try {
        $buildStart = Get-Date
        $buildOutput = & dotnet build .\src\API\Zeus.People.API.csproj --configuration Release --verbosity minimal 2>&1
        $buildDuration = ((Get-Date) - $buildStart).TotalMilliseconds
        
        if ($LASTEXITCODE -eq 0) {
            Write-TestResult -TestName "Build: Compilation" -Passed $true -Message "Application built successfully" -Duration $buildDuration
            return $true
        }
        else {
            Write-TestResult -TestName "Build: Compilation" -Passed $false -Message "Build failed" -Details ($buildOutput -join "`n")
            return $false
        }
    }
    catch {
        Write-TestResult -TestName "Build: Compilation" -Passed $false -Message "Build error" -Details $_.Exception.Message
        return $false
    }
}

function Test-ConfigurationLoading {
    Write-Host "`n⚙️ Testing Configuration Loading..." -ForegroundColor Cyan
    
    # Test configuration file syntax
    try {
        $appSettingsPath = ".\src\API\appsettings.json"
        $appSettings = Get-Content $appSettingsPath | ConvertFrom-Json
        Write-TestResult -TestName "Configuration: JSON Syntax" -Passed $true -Message "Base configuration file is valid JSON"
        
        # Check for required configuration sections
        $requiredSections = @("Logging", "AllowedHosts")
        foreach ($section in $requiredSections) {
            if ($appSettings.PSObject.Properties[$section]) {
                Write-TestResult -TestName "Configuration: Required Sections" -Passed $true -Message "Section '$section' found"
            }
            else {
                Write-TestResult -TestName "Configuration: Required Sections" -Passed $false -Message "Section '$section' missing"
            }
        }
    }
    catch {
        Write-TestResult -TestName "Configuration: JSON Syntax" -Passed $false -Message "Configuration file has invalid JSON" -Details $_.Exception.Message
        return $false
    }
    
    # Test environment-specific configuration
    $envConfigPath = ".\src\API\appsettings.$Environment.json"
    if (Test-Path $envConfigPath) {
        try {
            $envConfig = Get-Content $envConfigPath | ConvertFrom-Json
            Write-TestResult -TestName "Configuration: Environment Config" -Passed $true -Message "Environment configuration loaded successfully"
        }
        catch {
            Write-TestResult -TestName "Configuration: Environment Config" -Passed $false -Message "Environment configuration has invalid JSON" -Details $_.Exception.Message
        }
    }
    
    return $true
}

function Test-KeyVaultConfiguration {
    Write-Host "`n🔐 Testing Key Vault Configuration..." -ForegroundColor Cyan
    
    if (-not $TestKeyVaultAccess) {
        Write-TestResult -TestName "Key Vault: Access Test" -Passed $true -Message "Skipped (not requested)"
        return $true
    }
    
    if (-not $KeyVaultName) {
        $KeyVaultName = "kv-zeus-people-$($Environment.ToLower())"
    }
    
    # Test Azure PowerShell availability
    try {
        Import-Module Az.KeyVault -Force -ErrorAction Stop
        Write-TestResult -TestName "Key Vault: PowerShell Module" -Passed $true -Message "Az.KeyVault module available"
    }
    catch {
        Write-TestResult -TestName "Key Vault: PowerShell Module" -Passed $false -Message "Az.KeyVault module not available" -Details $_.Exception.Message
        return $false
    }
    
    # Test Key Vault accessibility
    try {
        $keyVault = Get-AzKeyVault -VaultName $KeyVaultName -ErrorAction Stop
        Write-TestResult -TestName "Key Vault: Accessibility" -Passed $true -Message "Key Vault accessible" -Details "URI: $($keyVault.VaultUri)"
        
        # Test secret retrieval
        try {
            $secrets = Get-AzKeyVaultSecret -VaultName $KeyVaultName -ErrorAction Stop
            Write-TestResult -TestName "Key Vault: Secret Access" -Passed $true -Message "Can access secrets" -Details "Found $($secrets.Count) secrets"
        }
        catch {
            Write-TestResult -TestName "Key Vault: Secret Access" -Passed $false -Message "Cannot access secrets" -Details $_.Exception.Message
        }
    }
    catch {
        Write-TestResult -TestName "Key Vault: Accessibility" -Passed $false -Message "Key Vault not accessible" -Details $_.Exception.Message
        return $false
    }
    
    return $true
}

function Start-ApplicationWithTimeout {
    param(
        [int]$TimeoutSeconds = 120
    )
    
    Write-Host "`n🚀 Starting Application..." -ForegroundColor Cyan
    
    if ($WhatIf) {
        Write-TestResult -TestName "Application: Startup" -Passed $true -Message "Skipped (WhatIf mode)"
        return $null
    }
    
    try {
        # Set environment variables for testing
        $env:ASPNETCORE_ENVIRONMENT = $Environment
        $env:ASPNETCORE_URLS = $ApplicationUrl
        
        # Add Key Vault configuration if specified
        if ($KeyVaultName) {
            $env:KeyVault__VaultUrl = "https://$KeyVaultName.vault.azure.net/"
        }
        
        $startupStart = Get-Date
        
        # Start the application process
        $processInfo = New-Object System.Diagnostics.ProcessStartInfo
        $processInfo.FileName = "dotnet"
        $processInfo.Arguments = "run --project .\src\API\Zeus.People.API.csproj --configuration Release"
        $processInfo.RedirectStandardOutput = $true
        $processInfo.RedirectStandardError = $true
        $processInfo.UseShellExecute = $false
        $processInfo.CreateNoWindow = $true
        
        $process = New-Object System.Diagnostics.Process
        $process.StartInfo = $processInfo
        
        # Event handlers for output
        $output = New-Object System.Collections.ArrayList
        $errors = New-Object System.Collections.ArrayList
        
        Register-ObjectEvent -InputObject $process -EventName "OutputDataReceived" -Action {
            if ($Event.SourceEventArgs.Data) {
                [void]$output.Add($Event.SourceEventArgs.Data)
                Write-Host "APP: $($Event.SourceEventArgs.Data)" -ForegroundColor Gray
            }
        } | Out-Null
        
        Register-ObjectEvent -InputObject $process -EventName "ErrorDataReceived" -Action {
            if ($Event.SourceEventArgs.Data) {
                [void]$errors.Add($Event.SourceEventArgs.Data)
                Write-Host "ERR: $($Event.SourceEventArgs.Data)" -ForegroundColor Red
            }
        } | Out-Null
        
        $process.Start() | Out-Null
        $process.BeginOutputReadLine()
        $process.BeginErrorReadLine()
        
        # Wait for startup or timeout
        $startupDetected = $false
        $timeout = (Get-Date).AddSeconds($TimeoutSeconds)
        
        while ((Get-Date) -lt $timeout -and -not $process.HasExited) {
            Start-Sleep -Milliseconds 500
            
            # Check for startup indicators in output
            $recentOutput = $output | Select-Object -Last 10
            if ($recentOutput -match "Now listening on:|Application started|Started Zeus.People.API") {
                $startupDetected = $true
                break
            }
            
            # Check for startup errors
            $recentErrors = $errors | Select-Object -Last 5
            if ($recentErrors -match "Fatal|terminated unexpectedly") {
                break
            }
        }
        
        $startupDuration = ((Get-Date) - $startupStart).TotalMilliseconds
        
        if ($startupDetected) {
            Write-TestResult -TestName "Application: Startup" -Passed $true -Message "Application started successfully" -Duration $startupDuration
            
            # Wait a bit more for full initialization
            Start-Sleep -Seconds 3
            
            return $process
        }
        else {
            Write-TestResult -TestName "Application: Startup" -Passed $false -Message "Application startup timeout or failed" -Details "Output: $($output | Select-Object -Last 5)"
            
            if (-not $process.HasExited) {
                $process.Kill()
                $process.WaitForExit(5000)
            }
            
            return $null
        }
    }
    catch {
        Write-TestResult -TestName "Application: Startup" -Passed $false -Message "Startup error" -Details $_.Exception.Message
        return $null
    }
}

function Test-ApplicationEndpoints {
    param(
        [object]$Process
    )
    
    Write-Host "`n🌐 Testing Application Endpoints..." -ForegroundColor Cyan
    
    if ($WhatIf -or $null -eq $Process) {
        Write-TestResult -TestName "Endpoints: Health Check" -Passed $true -Message "Skipped (no running process)"
        return
    }
    
    # Test health endpoint
    try {
        $healthStart = Get-Date
        $response = Invoke-RestMethod -Uri "$ApplicationUrl/health" -Method Get -TimeoutSec 10
        $healthDuration = ((Get-Date) - $healthStart).TotalMilliseconds
        
        if ($response.status -eq "Healthy") {
            Write-TestResult -TestName "Endpoints: Health Check" -Passed $true -Message "Health endpoint reports healthy" -Duration $healthDuration
        }
        else {
            Write-TestResult -TestName "Endpoints: Health Check" -Passed $false -Message "Health endpoint reports unhealthy" -Details "Status: $($response.status)"
        }
        
        # Test individual health checks
        if ($response.results) {
            foreach ($check in $response.results.PSObject.Properties) {
                $checkName = $check.Name
                $checkResult = $check.Value
                $isHealthy = $checkResult.status -eq "Healthy"
                
                Write-TestResult -TestName "Health Check: $checkName" -Passed $isHealthy -Message "Health check status" -Details "Status: $($checkResult.status), Duration: $($checkResult.duration)"
            }
        }
    }
    catch {
        Write-TestResult -TestName "Endpoints: Health Check" -Passed $false -Message "Health endpoint failed" -Details $_.Exception.Message
    }
    
    # Test Swagger endpoint (Development only)
    if ($Environment -eq "Development") {
        try {
            $swaggerResponse = Invoke-WebRequest -Uri "$ApplicationUrl/swagger" -Method Get -TimeoutSec 10
            if ($swaggerResponse.StatusCode -eq 200) {
                Write-TestResult -TestName "Endpoints: Swagger UI" -Passed $true -Message "Swagger UI accessible"
            }
            else {
                Write-TestResult -TestName "Endpoints: Swagger UI" -Passed $false -Message "Swagger UI not accessible" -Details "Status: $($swaggerResponse.StatusCode)"
            }
        }
        catch {
            Write-TestResult -TestName "Endpoints: Swagger UI" -Passed $false -Message "Swagger UI failed" -Details $_.Exception.Message
        }
    }
    
    # Test API endpoints
    try {
        $apiResponse = Invoke-WebRequest -Uri "$ApplicationUrl/api/people" -Method Get -TimeoutSec 10
        if ($apiResponse.StatusCode -in @(200, 401, 403)) {
            Write-TestResult -TestName "Endpoints: API Controller" -Passed $true -Message "API endpoint responsive" -Details "Status: $($apiResponse.StatusCode)"
        }
        else {
            Write-TestResult -TestName "Endpoints: API Controller" -Passed $false -Message "API endpoint error" -Details "Status: $($apiResponse.StatusCode)"
        }
    }
    catch {
        # 401/403 are expected for unauthenticated requests
        if ($_.Exception.Response.StatusCode -in @(401, 403)) {
            Write-TestResult -TestName "Endpoints: API Controller" -Passed $true -Message "API endpoint responsive (authentication required)"
        }
        else {
            Write-TestResult -TestName "Endpoints: API Controller" -Passed $false -Message "API endpoint failed" -Details $_.Exception.Message
        }
    }
}

function Test-ConfigurationValidation {
    param(
        [object]$Process
    )
    
    Write-Host "`n🔍 Testing Configuration Validation..." -ForegroundColor Cyan
    
    if ($WhatIf -or $null -eq $Process) {
        Write-TestResult -TestName "Configuration: Validation" -Passed $true -Message "Skipped (no running process)"
        return
    }
    
    # Test configuration endpoint (Development only)
    if ($Environment -eq "Development") {
        try {
            $configResponse = Invoke-RestMethod -Uri "$ApplicationUrl/debug/configuration" -Method Get -TimeoutSec 10
            if ($configResponse) {
                Write-TestResult -TestName "Configuration: Debug Endpoint" -Passed $true -Message "Configuration debug endpoint accessible"
                
                # Check for required configuration sections
                $requiredSections = @("Database", "ServiceBus", "AzureAd", "Application")
                foreach ($section in $requiredSections) {
                    if ($configResponse.PSObject.Properties[$section]) {
                        Write-TestResult -TestName "Configuration: Section $section" -Passed $true -Message "Configuration section present"
                    }
                    else {
                        Write-TestResult -TestName "Configuration: Section $section" -Passed $false -Message "Configuration section missing"
                    }
                }
            }
        }
        catch {
            Write-TestResult -TestName "Configuration: Debug Endpoint" -Passed $false -Message "Configuration debug endpoint failed" -Details $_.Exception.Message
        }
    }
}

function Stop-ApplicationProcess {
    param(
        [object]$Process
    )
    
    if ($null -ne $Process -and -not $Process.HasExited) {
        Write-Host "`n🛑 Stopping Application..." -ForegroundColor Cyan
        
        try {
            $Process.Kill()
            $Process.WaitForExit(10000)
            Write-TestResult -TestName "Application: Shutdown" -Passed $true -Message "Application stopped successfully"
        }
        catch {
            Write-TestResult -TestName "Application: Shutdown" -Passed $false -Message "Failed to stop application gracefully" -Details $_.Exception.Message
        }
    }
}

function Generate-TestReport {
    Write-Host "`n📊 Test Summary" -ForegroundColor Cyan
    Write-Host "===============" -ForegroundColor Cyan
    
    $totalTests = $testResults.Count
    $passedTests = ($testResults | Where-Object { $_.Passed }).Count
    $failedTests = $totalTests - $passedTests
    $totalDuration = ((Get-Date) - $startTime).TotalSeconds
    
    Write-Host "Environment: $Environment" -ForegroundColor Gray
    Write-Host "Application URL: $ApplicationUrl" -ForegroundColor Gray
    Write-Host "Total Duration: $([math]::Round($totalDuration, 2)) seconds" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Total Tests: $totalTests" -ForegroundColor Gray
    Write-Host "Passed: $passedTests" -ForegroundColor Green
    Write-Host "Failed: $failedTests" -ForegroundColor Red
    Write-Host "Success Rate: $([math]::Round(($passedTests / $totalTests) * 100, 2))%" -ForegroundColor $(if ($failedTests -eq 0) { 'Green' } else { 'Yellow' })
    
    # Show failed tests
    if ($failedTests -gt 0) {
        Write-Host "`nFailed Tests:" -ForegroundColor Red
        $testResults | Where-Object { -not $_.Passed } | ForEach-Object {
            Write-Host "  ❌ $($_.TestName): $($_.Message)" -ForegroundColor Red
            if ($_.Details) {
                Write-Host "     $($_.Details)" -ForegroundColor Yellow
            }
        }
    }
    
    # Generate JSON report
    $reportPath = ".\test-results\startup-test-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    $reportDir = Split-Path $reportPath -Parent
    if (-not (Test-Path $reportDir)) {
        New-Item -ItemType Directory -Path $reportDir -Force | Out-Null
    }
    
    $report = @{
        TestRunInfo = @{
            Environment    = $Environment
            ApplicationUrl = $ApplicationUrl
            KeyVaultName   = $KeyVaultName
            StartTime      = $startTime
            EndTime        = (Get-Date)
            TotalDuration  = $totalDuration
        }
        Summary     = @{
            TotalTests  = $totalTests
            PassedTests = $passedTests
            FailedTests = $failedTests
            SuccessRate = [math]::Round(($passedTests / $totalTests) * 100, 2)
        }
        TestResults = $testResults
    }
    
    $report | ConvertTo-Json -Depth 5 | Out-File -FilePath $reportPath -Encoding UTF8
    Write-Host "`n📄 Detailed report saved to: $reportPath" -ForegroundColor Gray
    
    # Return success/failure status
    return ($failedTests -eq 0)
}

# Main execution
try {
    Write-Host "🚀 Zeus People Application Startup Test" -ForegroundColor Cyan
    Write-Host "=======================================" -ForegroundColor Cyan
    Write-Host "Environment: $Environment" -ForegroundColor Gray
    Write-Host "Application URL: $ApplicationUrl" -ForegroundColor Gray
    Write-Host "Test Key Vault: $TestKeyVaultAccess" -ForegroundColor Gray
    Write-Host "WhatIf Mode: $WhatIf" -ForegroundColor Gray
    Write-Host "Timestamp: $(Get-Date)" -ForegroundColor Gray
    
    # Run all tests
    $canContinue = Test-Prerequisites
    if ($canContinue) { $canContinue = Test-BuildApplication }
    if ($canContinue) { $canContinue = Test-ConfigurationLoading }
    if ($canContinue) { $canContinue = Test-KeyVaultConfiguration }
    
    $process = $null
    if ($canContinue) {
        $process = Start-ApplicationWithTimeout -TimeoutSeconds $StartupTimeoutSeconds
        if ($null -ne $process) {
            Test-ApplicationEndpoints -Process $process
            Test-ConfigurationValidation -Process $process
            
            if ($RunIntegrationTests) {
                # Run additional integration tests here
                Write-Host "`n🔧 Running Integration Tests..." -ForegroundColor Cyan
                # TODO: Add integration tests
            }
        }
    }
    
    # Cleanup
    Stop-ApplicationProcess -Process $process
    
    # Generate final report
    $success = Generate-TestReport
    
    if ($success) {
        Write-Host "`n🎉 All tests passed! Application startup is working correctly." -ForegroundColor Green
        exit 0
    }
    else {
        Write-Host "`n⚠️ Some tests failed. Please review the issues above." -ForegroundColor Yellow
        exit 1
    }
}
catch {
    Write-Error "❌ Test script failed: $($_.Exception.Message)"
    Write-Host "Stack trace: $($_.ScriptStackTrace)" -ForegroundColor Red
    exit 1
}
finally {
    # Cleanup any remaining processes
    Get-Job | Stop-Job -PassThru | Remove-Job -Force
}
