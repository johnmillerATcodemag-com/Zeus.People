#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Comprehensive configuration testing for Zeus.People Academic Management System

.DESCRIPTION
    Tests all aspects of the secrets management configuration including:
    - Environment variables
    - Azure Key Vault integration  
    - App Service configuration
    - Application startup validation

.PARAMETER Environment
    Environment to test (Development, Staging, Production)

.PARAMETER TestType
    Type of configuration to test (EnvironmentVariables, KeyVault, AppService, All)

.EXAMPLE
    .\scripts\test-comprehensive-config.ps1 -Environment "Development" -TestType "EnvironmentVariables"

.EXAMPLE
    .\scripts\test-comprehensive-config.ps1 -Environment "Staging" -TestType "All"
#>

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("Development", "Staging", "Production")]
    [string]$Environment,
    
    [ValidateSet("EnvironmentVariables", "KeyVault", "AppService", "All")]
    [string]$TestType = "All"
)

# Color functions
function Write-ColorOutput([ConsoleColor]$ForegroundColor, [string]$Message) {
    $originalColor = $Host.UI.RawUI.ForegroundColor
    $Host.UI.RawUI.ForegroundColor = $ForegroundColor
    Write-Output $Message
    $Host.UI.RawUI.ForegroundColor = $originalColor
}

function Write-Success($Message) { Write-ColorOutput Green "✅ $Message" }
function Write-Warning($Message) { Write-ColorOutput Yellow "⚠️  $Message" }
function Write-Error($Message) { Write-ColorOutput Red "❌ $Message" }
function Write-Info($Message) { Write-ColorOutput Cyan "ℹ️  $Message" }

Write-Info "Zeus.People Academic Management System - Comprehensive Configuration Test"
Write-Info "========================================================================"
Write-Info "Environment: $Environment"
Write-Info "Test Type: $TestType"
Write-Host ""

$testResults = @{
    EnvironmentVariables = @{ Status = "Skipped"; Details = @() }
    KeyVault = @{ Status = "Skipped"; Details = @() }
    AppService = @{ Status = "Skipped"; Details = @() }
    ApplicationStartup = @{ Status = "Skipped"; Details = @() }
}

# Test Environment Variables
if ($TestType -eq "All" -or $TestType -eq "EnvironmentVariables") {
    Write-Info "🔍 Testing Environment Variables Configuration..."
    
    $envVars = @{
        "JWT_SECRET_KEY" = "JwtSettings:SecretKey"
        "AZURE_AD_TENANT_ID" = "AzureAd:TenantId"
        "AZURE_AD_CLIENT_ID" = "AzureAd:ClientId" 
        "AZURE_AD_CLIENT_SECRET" = "AzureAd:ClientSecret"
        "DATABASE_CONNECTION_STRING" = "ConnectionStrings:AcademicDatabase"
        "EVENT_STORE_CONNECTION_STRING" = "ConnectionStrings:EventStoreDatabase"
        "SERVICE_BUS_CONNECTION_STRING" = "ConnectionStrings:ServiceBus"
        "APPLICATION_INSIGHTS_CONNECTION_STRING" = "ApplicationInsights:ConnectionString"
        "APPLICATION_INSIGHTS_INSTRUMENTATION_KEY" = "ApplicationInsights:InstrumentationKey"
    }
    
    $envResults = @()
    $validCount = 0
    
    foreach ($envVar in $envVars.Keys) {
        $value = [Environment]::GetEnvironmentVariable($envVar)
        $configPath = $envVars[$envVar]
        
        if ([string]::IsNullOrEmpty($value)) {
            $envResults += "❌ $envVar -> $configPath (Not Set)"
        } elseif ($value.Contains("REPLACE_WITH")) {
            $envResults += "⚠️  $envVar -> $configPath (Placeholder)"
        } else {
            $maskedValue = if ($value.Length -gt 8) { $value.Substring(0, 4) + "..." + $value.Substring($value.Length - 4) } else { "***" }
            $envResults += "✅ $envVar -> $configPath ($maskedValue)"
            $validCount++
        }
    }
    
    $testResults.EnvironmentVariables.Details = $envResults
    if ($validCount -eq $envVars.Count) {
        $testResults.EnvironmentVariables.Status = "Passed"
        Write-Success "Environment Variables: All configured ($validCount/$($envVars.Count))"
    } elseif ($validCount -gt 0) {
        $testResults.EnvironmentVariables.Status = "Partial"
        Write-Warning "Environment Variables: Partially configured ($validCount/$($envVars.Count))"
    } else {
        $testResults.EnvironmentVariables.Status = "Failed"
        Write-Error "Environment Variables: Not configured (0/$($envVars.Count))"
    }
    
    Write-Host ""
}

