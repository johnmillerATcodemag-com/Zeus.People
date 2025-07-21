#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deploys Key Vault secrets for Zeus.People application configuration
.DESCRIPTION
    This script creates and configures Azure Key Vault with all required secrets for the Zeus.People application.
    It supports multiple environments (Development, Staging, Production) and sets up managed identity access.
.PARAMETER Environment
    Target environment (Development, Staging, Production)
.PARAMETER ResourceGroupName
    Name of the Azure Resource Group
.PARAMETER Location
    Azure region for deployment
.PARAMETER AppServiceName
    Name of the App Service (for managed identity configuration)
.PARAMETER SubscriptionId
    Azure subscription ID (optional, uses current subscription if not specified)
.EXAMPLE
    .\Deploy-KeyVaultSecrets.ps1 -Environment "Production" -ResourceGroupName "rg-zeus-people-prod" -Location "East US" -AppServiceName "app-zeus-people-prod"
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
    [string]$AppServiceName,
    
    [Parameter(Mandatory = $false)]
    [string]$SubscriptionId,
    
    [Parameter(Mandatory = $false)]
    [switch]$WhatIf
)

# Script start time for duration tracking
$scriptStartTime = Get-Date
Write-Host "🚀 Starting Key Vault deployment at $scriptStartTime" -ForegroundColor Green

# Set error action preference
$ErrorActionPreference = "Stop"

