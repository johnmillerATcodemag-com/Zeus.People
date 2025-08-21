# Configuration and Secrets Management

This document explains how to properly configure secrets and sensitive data for the Zeus.People Academic Management System.

## 🚨 Security Notice

**NEVER commit secrets, passwords, connection strings, or API keys to version control!**

All sensitive configuration values have been replaced with placeholder values in the configuration files. You must provide actual values through secure channels.

## Configuration Files

### Development Environment (`appsettings.Development.json`)

The following placeholders need to be replaced with actual values:

- `REPLACE_WITH_SECURE_KEY_FROM_ENVIRONMENT_OR_KEY_VAULT` - JWT Secret Key
- `REPLACE_WITH_ACTUAL_TENANT_ID` - Azure AD Tenant ID  
- `REPLACE_WITH_ACTUAL_CLIENT_ID` - Azure AD Client ID
- `REPLACE_WITH_ACTUAL_CLIENT_SECRET` - Azure AD Client Secret

### Staging/Production Environment (`appsettings.Staging.Azure.json`)

This file uses empty strings for sensitive values. These should be populated via:

1. **Azure Key Vault** (recommended for production)
2. **Environment Variables** 
3. **Azure App Service Configuration**

## How to Configure Secrets

### Option 1: Environment Variables (Development)

Set these environment variables:

```bash
# JWT Settings
JWT_SECRET_KEY=your-secure-jwt-key-here

# Azure AD
AZURE_AD_TENANT_ID=your-tenant-id
AZURE_AD_CLIENT_ID=your-client-id  
AZURE_AD_CLIENT_SECRET=your-client-secret

# Database Connections
DATABASE_CONNECTION_STRING=your-database-connection
EVENT_STORE_CONNECTION_STRING=your-eventstore-connection

# Service Bus
SERVICE_BUS_CONNECTION_STRING=your-servicebus-connection

# Application Insights
APPLICATION_INSIGHTS_CONNECTION_STRING=your-app-insights-connection
APPLICATION_INSIGHTS_INSTRUMENTATION_KEY=your-instrumentation-key
```

### Option 2: Azure Key Vault (Production)

1. Store secrets in Azure Key Vault
2. Configure the application to read from Key Vault using Managed Identity
3. Use the existing `KeyVaultSettings` configuration

### Option 3: Azure App Service Configuration

For Azure App Service deployments:

1. Go to Azure Portal → App Service → Configuration
2. Add Application Settings for each secret
3. Use the naming convention that matches your configuration structure

## Security Best Practices

1. ✅ Use Azure Key Vault for production secrets
2. ✅ Use Managed Identity for authentication  
3. ✅ Rotate secrets regularly
4. ✅ Use different secrets for each environment
5. ✅ Monitor secret access and usage
6. ❌ Never commit secrets to git
7. ❌ Never hardcode secrets in configuration files
8. ❌ Never share secrets in plain text

## Configuration Validation

Use the provided PowerShell scripts to validate your configuration:

```powershell
# Test application configuration
.\test-app-config.ps1

# Test Azure-specific configuration  
.\test-azure-config.ps1

# Validate monitoring configuration
.\test-monitoring-validation.ps1
```

## Git Security

The `.gitignore` file has been updated to prevent accidental commit of:

- `appsettings.Production.json`
- `appsettings.*.Local.json` 
- `appsettings.Secrets.json`
- `.env` files
- Certificate files (`.pfx`, `.p12`, `.key`, `.pem`)
- Azure credential files

## Emergency Response

If secrets are accidentally committed:

1. **Immediately rotate all exposed secrets**
2. **Remove from git history using `git filter-branch` or BFG Repo-Cleaner**
3. **Force push the cleaned history**
4. **Notify all team members**
5. **Update security monitoring**

## Contact

For questions about secrets management or security concerns, contact the development team.

---
**Remember: Security is everyone's responsibility! 🔐**
