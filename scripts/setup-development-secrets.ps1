#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Setup development environment secrets via environment variables

.DESCRIPTION
    This script helps configure development secrets securely using environment variables.
    It prompts for secrets and sets them in the current PowerShell session and optionally
    saves them to a local .env file for development use.

.PARAMETER SaveToEnvFile
    Save environment variables to .env.local file for development use

.PARAMETER ShowCurrentValues
    Display currently configured environment variables (masked)

.EXAMPLE
    .\setup-development-secrets.ps1
    
.EXAMPLE
    .\setup-development-secrets.ps1 -SaveToEnvFile
    
.EXAMPLE
    .\setup-development-secrets.ps1 -ShowCurrentValues
#>

param(
    [switch]$SaveToEnvFile,
    [switch]$ShowCurrentValues
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

# Function to mask secrets for display
function Get-MaskedValue($Value) {
    if ([string]::IsNullOrEmpty($Value)) {
        return "NOT_SET"
    }
    if ($Value.Length -le 8) {
        return "***"
    }
    return $Value.Substring(0, 4) + "..." + $Value.Substring($Value.Length - 4)
}

# Function to read secure input
function Read-SecureValue($Prompt, $CurrentValue) {
    if ([string]::IsNullOrEmpty($CurrentValue)) {
        $maskedCurrent = "NOT_SET"
    }
    else {
        $maskedCurrent = Get-MaskedValue $CurrentValue
    }
    
    Write-Host "$Prompt" -ForegroundColor Cyan
    Write-Host "Current value: $maskedCurrent" -ForegroundColor Gray
    Write-Host "Enter new value (or press Enter to keep current): " -NoNewline
    
    $secureString = Read-Host -AsSecureString
    $plainText = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureString))
    
    if ([string]::IsNullOrEmpty($plainText)) {
        return $CurrentValue
    }
    
    return $plainText
}

Write-Info "Zeus.People Academic Management System - Development Secrets Setup"
Write-Info "=================================================================="

if ($ShowCurrentValues) {
    Write-Info "Current Environment Variables:"
    Write-Host ""
    
    $secrets = @{
        "JWT_SECRET_KEY"                           = $env:JWT_SECRET_KEY
        "AZURE_AD_TENANT_ID"                       = $env:AZURE_AD_TENANT_ID
        "AZURE_AD_CLIENT_ID"                       = $env:AZURE_AD_CLIENT_ID
        "AZURE_AD_CLIENT_SECRET"                   = $env:AZURE_AD_CLIENT_SECRET
        "DATABASE_CONNECTION_STRING"               = $env:DATABASE_CONNECTION_STRING
        "EVENT_STORE_CONNECTION_STRING"            = $env:EVENT_STORE_CONNECTION_STRING
        "SERVICE_BUS_CONNECTION_STRING"            = $env:SERVICE_BUS_CONNECTION_STRING
        "APPLICATION_INSIGHTS_CONNECTION_STRING"   = $env:APPLICATION_INSIGHTS_CONNECTION_STRING
        "APPLICATION_INSIGHTS_INSTRUMENTATION_KEY" = $env:APPLICATION_INSIGHTS_INSTRUMENTATION_KEY
    }
    
    foreach ($key in $secrets.Keys) {
        $maskedValue = Get-MaskedValue $secrets[$key]
        Write-Host "  $key = $maskedValue" -ForegroundColor Gray
    }
    
    return
}

Write-Warning "🔐 This script will help you configure sensitive development secrets."
Write-Warning "⚠️  Secrets will be stored as environment variables in this session."

if ($SaveToEnvFile) {
    Write-Warning "⚠️  Secrets will also be saved to .env.local file (ensure it's in .gitignore!)"
}

Write-Host ""
$confirm = Read-Host "Continue? (y/N)"
if ($confirm -ne 'y' -and $confirm -ne 'Y') {
    Write-Info "Setup cancelled."
    return
}

Write-Host ""
Write-Info "📝 Configuring Development Secrets..."
Write-Host ""

# JWT Settings
Write-Info "1. JWT Configuration"
$jwtSecret = Read-SecureValue "Enter JWT Secret Key (minimum 32 characters):" $env:JWT_SECRET_KEY

if ([string]::IsNullOrEmpty($jwtSecret) -or $jwtSecret.Length -lt 32) {
    Write-Warning "Generating secure JWT secret key..."
    $jwtSecret = [System.Web.Security.Membership]::GeneratePassword(64, 16)
    Write-Success "Generated secure JWT secret key"
}