# Test Azure Key Vault
if (($TestType -eq "All" -or $TestType -eq "KeyVault") -and $Environment -ne "Development") {
    Write-Info "🔍 Testing Azure Key Vault Configuration..."
    
    # Check if Azure CLI is available
    if (Get-Command az -ErrorAction SilentlyContinue) {
        try {
            # Try to get current Azure account
            $account = az account show --query "user.name" -o tsv 2>$null
            if ($account) {
                Write-Success "Azure CLI authenticated as: $account"
                
                # Try to find Key Vault from configuration (you may need to adjust this path)
                $keyVaultName = "kv2ymnmfmrvsb3w"  # This should come from config
                
                Write-Info "Testing Key Vault access: $keyVaultName"
                $keyVault = az keyvault show --name $keyVaultName --query "properties.vaultUri" -o tsv 2>$null
                
                if ($keyVault) {
                    Write-Success "Key Vault accessible: $keyVault"
                    
                    # Test reading a sample secret (non-sensitive)
                    $testSecret = az keyvault secret show --vault-name $keyVaultName --name "JwtSettings--SecretKey" --query "value" -o tsv 2>$null
                    if ($testSecret) {
                        Write-Success "Key Vault secrets readable"
                        $testResults.KeyVault.Status = "Passed"
                        $testResults.KeyVault.Details = @("✅ Key Vault accessible", "✅ Secrets readable")
                    } else {
                        Write-Warning "Key Vault accessible but secrets not readable or not configured"
                        $testResults.KeyVault.Status = "Partial"
                        $testResults.KeyVault.Details = @("✅ Key Vault accessible", "⚠️  Secrets not readable")
                    }
                } else {
                    Write-Error "Key Vault not accessible"
                    $testResults.KeyVault.Status = "Failed"
                    $testResults.KeyVault.Details = @("❌ Key Vault not accessible")
                }
            } else {
                Write-Warning "Not authenticated to Azure"
                $testResults.KeyVault.Status = "Failed"
                $testResults.KeyVault.Details = @("❌ Not authenticated to Azure")
            }
        } catch {
            Write-Error "Error testing Key Vault: $($_.Exception.Message)"
            $testResults.KeyVault.Status = "Failed"
            $testResults.KeyVault.Details = @("❌ Error: $($_.Exception.Message)")
        }
    } else {
        Write-Warning "Azure CLI not available for Key Vault testing"
        $testResults.KeyVault.Status = "Skipped"
        $testResults.KeyVault.Details = @("⏭️  Azure CLI not available")
    }
    
    Write-Host ""
}

