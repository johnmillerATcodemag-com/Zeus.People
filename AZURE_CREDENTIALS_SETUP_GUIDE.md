# Azure Credentials Setup Guide for GitHub Actions

## Required GitHub Secrets

Your staging deployment pipeline requires the following GitHub repository secrets:

### 1. Core Azure Service Principal Secrets
- `AZURE_CREDENTIALS` - JSON object with service principal credentials
- `AZURE_CLIENT_ID` - Service principal client ID
- `AZURE_CLIENT_SECRET` - Service principal client secret
- `AZURE_TENANT_ID` - Azure tenant ID

### 2. Application-Specific Secrets
- `MANAGED_IDENTITY_CLIENT_ID` - Managed identity client ID for the web app
- `APP_INSIGHTS_CONNECTION_STRING` - Application Insights connection string

## Step 1: Create Azure Service Principal

Run these commands in Azure CLI to create a service principal with the necessary permissions:

```bash
# Login to Azure
az login

# Set your subscription (replace with your subscription ID)
az account set --subscription "your-subscription-id"

# Create service principal for GitHub Actions
az ad sp create-for-rbac --name "GitHub-Actions-Zeus-People" \
  --role contributor \
  --scopes /subscriptions/your-subscription-id \
  --sdk-auth

# Note: Save the output JSON - you'll need it for AZURE_CREDENTIALS
```

The output will look like this:
```json
{
  "clientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "clientSecret": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "subscriptionId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "tenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com/",
  "activeDirectoryGraphResourceId": "https://graph.windows.net/",
  "sqlManagementEndpointUrl": "https://management.core.windows.net:8443/",
  "galleryEndpointUrl": "https://gallery.azure.com/",
  "managementEndpointUrl": "https://management.core.windows.net/"
}
```

## Step 2: Get Application-Specific Values

### Managed Identity Client ID
```bash
# Get the managed identity client ID (replace with your resource group name)
az identity show --resource-group rg-academic-staging-westus2 \
  --name managed-identity-academic-staging-2ymnmfmrvsb3w \
  --query clientId --output tsv
```

### Application Insights Connection String
```bash
# Get Application Insights connection string (replace with your resource group name)
az monitor app-insights component show \
  --app app-insights-academic-staging-2ymnmfmrvsb3w \
  --resource-group rg-academic-staging-westus2 \
  --query connectionString --output tsv
```

## Step 3: Add Secrets to GitHub Repository

1. Go to your GitHub repository: https://github.com/johnmillerATcodemag-com/Zeus.People
2. Navigate to **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret** for each of the following:

### AZURE_CREDENTIALS
```json
{
  "clientId": "your-service-principal-client-id",
  "clientSecret": "your-service-principal-client-secret",
  "subscriptionId": "your-subscription-id",
  "tenantId": "your-tenant-id",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com/",
  "activeDirectoryGraphResourceId": "https://graph.windows.net/",
  "sqlManagementEndpointUrl": "https://management.core.windows.net:8443/",
  "galleryEndpointUrl": "https://gallery.azure.com/",
  "managementEndpointUrl": "https://management.core.windows.net/"
}
```

### AZURE_CLIENT_ID
```
your-service-principal-client-id
```

### AZURE_CLIENT_SECRET
```
your-service-principal-client-secret
```

### AZURE_TENANT_ID
```
your-tenant-id
```

### MANAGED_IDENTITY_CLIENT_ID
```
your-managed-identity-client-id
```

### APP_INSIGHTS_CONNECTION_STRING
```
InstrumentationKey=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx;IngestionEndpoint=https://westus2-1.in.applicationinsights.azure.com/;LiveEndpoint=https://westus2.livediagnostics.monitor.azure.com/;ApplicationId=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
```

## Step 4: Verify Access

After setting up the secrets, you can verify the service principal has the correct permissions:

```bash
# Test login with service principal
az login --service-principal \
  --username your-service-principal-client-id \
  --password your-service-principal-client-secret \
  --tenant your-tenant-id

# Test access to resource group
az group show --name rg-academic-staging-westus2

# Test access to deployment
az deployment group list --resource-group rg-academic-staging-westus2
```

## Troubleshooting

### Common Issues:
1. **Insufficient permissions**: Ensure the service principal has "Contributor" role on the subscription or resource group
2. **Resource not found**: Verify resource names match what exists in Azure
3. **Authentication failed**: Double-check client ID, secret, and tenant ID are correct

### Additional Permissions Needed:
If you encounter permission issues, you may need to add these roles to the service principal:
```bash
# Add Key Vault access (if using Key Vault)
az role assignment create \
  --assignee your-service-principal-client-id \
  --role "Key Vault Contributor" \
  --scope /subscriptions/your-subscription-id/resourceGroups/rg-academic-staging-westus2

# Add Application Insights access
az role assignment create \
  --assignee your-service-principal-client-id \
  --role "Monitoring Contributor" \
  --scope /subscriptions/your-subscription-id/resourceGroups/rg-academic-staging-westus2
```
