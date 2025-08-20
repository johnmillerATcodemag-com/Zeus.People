# Azure Secrets Values Reference

## What Each Secret Should Contain

### `AZURE_CLIENT_ID`

- **Value**: The Application (client) ID of your Azure Service Principal
- **Format**: UUID (e.g., `12345678-1234-1234-1234-123456789012`)
- **Where to find**: Azure Portal → Azure Active Directory → App registrations → [Your Service Principal] → Application (client) ID

### `AZURE_CLIENT_SECRET`

- **Value**: The client secret/password you created for the service principal
- **Format**: Random string (e.g., `abcd1234-EFGH-5678-ijkl-9012MNOP3456`)
- **Where to find**: Azure Portal → Azure Active Directory → App registrations → [Your Service Principal] → Certificates & secrets
- **Note**: You can only see this value once when you create it. If lost, create a new one.

### `AZURE_TENANT_ID`

- **Value**: Your Azure Active Directory tenant ID
- **Format**: UUID (e.g., `87654321-4321-4321-4321-210987654321`)
- **Where to find**: Azure Portal → Azure Active Directory → Properties → Tenant ID

### `AZURE_SUBSCRIPTION_ID`

- **Value**: The subscription ID where your resources will be deployed
- **Format**: UUID (e.g., `11111111-2222-3333-4444-555555555555`)
- **Where to find**: Azure Portal → Subscriptions → [Your Subscription] → Subscription ID

## How to Check Current Values

### Option 1: Azure CLI Commands

```bash
# Get tenant ID
az account show --query tenantId -o tsv

# Get subscription ID
az account show --query id -o tsv

# List service principals (to find your GitHub Actions one)
az ad sp list --display-name "GitHub-Actions-Zeus-People" --query "[].{Name:displayName, AppId:appId}" -o table
```

### Option 2: Azure PowerShell Commands

```powershell
# Get tenant and subscription info
Get-AzContext | Select-Object Tenant, Subscription

# Get service principal info
Get-AzADServicePrincipal -DisplayName "GitHub-Actions-Zeus-People" | Select-Object DisplayName, ApplicationId
```

### Option 3: Azure Portal Navigation

1. **Azure Portal** → **Azure Active Directory**
2. **App registrations** → Find your service principal
3. Copy the **Application (client) ID** → This is your `AZURE_CLIENT_ID`
4. Go to **Certificates & secrets** → Client secrets → This is your `AZURE_CLIENT_SECRET`
5. Go to **Properties** → Copy **Tenant ID** → This is your `AZURE_TENANT_ID`
6. **Subscriptions** → Select your subscription → Copy **Subscription ID** → This is your `AZURE_SUBSCRIPTION_ID`

## Expected Values Should Be Identical Across Environments

**Important**: For the Zeus.People project, all environments (staging, production, repository-level) should use the **same values** for these secrets because:

1. **Same Azure Tenant**: You're deploying to the same Azure tenant
2. **Same Service Principal**: Using one service principal for all deployments
3. **Same Subscription**: Likely deploying to the same Azure subscription
4. **Different Resource Groups**: Environments are separated by resource group names, not credentials

## Environment-Specific Considerations

If you want environment-specific separation:

### Option 1: Same Secrets (Recommended for Zeus.People)

- Use identical values across all environments
- Separate staging/production by resource group names in your Bicep templates
- Simpler management, single service principal

### Option 2: Environment-Specific Service Principals (Advanced)

- Create separate service principals for staging vs production
- Different `AZURE_CLIENT_ID` and `AZURE_CLIENT_SECRET` per environment
- Same `AZURE_TENANT_ID` and potentially different `AZURE_SUBSCRIPTION_ID`
- More complex but better security isolation

## Current Service Principal Status

Based on previous conversation, you should have:

- **Service Principal Name**: `GitHub-Actions-Zeus-People`
- **Permissions**: Contributor role on your subscription
- **Status**: Already created and tested locally

## How to Retrieve Your Current Values

Since I cannot see your stored secrets, you need to:

1. **Check your local Azure CLI context**:

   ```bash
   az account show
   ```

2. **Find your service principal**:

   ```bash
   az ad sp list --display-name "GitHub-Actions-Zeus-People"
   ```

3. **If you don't have the client secret anymore**:

   - Go to Azure Portal → Azure AD → App registrations → Your app → Certificates & secrets
   - Create a new client secret
   - Update GitHub secrets with the new value

4. **Verify the service principal has correct permissions**:
   ```bash
   az role assignment list --assignee {your-client-id} --output table
   ```

## Security Note

**Never commit these values to your repository or share them in plain text.** They should only exist in:

- Azure Portal (for viewing/management)
- GitHub repository secrets (encrypted storage)
- Your local secure password manager (for backup)
