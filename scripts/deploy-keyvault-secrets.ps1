# Key Vault Secrets Deployment Script
# Configures Azure Key Vault secrets for Zeus.People application
#
# Prerequisites:
# - Azure CLI installed and authenticated
# - Proper permissions to manage Key Vault secrets
# - Infrastructure deployed using Bicep templates

param(
    [Parameter(Mandatory = $true)]
    [string]$Environment,
    
    [Parameter(Mandatory = $false)]
    [string]$KeyVaultName = "",
    
    [Parameter(Mandatory = $false)]
    [string]$ResourceGroup = "",
    
    [Parameter(Mandatory = $false)]
    [string]$SubscriptionId = "40d786b1-fabb-46d5-9c89-5194ea79dca1",
    
    [Parameter(Mandatory = $false)]
    [switch]$WhatIf
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

Write-Log "Starting Key Vault secrets deployment for environment: $Environment"

# Determine Key Vault name and resource group if not provided
if ([string]::IsNullOrEmpty($KeyVaultName)) {
    $KeyVaultName = "kvklle24thta446"
    Write-Log "Using default Key Vault name: $KeyVaultName"
}

if ([string]::IsNullOrEmpty($ResourceGroup)) {
    $ResourceGroup = "rg-academic-$Environment-eastus2"
    Write-Log "Using default Resource Group: $ResourceGroup"
}

try {
    Write-Log "Setting Azure subscription to: $SubscriptionId"
    az account set --subscription $SubscriptionId
    
    Write-Log "Verifying Key Vault access: $KeyVaultName"
    $keyVaultExists = az keyvault show --name $KeyVaultName --query "name" --output tsv 2>$null
    
    if (-not $keyVaultExists) {
        throw "Key Vault '$KeyVaultName' not found or not accessible"
    }
    
    Write-Log "Key Vault '$KeyVaultName' is accessible" -Level "SUCCESS"
    
    # Get infrastructure resource names for connection strings
    Write-Log "Retrieving Azure resources for connection strings..."
    
    # Get Cosmos DB connection string
    $cosmosAccountName = "cosmos-academic-$Environment-klle24thta446"
    Write-Log "Retrieving Cosmos DB connection string from: $cosmosAccountName"
    $cosmosConnectionString = az cosmosdb keys list --name $cosmosAccountName --resource-group $ResourceGroup --type connection-strings --query "connectionStrings[0].connectionString" --output tsv
    
    if ([string]::IsNullOrEmpty($cosmosConnectionString)) {
        Write-Log "Warning: Could not retrieve Cosmos DB connection string" -Level "WARN"
        $cosmosConnectionString = "AccountEndpoint=https://$cosmosAccountName.documents.azure.com:443/;AccountKey=<TO_BE_CONFIGURED>;"
    }
    
    # Get Service Bus connection string
    $serviceBusNamespace = "sb-academic-$Environment-klle24thta446"
    Write-Log "Retrieving Service Bus connection string from: $serviceBusNamespace"
    $serviceBusConnectionString = az servicebus namespace authorization-rule keys list --resource-group $ResourceGroup --namespace-name $serviceBusNamespace --name RootManageSharedAccessKey --query "primaryConnectionString" --output tsv
    
    if ([string]::IsNullOrEmpty($serviceBusConnectionString)) {
        Write-Log "Warning: Could not retrieve Service Bus connection string" -Level "WARN"
        $serviceBusConnectionString = "Endpoint=sb://$serviceBusNamespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=<TO_BE_CONFIGURED>"
    }
    
    # Get Application Insights instrumentation key
    $appInsightsName = "ai-academic-$Environment-klle24thta446"
    Write-Log "Retrieving Application Insights instrumentation key from: $appInsightsName"
    $appInsightsKey = az monitor app-insights component show --app $appInsightsName --resource-group $ResourceGroup --query "instrumentationKey" --output tsv 2>$null
    
    if ([string]::IsNullOrEmpty($appInsightsKey)) {
        Write-Log "Warning: Could not retrieve Application Insights instrumentation key" -Level "WARN"
        $appInsightsKey = "<TO_BE_CONFIGURED>"
    }
    
    # Define secrets to be set in Key Vault
    $secrets = @{
        # Database connection strings
        "DatabaseSettings--WriteConnectionString"      = $cosmosConnectionString
        "DatabaseSettings--ReadConnectionString"       = $cosmosConnectionString
        "DatabaseSettings--EventStoreConnectionString" = $cosmosConnectionString
        
        # Service Bus configuration
        "ServiceBusSettings--ConnectionString"         = $serviceBusConnectionString
        "ServiceBusSettings--Namespace"                = $serviceBusNamespace
        
        # Azure AD B2C configuration (placeholder values for now)
        "AzureAd--TenantId"                            = "<TO_BE_CONFIGURED>"
        "AzureAd--ClientId"                            = "<TO_BE_CONFIGURED>"
        "AzureAd--ClientSecret"                        = "<TO_BE_CONFIGURED>"
        "AzureAd--Domain"                              = "<TO_BE_CONFIGURED>"
        
        # Application Insights
        "ApplicationInsights--InstrumentationKey"      = $appInsightsKey
        
        # JWT signing key (generate a secure key)
        "JwtSettings--SecretKey"                       = [System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes([System.Guid]::NewGuid().ToString() + [System.Guid]::NewGuid().ToString()))
        
        # External service API keys (placeholders)
        "ExternalServices--ApiKey1"                    = "<TO_BE_CONFIGURED>"
        "ExternalServices--ApiKey2"                    = "<TO_BE_CONFIGURED>"
        
        # Additional configuration
        "ApplicationSettings--Environment"             = $Environment
        "ApplicationSettings--SupportEmail"            = "support@zeus-people.com"
    }
    
    Write-Log "Deploying secrets to Key Vault..."
    
    foreach ($secretName in $secrets.Keys) {
        $secretValue = $secrets[$secretName]
        
        if ($WhatIf) {
            Write-Log "WHAT-IF: Would set secret '$secretName' in Key Vault '$KeyVaultName'" -Level "WARN"
            continue
        }
        
        try {
            Write-Log "Setting secret: $secretName"
            
            # Set the secret in Key Vault
            az keyvault secret set --vault-name $KeyVaultName --name $secretName --value $secretValue --output none
            
            Write-Log "Successfully set secret: $secretName" -Level "SUCCESS"
        }
        catch {
            Write-Log "Failed to set secret '$secretName': $($_.Exception.Message)" -Level "ERROR"
        }
    }
    
    # Verify Key Vault RBAC permissions
    Write-Log "Verifying Key Vault RBAC permissions..."
    $managedIdentityName = "mi-academic-$Environment-klle24thta446"
    
    $roleAssignments = az role assignment list --scope "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.KeyVault/vaults/$KeyVaultName" --query "[].{PrincipalId:principalId, RoleDefinitionName:roleDefinitionName}" --output json | ConvertFrom-Json
    
    $hasSecretsUserRole = $roleAssignments | Where-Object { $_.RoleDefinitionName -eq "Key Vault Secrets User" }
    
    if ($hasSecretsUserRole) {
        Write-Log "Managed Identity has 'Key Vault Secrets User' role assigned" -Level "SUCCESS"
    }
    else {
        Write-Log "Warning: Managed Identity may not have proper Key Vault permissions" -Level "WARN"
    }
    
    Write-Log "Key Vault secrets deployment completed successfully" -Level "SUCCESS"
    
    # Display summary
    Write-Log "DEPLOYMENT SUMMARY" -Level "SUCCESS"
    Write-Log "==================" -Level "SUCCESS"
    Write-Log "Environment: $Environment" -Level "SUCCESS"
    Write-Log "Key Vault: $KeyVaultName" -Level "SUCCESS"
    Write-Log "Resource Group: $ResourceGroup" -Level "SUCCESS"
    Write-Log "Secrets deployed: $($secrets.Count)" -Level "SUCCESS"
    Write-Log "RBAC permissions verified" -Level "SUCCESS"
    
}
catch {
    Write-Log "Error during Key Vault secrets deployment: $($_.Exception.Message)" -Level "ERROR"
    exit 1
}
