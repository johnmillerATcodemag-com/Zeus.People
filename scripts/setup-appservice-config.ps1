#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Configure Azure App Service application settings for Zeus.People Academic Management System

.DESCRIPTION
    This script helps configure production secrets as Azure App Service application settings.
    It connects to your Azure subscription and App Service to set required configuration.

.PARAMETER AppServiceName
    Name of the Azure App Service

.PARAMETER ResourceGroupName
    Name of the resource group containing the App Service

.PARAMETER SubscriptionId
    Azure subscription ID (optional, uses current if not specified)

.PARAMETER Environment
    Environment name (staging, production, etc.)

.EXAMPLE
    .\setup-appservice-config.ps1 -AppServiceName "app-academic-staging-2ymnmfmrvsb3w" -ResourceGroupName "rg-academic-staging-westus2" -Environment "staging"

.EXAMPLE
    .\setup-appservice-config.ps1 -AppServiceName "your-app-service" -ResourceGroupName "your-rg" -SubscriptionId "your-sub-id" -Environment "production"
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$AppServiceName,
    
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,
    
    [string]$SubscriptionId,
    
    [Parameter(Mandatory = $true)]
    [string]$Environment
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

# Function to read secure input
function Read-SecureValue($Prompt, $Required = $true) {
    do {
        Write-Host "$Prompt" -ForegroundColor Cyan
        if (-not $Required) {
            Write-Host "(Optional - press Enter to skip): " -NoNewline
        }
        else {
            Write-Host "(Required): " -NoNewline
        }
        
        $secureString = Read-Host -AsSecureString
        $plainText = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto(
            [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureString))
        
        if ($Required -and [string]::IsNullOrEmpty($plainText)) {
            Write-Warning "This field is required. Please enter a value."
            continue
        }
        
        return $plainText
    } while ($true)
}

Write-Info "Zeus.People Academic Management System - Azure App Service Configuration"
Write-Info "======================================================================="
Write-Info "Environment: $Environment"
Write-Info "App Service: $AppServiceName"
Write-Info "Resource Group: $ResourceGroupName"

if ($SubscriptionId) {
    Write-Info "Subscription: $SubscriptionId"
}

Write-Host ""

# Check if Azure CLI is installed
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Error "Azure CLI is not installed. Please install it first."
    Write-Info "Download from: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
}

# Check if logged in to Azure
$account = az account show --query "user.name" -o tsv 2>$null
if (-not $account) {
    Write-Warning "Not logged in to Azure. Please login first."
    az login
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to login to Azure"
        exit 1
    }
}

Write-Success "Logged in to Azure as: $account"

# Set subscription if provided
if ($SubscriptionId) {
    Write-Info "Setting subscription to: $SubscriptionId"
    az account set --subscription $SubscriptionId
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to set subscription"
        exit 1
    }
}

# Verify App Service exists and we have access
Write-Info "Verifying App Service access..."
$appService = az webapp show --name $AppServiceName --resource-group $ResourceGroupName --query "defaultHostName" -o tsv 2>$null
if (-not $appService) {
    Write-Error "Cannot access App Service '$AppServiceName' in resource group '$ResourceGroupName'"
    Write-Info "Please ensure:"
    Write-Info "  1. App Service exists"
    Write-Info "  2. You have 'Website Contributor' or 'Contributor' role"
    exit 1
}

Write-Success "App Service access verified: https://$appService"

# Check if App Service has Managed Identity enabled
Write-Info "Checking Managed Identity configuration..."
$managedIdentity = az webapp identity show --name $AppServiceName --resource-group $ResourceGroupName --query "principalId" -o tsv 2>$null
if (-not $managedIdentity) {
    Write-Warning "Managed Identity is not enabled on the App Service"
    $enableMI = Read-Host "Enable System-Assigned Managed Identity? (Y/n)"
    if ($enableMI -ne 'n' -and $enableMI -ne 'N') {
        Write-Info "Enabling System-Assigned Managed Identity..."
        $identity = az webapp identity assign --name $AppServiceName --resource-group $ResourceGroupName --query "principalId" -o tsv
        if ($identity) {
            Write-Success "Managed Identity enabled with Principal ID: $identity"
            $managedIdentity = $identity
        }
        else {
            Write-Error "Failed to enable Managed Identity"
        }
    }
}
else {
    Write-Success "Managed Identity is enabled with Principal ID: $managedIdentity"
}

# Confirm before proceeding
Write-Host ""
Write-Warning "🔐 This script will configure sensitive settings in Azure App Service."
Write-Warning "⚠️  These will be stored as application settings (environment variables)."
Write-Host ""
$confirm = Read-Host "Continue? (y/N)"
if ($confirm -ne 'y' -and $confirm -ne 'Y') {
    Write-Info "Setup cancelled."
    exit 0
}

# Define the application settings we need to configure
$appSettings = @{
    "JwtSettings__SecretKey"                  = @{
        Description = "JWT Secret Key (minimum 32 characters)"
        Required    = $true
        Generate    = $true
    }
    "AzureAd__TenantId"                       = @{
        Description = "Azure AD Tenant ID"
        Required    = $true
        Generate    = $false
    }
    "AzureAd__ClientId"                       = @{
        Description = "Azure AD Client ID (Application ID)"
        Required    = $true
        Generate    = $false
    }
    "AzureAd__ClientSecret"                   = @{
        Description = "Azure AD Client Secret"
        Required    = $true
        Generate    = $false
    }
    "ConnectionStrings__AcademicDatabase"     = @{
        Description = "Academic Database Connection String"
        Required    = $true
        Generate    = $false
    }
    "ConnectionStrings__EventStoreDatabase"   = @{
        Description = "Event Store Database Connection String"
        Required    = $true
        Generate    = $false
    }
    "ConnectionStrings__ServiceBus"           = @{
        Description = "Service Bus Connection String"
        Required    = $true
        Generate    = $false
    }
    "ApplicationInsights__ConnectionString"   = @{
        Description = "Application Insights Connection String"
        Required    = $false
        Generate    = $false
    }
    "ApplicationInsights__InstrumentationKey" = @{
        Description = "Application Insights Instrumentation Key"
        Required    = $false
        Generate    = $false
    }
    "ASPNETCORE_ENVIRONMENT"                  = @{
        Description  = "ASP.NET Core Environment"
        Required     = $true
        Generate     = $false
        DefaultValue = $Environment
    }
}

