# 🔑 GitHub Secrets Configuration Summary

## Required GitHub Repository Secrets

Go to: https://github.com/johnmillerATcodemag-com/Zeus.People/settings/secrets/actions

### 1. AZURE_CREDENTIALS

**Value (JSON):**

```json
{
  "clientId": "a90252fe-4d36-4e18-8a85-7e8ecbf04ed0",
  "clientSecret": "[REDACTED - Use value from service principal creation]",
  "subscriptionId": "5232b409-b25e-441c-9951-16e69069f224",
  "tenantId": "24db396b-b795-45c9-bcfa-d3559193f2f7",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com/",
  "activeDirectoryGraphResourceId": "https://graph.windows.net/",
  "sqlManagementEndpointUrl": "https://management.core.windows.net:8443/",
  "galleryEndpointUrl": "https://gallery.azure.com/",
  "managementEndpointUrl": "https://management.core.windows.net/"
}
```

### 2. AZURE_CLIENT_ID

**Value:**

```
a90252fe-4d36-4e18-8a85-7e8ecbf04ed0
```

### 3. AZURE_CLIENT_SECRET

**Value:**

```
[REDACTED - Use value from service principal creation output]
```

### 4. AZURE_TENANT_ID

**Value:**

```
24db396b-b795-45c9-bcfa-d3559193f2f7
```

### 5. MANAGED_IDENTITY_CLIENT_ID

**Value:** (Will be populated after first successful infrastructure deployment)

```
TO_BE_ADDED_AFTER_INFRASTRUCTURE_DEPLOYMENT
```

### 6. APP_INSIGHTS_CONNECTION_STRING

**Value:** (Will be populated after first successful infrastructure deployment)

```
TO_BE_ADDED_AFTER_INFRASTRUCTURE_DEPLOYMENT
```

## 📋 Step-by-Step Instructions

1. **Add Core Secrets First**: Add the first 4 secrets (AZURE_CREDENTIALS, AZURE_CLIENT_ID, AZURE_CLIENT_SECRET, AZURE_TENANT_ID)
2. **Set Placeholder Values**: For the last 2 secrets, set them to "PLACEHOLDER" for now
3. **Run Pipeline**: This will deploy the infrastructure and create the resources
4. **Update Missing Secrets**: After successful infrastructure deployment, update the last 2 secrets with actual values

## 🚀 After Adding Secrets

Once you've added all secrets to GitHub:

1. Go to the **Actions** tab in your repository
2. The pipeline should automatically start running
3. If not, click **Run workflow** on the "🚀 Staging Deployment Pipeline"
4. Monitor the progress - it should now complete all stages successfully

## 🔧 If Infrastructure Deployment Succeeds

After the infrastructure is deployed, run this command to get the missing values:

```bash
# Get Managed Identity Client ID
az identity show --resource-group rg-academic-staging-westus2 --name managed-identity-academic-staging-2ymnmfmrvsb3w --query clientId --output tsv

# Get Application Insights Connection String
az monitor app-insights component show --app app-insights-academic-staging-2ymnmfmrvsb3w --resource-group rg-academic-staging-westus2 --query connectionString --output tsv
```

Then update these two GitHub secrets:

- **MANAGED_IDENTITY_CLIENT_ID**: Replace with the actual managed identity client ID
- **APP_INSIGHTS_CONNECTION_STRING**: Replace with the actual connection string

## ⚠️ Security Note

**IMPORTANT**: These credentials provide access to your Azure subscription. Keep them secure and never commit them to source code.

The service principal has been created with "Contributor" role on your subscription, which allows it to create and manage Azure resources as needed by the pipeline.
