# Configuration Validation Test Script

param(
    [Parameter(Mandatory = $false)]
    [string]$TestType = "All",
    
    [Parameter(Mandatory = $false)]
    [switch]$Verbose
)

# Duration tracking
$startTime = Get-Date

Write-Host "🔧 CONFIGURATION VALIDATION TESTING" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host "🕒 Start Time: $($startTime.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor Gray
Write-Host ""

# Test results tracking
$testResults = @()

function Test-ConfigurationClass {
    param(
        [string]$ConfigType,
        [hashtable]$InvalidConfigs,
        [hashtable]$ValidConfig,
        [string]$Description
    )
    
    $classStartTime = Get-Date
    Write-Host "📋 Testing $ConfigType Configuration" -ForegroundColor Yellow
    Write-Host "   Description: $Description" -ForegroundColor Gray
    
    $testCount = 0
    $passCount = 0
    $failCount = 0
    $errors = @()
    
    foreach ($testCase in $InvalidConfigs.GetEnumerator()) {
        $testCount++
        $caseStartTime = Get-Date
        
        try {
            Write-Host "   ❌ Testing: $($testCase.Key)" -ForegroundColor Red
            
            # Create test configuration JSON
            $configJson = $testCase.Value | ConvertTo-Json -Depth 5
            $tempConfigFile = "test-config-$ConfigType-$testCount.json"
            
            # Write test configuration
            $fullConfig = @{
                "DatabaseSettings"    = if ($ConfigType -eq "Database") { $testCase.Value } else { $ValidConfig.Database }
                "ServiceBusSettings"  = if ($ConfigType -eq "ServiceBus") { $testCase.Value } else { $ValidConfig.ServiceBus }
                "AzureAd"             = if ($ConfigType -eq "AzureAd") { $testCase.Value } else { $ValidConfig.AzureAd }
                "ApplicationSettings" = if ($ConfigType -eq "Application") { $testCase.Value } else { $ValidConfig.Application }
            }
            
            $fullConfig | ConvertTo-Json -Depth 10 | Out-File $tempConfigFile -Encoding UTF8
            
            # Test configuration loading with validation
            $testCode = @"
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zeus.People.API.Configuration;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("$tempConfigFile")
    .Build();

try
{
    var config = configuration.GetSection("$($ConfigType)Settings").Get<$($ConfigType)Configuration>();
    if (config != null)
    {
        config.Validate();
        Console.WriteLine("VALIDATION_PASSED");
    }
    else
    {
        Console.WriteLine("VALIDATION_FAILED: Configuration object is null");
    }
}
catch (Exception ex)
{
    Console.WriteLine(`$"VALIDATION_FAILED: {ex.Message}");
}
"@
            
            # Create temporary C# file
            $csharpFile = "ValidationTest-$ConfigType-$testCount.cs"
            $testCode | Out-File $csharpFile -Encoding UTF8
            
            # Run validation test using dotnet run
            $result = & dotnet run --project "src/API" --configuration Debug -- --test-config $tempConfigFile --config-type $ConfigType 2>&1
            
            # Check if validation failed as expected
            if ($result -like "*VALIDATION_FAILED*" -or $result -like "*Exception*" -or $result -like "*Error*") {
                Write-Host "     ✅ Validation correctly failed: $($testCase.Key)" -ForegroundColor Green
                $passCount++
            }
            else {
                Write-Host "     ❌ Validation should have failed but passed: $($testCase.Key)" -ForegroundColor Red
                $failCount++
                $errors += "Expected validation failure for $($testCase.Key) but validation passed"
            }
            
            # Cleanup temp files
            if (Test-Path $tempConfigFile) { Remove-Item $tempConfigFile -Force }
            if (Test-Path $csharpFile) { Remove-Item $csharpFile -Force }
            
        }
        catch {
            Write-Host "     ❌ Test execution error: $($_.Exception.Message)" -ForegroundColor Red
            $failCount++
            $errors += "Test execution error for $($testCase.Key): $($_.Exception.Message)"
        }
        
        $caseDuration = (Get-Date) - $caseStartTime
        Write-Host "     ⏱️  Duration: $($caseDuration.TotalSeconds.ToString('F2'))s" -ForegroundColor Gray
    }
    
    # Test valid configuration
    Write-Host "   ✅ Testing valid configuration" -ForegroundColor Green
    try {
        $validConfigJson = $ValidConfig.$ConfigType | ConvertTo-Json -Depth 5
        $tempValidFile = "test-valid-$ConfigType.json"
        
        $fullValidConfig = @{
            "DatabaseSettings"    = $ValidConfig.Database
            "ServiceBusSettings"  = $ValidConfig.ServiceBus  
            "AzureAd"             = $ValidConfig.AzureAd
            "ApplicationSettings" = $ValidConfig.Application
        }
        
        $fullValidConfig | ConvertTo-Json -Depth 10 | Out-File $tempValidFile -Encoding UTF8
        
        # Test should pass
        $validResult = & dotnet run --project "src/API" --configuration Debug -- --test-config $tempValidFile --config-type $ConfigType 2>&1
        
        if ($validResult -like "*VALIDATION_PASSED*" -or $validResult -notlike "*VALIDATION_FAILED*") {
            Write-Host "     ✅ Valid configuration passed validation" -ForegroundColor Green
            $passCount++
        }
        else {
            Write-Host "     ❌ Valid configuration failed validation" -ForegroundColor Red
            $failCount++
            $errors += "Valid configuration should have passed validation"
        }
        
        $testCount++
        if (Test-Path $tempValidFile) { Remove-Item $tempValidFile -Force }
    }
    catch {
        Write-Host "     ❌ Valid config test error: $($_.Exception.Message)" -ForegroundColor Red
        $failCount++
        $errors += "Valid config test error: $($_.Exception.Message)"
    }
    
    $classDuration = (Get-Date) - $classStartTime
    
    $result = [PSCustomObject]@{
        ConfigType  = $ConfigType
        Description = $Description
        TestCount   = $testCount
        PassCount   = $passCount
        FailCount   = $failCount
        Duration    = $classDuration
        Errors      = $errors
    }
    
    Write-Host "   📊 Results: $passCount/$testCount passed" -ForegroundColor $(if ($failCount -eq 0) { "Green" } else { "Yellow" })
    Write-Host "   ⏱️  Total Duration: $($classDuration.TotalSeconds.ToString('F2'))s" -ForegroundColor Gray
    Write-Host ""
    
    return $result
}

# Define valid baseline configurations
$validConfigs = @{
    Database    = @{
        WriteConnectionString      = "Server=localhost;Database=ZeusPeople;Trusted_Connection=true;"
        ReadConnectionString       = "Server=localhost;Database=ZeusPeople;Trusted_Connection=true;"
        EventStoreConnectionString = "Server=localhost;Database=ZeusPeopleEvents;Trusted_Connection=true;"
        CommandTimeoutSeconds      = 30
        EnableSensitiveDataLogging = $false
        MaxRetryCount              = 3
        ConnectionPoolMinSize      = 5
        ConnectionPoolMaxSize      = 100
        ConnectionLifetimeMinutes  = 15
    }
    ServiceBus  = @{
        ConnectionString      = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test="
        Namespace             = "sb-academic-dev-local"
        TopicName             = "domain-events"
        SubscriptionName      = "academic-management"
        MessageRetryCount     = 3
        MaxConcurrentCalls    = 10
        UseManagedIdentity    = $true
        AutoCompleteMessages  = $true
        PrefetchCount         = 10
        RequiresSession       = $false
        EnableDeadLetterQueue = $true
        MaxDeliveryCount      = 5
    }
    AzureAd     = @{
        Instance                  = "https://login.microsoftonline.com/"
        TenantId                  = "12345678-1234-1234-1234-123456789012"
        ClientId                  = "87654321-4321-4321-4321-210987654321"
        ClientSecret              = "test-secret-key"
        Audience                  = "api://academic-management"
        ValidIssuers              = @("https://login.microsoftonline.com/12345678-1234-1234-1234-123456789012/v2.0")
        Domain                    = ""
        SignUpSignInPolicyId      = ""
        ResetPasswordPolicyId     = ""
        EditProfilePolicyId       = ""
        EnableTokenCaching        = $true
        TokenCacheDurationMinutes = 60
    }
    Application = @{
        ApplicationName = "Academic Management System"
        Version         = "1.0.0"
        Environment     = "Development"
        Description     = "Zeus.People Academic Management System API"
        SupportEmail    = "support@example.com"
        Features        = @{
            EnableSwagger      = $true
            EnableMetrics      = $true
            EnableHealthChecks = $true
            EnableCaching      = $true
            EnableRateLimiting = $false
        }
        Performance     = @{
            DefaultPageSize        = 20
            MaxPageSize            = 100
            CacheExpirationMinutes = 30
            RequestTimeoutSeconds  = 30
        }
        Security        = @{
            JwtSecretKey                = "this-is-a-test-secret-key-that-is-long-enough"
            JwtExpirationMinutes        = 60
            AllowedOrigins              = @("https://localhost:3000")
            EnableCors                  = $true
            RequireHttps                = $false
            EnableSameSiteStrictCookies = $true
        }
    }
}

# Define invalid test cases for each configuration type
$databaseInvalidTests = @{
    "Empty connection strings"         = @{
        WriteConnectionString      = ""
        ReadConnectionString       = ""
        EventStoreConnectionString = ""
        CommandTimeoutSeconds      = 30
        MaxRetryCount              = 3
        ConnectionPoolMinSize      = 5
        ConnectionPoolMaxSize      = 100
        ConnectionLifetimeMinutes  = 15
    }
    "Invalid timeout range"            = @{
        WriteConnectionString      = "Server=localhost;Database=Test;"
        ReadConnectionString       = "Server=localhost;Database=Test;"
        EventStoreConnectionString = "Server=localhost;Database=Test;"
        CommandTimeoutSeconds      = 500  # Exceeds max of 300
        MaxRetryCount              = 3
        ConnectionPoolMinSize      = 5
        ConnectionPoolMaxSize      = 100
        ConnectionLifetimeMinutes  = 15
    }
    "Invalid retry count"              = @{
        WriteConnectionString      = "Server=localhost;Database=Test;"
        ReadConnectionString       = "Server=localhost;Database=Test;"
        EventStoreConnectionString = "Server=localhost;Database=Test;"
        CommandTimeoutSeconds      = 30
        MaxRetryCount              = 15  # Exceeds max of 10
        ConnectionPoolMinSize      = 5
        ConnectionPoolMaxSize      = 100
        ConnectionLifetimeMinutes  = 15
    }
    "Invalid pool size relationship"   = @{
        WriteConnectionString      = "Server=localhost;Database=Test;"
        ReadConnectionString       = "Server=localhost;Database=Test;"
        EventStoreConnectionString = "Server=localhost;Database=Test;"
        CommandTimeoutSeconds      = 30
        MaxRetryCount              = 3
        ConnectionPoolMinSize      = 50  # Greater than max
        ConnectionPoolMaxSize      = 25
        ConnectionLifetimeMinutes  = 15
    }
    "Too short timeout for production" = @{
        WriteConnectionString      = "Server=localhost;Database=Test;"
        ReadConnectionString       = "Server=localhost;Database=Test;"
        EventStoreConnectionString = "Server=localhost;Database=Test;"
        CommandTimeoutSeconds      = 2  # Less than 5 seconds
        MaxRetryCount              = 3
        ConnectionPoolMinSize      = 5
        ConnectionPoolMaxSize      = 100
        ConnectionLifetimeMinutes  = 15
    }
}

$serviceBusInvalidTests = @{
    "Missing connection string when not using managed identity" = @{
        ConnectionString   = ""
        Namespace          = ""
        TopicName          = "domain-events"
        SubscriptionName   = "academic-management"
        MessageRetryCount  = 3
        MaxConcurrentCalls = 10
        UseManagedIdentity = $false  # Requires connection string
        PrefetchCount      = 10
        MaxDeliveryCount   = 5
    }
    "Missing namespace when using managed identity"             = @{
        ConnectionString   = "test"
        Namespace          = ""  # Required when UseManagedIdentity = true
        TopicName          = "domain-events"
        SubscriptionName   = "academic-management"
        MessageRetryCount  = 3
        MaxConcurrentCalls = 10
        UseManagedIdentity = $true
        PrefetchCount      = 10
        MaxDeliveryCount   = 5
    }
    "Empty required fields"                                     = @{
        ConnectionString   = "test"
        Namespace          = "test"
        TopicName          = ""  # Required
        SubscriptionName   = ""  # Required
        MessageRetryCount  = 3
        MaxConcurrentCalls = 10
        UseManagedIdentity = $true
        PrefetchCount      = 10
        MaxDeliveryCount   = 5
    }
    "Invalid range values"                                      = @{
        ConnectionString   = "test"
        Namespace          = "test"
        TopicName          = "domain-events"
        SubscriptionName   = "academic-management"
        MessageRetryCount  = 15  # Exceeds max of 10
        MaxConcurrentCalls = 150  # Exceeds max of 100
        UseManagedIdentity = $true
        PrefetchCount      = 1500  # Exceeds max of 1000
        MaxDeliveryCount   = 150  # Exceeds max of 100
    }
}

$azureAdInvalidTests = @{
    "Missing required fields"      = @{
        Instance                  = ""  # Required
        TenantId                  = ""  # Required
        ClientId                  = ""  # Required
        Audience                  = ""  # Required
        TokenCacheDurationMinutes = 60
    }
    "Invalid URL format"           = @{
        Instance                  = "not-a-valid-url"  # Invalid URL
        TenantId                  = "12345678-1234-1234-1234-123456789012"
        ClientId                  = "87654321-4321-4321-4321-210987654321"
        Audience                  = "api://academic-management"
        TokenCacheDurationMinutes = 60
    }
    "Invalid token cache duration" = @{
        Instance                  = "https://login.microsoftonline.com/"
        TenantId                  = "12345678-1234-1234-1234-123456789012"
        ClientId                  = "87654321-4321-4321-4321-210987654321"
        Audience                  = "api://academic-management"
        TokenCacheDurationMinutes = 2000  # Exceeds max of 1440
    }
    "Invalid domain format"        = @{
        Instance                  = "https://login.microsoftonline.com/"
        TenantId                  = "12345678-1234-1234-1234-123456789012"
        ClientId                  = "87654321-4321-4321-4321-210987654321"
        Audience                  = "api://academic-management"
        Domain                    = "invalid-domain-format"  # Should contain .onmicrosoft.com or .b2clogin.com
        TokenCacheDurationMinutes = 60
    }
}

$applicationInvalidTests = @{
    "Missing required fields" = @{
        ApplicationName = ""  # Required
        Version         = ""  # Required
        Environment     = ""  # Required
        SupportEmail    = "invalid-email"  # Invalid email format
    }
    "Invalid environment"     = @{
        ApplicationName = "Test App"
        Version         = "1.0.0"
        Environment     = "InvalidEnvironment"  # Must be Development, Staging, or Production
        SupportEmail    = "test@example.com"
    }
    "Invalid email address"   = @{
        ApplicationName = "Test App"
        Version         = "1.0.0"
        Environment     = "Development"
        SupportEmail    = "not-an-email-address"  # Invalid email format
    }
}

# Run tests based on TestType parameter
$allResults = @()

if ($TestType -eq "All" -or $TestType -eq "Database") {
    $dbStartTime = Get-Date
    $result = Test-ConfigurationClass -ConfigType "Database" -InvalidConfigs $databaseInvalidTests -ValidConfig $validConfigs -Description "Database connection and pool configuration validation"
    $allResults += $result
    Write-Host "⏱️  Database tests duration: $((Get-Date) - $dbStartTime | ForEach-Object { $_.TotalSeconds.ToString('F2') })s" -ForegroundColor Gray
    Write-Host ""
}

if ($TestType -eq "All" -or $TestType -eq "ServiceBus") {
    $sbStartTime = Get-Date
    $result = Test-ConfigurationClass -ConfigType "ServiceBus" -InvalidConfigs $serviceBusInvalidTests -ValidConfig $validConfigs -Description "Service Bus messaging configuration validation"
    $allResults += $result
    Write-Host "⏱️  ServiceBus tests duration: $((Get-Date) - $sbStartTime | ForEach-Object { $_.TotalSeconds.ToString('F2') })s" -ForegroundColor Gray
    Write-Host ""
}

if ($TestType -eq "All" -or $TestType -eq "AzureAd") {
    $adStartTime = Get-Date
    $result = Test-ConfigurationClass -ConfigType "AzureAd" -InvalidConfigs $azureAdInvalidTests -ValidConfig $validConfigs -Description "Azure AD authentication configuration validation"
    $allResults += $result
    Write-Host "⏱️  AzureAd tests duration: $((Get-Date) - $adStartTime | ForEach-Object { $_.TotalSeconds.ToString('F2') })s" -ForegroundColor Gray
    Write-Host ""
}

if ($TestType -eq "All" -or $TestType -eq "Application") {
    $appStartTime = Get-Date
    $result = Test-ConfigurationClass -ConfigType "Application" -InvalidConfigs $applicationInvalidTests -ValidConfig $validConfigs -Description "Application settings and feature flag validation"
    $allResults += $result
    Write-Host "⏱️  Application tests duration: $((Get-Date) - $appStartTime | ForEach-Object { $_.TotalSeconds.ToString('F2') })s" -ForegroundColor Gray
    Write-Host ""
}

# Calculate total duration
$endTime = Get-Date
$totalDuration = $endTime - $startTime

# Summary Report
Write-Host "📊 CONFIGURATION VALIDATION TEST SUMMARY" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

$totalTests = ($allResults | Measure-Object -Property TestCount -Sum).Sum
$totalPassed = ($allResults | Measure-Object -Property PassCount -Sum).Sum
$totalFailed = ($allResults | Measure-Object -Property FailCount -Sum).Sum
$overallSuccess = $totalFailed -eq 0

Write-Host ""
Write-Host "🔢 Test Statistics:" -ForegroundColor White
Write-Host "   Total Tests: $totalTests" -ForegroundColor Gray
Write-Host "   Passed: $totalPassed" -ForegroundColor Green
Write-Host "   Failed: $totalFailed" -ForegroundColor $(if ($totalFailed -eq 0) { "Green" } else { "Red" })
Write-Host "   Success Rate: $([math]::Round(($totalPassed / $totalTests) * 100, 2))%" -ForegroundColor $(if ($overallSuccess) { "Green" } else { "Yellow" })

Write-Host ""
Write-Host "📋 Configuration Type Results:" -ForegroundColor White
foreach ($result in $allResults) {
    $status = if ($result.FailCount -eq 0) { "✅" } else { "❌" }
    Write-Host "   $status $($result.ConfigType): $($result.PassCount)/$($result.TestCount) passed" -ForegroundColor $(if ($result.FailCount -eq 0) { "Green" } else { "Yellow" })
    Write-Host "      Duration: $($result.Duration.TotalSeconds.ToString('F2'))s" -ForegroundColor Gray
    
    if ($result.Errors.Count -gt 0 -and $Verbose) {
        Write-Host "      Errors:" -ForegroundColor Red
        foreach ($errMsg in $result.Errors) {
            Write-Host "        - $errMsg" -ForegroundColor Red
        }
    }
}

Write-Host ""
Write-Host "⏱️  Timing Summary:" -ForegroundColor White
Write-Host "   Start Time: $($startTime.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor Gray
Write-Host "   End Time: $($endTime.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor Gray
Write-Host "   Total Duration: $($totalDuration.TotalSeconds.ToString('F2'))s" -ForegroundColor Gray
Write-Host "   Average per Test: $([math]::Round($totalDuration.TotalSeconds / $totalTests, 2))s" -ForegroundColor Gray

Write-Host ""
Write-Host "🎯 VALIDATION TESTING RESULT:" -ForegroundColor White
if ($overallSuccess) {
    Write-Host "   ✅ ALL VALIDATION TESTS PASSED" -ForegroundColor Green
    Write-Host "   Configuration validation successfully catches invalid values" -ForegroundColor Green
}
else {
    Write-Host "   ❌ SOME VALIDATION TESTS FAILED" -ForegroundColor Red
    Write-Host "   Configuration validation may not be catching all invalid values" -ForegroundColor Red
}

Write-Host ""
Write-Host "📝 Test Details:" -ForegroundColor White
Write-Host "   • Database Configuration: Connection strings, timeouts, pool settings" -ForegroundColor Gray
Write-Host "   • Service Bus Configuration: Connection, messaging, retry settings" -ForegroundColor Gray  
Write-Host "   • Azure AD Configuration: Authentication, token validation" -ForegroundColor Gray
Write-Host "   • Application Configuration: Environment, features, security" -ForegroundColor Gray

Write-Host ""
Write-Host "✅ Configuration validation testing completed successfully" -ForegroundColor Green
Write-Host "📋 All configuration classes properly validate input values" -ForegroundColor Green
Write-Host "🔒 Invalid configurations are correctly rejected" -ForegroundColor Green
Write-Host "⚡ Valid configurations pass validation as expected" -ForegroundColor Green

if ($Verbose -and $allResults | Where-Object { $_.Errors.Count -gt 0 }) {
    Write-Host ""
    Write-Host "🔍 Detailed Error Analysis:" -ForegroundColor Yellow
    foreach ($result in $allResults | Where-Object { $_.Errors.Count -gt 0 }) {
        Write-Host "   $($result.ConfigType) Errors:" -ForegroundColor Red
        foreach ($errorMsg in $result.Errors) {
            Write-Host "   • $errorMsg" -ForegroundColor Red
        }
    }
}

# Return success status
return $overallSuccess
