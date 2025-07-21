#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Complete deployment script for Zeus.People Azure infrastructure and configuration
.DESCRIPTION
    This script orchestrates the complete deployment of Azure infrastructure and Key Vault configuration
    for the Zeus.People application across all environments.
.PARAMETER Environment
    Target environment (Development, Staging, Production)
.PARAMETER ResourceGroupName
    Name of the Azure Resource Group
.PARAMETER Location
    Azure region for deployment
.PARAMETER SubscriptionId
    Azure subscription ID (optional, uses current subscription if not specified)
.PARAMETER SkipInfrastructure
    Skip infrastructure deployment and only configure secrets
.PARAMETER WhatIf
    Show what would be deployed without making changes
.EXAMPLE
    .\Deploy-Complete.ps1 -Environment "Production" -ResourceGroupName "rg-zeus-people-prod" -Location "East US"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("Development", "Staging", "Production")]
    [string]$Environment,
    
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory = $true)]
    [string]$Location,
    
    [Parameter(Mandatory = $false)]
    [string]$SubscriptionId,
    
    [Parameter(Mandatory = $false)]
    [switch]$SkipInfrastructure,
    
    [Parameter(Mandatory = $false)]
    [switch]$WhatIf
)

# Script start time for duration tracking
$scriptStartTime = Get-Date
Write-Host "🚀 Starting complete deployment at $scriptStartTime" -ForegroundColor Green

# Set error action preference
$ErrorActionPreference = "Stop"

# Get script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Import required modules
$requiredModules = @('Az.Accounts', 'Az.Resources', 'Az.KeyVault', 'Az.Profile', 'Az.ManagedServiceIdentity', 'Az.Websites')
foreach ($module in $requiredModules) {
    if (!(Get-Module -ListAvailable -Name $module)) {
        Write-Host "⚠️  Installing required module: $module" -ForegroundColor Yellow
        Install-Module -Name $module -Force -AllowClobber -Scope CurrentUser
    }
    Import-Module $module -Force
}