Write-Host ""
Write-Info "📝 Configuring App Service Application Settings..."
Write-Host ""

# Get current app settings
Write-Info "Retrieving current application settings..."
$currentSettings = az webapp config appsettings list --name $AppServiceName --resource-group $ResourceGroupName --query "[].{name:name, value:value}" -o json | ConvertFrom-Json
$currentSettingsHash = @{}
foreach ($setting in $currentSettings) {
    $currentSettingsHash[$setting.name] = $setting.value
}

$configuredSettings = @()
$skippedSettings = @()
$settingsToUpdate = @()

foreach ($settingName in $appSettings.Keys) {
    $settingInfo = $appSettings[$settingName]
    
    Write-Host ""
    Write-Info "Configuring: $settingName"
    
    # Check if setting already exists
    $currentValue = $currentSettingsHash[$settingName]
    if ($currentValue) {
        Write-Warning "Application setting '$settingName' already exists"
        $overwrite = Read-Host "Overwrite existing setting? (y/N)"
        if ($overwrite -ne 'y' -and $overwrite -ne 'Y') {
            Write-Info "Skipping '$settingName'"
            $skippedSettings += $settingName
            continue
        }
    }
    
    # Get the setting value
    $settingValue = $null
    
    if ($settingInfo.DefaultValue) {
        $useDefault = Read-Host "Use default value '$($settingInfo.DefaultValue)'? (Y/n)"
        if ($useDefault -ne 'n' -and $useDefault -ne 'N') {
            $settingValue = $settingInfo.DefaultValue
        }
    }
    
    if (-not $settingValue -and $settingInfo.Generate -and $settingName -eq "JwtSettings__SecretKey") {
        $generate = Read-Host "Generate secure JWT secret automatically? (Y/n)"
        if ($generate -ne 'n' -and $generate -ne 'N') {
            # Generate secure JWT secret
            $settingValue = [System.Web.Security.Membership]::GeneratePassword(64, 16)
            Write-Success "Generated secure JWT secret key"
        }
    }
    
    if (-not $settingValue) {
        $settingValue = Read-SecureValue $settingInfo.Description $settingInfo.Required
    }
    
    if ([string]::IsNullOrEmpty($settingValue)) {
        if ($settingInfo.Required) {
            Write-Warning "Required setting '$settingName' not provided"
            $skippedSettings += $settingName
        }
        else {
            Write-Info "Optional setting '$settingName' skipped"
            $skippedSettings += $settingName
        }
        continue
    }
    
    # Add to settings to update
    $settingsToUpdate += @{
        Name  = $settingName
        Value = $settingValue
    }
    $configuredSettings += $settingName
}

# Update app settings in batch
if ($settingsToUpdate.Count -gt 0) {
    Write-Host ""
    Write-Info "Updating App Service application settings..."
    
    $settingsArgs = @()
    foreach ($setting in $settingsToUpdate) {
        $settingsArgs += "$($setting.Name)=$($setting.Value)"
    }
    
    $result = az webapp config appsettings set --name $AppServiceName --resource-group $ResourceGroupName --settings @settingsArgs 2>$null
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Successfully updated application settings"
    }
    else {
        Write-Error "Failed to update some application settings"
    }
}
else {
    Write-Info "No settings to update"
}

# Summary
Write-Host ""
Write-Info "🎉 App Service Configuration Complete!"
Write-Host ""

if ($configuredSettings.Count -gt 0) {
    Write-Success "Successfully configured application settings:"
    foreach ($setting in $configuredSettings) {
        Write-Host "  ✅ $setting" -ForegroundColor Green
    }
}

if ($skippedSettings.Count -gt 0) {
    Write-Warning "Skipped application settings:"
    foreach ($setting in $skippedSettings) {
        Write-Host "  ⏭️  $setting" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Info "📋 Next Steps:"
Write-Host "  1. Restart your App Service to apply new settings:"
Write-Host "     az webapp restart --name $AppServiceName --resource-group $ResourceGroupName"
Write-Host "  2. Monitor application logs to verify configuration"
Write-Host "  3. Test your application endpoints"
Write-Host "  4. Consider using Key Vault references for enhanced security:"
Write-Host "     Format: @Microsoft.KeyVault(VaultName=<vault>;SecretName=<secret>)"
Write-Host ""
Write-Info "🔍 App Service URL: https://$appService"

# Option to restart App Service
Write-Host ""
$restart = Read-Host "Restart App Service now to apply settings? (Y/n)"
if ($restart -ne 'n' -and $restart -ne 'N') {
    Write-Info "Restarting App Service..."
    az webapp restart --name $AppServiceName --resource-group $ResourceGroupName
    if ($LASTEXITCODE -eq 0) {
        Write-Success "App Service restarted successfully"
    }
    else {
        Write-Warning "Failed to restart App Service - you may need to restart it manually"
    }
}