# Azure AD Configuration
Write-Host ""
Write-Info "2. Azure AD Configuration"
$tenantId = Read-SecureValue "Enter Azure AD Tenant ID:" $env:AZURE_AD_TENANT_ID
$clientId = Read-SecureValue "Enter Azure AD Client ID:" $env:AZURE_AD_CLIENT_ID
$clientSecret = Read-SecureValue "Enter Azure AD Client Secret:" $env:AZURE_AD_CLIENT_SECRET

# Database Configuration
Write-Host ""
Write-Info "3. Database Configuration"
$dbConnection = Read-SecureValue "Enter Database Connection String:" $env:DATABASE_CONNECTION_STRING
$eventStoreConnection = Read-SecureValue "Enter Event Store Connection String:" $env:EVENT_STORE_CONNECTION_STRING

if ([string]::IsNullOrEmpty($dbConnection)) {
    $dbConnection = "Server=(localdb)\\MSSQLLocalDB;Database=Zeus.People.Academic.Dev;Trusted_Connection=True;MultipleActiveResultSets=true"
    Write-Info "Using default LocalDB connection string for development"
}

if ([string]::IsNullOrEmpty($eventStoreConnection)) {
    $eventStoreConnection = "Server=(localdb)\\MSSQLLocalDB;Database=Zeus.People.EventStore.Dev;Trusted_Connection=True;MultipleActiveResultSets=true"
    Write-Info "Using default LocalDB connection string for Event Store"
}

# Service Bus Configuration
Write-Host ""
Write-Info "4. Service Bus Configuration"
$serviceBusConnection = Read-SecureValue "Enter Service Bus Connection String:" $env:SERVICE_BUS_CONNECTION_STRING

if ([string]::IsNullOrEmpty($serviceBusConnection)) {
    $serviceBusConnection = "UseDevelopmentInMemory"
    Write-Info "Using in-memory Service Bus for development"
}

# Application Insights Configuration
Write-Host ""
Write-Info "5. Application Insights Configuration"
$appInsightsConnection = Read-SecureValue "Enter Application Insights Connection String:" $env:APPLICATION_INSIGHTS_CONNECTION_STRING
$appInsightsKey = Read-SecureValue "Enter Application Insights Instrumentation Key:" $env:APPLICATION_INSIGHTS_INSTRUMENTATION_KEY

# Set environment variables
Write-Host ""
Write-Info "🔧 Setting Environment Variables..."

$env:JWT_SECRET_KEY = $jwtSecret
$env:AZURE_AD_TENANT_ID = $tenantId
$env:AZURE_AD_CLIENT_ID = $clientId
$env:AZURE_AD_CLIENT_SECRET = $clientSecret
$env:DATABASE_CONNECTION_STRING = $dbConnection
$env:EVENT_STORE_CONNECTION_STRING = $eventStoreConnection
$env:SERVICE_BUS_CONNECTION_STRING = $serviceBusConnection
$env:APPLICATION_INSIGHTS_CONNECTION_STRING = $appInsightsConnection
$env:APPLICATION_INSIGHTS_INSTRUMENTATION_KEY = $appInsightsKey

Write-Success "Environment variables configured for this session"

# Save to .env file if requested
if ($SaveToEnvFile) {
    $envFilePath = ".env.local"
    Write-Info "💾 Saving to $envFilePath..."
    
    $envContent = @"
# Zeus.People Development Environment Variables
# Generated on $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
# DO NOT commit this file to version control!

# JWT Settings
JWT_SECRET_KEY=$jwtSecret

# Azure AD
AZURE_AD_TENANT_ID=$tenantId
AZURE_AD_CLIENT_ID=$clientId
AZURE_AD_CLIENT_SECRET=$clientSecret

# Database Connections
DATABASE_CONNECTION_STRING=$dbConnection
EVENT_STORE_CONNECTION_STRING=$eventStoreConnection

# Service Bus
SERVICE_BUS_CONNECTION_STRING=$serviceBusConnection

# Application Insights
APPLICATION_INSIGHTS_CONNECTION_STRING=$appInsightsConnection
APPLICATION_INSIGHTS_INSTRUMENTATION_KEY=$appInsightsKey
"@

    $envContent | Out-File -FilePath $envFilePath -Encoding UTF8
    Write-Success "Environment variables saved to $envFilePath"
    Write-Warning "⚠️  Ensure $envFilePath is in your .gitignore file!"
}

Write-Host ""
Write-Success "🎉 Development secrets configuration complete!"
Write-Host ""
Write-Info "📋 Next Steps:"
Write-Host "  1. Run the application to test the configuration"
Write-Host "  2. Use .\test-app-config.ps1 to validate settings"
Write-Host "  3. For production, use Azure Key Vault instead"
Write-Host ""
Write-Info "🔍 To view current settings (masked): .\setup-development-secrets.ps1 -ShowCurrentValues"