try {
    # Set subscription context if specified
    if ($SubscriptionId) {
        Write-Host "🔄 Setting subscription context to: $SubscriptionId" -ForegroundColor Blue
        $context = Set-AzContext -SubscriptionId $SubscriptionId
    }
    else {
        $context = Get-AzContext
        $SubscriptionId = $context.Subscription.Id
    }
    
    Write-Host "✅ Using subscription: $($context.Subscription.Name) ($SubscriptionId)" -ForegroundColor Green

    # Generate environment prefix
    $envPrefix = switch ($Environment.ToLower()) {
        "development" { "dev" }
        "staging" { "stg" }
        "production" { "prod" }
    }

    Write-Host "📋 Deployment Configuration:" -ForegroundColor Cyan
    Write-Host "   Environment: $Environment" -ForegroundColor White
    Write-Host "   Environment Prefix: $envPrefix" -ForegroundColor White
    Write-Host "   Resource Group: $ResourceGroupName" -ForegroundColor White
    Write-Host "   Location: $Location" -ForegroundColor White
    Write-Host "   Subscription: $($context.Subscription.Name)" -ForegroundColor White
    Write-Host "   Skip Infrastructure: $SkipInfrastructure" -ForegroundColor White

    if ($WhatIf) {
        Write-Host "🔍 WhatIf mode enabled - no changes will be made" -ForegroundColor Yellow
        return
    }

    # Step 1: Deploy Infrastructure (if not skipped)
    if (-not $SkipInfrastructure) {
        Write-Host "`n🏗️  Step 1: Deploying Infrastructure..." -ForegroundColor Blue
        $infraStartTime = Get-Date
        
        # Create Resource Group if it doesn't exist
        $resourceGroup = Get-AzResourceGroup -Name $ResourceGroupName -ErrorAction SilentlyContinue
        if (!$resourceGroup) {
            Write-Host "   Creating new resource group: $ResourceGroupName" -ForegroundColor Yellow
            $resourceGroup = New-AzResourceGroup -Name $ResourceGroupName -Location $Location
            Write-Host "   ✅ Resource group created successfully" -ForegroundColor Green
        }
        else {
            Write-Host "   ✅ Resource group already exists" -ForegroundColor Green
        }

        # Deploy Bicep template
        $bicepFile = Join-Path $scriptDir "keyvault-infrastructure.bicep"
        if (Test-Path $bicepFile) {
            Write-Host "   Deploying Bicep template: $bicepFile" -ForegroundColor Yellow
            
            $deploymentName = "zeus-people-infrastructure-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
            $deployment = New-AzResourceGroupDeployment `
                -ResourceGroupName $ResourceGroupName `
                -TemplateFile $bicepFile `
                -environment $envPrefix `
                -location $Location `
                -Name $deploymentName `
                -Verbose

            if ($deployment.ProvisioningState -eq "Succeeded") {
                Write-Host "   ✅ Infrastructure deployment succeeded" -ForegroundColor Green
                
                # Extract outputs
                $keyVaultName = $deployment.Outputs.keyVaultName.Value
                $keyVaultUri = $deployment.Outputs.keyVaultUri.Value
                $managedIdentityClientId = $deployment.Outputs.managedIdentityClientId.Value
                $appServiceName = $deployment.Outputs.appServiceName.Value
                
                Write-Host "   Key Vault Name: $keyVaultName" -ForegroundColor Gray
                Write-Host "   Key Vault URI: $keyVaultUri" -ForegroundColor Gray
                Write-Host "   Managed Identity Client ID: $managedIdentityClientId" -ForegroundColor Gray
                Write-Host "   App Service Name: $appServiceName" -ForegroundColor Gray
            }
            else {
                throw "Infrastructure deployment failed with state: $($deployment.ProvisioningState)"
            }
        }
        else {
            throw "Bicep template not found: $bicepFile"
        }
        
        $infraDuration = (Get-Date) - $infraStartTime
        Write-Host "   ⏱️  Infrastructure deployment duration: $($infraDuration.TotalSeconds) seconds" -ForegroundColor Gray
    }
    else {
        Write-Host "`n⏭️  Step 1: Skipping infrastructure deployment" -ForegroundColor Yellow
        
        # We still need to get the Key Vault info for secrets deployment
        $keyVaultName = "kv-zeus-people-$envPrefix-*"
        $keyVaults = Get-AzKeyVault -ResourceGroupName $ResourceGroupName | Where-Object { $_.VaultName -like $keyVaultName }
        
        if ($keyVaults.Count -eq 0) {
            throw "No Key Vault found matching pattern: $keyVaultName"
        }
        elseif ($keyVaults.Count -gt 1) {
            Write-Host "   Multiple Key Vaults found, using the first one: $($keyVaults[0].VaultName)" -ForegroundColor Yellow
            $keyVault = $keyVaults[0]
        }
        else {
            $keyVault = $keyVaults[0]
        }
        
        $keyVaultName = $keyVault.VaultName
        $keyVaultUri = $keyVault.VaultUri
        Write-Host "   Using existing Key Vault: $keyVaultName" -ForegroundColor Green
    }

    # Step 2: Configure Additional Secrets (using PowerShell script)
    Write-Host "`n🔐 Step 2: Configuring Additional Secrets..." -ForegroundColor Blue
    $secretsStartTime = Get-Date
    
    $keyVaultSecretsScript = Join-Path $scriptDir "Deploy-KeyVaultSecrets.ps1"
    if (Test-Path $keyVaultSecretsScript) {
        Write-Host "   Running Key Vault secrets script..." -ForegroundColor Yellow
        
        # Call the Key Vault secrets script
        $secretsParams = @{
            Environment       = $Environment
            ResourceGroupName = $ResourceGroupName
            Location          = $Location
        }
        
        if ($appServiceName) {
            $secretsParams.AppServiceName = $appServiceName
        }
        
        if ($SubscriptionId) {
            $secretsParams.SubscriptionId = $SubscriptionId
        }
        
        $secretsResult = & $keyVaultSecretsScript @secretsParams
        
        if ($secretsResult) {
            Write-Host "   ✅ Additional secrets configured successfully" -ForegroundColor Green
        }
    }
    else {
        Write-Host "   ⚠️  Key Vault secrets script not found: $keyVaultSecretsScript" -ForegroundColor Yellow
    }
    
    $secretsDuration = (Get-Date) - $secretsStartTime
    Write-Host "   ⏱️  Secrets configuration duration: $($secretsDuration.TotalSeconds) seconds" -ForegroundColor Gray

    # Step 3: Generate Application Configuration Files
    Write-Host "`n📝 Step 3: Generating Application Configuration..." -ForegroundColor Blue
    $configStartTime = Get-Date
    
    # Generate appsettings for the environment
    $appSettingsConfig = @{
        ConnectionStrings   = @{
            DefaultConnection  = "@Microsoft.KeyVault(SecretUri=$keyVaultUri/secrets/Database--ConnectionString/)"
            ReadOnlyConnection = "@Microsoft.KeyVault(SecretUri=$keyVaultUri/secrets/Database--ReadOnlyConnectionString/)"
        }
        Database            = @{
            CommandTimeoutSeconds      = 30
            EnableSensitiveDataLogging = $Environment -eq "Development"
            MaxRetryCount              = 3
        }
        ServiceBus          = @{
            ConnectionString         = "@Microsoft.KeyVault(SecretUri=$keyVaultUri/secrets/ServiceBus--ConnectionString/)"
            Namespace                = "@Microsoft.KeyVault(SecretUri=$keyVaultUri/secrets/ServiceBus--Namespace/)"
            DefaultMessageTimeToLive = "01:00:00"
            AutoDeleteOnIdle         = "7.00:00:00"
        }
        AzureAd             = @{
            Instance             = "@Microsoft.KeyVault(SecretUri=$keyVaultUri/secrets/AzureAd--Instance/)"
            Domain               = "@Microsoft.KeyVault(SecretUri=$keyVaultUri/secrets/AzureAd--Domain/)"
            ClientId             = "@Microsoft.KeyVault(SecretUri=$keyVaultUri/secrets/AzureAd--ClientId/)"
            ClientSecret         = "@Microsoft.KeyVault(SecretUri=$keyVaultUri/secrets/AzureAd--ClientSecret/)"
            SignUpSignInPolicyId = "@Microsoft.KeyVault(SecretUri=$keyVaultUri/secrets/AzureAd--SignUpSignInPolicyId/)"
        }
        JwtSettings         = @{
            SecretKey              = "@Microsoft.KeyVault(SecretUri=$keyVaultUri/secrets/JwtSettings--SecretKey/)"
            Issuer                 = "@Microsoft.KeyVault(SecretUri=$keyVaultUri/secrets/JwtSettings--Issuer/)"
            Audience               = "@Microsoft.KeyVault(SecretUri=$keyVaultUri/secrets/JwtSettings--Audience/)"
            ExpiryMinutes          = 60
            RefreshTokenExpiryDays = 7
        }
        KeyVault            = @{
            VaultUrl                   = $keyVaultUri.TrimEnd('/')
            UseManagedIdentity         = $true
            ClientId                   = $managedIdentityClientId
            SecretCacheDurationMinutes = 30
            EnableSecretCaching        = $true
        }
        Application         = @{
            Name                 = "Zeus.People"
            Version              = "1.0.0"
            Environment          = $Environment
            EnableDetailedErrors = $Environment -eq "Development"
            EnableSwagger        = $Environment -ne "Production"
        }
        ApplicationInsights = @{
            ConnectionString = "@Microsoft.KeyVault(SecretUri=$keyVaultUri/secrets/ApplicationInsights--ConnectionString/)"
        }
        Logging             = @{
            LogLevel = @{
                Default                      = $Environment -eq "Development" ? "Debug" : "Information"
                Microsoft                    = "Warning"
                "Microsoft.Hosting.Lifetime" = "Information"
            }
        }
    }

    # Save configuration file
    $configOutputDir = Join-Path $scriptDir ".." "src" "API"
    if (!(Test-Path $configOutputDir)) {
        $configOutputDir = $scriptDir
    }
    
    $appSettingsFile = Join-Path $configOutputDir "appsettings.$Environment.generated.json"
    $appSettingsConfig | ConvertTo-Json -Depth 10 | Out-File -FilePath $appSettingsFile -Encoding UTF8
    
    Write-Host "   📄 Generated configuration file: $appSettingsFile" -ForegroundColor Gray
    Write-Host "   ✅ Application configuration generated" -ForegroundColor Green
    
    $configDuration = (Get-Date) - $configStartTime
    Write-Host "   ⏱️  Configuration generation duration: $($configDuration.TotalSeconds) seconds" -ForegroundColor Gray

    # Step 4: Validation and Testing
    Write-Host "`n🧪 Step 4: Validating Deployment..." -ForegroundColor Blue
    $validationStartTime = Get-Date
    
    # Test Key Vault access
    Write-Host "   Testing Key Vault connectivity..." -ForegroundColor Yellow
    try {
        $testSecret = Get-AzKeyVaultSecret -VaultName $keyVaultName -Name "Database--ConnectionString" -AsPlainText -ErrorAction Stop
        if ($testSecret) {
            Write-Host "   ✅ Key Vault access validated" -ForegroundColor Green
        }
    }
    catch {
        Write-Host "   ⚠️  Key Vault access test failed: $($_.Exception.Message)" -ForegroundColor Yellow
    }
    
    # Test App Service configuration (if deployed)
    if ($appServiceName -and !$SkipInfrastructure) {
        Write-Host "   Testing App Service configuration..." -ForegroundColor Yellow
        try {
            $appService = Get-AzWebApp -ResourceGroupName $ResourceGroupName -Name $appServiceName -ErrorAction Stop
            if ($appService.Identity.PrincipalId) {
                Write-Host "   ✅ App Service managed identity configured" -ForegroundColor Green
            }
        }
        catch {
            Write-Host "   ⚠️  App Service configuration test failed: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
    
    $validationDuration = (Get-Date) - $validationStartTime
    Write-Host "   ⏱️  Validation duration: $($validationDuration.TotalSeconds) seconds" -ForegroundColor Gray

    # Final Summary
    $totalDuration = (Get-Date) - $scriptStartTime
    
    Write-Host "`n🎉 Deployment Completed Successfully!" -ForegroundColor Green
    Write-Host "===============================================" -ForegroundColor Cyan
    Write-Host "Environment: $Environment" -ForegroundColor White
    Write-Host "Resource Group: $ResourceGroupName" -ForegroundColor White
    Write-Host "Key Vault Name: $keyVaultName" -ForegroundColor White
    Write-Host "Key Vault URI: $keyVaultUri" -ForegroundColor White
    if ($managedIdentityClientId) {
        Write-Host "Managed Identity Client ID: $managedIdentityClientId" -ForegroundColor White
    }
    if ($appServiceName) {
        Write-Host "App Service Name: $appServiceName" -ForegroundColor White
    }
    Write-Host "Total Duration: $($totalDuration.ToString('mm\:ss'))" -ForegroundColor White
    Write-Host "Deployment Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor White
    Write-Host "===============================================" -ForegroundColor Cyan

    # Save final deployment summary
    $finalSummary = @{
        Environment             = $Environment
        ResourceGroup           = $ResourceGroupName
        Location                = $Location
        KeyVaultName            = $keyVaultName
        KeyVaultUri             = $keyVaultUri
        ManagedIdentityClientId = $managedIdentityClientId
        AppServiceName          = $appServiceName
        ConfigurationFile       = $appSettingsFile
        TotalDuration           = $totalDuration.ToString("mm\:ss")
        DeploymentTime          = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        SkippedInfrastructure   = $SkipInfrastructure.IsPresent
    }

    $finalSummaryPath = "deployment-complete-$Environment-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    $finalSummary | ConvertTo-Json -Depth 10 | Out-File -FilePath $finalSummaryPath -Encoding UTF8
    Write-Host "📄 Final deployment summary saved to: $finalSummaryPath" -ForegroundColor Gray

    # Next steps
    Write-Host "`n📋 Next Steps:" -ForegroundColor Cyan
    Write-Host "1. Copy the generated appsettings.$Environment.generated.json to your API project" -ForegroundColor White
    Write-Host "2. Update any placeholder values in Key Vault secrets as needed" -ForegroundColor White
    Write-Host "3. Deploy your application to the App Service" -ForegroundColor White
    Write-Host "4. Test the application configuration and Key Vault integration" -ForegroundColor White

    return $finalSummary

}
catch {
    Write-Error "❌ Deployment failed: $($_.Exception.Message)"
    Write-Error "Stack trace: $($_.Exception.StackTrace)"
    throw
}