# Test Application Startup
if ($TestType -eq "All") {
    Write-Info "🔍 Testing Application Startup..."
    
    # Set environment
    $env:ASPNETCORE_ENVIRONMENT = $Environment
    
    try {
        # Test build
        Write-Info "Building application..."
        $buildOutput = & dotnet build src/API/Zeus.People.API.csproj --configuration Release --verbosity quiet 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Application builds successfully"
            
            # Test configuration loading (dry run)
            Write-Info "Testing configuration loading..."
            
            # Create a minimal test to validate configuration without starting the full app
            $configTest = @'
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Zeus.People.API.Configuration;

var builder = Host.CreateDefaultBuilder();
builder.ConfigureAppConfiguration(config => {
    config.AddJsonFile("src/API/appsettings.json");
    config.AddJsonFile($"src/API/appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true);
    config.AddEnvironmentVariables();
});

var app = builder.Build();
var configuration = app.Services.GetRequiredService<IConfiguration>();

// Test key configuration values
var jwtSecret = configuration["JwtSettings:SecretKey"];
var dbConnection = configuration["ConnectionStrings:AcademicDatabase"];

Console.WriteLine($"JWT Secret configured: {!string.IsNullOrEmpty(jwtSecret) && !jwtSecret.Contains("REPLACE_WITH")}");
Console.WriteLine($"Database connection configured: {!string.IsNullOrEmpty(dbConnection) && !dbConnection.Contains("REPLACE_WITH")}");

return 0;
'@
            
            # Save and run the test
            $configTest | Out-File -FilePath "ConfigTest.cs" -Encoding UTF8
            $testResult = & dotnet run --project . --file ConfigTest.cs 2>&1
            Remove-Item "ConfigTest.cs" -ErrorAction SilentlyContinue
            
            if ($testResult -match "JWT Secret configured: True" -and $testResult -match "Database connection configured: True") {
                Write-Success "Application configuration loads successfully"
                $testResults.ApplicationStartup.Status = "Passed"
                $testResults.ApplicationStartup.Details = @("✅ Build successful", "✅ Configuration loads")
            } else {
                Write-Warning "Application builds but configuration has issues"
                $testResults.ApplicationStartup.Status = "Partial"  
                $testResults.ApplicationStartup.Details = @("✅ Build successful", "⚠️  Configuration issues")
            }
        } else {
            Write-Error "Application build failed"
            $testResults.ApplicationStartup.Status = "Failed"
            $testResults.ApplicationStartup.Details = @("❌ Build failed: $buildOutput")
        }
    } catch {
        Write-Error "Error testing application startup: $($_.Exception.Message)"
        $testResults.ApplicationStartup.Status = "Failed"
        $testResults.ApplicationStartup.Details = @("❌ Error: $($_.Exception.Message)")
    }
    
    Write-Host ""
}

# Summary Report
Write-Info "📋 Configuration Test Summary"
Write-Info "============================="

$passedTests = 0
$totalTests = 0

foreach ($testName in $testResults.Keys) {
    $result = $testResults[$testName]
    if ($result.Status -ne "Skipped") {
        $totalTests++
        if ($result.Status -eq "Passed") {
            $passedTests++
        }
    }
    
    $statusIcon = switch ($result.Status) {
        "Passed" { "✅" }
        "Partial" { "⚠️" }
        "Failed" { "❌" }
        "Skipped" { "⏭️" }
    }
    
    Write-Host "$statusIcon $testName`: $($result.Status)"
    
    if ($result.Details.Count -gt 0) {
        foreach ($detail in $result.Details) {
            Write-Host "   $detail" -ForegroundColor Gray
        }
    }
}

Write-Host ""

if ($totalTests -gt 0) {
    $successRate = [math]::Round(($passedTests / $totalTests) * 100, 1)
    
    if ($passedTests -eq $totalTests) {
        Write-Success "🎉 All tests passed! ($passedTests/$totalTests - $successRate%)"
    } elseif ($passedTests -gt 0) {
        Write-Warning "⚠️  Some tests failed ($passedTests/$totalTests - $successRate%)"
    } else {
        Write-Error "❌ All tests failed (0/$totalTests - 0%)"
    }
} else {
    Write-Info "ℹ️  No tests were executed"
}

Write-Host ""
Write-Info "📖 Next Steps:"

if ($testResults.EnvironmentVariables.Status -eq "Failed" -or $testResults.EnvironmentVariables.Status -eq "Partial") {
    Write-Host "  1. Configure development secrets: .\scripts\setup-development-secrets.ps1"
}

if ($testResults.KeyVault.Status -eq "Failed" -and $Environment -ne "Development") {
    Write-Host "  2. Configure Key Vault secrets: .\scripts\setup-keyvault-secrets.ps1"
}

if ($testResults.ApplicationStartup.Status -ne "Passed") {
    Write-Host "  3. Review configuration files and ensure all required secrets are set"
}

Write-Host "  4. Refer to CONFIGURATION_SECRETS.md for detailed setup instructions"
