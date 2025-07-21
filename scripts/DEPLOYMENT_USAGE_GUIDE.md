# Deployment Scripts Usage Guide

This guide provides instructions for using the deployment scripts to deploy the Zeus People application configuration to Azure.

## Prerequisites

1. **Azure PowerShell**: Install the required Azure PowerShell modules

   ```powershell
   Install-Module -Name Az.Accounts, Az.Resources, Az.KeyVault, Az.Profile, Az.ManagedServiceIdentity, Az.Websites -Force
   ```

2. **Azure CLI**: Install Azure CLI for Bicep template deployment

   ```powershell
   winget install -e --id Microsoft.AzureCLI
   ```

3. **Azure Authentication**: Login to Azure

   ```powershell
   Connect-AzAccount
   az login
   ```

4. **Bicep CLI**: Install Bicep CLI
   ```powershell
   az bicep install
   ```

## Available Scripts

### 1. Deploy-Complete.ps1 (Recommended)

**Purpose**: Complete deployment orchestration script that handles infrastructure and secrets.

**Usage**:

```powershell
.\scripts\Deploy-Complete.ps1 -Environment "Development" -ResourceGroupName "rg-zeus-people-dev" -Location "East US" -SubscriptionId "your-subscription-id"
```

**Parameters**:

- `Environment`: Target environment (Development, Staging, Production)
- `ResourceGroupName`: Azure resource group name
- `Location`: Azure region
- `SubscriptionId`: Azure subscription ID
- `SecretsFilePath`: Optional path to secrets JSON file
- `WhatIf`: Test deployment without making changes

### 2. Deploy-KeyVaultSecrets.ps1

**Purpose**: Deploy Key Vault infrastructure and configure secrets.

**Usage**:

```powershell
.\scripts\Deploy-KeyVaultSecrets.ps1 -Environment "Development" -ResourceGroupName "rg-zeus-people-dev" -Location "East US" -SubscriptionId "your-subscription-id"
```

### 3. Update-KeyVaultSecrets.ps1

**Purpose**: Update existing secrets in deployed Key Vault.

**Usage**:

```powershell
# Update single secret
.\scripts\Update-KeyVaultSecrets.ps1 -KeyVaultName "kv-zeus-people-dev" -SecretName "Database--ConnectionString" -SecretValue "new-connection-string"

# Bulk update from file
.\scripts\Update-KeyVaultSecrets.ps1 -KeyVaultName "kv-zeus-people-dev" -SecretsFilePath ".\scripts\secrets-development.json"
```

## Environment-Specific Deployment

### Development Environment

```powershell
# Complete deployment
.\scripts\Deploy-Complete.ps1 -Environment "Development" -ResourceGroupName "rg-zeus-people-dev" -Location "East US" -SubscriptionId "your-subscription-id" -SecretsFilePath ".\scripts\secrets-development.json"
```

### Staging Environment

```powershell
# Complete deployment
.\scripts\Deploy-Complete.ps1 -Environment "Staging" -ResourceGroupName "rg-zeus-people-stg" -Location "East US" -SubscriptionId "your-subscription-id" -SecretsFilePath ".\scripts\secrets-staging.json"
```

### Production Environment

```powershell
# Complete deployment
.\scripts\Deploy-Complete.ps1 -Environment "Production" -ResourceGroupName "rg-zeus-people-prod" -Location "East US" -SubscriptionId "your-subscription-id" -SecretsFilePath ".\scripts\secrets-production.json"
```

## Configuration Files

### Secrets Configuration

Before deployment, create environment-specific secrets files based on the sample files:

1. Copy sample files:

   ```powershell
   Copy-Item ".\scripts\secrets-development.sample.json" ".\scripts\secrets-development.json"
   Copy-Item ".\scripts\secrets-staging.sample.json" ".\scripts\secrets-staging.json"
   Copy-Item ".\scripts\secrets-production.sample.json" ".\scripts\secrets-production.json"
   ```

