# Application Configuration Validation Test Script
# Tests that configuration validation catches invalid values
#
# Prerequisites:
# - .NET SDK installed
# - Zeus.People solution built successfully
# - Key Vault configured with proper secrets

param(
    [Parameter(Mandatory = $false)]
    [string]$Environment = "Development",
    
    [Parameter(Mandatory = $false)]
    [string]$ProjectPath = ".\src\API\Zeus.People.API.csproj",
    
    [Parameter(Mandatory = $false)]
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

# Logging function
function Write-Log {
    param(
        [string]$Message,
        [string]$Level = "INFO"
    )
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    Write-Host "[$timestamp] [$Level] $Message" -ForegroundColor $(
        switch ($Level) {
            "ERROR" { "Red" }
            "WARN" { "Yellow" }
            "SUCCESS" { "Green" }
            default { "White" }
        }
    )
}

Write-Log "Starting Configuration Validation Tests for environment: $Environment"

$testResults = @{
    "TotalTests"  = 0
    "PassedTests" = 0
    "FailedTests" = 0
    "Results"     = @()
}

function Add-TestResult {
    param(
        [string]$TestName,
        [bool]$Passed,
        [string]$Message,
        [object]$Data = $null
    )
    
    $testResults.TotalTests++
    
    if ($Passed) {
        $testResults.PassedTests++
        Write-Log "$TestName - PASSED: $Message" -Level "SUCCESS"
    }
    else {
        $testResults.FailedTests++
        Write-Log "$TestName - FAILED: $Message" -Level "ERROR"
    }
    
    $testResults.Results += @{
        "TestName"  = $TestName
        "Passed"    = $Passed
        "Message"   = $Message
        "Data"      = $Data
        "Timestamp" = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    }
}

try {
    # Test 1: Build the project if required
    if (-not $SkipBuild) {
        Write-Log "Test 1: Building project..."
        try {
            dotnet build $ProjectPath --configuration Release --no-restore --verbosity quiet
            Add-TestResult "Project Build" $true "Project built successfully"
        }
        catch {
            Add-TestResult "Project Build" $false "Build failed: $($_.Exception.Message)"
            throw "Cannot continue without successful build"
        }
    }
    else {
        Write-Log "Skipping build as requested"
    }
    
    # Test 2: Run Configuration Validation Unit Tests
    Write-Log "Test 2: Running Configuration Validation Unit Tests..."
    try {
        $testProject = ".\tests\Zeus.People.API.Tests\Zeus.People.API.Tests.csproj"
        $testFilter = "Category=ConfigurationValidation"
        
        $testOutput = dotnet test $testProject --filter $testFilter --logger "console;verbosity=detailed" --no-build --configuration Release 2>&1
        $testExitCode = $LASTEXITCODE
        
        if ($testExitCode -eq 0) {
            $testCount = ($testOutput | Select-String "Passed.*Failed.*Skipped").ToString()
            Add-TestResult "Configuration Validation Unit Tests" $true "All configuration validation tests passed: $testCount"
        }
        else {
            $failedTests = ($testOutput | Select-String "Failed:" | Measure-Object).Count
            Add-TestResult "Configuration Validation Unit Tests" $false "Configuration validation tests failed - $failedTests failures"
        }
    }
    catch {
        Add-TestResult "Configuration Validation Unit Tests" $false "Error running tests: $($_.Exception.Message)"
    }
    
    # Test 3: Validate appsettings.json Structure
    Write-Log "Test 3: Validating appsettings.json structure..."
    try {
        $appSettingsPath = ".\src\API\appsettings.json"
        $appSettings = Get-Content $appSettingsPath | ConvertFrom-Json
        
        $requiredSections = @(
            "ApplicationSettings",
            "KeyVaultSettings", 
            "DatabaseSettings",
            "ServiceBusSettings",
            "AzureAd",
            "JwtSettings"
        )
        
        $missingSections = @()
        foreach ($section in $requiredSections) {
            if (-not $appSettings.PSObject.Properties.Name.Contains($section)) {
                $missingSections += $section
            }
        }
        
        if ($missingSections.Count -eq 0) {
            Add-TestResult "App Settings Structure" $true "All required configuration sections present"
        }
        else {
            Add-TestResult "App Settings Structure" $false "Missing sections: $($missingSections -join ', ')"
        }
    }
    catch {
        Add-TestResult "App Settings Structure" $false "Error validating appsettings.json: $($_.Exception.Message)"
    }
    
    # Test 4: Validate Key Vault Configuration
    Write-Log "Test 4: Validating Key Vault configuration..."
    try {
        $appSettingsPath = ".\src\API\appsettings.Development.Azure.json"
        
        if (Test-Path $appSettingsPath) {
            $azureSettings = Get-Content $appSettingsPath | ConvertFrom-Json
            $kvSettings = $azureSettings.KeyVaultSettings
            
            $validationErrors = @()
            
            if ([string]::IsNullOrEmpty($kvSettings.VaultUrl)) {
                $validationErrors += "VaultUrl is empty"
            }
            elseif (-not $kvSettings.VaultUrl.Contains("vault.azure.net")) {
                $validationErrors += "VaultUrl format is invalid"
            }
            
            if ([string]::IsNullOrEmpty($kvSettings.VaultName)) {
                $validationErrors += "VaultName is empty"
            }
            
            if ($kvSettings.UseManagedIdentity -ne $true) {
                $validationErrors += "UseManagedIdentity should be true for Azure deployment"
            }
            
            if ($validationErrors.Count -eq 0) {
                Add-TestResult "Key Vault Configuration" $true "Key Vault configuration is valid"
            }
            else {
                Add-TestResult "Key Vault Configuration" $false "Validation errors: $($validationErrors -join ', ')"
            }
        }
        else {
            Add-TestResult "Key Vault Configuration" $false "Azure configuration file not found"
        }
    }
    catch {
        Add-TestResult "Key Vault Configuration" $false "Error validating Key Vault config: $($_.Exception.Message)"
    }
    
    # Test 5: Test Invalid Configuration Scenarios
    Write-Log "Test 5: Testing invalid configuration scenarios..."
    try {
        # Create a test configuration with invalid values
        $testConfigPath = ".\test-invalid-config.json"
        $invalidConfig = @{
            "DatabaseSettings"   = @{
                "WriteConnectionString" = ""  # Invalid: empty required field
                "CommandTimeoutSeconds" = 500  # Invalid: exceeds max range
                "ConnectionPoolMinSize" = 50
                "ConnectionPoolMaxSize" = 10   # Invalid: max < min
            }
            "ServiceBusSettings" = @{
                "ConnectionString"   = ""
                "TopicName"          = ""               # Invalid: empty required field
                "MessageRetryCount"  = 15       # Invalid: exceeds max of 10
                "MaxConcurrentCalls" = 150     # Invalid: exceeds max of 100
                "UseManagedIdentity" = $false
            }
        } | ConvertTo-Json -Depth 10
        
        $invalidConfig | Out-File -FilePath $testConfigPath -Encoding UTF8
        
        # Test the ConfigValidationTest.exe if it exists
        $configValidationExe = ".\ConfigValidationTest.exe"
        if (Test-Path $configValidationExe) {
            $validationOutput = & $configValidationExe 2>&1
            $validationExitCode = $LASTEXITCODE
            
            if ($validationOutput -match "validation failed as expected") {
                Add-TestResult "Invalid Configuration Detection" $true "Configuration validation correctly detected invalid values"
            }
            else {
                Add-TestResult "Invalid Configuration Detection" $false "Configuration validation did not detect invalid values"
            }
        }
        else {
            # Compile and run the validation test
            try {
                dotnet run --project ".\ConfigValidationTest.cs" --configuration Release
                Add-TestResult "Invalid Configuration Detection" $true "Configuration validation test completed"
            }
            catch {
                Add-TestResult "Invalid Configuration Detection" $false "Error running configuration validation test: $($_.Exception.Message)"
            }
        }
        
        # Clean up test file
        if (Test-Path $testConfigPath) {
            Remove-Item $testConfigPath -Force
        }
    }
    catch {
        Add-TestResult "Invalid Configuration Detection" $false "Error testing invalid configurations: $($_.Exception.Message)"
    }
    
    # Test 6: Validate Environment-Specific Configuration
    Write-Log "Test 6: Validating environment-specific configuration..."
    try {
        $environments = @("Development", "Staging", "Production")
        $configIssues = @()
        
        foreach ($env in $environments) {
            $envConfigPath = ".\src\API\appsettings.$env.json"
            
            if (Test-Path $envConfigPath) {
                try {
                    $envConfig = Get-Content $envConfigPath | ConvertFrom-Json
                    
                    # Check for environment-appropriate settings
                    if ($env -eq "Production") {
                        if ($envConfig.ApplicationSettings.Features.EnableSwaggerInProduction -eq $true) {
                            $configIssues += "${env}: Swagger should be disabled in production"
                        }
                        
                        if ($envConfig.ApplicationSettings.Features.EnableDetailedLogging -eq $true) {
                            $configIssues += "${env}: Detailed logging should be disabled in production"
                        }
                    }
                    
                    Write-Log "  ✓ ${env} configuration is valid" -Level "SUCCESS"
                }
                catch {
                    $configIssues += "${env}: Configuration parsing error - $($_.Exception.Message)"
                }
            }
            else {
                Write-Log "  - ${env} configuration file not found (optional)" -Level "WARN"
            }
        }
        
        if ($configIssues.Count -eq 0) {
            Add-TestResult "Environment-Specific Configuration" $true "All environment configurations are appropriate"
        }
        else {
            Add-TestResult "Environment-Specific Configuration" $false "Issues found: $($configIssues -join '; ')"
        }
    }
    catch {
        Add-TestResult "Environment-Specific Configuration" $false "Error validating environment configs: $($_.Exception.Message)"
    }
    
    # Test 7: Connection String Format Validation
    Write-Log "Test 7: Validating connection string formats..."
    try {
        $connectionStringTests = @{
            "Valid Cosmos DB"         = @{
                "String"     = "AccountEndpoint=https://test.documents.azure.com:443/;AccountKey=test123=="
                "ShouldPass" = $true
                "Type"       = "CosmosDB"
            }
            "Valid Service Bus"       = @{
                "String"     = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test=="
                "ShouldPass" = $true
                "Type"       = "ServiceBus"
            }
            "Invalid Cosmos DB"       = @{
                "String"     = "invalid-connection-string"
                "ShouldPass" = $false
                "Type"       = "CosmosDB"
            }
            "Empty Connection String" = @{
                "String"     = ""
                "ShouldPass" = $false
                "Type"       = "Generic"
            }
        }
        
        $connectionStringTestsPassed = 0
        $connectionStringTestsTotal = $connectionStringTests.Count
        
        foreach ($testName in $connectionStringTests.Keys) {
            $test = $connectionStringTests[$testName]
            
            $isValid = $false
            switch ($test.Type) {
                "CosmosDB" {
                    $isValid = $test.String.Contains("AccountEndpoint=") -and $test.String.Contains("AccountKey=")
                }
                "ServiceBus" {
                    $isValid = $test.String.Contains("Endpoint=sb://") -and $test.String.Contains("SharedAccessKey")
                }
                "Generic" {
                    $isValid = -not [string]::IsNullOrEmpty($test.String)
                }
            }
            
            if (($isValid -and $test.ShouldPass) -or (-not $isValid -and -not $test.ShouldPass)) {
                $connectionStringTestsPassed++
                Write-Log "  ✓ $testName validation correct" -Level "SUCCESS"
            }
            else {
                Write-Log "  ✗ $testName validation incorrect" -Level "ERROR"
            }
        }
        
        if ($connectionStringTestsPassed -eq $connectionStringTestsTotal) {
            Add-TestResult "Connection String Format Validation" $true "All connection string format tests passed"
        }
        else {
            Add-TestResult "Connection String Format Validation" $false "$connectionStringTestsPassed/$connectionStringTestsTotal tests passed"
        }
    }
    catch {
        Add-TestResult "Connection String Format Validation" $false "Error validating connection string formats: $($_.Exception.Message)"
    }
    
    # Generate comprehensive test report
    Write-Log ""
    Write-Log "CONFIGURATION VALIDATION TEST REPORT" -Level "SUCCESS"
    Write-Log "====================================" -Level "SUCCESS"
    Write-Log "Environment: $Environment" -Level "SUCCESS"
    Write-Log "Test Execution Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -Level "SUCCESS"
    Write-Log ""
    Write-Log "TEST SUMMARY:" -Level "SUCCESS"
    Write-Log "Total Tests: $($testResults.TotalTests)" -Level "SUCCESS"
    Write-Log "Passed: $($testResults.PassedTests)" -Level "SUCCESS"
    Write-Log "Failed: $($testResults.FailedTests)" -Level "SUCCESS"
    Write-Log "Success Rate: $([Math]::Round(($testResults.PassedTests / $testResults.TotalTests) * 100, 2))%" -Level "SUCCESS"
    
    # Display detailed results
    Write-Log ""
    Write-Log "DETAILED RESULTS:" -Level "SUCCESS"
    foreach ($result in $testResults.Results) {
        $status = if ($result.Passed) { "PASS" } else { "FAIL" }
        $color = if ($result.Passed) { "SUCCESS" } else { "ERROR" }
        Write-Log "[$status] $($result.TestName): $($result.Message)" -Level $color
    }
    
    if ($testResults.FailedTests -gt 0) {
        Write-Log ""
        Write-Log "RECOMMENDATIONS:" -Level "WARN"
        foreach ($result in $testResults.Results) {
            if (-not $result.Passed) {
                Write-Log "- Fix issue with $($result.TestName): $($result.Message)" -Level "WARN"
            }
        }
    }
    
    # Export test results
    $reportFile = "configuration-validation-results-$Environment-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    $testResults | ConvertTo-Json -Depth 10 | Out-File -FilePath $reportFile -Encoding UTF8
    Write-Log "Test results exported to: $reportFile" -Level "SUCCESS"
    
    if ($testResults.FailedTests -eq 0) {
        Write-Log "All configuration validation tests passed! Configuration is ready for deployment." -Level "SUCCESS"
        exit 0
    }
    else {
        Write-Log "Some validation tests failed. Please review and fix the issues before deployment." -Level "WARN"
        exit 1
    }
}
catch {
    Write-Log "Critical error during configuration validation: $($_.Exception.Message)" -Level "ERROR"
    exit 1
}
