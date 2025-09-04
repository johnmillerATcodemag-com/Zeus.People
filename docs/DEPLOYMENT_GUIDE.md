# Azure Bicep Deployment Validation and Testing Guide

## ✅ **Template Validation Status**

### Bicep Compilation Results

- **Main Template**: ✅ Compiles successfully
- **All Modules**: ✅ Compile successfully
- **Minor Warnings**: Connection string outputs (expected for Key Vault integration)
- **No Errors**: All syntax and dependency issues resolved

## 🔧 **Pre-Deployment Checklist**

### 1. Azure CLI Authentication

```bash
# Verify current subscription
az account show --output table

# Check user permissions
az role assignment list --assignee $(az rest --method GET --url "https://graph.microsoft.com/v1.0/me" --query id --output tsv) --output table
```

### 2. Resource Group and Permissions

Since the template uses subscription-level deployment, it will automatically create the resource group.

**Required Permissions:**

- **Contributor** role on subscription (for resource creation)
- **User Access Administrator** role (for RBAC assignments)

### 3. Parameter File Configuration

✅ **Updated**: `main.parameters.dev.json` configured with:

- Environment: `dev`
- SQL Admin Credentials: `sqladmin` / `P@ssw0rd123!`
- Key Vault Access Principal: Current user ID
- Azure Regions: `eastus2` (primary), `westus2` (secondary)

## 🚀 **Deployment Commands**

### Template Validation

```bash
# Navigate to infrastructure directory
cd c:\git\blogcode\Zeus.People\infra

# Validate Bicep syntax
az bicep build --file main.bicep

# Validate deployment (what-if)
az deployment sub what-if \
  --location eastus2 \
  --template-file main.bicep \
  --parameters main.parameters.dev.json
```

### Development Deployment

```bash
# Deploy to development (subscription-level)
az deployment sub create \
  --name "zeus-people-dev-$(date +%Y%m%d-%H%M%S)" \
  --location eastus2 \
  --template-file main.bicep \
  --parameters main.parameters.dev.json
```

### Alternative: Resource Group Deployment

If subscription-level deployment permissions are not available:

```bash
# Create resource group manually
az group create --name rg-academic-dev --location eastus2

# Deploy to existing resource group
az deployment group create \
  --resource-group rg-academic-dev \
  --template-file main.bicep \
  --parameters main.parameters.dev.json
```

## 🧪 **Post-Deployment Testing**

### 1. Verify Resource Creation

```bash
# List all resources in the resource group
az resource list --resource-group rg-academic-dev --output table

# Check App Service status
az webapp show --name app-academic-dev-[token] --resource-group rg-academic-dev --query "state" --output tsv

# Verify SQL Database
az sql db list --server sql-academic-dev-[token] --resource-group rg-academic-dev --output table

# Check Cosmos DB
az cosmosdb show --name cosmos-academic-dev-[token] --resource-group rg-academic-dev --query "provisioningState" --output tsv
```

### 2. Test Database Connectivity

```bash
# Test SQL Database connection
az sql db show-connection-string --client ado.net --server sql-academic-dev-[token] --name Zeus.People

# Test Cosmos DB connection
az cosmosdb keys list --name cosmos-academic-dev-[token] --resource-group rg-academic-dev --type connection-strings
```

### 3. Verify Managed Identity Configuration

```bash
# Check managed identity
az identity show --name mi-academic-dev-[token] --resource-group rg-academic-dev --output table

# Verify Key Vault access
az keyvault show --name kv-academic-[token8]-dev --resource-group rg-academic-dev --query "properties.accessPolicies" --output json
```

### 4. Test Key Vault Access and Secrets

```bash
# List Key Vault secrets
az keyvault secret list --vault-name kv-academic-[token8]-dev --output table

# Test secret retrieval
az keyvault secret show --name DefaultConnection --vault-name kv-academic-[token8]-dev --query "value" --output tsv
```

### 5. Application Insights and Monitoring

```bash
# Check Application Insights
az monitor app-insights component show --app ai-academic-dev-[token] --resource-group rg-academic-dev --query "provisioningState" --output tsv

# Verify Log Analytics workspace
az monitor log-analytics workspace show --workspace-name law-academic-dev-[token] --resource-group rg-academic-dev --query "provisioningState" --output tsv
```

## 🔒 **Security Validation**

### Managed Identity Permissions

```bash
# Verify RBAC assignments for managed identity
az role assignment list --assignee $(az identity show --name mi-academic-dev-[token] --resource-group rg-academic-dev --query principalId --output tsv) --output table
```

### Network Security

```bash
# Check App Service network restrictions
az webapp config access-restriction show --name app-academic-dev-[token] --resource-group rg-academic-dev

# Verify HTTPS-only configuration
az webapp show --name app-academic-dev-[token] --resource-group rg-academic-dev --query "httpsOnly" --output tsv
```

## 📊 **Expected Resources**

The deployment will create approximately **20+ Azure resources**:

### Core Infrastructure

- Resource Group: `rg-academic-dev-eastus2`
- Log Analytics Workspace: `law-academic-dev-[token]`
- Application Insights: `ai-academic-dev-[token]`
- Managed Identity: `mi-academic-dev-[token]`

### Security & Secrets

- Key Vault: `kv-academic-[8chars]-dev`
- Key Vault Secrets: 5 connection strings
- RBAC Role Assignments: Multiple for managed identity

### Data Services

- SQL Server: `sql-academic-dev-[token]`
- SQL Databases: `Zeus.People`, `Zeus.People.EventStore`
- Cosmos DB Account: `cosmos-academic-dev-[token]`
- Cosmos DB Database & Containers: 4 containers

### Messaging & Compute

- Service Bus Namespace: `sb-academic-dev-[token]`
- Service Bus Topic & Subscription
- App Service Plan: `asp-academic-dev-[token]`
- App Service: `app-academic-dev-[token]`

## ⚠️ **Common Issues and Solutions**

### Permission Issues

- **Error**: Authorization failed for deployment actions
- **Solution**: Ensure user has Contributor + User Access Administrator roles

### Key Vault Access

- **Error**: Cannot access Key Vault secrets
- **Solution**: Verify managed identity has Key Vault Secrets User role

### SQL Database Connection

- **Error**: Cannot connect to SQL Database
- **Solution**: Ensure managed identity is configured as Azure AD admin

### Resource Naming Conflicts

- **Error**: Resource names already exist
- **Solution**: Resource token should ensure uniqueness; check for existing resources

## 💡 **Next Steps After Successful Deployment**

1. **Application Configuration**: Update app settings with Key Vault references
2. **Database Schema**: Deploy Entity Framework migrations
3. **Monitoring Setup**: Configure alerts and dashboards
4. **CI/CD Pipeline**: Set up automated deployments
5. **Security Review**: Validate all security configurations

---

**Note**: Replace `[token]` with the actual unique resource token generated during deployment. The token is created using `uniqueString(subscription().subscriptionId, environmentName)`.