2. Update the copied files with actual values:
   - Replace `REPLACE_WITH_ACTUAL_*` placeholders with real values
   - Update connection strings with actual server names
   - Set proper client IDs and secrets

### Required Secrets

Each environment requires these secrets:

- `Database--ConnectionString`: Primary database connection
- `Database--ReadOnlyConnectionString`: Read-only database connection
- `ServiceBus--ConnectionString`: Service Bus connection
- `AzureAd--ClientSecret`: Azure AD B2C client secret
- `JwtSettings--SecretKey`: JWT signing key (Base64 encoded)
- `ApplicationInsights--ConnectionString`: Application Insights connection
- `ExternalServices--ApiKey`: External API key
- `ExternalServices--SecretKey`: External service secret

## Deployment Steps

### Step 1: Prepare Configuration

1. Ensure you have the correct Azure subscription selected
2. Create and update secrets configuration files
3. Verify resource naming conventions

### Step 2: Test Deployment

```powershell
# Test without making changes
.\scripts\Deploy-Complete.ps1 -Environment "Development" -ResourceGroupName "rg-zeus-people-dev" -Location "East US" -SubscriptionId "your-subscription-id" -WhatIf
```

### Step 3: Deploy Infrastructure

```powershell
# Deploy to development first
.\scripts\Deploy-Complete.ps1 -Environment "Development" -ResourceGroupName "rg-zeus-people-dev" -Location "East US" -SubscriptionId "your-subscription-id" -SecretsFilePath ".\scripts\secrets-development.json"
```

### Step 4: Verify Deployment

1. Check Azure portal for created resources
2. Verify Key Vault secrets are created
3. Test managed identity access
4. Validate application startup

### Step 5: Deploy to Other Environments

Repeat the deployment process for Staging and Production environments.

## Troubleshooting

### Common Issues

1. **Authentication Errors**

   - Ensure you're logged into Azure: `Connect-AzAccount`
   - Verify subscription access: `Get-AzSubscription`

2. **Permission Errors**

   - Ensure your account has Contributor role on the subscription
   - Verify Key Vault access policies are correct

3. **Resource Naming Conflicts**

   - Key Vault names must be globally unique
   - App Service names must be globally unique

4. **Bicep Deployment Errors**
   - Check Azure CLI is installed: `az --version`
   - Verify Bicep CLI: `az bicep version`

### Validation Commands

```powershell
# Check resource group
Get-AzResourceGroup -Name "rg-zeus-people-dev"

# Verify Key Vault
Get-AzKeyVault -ResourceGroupName "rg-zeus-people-dev"

# List secrets
Get-AzKeyVaultSecret -VaultName "kv-zeus-people-dev"

# Test managed identity
Get-AzUserAssignedIdentity -ResourceGroupName "rg-zeus-people-dev"
```

## Security Considerations

1. **Secrets Management**

   - Never commit actual secrets to source control
   - Use `.gitignore` for secrets files: `secrets-*.json`
   - Rotate secrets regularly

2. **Access Control**

   - Use managed identities where possible
   - Apply principle of least privilege
   - Regular access reviews

3. **Environment Isolation**
   - Separate resource groups per environment
   - Different Key Vaults per environment
   - Environment-specific managed identities

## Maintenance

### Regular Tasks

1. **Secret Rotation**: Use `Update-KeyVaultSecrets.ps1` to update secrets
2. **Access Review**: Verify Key Vault access policies
3. **Monitoring**: Check Application Insights for configuration issues

### Updates

To update the configuration system:

1. Test changes in Development environment
2. Update secrets as needed
3. Deploy to Staging for validation
4. Deploy to Production after approval

## Support

For issues with deployment scripts:

1. Check script output logs
2. Verify Azure permissions
3. Validate configuration files
4. Test in Development environment first

Remember to follow your organization's deployment approval process for Production environments.
