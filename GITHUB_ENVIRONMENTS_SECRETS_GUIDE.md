# GitHub Environments and Required Azure Secrets

## Required Azure Secrets

The following secrets are required for Azure authentication in your workflows:

- `AZURE_CLIENT_ID` - Service Principal Application ID
- `AZURE_CLIENT_SECRET` - Service Principal Password/Secret  
- `AZURE_TENANT_ID` - Azure Active Directory Tenant ID
- `AZURE_SUBSCRIPTION_ID` - Target Azure Subscription ID

## Additional Application Secrets

The following additional secrets are required for application functionality:

- `MANAGED_IDENTITY_CLIENT_ID` - User-Assigned Managed Identity Client ID for KeyVault access
- `APP_INSIGHTS_CONNECTION_STRING` - Application Insights connection string for telemetry
- `ALERT_EMAIL_ADDRESS` - Email address for monitoring alerts and notifications (e.g., `admin@yourcompany.com`)

## GitHub Environments That Need These Secrets

### 1. **production** Environment

**Used by:**

- `ci-cd-pipeline.yml` - Production deployment job
- `ci-cd-pipeline.yml` - Rollback job (production rollbacks)

**Requirements:**

- Manual approval protection (recommended)
- All 4 Azure secrets configured
- Deploy from `main` branch only

### 2. **staging** Environment

**Used by:**

- `test-staging-deployment.yml` - Staging deployment testing
- Dynamic environment selection in various workflows

**Requirements:**

- All 4 Azure secrets configured
- Can deploy from feature branches

### 3. **Repository-Level Secrets (No Environment)**

**Used by workflows that don't specify an environment:**

- `staging-deployment.yml` - Main staging deployment workflow
- `infrastructure-validation.yml` - Bicep template validation
- `monitoring.yml` - Infrastructure monitoring
- `database-migration.yml` - Database migrations

**Note:** These workflows access secrets directly at the repository level, not through environment protection.

## Secret Configuration Locations

### Option 1: Repository-Level Secrets (Recommended for Development)

**Path:** `Settings → Secrets and variables → Actions → Repository secrets`

**Pros:**

- Simplest setup
- Works for all workflows immediately
- Good for development/testing

**Cons:**

- No environment-specific protection
- All workflows can access all secrets

### Option 2: Environment-Specific Secrets (Recommended for Production)

**Path:** `Settings → Environments → [Environment Name] → Secrets`

**Pros:**

- Environment-specific access control
- Can require manual approval for production
- Better security model

**Cons:**

- Must configure secrets in multiple places
- More complex setup

## Recommended Configuration

### Phase 1: Repository-Level (Quick Start)

1. Configure all 4 secrets at repository level
2. Test workflows execute successfully
3. Verify Azure authentication works

### Phase 2: Environment-Specific (Production Ready)

1. Create **production** environment with:

   - Manual approval required
   - Production-specific Azure credentials
   - Branch protection (main only)

2. Create **staging** environment with:

   - Staging-specific Azure credentials
   - Allow any branch deployment

3. Keep repository-level secrets for:
   - Infrastructure validation workflows
   - Monitoring workflows
   - Database migration workflows

## Workflow-to-Environment Mapping

| Workflow File                     | Environment Used               | Secrets Access      |
| --------------------------------- | ------------------------------ | ------------------- |
| `staging-deployment.yml`          | None (repo-level)              | Repository secrets  |
| `ci-cd-pipeline.yml` (staging)    | None                           | Repository secrets  |
| `ci-cd-pipeline.yml` (production) | `production`                   | Environment secrets |
| `ci-cd-pipeline.yml` (rollback)   | `production`                   | Environment secrets |
| `test-staging-deployment.yml`     | `staging`                      | Environment secrets |
| `emergency-rollback.yml`          | Dynamic (`inputs.environment`) | Environment secrets |
| `infrastructure-validation.yml`   | None                           | Repository secrets  |
| `monitoring.yml`                  | None                           | Repository secrets  |
| `database-migration.yml`          | None                           | Repository secrets  |

## Security Best Practices

1. **Production Environment:**

   - Enable required reviewers
   - Restrict to protected branches only
   - Use separate Azure service principal if possible

2. **Repository Secrets:**

   - Use least-privilege Azure service principal
   - Regularly rotate secrets
   - Monitor access logs

3. **Environment Secrets:**
   - Override repository secrets with environment-specific values
   - Use different Azure subscriptions for prod/staging if possible

## Quick Setup Commands

### Azure Service Principal Creation

```bash
az ad sp create-for-rbac --name "GitHub-Actions-Zeus-People" \
  --role contributor \
  --scopes /subscriptions/{subscription-id} \
  --sdk-auth
```

### Repository Secrets (GitHub CLI)

```bash
gh secret set AZURE_CLIENT_ID -b "{client-id}"
gh secret set AZURE_CLIENT_SECRET -b "{client-secret}"
gh secret set AZURE_TENANT_ID -b "{tenant-id}"
gh secret set AZURE_SUBSCRIPTION_ID -b "{subscription-id}"
```

### Environment Secrets (GitHub CLI)

```bash
# Production environment
gh secret set AZURE_CLIENT_ID -b "{client-id}" --env production
gh secret set AZURE_CLIENT_SECRET -b "{client-secret}" --env production
gh secret set AZURE_TENANT_ID -b "{tenant-id}" --env production
gh secret set AZURE_SUBSCRIPTION_ID -b "{subscription-id}" --env production

# Staging environment
gh secret set AZURE_CLIENT_ID -b "{client-id}" --env staging
gh secret set AZURE_CLIENT_SECRET -b "{client-secret}" --env staging
gh secret set AZURE_TENANT_ID -b "{tenant-id}" --env staging
gh secret set AZURE_SUBSCRIPTION_ID -b "{subscription-id}" --env staging
```