# Import required modules
$requiredModules = @('Az.Accounts', 'Az.Resources', 'Az.KeyVault', 'Az.Profile', 'Az.ManagedServiceIdentity')
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

    # Generate resource names based on environment
    $envPrefix = switch ($Environment.ToLower()) {
        "development" { "dev" }
        "staging" { "stg" }
        "production" { "prod" }
    }
    
    $keyVaultName = "kv-zeus-people-$envPrefix-$(Get-Random -Minimum 1000 -Maximum 9999)"
    $managedIdentityName = "id-zeus-people-$envPrefix"

    Write-Host "📋 Deployment Configuration:" -ForegroundColor Cyan
    Write-Host "   Environment: $Environment" -ForegroundColor White
    Write-Host "   Resource Group: $ResourceGroupName" -ForegroundColor White
    Write-Host "   Location: $Location" -ForegroundColor White
    Write-Host "   Key Vault Name: $keyVaultName" -ForegroundColor White
    Write-Host "   Managed Identity: $managedIdentityName" -ForegroundColor White
    if ($AppServiceName) {
        Write-Host "   App Service: $AppServiceName" -ForegroundColor White
    }

    if ($WhatIf) {
        Write-Host "🔍 WhatIf mode enabled - no changes will be made" -ForegroundColor Yellow
        return
    }

    # Step 1: Create Resource Group if it doesn't exist
    Write-Host "`n📁 Step 1: Creating Resource Group..." -ForegroundColor Blue
    $rgStartTime = Get-Date
    
    $resourceGroup = Get-AzResourceGroup -Name $ResourceGroupName -ErrorAction SilentlyContinue
    if (!$resourceGroup) {
        Write-Host "   Creating new resource group: $ResourceGroupName" -ForegroundColor Yellow
        $resourceGroup = New-AzResourceGroup -Name $ResourceGroupName -Location $Location
        Write-Host "   ✅ Resource group created successfully" -ForegroundColor Green
    }
    else {
        Write-Host "   ✅ Resource group already exists" -ForegroundColor Green
    }
    
    $rgDuration = (Get-Date) - $rgStartTime
    Write-Host "   ⏱️  Duration: $($rgDuration.TotalSeconds) seconds" -ForegroundColor Gray

    # Step 2: Create User-Assigned Managed Identity
    Write-Host "`n🔐 Step 2: Creating Managed Identity..." -ForegroundColor Blue
    $miStartTime = Get-Date
    
    $managedIdentity = Get-AzUserAssignedIdentity -ResourceGroupName $ResourceGroupName -Name $managedIdentityName -ErrorAction SilentlyContinue
    if (!$managedIdentity) {
        Write-Host "   Creating managed identity: $managedIdentityName" -ForegroundColor Yellow
        $managedIdentity = New-AzUserAssignedIdentity -ResourceGroupName $ResourceGroupName -Name $managedIdentityName -Location $Location
        Write-Host "   ✅ Managed identity created successfully" -ForegroundColor Green
        Write-Host "   Client ID: $($managedIdentity.ClientId)" -ForegroundColor Gray
        Write-Host "   Principal ID: $($managedIdentity.PrincipalId)" -ForegroundColor Gray
    }
    else {
        Write-Host "   ✅ Managed identity already exists" -ForegroundColor Green
        Write-Host "   Client ID: $($managedIdentity.ClientId)" -ForegroundColor Gray
        Write-Host "   Principal ID: $($managedIdentity.PrincipalId)" -ForegroundColor Gray
    }
    
    $miDuration = (Get-Date) - $miStartTime
    Write-Host "   ⏱️  Duration: $($miDuration.TotalSeconds) seconds" -ForegroundColor Gray

    # Step 3: Create Key Vault
    Write-Host "`n🔑 Step 3: Creating Key Vault..." -ForegroundColor Blue
    $kvStartTime = Get-Date
    
    $keyVault = Get-AzKeyVault -ResourceGroupName $ResourceGroupName -VaultName $keyVaultName -ErrorAction SilentlyContinue
    if (!$keyVault) {
        Write-Host "   Creating Key Vault: $keyVaultName" -ForegroundColor Yellow
        $keyVault = New-AzKeyVault `
            -ResourceGroupName $ResourceGroupName `
            -VaultName $keyVaultName `
            -Location $Location `
            -EnabledForDeployment `
            -EnabledForTemplateDeployment `
            -EnabledForDiskEncryption `
            -EnableRbacAuthorization:$false `
            -SoftDeleteRetentionInDays 7
            
        Write-Host "   ✅ Key Vault created successfully" -ForegroundColor Green
        Write-Host "   Vault URI: $($keyVault.VaultUri)" -ForegroundColor Gray
    }
    else {
        Write-Host "   ✅ Key Vault already exists" -ForegroundColor Green
        Write-Host "   Vault URI: $($keyVault.VaultUri)" -ForegroundColor Gray
    }
    
    $kvDuration = (Get-Date) - $kvStartTime
    Write-Host "   ⏱️  Duration: $($kvDuration.TotalSeconds) seconds" -ForegroundColor Gray

    # Step 4: Grant Managed Identity access to Key Vault
    Write-Host "`n🔐 Step 4: Configuring Key Vault Access..." -ForegroundColor Blue
    $accessStartTime = Get-Date
    
    # Grant secrets permissions to managed identity
    Write-Host "   Granting Key Vault access to managed identity..." -ForegroundColor Yellow
    Set-AzKeyVaultAccessPolicy `
        -VaultName $keyVaultName `
        -ObjectId $managedIdentity.PrincipalId `
        -PermissionsToSecrets Get, List `
        -PermissionsToKeys Get, List `
        -PermissionsToCertificates Get, List
    
    # Grant current user full access for secret management
    $currentUser = (Get-AzContext).Account.Id
    $userObjectId = (Get-AzADUser -UserPrincipalName $currentUser).Id
    if ($userObjectId) {
        Write-Host "   Granting Key Vault access to current user..." -ForegroundColor Yellow
        Set-AzKeyVaultAccessPolicy `
            -VaultName $keyVaultName `
            -ObjectId $userObjectId `
            -PermissionsToSecrets Get, List, Set, Delete, Backup, Restore, Recover, Purge `
            -PermissionsToKeys Get, List, Create, Delete, Update, Import, Backup, Restore, Recover, Purge `
            -PermissionsToCertificates Get, List, Create, Delete, Update, Import, Backup, Restore, Recover, Purge, ManageContacts, ManageIssuers, GetIssuers, ListIssuers, SetIssuers, DeleteIssuers
    }
    
    Write-Host "   ✅ Key Vault access configured successfully" -ForegroundColor Green
    
    $accessDuration = (Get-Date) - $accessStartTime
    Write-Host "   ⏱️  Duration: $($accessDuration.TotalSeconds) seconds" -ForegroundColor Gray

    # Step 5: Create environment-specific secrets
    Write-Host "`n🔧 Step 5: Creating Key Vault Secrets..." -ForegroundColor Blue
    $secretsStartTime = Get-Date
    
    # Define secrets based on environment
    $secrets = @{
        # Database Configuration
        "Database--ConnectionString"            = "Server=tcp:sql-zeus-people-$envPrefix.database.windows.net,1433;Initial Catalog=ZeusPeople$Environment;Authentication=Active Directory Managed Identity;Encrypt=True;TrustServerCertificate=False"
        "Database--ReadOnlyConnectionString"    = "Server=tcp:sql-zeus-people-$envPrefix.database.windows.net,1433;Initial Catalog=ZeusPeople$Environment;Authentication=Active Directory Managed Identity;Encrypt=True;TrustServerCertificate=False;ApplicationIntent=ReadOnly"
        
        # Service Bus Configuration
        "ServiceBus--ConnectionString"          = "Endpoint=sb://sb-zeus-people-$envPrefix.servicebus.windows.net/;Authentication=Managed Identity"
        "ServiceBus--Namespace"                 = "sb-zeus-people-$envPrefix.servicebus.windows.net"
        
        # Azure AD B2C Configuration
        "AzureAd--ClientSecret"                 = "$(New-Guid)"
        "AzureAd--Instance"                     = "https://zeuspeopleaad$envPrefix.b2clogin.com"
        "AzureAd--Domain"                       = "zeuspeopleaad$envPrefix.onmicrosoft.com"
        "AzureAd--ClientId"                     = "$(New-Guid)"
        "AzureAd--SignUpSignInPolicyId"         = "B2C_1_SignUpSignIn"
        
        # JWT Configuration
        "JwtSettings--SecretKey"                = [System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes((New-Guid).ToString() + (New-Guid).ToString()))
        "JwtSettings--Issuer"                   = "https://app-zeus-people-$envPrefix.azurewebsites.net"
        "JwtSettings--Audience"                 = "https://app-zeus-people-$envPrefix.azurewebsites.net"
        
        # Application Insights
        "ApplicationInsights--ConnectionString" = "InstrumentationKey=$(New-Guid);IngestionEndpoint=https://eastus-8.in.applicationinsights.azure.com/"
        
        # External Service Keys (placeholder values)
        "ExternalServices--ApiKey"              = "$(New-Guid)"
        "ExternalServices--SecretKey"           = [System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes((New-Guid).ToString()))
        
        # Application Settings
        "Application--Environment"              = $Environment
        "Application--Version"                  = "1.0.0"
    }

    # Environment-specific overrides
    switch ($Environment.ToLower()) {
        "development" {
            $secrets["Database--ConnectionString"] = "Server=(localdb)\MSSQLLocalDB;Database=ZeusPeopleDev;Integrated Security=true;Encrypt=False"
            $secrets["ServiceBus--ConnectionString"] = "Endpoint=sb://sb-zeus-people-dev.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=PLACEHOLDER_KEY"
        }
        "staging" {
            $secrets["JwtSettings--Issuer"] = "https://app-zeus-people-stg.azurewebsites.net"
            $secrets["JwtSettings--Audience"] = "https://app-zeus-people-stg.azurewebsites.net"
        }
        "production" {
            $secrets["JwtSettings--Issuer"] = "https://app-zeus-people.azurewebsites.net"
            $secrets["JwtSettings--Audience"] = "https://app-zeus-people.azurewebsites.net"
        }
    }

    Write-Host "   Creating $($secrets.Count) secrets..." -ForegroundColor Yellow
    $secretCount = 0
    
    foreach ($secretName in $secrets.Keys) {
        $secretCount++
        Write-Host "   [$secretCount/$($secrets.Count)] Setting secret: $secretName" -ForegroundColor Gray
        
        $secureString = ConvertTo-SecureString -String $secrets[$secretName] -AsPlainText -Force
        Set-AzKeyVaultSecret -VaultName $keyVaultName -Name $secretName -SecretValue $secureString | Out-Null
    }
    
    Write-Host "   ✅ All secrets created successfully" -ForegroundColor Green
    
    $secretsDuration = (Get-Date) - $secretsStartTime
    Write-Host "   ⏱️  Duration: $($secretsDuration.TotalSeconds) seconds" -ForegroundColor Gray

    # Step 6: Configure App Service Managed Identity (if App Service is specified)
    if ($AppServiceName) {
        Write-Host "`n🌐 Step 6: Configuring App Service Managed Identity..." -ForegroundColor Blue
        $appStartTime = Get-Date
        
        try {
            # Enable system-assigned managed identity on App Service
            Write-Host "   Enabling managed identity on App Service: $AppServiceName" -ForegroundColor Yellow
            $appService = Get-AzWebApp -ResourceGroupName $ResourceGroupName -Name $AppServiceName -ErrorAction SilentlyContinue
            
            if ($appService) {
                # Enable system-assigned managed identity
                Set-AzWebApp -ResourceGroupName $ResourceGroupName -Name $AppServiceName -AssignIdentity $true | Out-Null
                
                # Get the managed identity principal ID
                $appService = Get-AzWebApp -ResourceGroupName $ResourceGroupName -Name $AppServiceName
                $principalId = $appService.Identity.PrincipalId
                
                if ($principalId) {
                    Write-Host "   Granting Key Vault access to App Service managed identity..." -ForegroundColor Yellow
                    Set-AzKeyVaultAccessPolicy `
                        -VaultName $keyVaultName `
                        -ObjectId $principalId `
                        -PermissionsToSecrets Get, List
                    
                    Write-Host "   ✅ App Service managed identity configured" -ForegroundColor Green
                }
                else {
                    Write-Host "   ⚠️  Could not retrieve App Service managed identity principal ID" -ForegroundColor Yellow
                }
            }
            else {
                Write-Host "   ⚠️  App Service not found: $AppServiceName" -ForegroundColor Yellow
            }
        }
        catch {
            Write-Host "   ⚠️  Failed to configure App Service managed identity: $($_.Exception.Message)" -ForegroundColor Yellow
        }
        
        $appDuration = (Get-Date) - $appStartTime
        Write-Host "   ⏱️  Duration: $($appDuration.TotalSeconds) seconds" -ForegroundColor Gray
    }

    # Step 7: Generate deployment summary
    Write-Host "`n📊 Step 7: Generating Deployment Summary..." -ForegroundColor Blue
    
    $totalDuration = (Get-Date) - $scriptStartTime
    
    $deploymentSummary = @{
        Environment                = $Environment
        ResourceGroup              = $ResourceGroupName
        Location                   = $Location
        KeyVaultName               = $keyVaultName
        KeyVaultUri                = $keyVault.VaultUri
        ManagedIdentityName        = $managedIdentityName
        ManagedIdentityClientId    = $managedIdentity.ClientId
        ManagedIdentityPrincipalId = $managedIdentity.PrincipalId
        SecretsCreated             = $secrets.Count
        TotalDuration              = $totalDuration.ToString("mm\:ss")
        DeploymentTime             = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    }

    # Output deployment summary
    Write-Host "`n🎉 Deployment Completed Successfully!" -ForegroundColor Green
    Write-Host "===============================================" -ForegroundColor Cyan
    Write-Host "Environment: $($deploymentSummary.Environment)" -ForegroundColor White
    Write-Host "Resource Group: $($deploymentSummary.ResourceGroup)" -ForegroundColor White
    Write-Host "Key Vault Name: $($deploymentSummary.KeyVaultName)" -ForegroundColor White
    Write-Host "Key Vault URI: $($deploymentSummary.KeyVaultUri)" -ForegroundColor White
    Write-Host "Managed Identity: $($deploymentSummary.ManagedIdentityName)" -ForegroundColor White
    Write-Host "Client ID: $($deploymentSummary.ManagedIdentityClientId)" -ForegroundColor White
    Write-Host "Secrets Created: $($deploymentSummary.SecretsCreated)" -ForegroundColor White
    Write-Host "Total Duration: $($deploymentSummary.TotalDuration)" -ForegroundColor White
    Write-Host "===============================================" -ForegroundColor Cyan

    # Save deployment summary to file
    $summaryPath = "deployment-summary-$Environment-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    $deploymentSummary | ConvertTo-Json -Depth 10 | Out-File -FilePath $summaryPath -Encoding UTF8
    Write-Host "📄 Deployment summary saved to: $summaryPath" -ForegroundColor Gray

    # Output configuration for appsettings
    Write-Host "`n📋 Configuration for appsettings.$Environment.json:" -ForegroundColor Cyan
    $appSettingsConfig = @{
        KeyVault = @{
            VaultUrl           = $keyVault.VaultUri.TrimEnd('/')
            UseManagedIdentity = $true
            ClientId           = $managedIdentity.ClientId
        }
    }
    
    Write-Host ($appSettingsConfig | ConvertTo-Json -Depth 10) -ForegroundColor Yellow

    return $deploymentSummary

}
catch {
    Write-Error "❌ Deployment failed: $($_.Exception.Message)"
    Write-Error "Stack trace: $($_.Exception.StackTrace)"
    throw
}
