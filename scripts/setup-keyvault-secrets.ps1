#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Configure Azure Key Vault secrets for Zeus.People Academic Management System

.DESCRIPTION
    This script helps configure production secrets in Azure Key Vault.
    It connects to your Azure subscription and Key Vault to set required secrets.

.PARAMETER KeyVaultName
    Name of the Azure Key Vault

.PARAMETER ResourceGroupName
    Name of the resource group containing the Key Vault

.PARAMETER SubscriptionId
    Azure subscription ID (optional, uses current if not specified)

.PARAMETER Environment
    Environment name (staging, production, etc.)

.EXAMPLE
    .\setup-keyvault-secrets.ps1 -KeyVaultName "kv2ymnmfmrvsb3w" -ResourceGroupName "rg-academic-staging-westus2" -Environment "staging"

.EXAMPLE
    .\setup-keyvault-secrets.ps1 -KeyVaultName "your-keyvault" -ResourceGroupName "your-rg" -SubscriptionId "your-sub-id" -Environment "production"
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$KeyVaultName,
    
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,
    
    [string]$SubscriptionId,
    
    [Parameter(Mandatory=$true)]
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
        } else {
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

Write-Info "Zeus.People Academic Management System - Azure Key Vault Setup"
Write-Info "==============================================================="
Write-Info "Environment: $Environment"
Write-Info "Key Vault: $KeyVaultName"
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

# Verify Key Vault exists and we have access
Write-Info "Verifying Key Vault access..."
$keyVault = az keyvault show --name $KeyVaultName --resource-group $ResourceGroupName --query "properties.vaultUri" -o tsv 2>$null
if (-not $keyVault) {
    Write-Error "Cannot access Key Vault '$KeyVaultName' in resource group '$ResourceGroupName'"
    Write-Info "Please ensure:"
    Write-Info "  1. Key Vault exists"
    Write-Info "  2. You have 'Key Vault Secrets Officer' or 'Key Vault Administrator' role"
    Write-Info "  3. Key Vault access policies or RBAC are configured correctly"
    exit 1
}

Write-Success "Key Vault access verified: $keyVault"

# Confirm before proceeding
Write-Host ""
Write-Warning "🔐 This script will configure sensitive secrets in Azure Key Vault."
Write-Warning "⚠️  Make sure you have the correct values ready."
Write-Host ""
$confirm = Read-Host "Continue? (y/N)"
if ($confirm -ne 'y' -and $confirm -ne 'Y') {
    Write-Info "Setup cancelled."
    exit 0
}

# Define the secrets we need to configure
$secrets = @{
    "JwtSettings--SecretKey" = @{
        Description = "JWT Secret Key (minimum 32 characters)"
        Required = $true
        Generate = $true
    }
    "AzureAd--TenantId" = @{
        Description = "Azure AD Tenant ID"
        Required = $true
        Generate = $false
    }
    "AzureAd--ClientId" = @{
        Description = "Azure AD Client ID (Application ID)"
        Required = $true
        Generate = $false
    }
    "AzureAd--ClientSecret" = @{
        Description = "Azure AD Client Secret"
        Required = $true
        Generate = $false
    }
    "ConnectionStrings--AcademicDatabase" = @{
        Description = "Academic Database Connection String"
        Required = $true
        Generate = $false
    }
    "ConnectionStrings--EventStoreDatabase" = @{
        Description = "Event Store Database Connection String"
        Required = $true
        Generate = $false
    }
    "ConnectionStrings--ServiceBus" = @{
        Description = "Service Bus Connection String"
        Required = $true
        Generate = $false
    }
    "ApplicationInsights--ConnectionString" = @{
        Description = "Application Insights Connection String"
        Required = $false
        Generate = $false
    }
    "ApplicationInsights--InstrumentationKey" = @{
        Description = "Application Insights Instrumentation Key"
        Required = $false
        Generate = $false
    }
}

Write-Host ""
Write-Info "📝 Configuring Key Vault Secrets..."
Write-Host ""

$configuredSecrets = @()
$skippedSecrets = @()

foreach ($secretName in $secrets.Keys) {
    $secretInfo = $secrets[$secretName]
    $displayName = $secretName -replace '--', ':'
    
    Write-Host ""
    Write-Info "Configuring: $displayName"
    
    # Check if secret already exists
    $existingSecret = az keyvault secret show --vault-name $KeyVaultName --name $secretName --query "value" -o tsv 2>$null
    if ($existingSecret) {
        Write-Warning "Secret '$secretName' already exists in Key Vault"
        $overwrite = Read-Host "Overwrite existing secret? (y/N)"
        if ($overwrite -ne 'y' -and $overwrite -ne 'Y') {
            Write-Info "Skipping '$secretName'"
            $skippedSecrets += $secretName
            continue
        }
    }
    
    # Get the secret value
    $secretValue = $null
    
    if ($secretInfo.Generate -and $secretName -eq "JwtSettings--SecretKey") {
        $generate = Read-Host "Generate secure JWT secret automatically? (Y/n)"
        if ($generate -ne 'n' -and $generate -ne 'N') {
            # Generate secure JWT secret
            $secretValue = [System.Web.Security.Membership]::GeneratePassword(64, 16)
            Write-Success "Generated secure JWT secret key"
        }
    }
    
    if (-not $secretValue) {
        $secretValue = Read-SecureValue $secretInfo.Description $secretInfo.Required
    }
    
    if ([string]::IsNullOrEmpty($secretValue)) {
        if ($secretInfo.Required) {
            Write-Warning "Required secret '$secretName' not provided"
            $skippedSecrets += $secretName
        } else {
            Write-Info "Optional secret '$secretName' skipped"
            $skippedSecrets += $secretName
        }
        continue
    }
    
    # Set the secret in Key Vault
    Write-Info "Setting secret in Key Vault..."
    $result = az keyvault secret set --vault-name $KeyVaultName --name $secretName --value $secretValue --query "id" -o tsv 2>$null
    
    if ($LASTEXITCODE -eq 0 -and $result) {
        Write-Success "✅ Successfully set '$secretName'"
        $configuredSecrets += $secretName
    } else {
        Write-Error "❌ Failed to set '$secretName'"
        $skippedSecrets += $secretName
    }
}

# Summary
Write-Host ""
Write-Info "🎉 Key Vault Configuration Complete!"
Write-Host ""

if ($configuredSecrets.Count -gt 0) {
    Write-Success "Successfully configured secrets:"
    foreach ($secret in $configuredSecrets) {
        Write-Host "  ✅ $($secret -replace '--', ':')" -ForegroundColor Green
    }
}

if ($skippedSecrets.Count -gt 0) {
    Write-Warning "Skipped secrets:"
    foreach ($secret in $skippedSecrets) {
        Write-Host "  ⏭️  $($secret -replace '--', ':')" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Info "📋 Next Steps:"
Write-Host "  1. Verify your App Service has Managed Identity enabled"
Write-Host "  2. Grant your App Service access to Key Vault:"
Write-Host "     az role assignment create --role 'Key Vault Secrets User' --assignee <app-service-principal-id> --scope /subscriptions/<sub>/resourceGroups/$ResourceGroupName/providers/Microsoft.KeyVault/vaults/$KeyVaultName"
Write-Host "  3. Deploy your application - it will automatically read from Key Vault"
Write-Host "  4. Monitor Key Vault access logs for troubleshooting"
Write-Host ""
Write-Info "🔍 Key Vault URL: $keyVault"
