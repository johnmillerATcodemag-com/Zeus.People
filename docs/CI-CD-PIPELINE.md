# Zeus.People CI/CD Pipeline Documentation

This repository contains a comprehensive CI/CD pipeline setup for the Zeus.People Academic Management System, implementing modern DevOps practices with Azure integration.

## 🚀 Pipeline Overview

Our CI/CD pipeline provides automated build, test, security scanning, and deployment capabilities across multiple environments with comprehensive monitoring and rollback procedures.

### Key Features

- ✅ **Automated Build & Test** - Multi-stage testing with coverage reporting
- ✅ **Security Scanning** - SAST, dependency scanning, container security
- ✅ **Blue-Green Deployment** - Zero-downtime production deployments
- ✅ **Infrastructure as Code** - Bicep templates with validation
- ✅ **Monitoring & Alerting** - Application Insights integration
- ✅ **Emergency Rollback** - Quick rollback procedures
- ✅ **Database Migrations** - Automated schema updates

## 📋 Pipeline Components

### 1. Main CI/CD Pipeline (`ci-cd-pipeline.yml`)

**Triggers:** Push to main/develop, Pull Requests, Manual dispatch

**Stages:**

1. **Build & Validate** - Code compilation and basic validation
2. **Unit & Integration Tests** - Comprehensive test execution
3. **Code Quality & Security** - CodeQL analysis and vulnerability scanning
4. **Package Application** - Build deployment artifacts
5. **Deploy to Staging** - Automated staging deployment
6. **End-to-End Tests** - Automated E2E testing against staging
7. **Deploy to Production** - Blue-green production deployment (manual approval)
8. **Cleanup & Notifications** - Post-deployment cleanup and notifications

### 2. Infrastructure Validation (`infrastructure-validation.yml`)

**Triggers:** Changes to `infra/` folder or `azure.yaml`

**Features:**

- Bicep template validation
- What-if deployment analysis
- Security scanning with Checkov
- Resource naming convention validation

### 3. Comprehensive Testing (`comprehensive-testing.yml`)

**Test Types:**

- **Unit Tests** - Domain and Application layer tests
- **Integration Tests** - Infrastructure and API tests with Cosmos DB emulator
- **Performance Tests** - Load testing with Artillery
- **Security Tests** - Dependency and code security analysis

### 4. Security Scanning (`security-scanning.yml`)

**Security Checks:**

- **SAST** - Static Application Security Testing with CodeQL
- **Dependency Scanning** - NuGet package vulnerability analysis
- **Container Security** - Trivy container image scanning
- **Infrastructure Security** - Checkov Bicep template analysis
- **Secrets Scanning** - GitLeaks and custom pattern detection

### 5. Database Migration (`database-migration.yml`)

**Migration Types:**

- **Update** - Apply latest migrations
- **Rollback** - Revert to previous migration
- **Seed** - Initialize with test data

### 6. Monitoring Setup (`monitoring.yml`)

**Monitoring Components:**

- Application Insights alerts
- Availability tests
- Performance baseline verification
- Security validation
- SSL/TLS certificate checks

### 7. Emergency Rollback (`emergency-rollback.yml`)

**Rollback Options:**

- **Slot Swap** - Quick production/staging slot swap
- **Previous Deployment** - Rollback to previous version
- **Specific Version** - Target specific version rollback

## 🏗️ Infrastructure as Code

### Azure Resources Deployed

- **App Service** - Web application hosting
- **Cosmos DB** - Document database
- **Key Vault** - Secrets management
- **Application Insights** - Application monitoring
- **Log Analytics** - Centralized logging
- **Storage Account** - Blob storage for artifacts

### Environment Configuration

```yaml
# Staging Environment
- Location: East US 2
- App Service Plan: B1 (Basic)
- Cosmos DB: 400 RU/s
- Deployment Slots: 1

# Production Environment
- Location: East US 2 (Primary), West US 2 (Secondary)
- App Service Plan: P1v3 (Premium)
- Cosmos DB: 1000 RU/s
- Deployment Slots: 2 (Blue-Green)
- Auto-scaling: Enabled
- Backup: Daily retention (30 days)
```

## 🔧 Setup Instructions

### Prerequisites

1. **Azure Subscription** with appropriate permissions
2. **GitHub Repository** with Actions enabled
3. **Azure Service Principal** for authentication

### 1. Azure Service Principal Setup

```bash
# Create service principal
az ad sp create-for-rbac \
  --name "zeus-people-cicd" \
  --role "Contributor" \
  --scopes "/subscriptions/{subscription-id}" \
  --sdk-auth

# Note the output for GitHub secrets
```

### 2. GitHub Secrets Configuration

Add the following secrets to your GitHub repository:

```
AZURE_CLIENT_ID          # Service Principal Client ID
AZURE_CLIENT_SECRET      # Service Principal Client Secret
AZURE_TENANT_ID          # Azure Tenant ID
AZURE_SUBSCRIPTION_ID    # Azure Subscription ID
ALERT_EMAIL              # Email for monitoring alerts
TEAMS_WEBHOOK_URL        # Teams webhook for notifications
```

### 3. Environment Setup

1. **Fork/Clone** this repository
2. **Configure** environment-specific settings in `.github/config/deployment-config.yml`
3. **Update** Azure resource names in `azure.yaml` and Bicep templates
4. **Test** pipeline by pushing to develop branch

### 4. Initial Deployment

```bash
# Initialize AZD project
azd init

# Set environment variables
azd env set AZURE_ENV_NAME zeus-people-dev
azd env set AZURE_LOCATION eastus2
azd env set AZURE_SUBSCRIPTION_ID {your-subscription-id}

# Deploy infrastructure and application
azd up
```

## 🔄 Deployment Process

### Staging Deployment (Automatic)

1. **Trigger:** Push to `main` or `develop` branch
2. **Process:** Build → Test → Security Scan → Deploy → E2E Tests
3. **URL:** `https://app-zeus-people-staging.azurewebsites.net`

### Production Deployment (Manual Approval)

1. **Trigger:** Successful staging deployment
2. **Approval:** Required before production deployment
3. **Strategy:** Blue-Green deployment with slot swapping
4. **URL:** `https://app-zeus-people-prod.azurewebsites.net`

### Emergency Rollback

1. **Access:** GitHub Actions → Emergency Rollback workflow
2. **Options:** Slot swap, previous deployment, or specific version
3. **Verification:** Automated health checks and performance validation

## 📊 Monitoring & Alerting

### Application Insights Metrics

- **Response Time** - Alert if > 2 seconds (staging) / 1 second (production)
- **Error Rate** - Alert if > 5% (staging) / 1% (production)
- **Availability** - Alert if < 99% (staging) / 99.9% (production)

### Availability Tests

- **Health Endpoint** - Every 5 minutes from multiple regions
- **API Endpoints** - Functional testing every 15 minutes
- **Performance Baseline** - Response time monitoring

### Alert Channels

- **Email** - Critical alerts to operations team
- **Teams** - Real-time notifications to development channel
- **Dashboard** - Azure portal monitoring dashboard

## 🧪 Testing Strategy

### Test Pyramid

```
    E2E Tests (10%)
   ─────────────────
  Integration Tests (20%)
 ─────────────────────────
Unit Tests (70%)
```

### Test Execution

- **Unit Tests** - Run on every commit
- **Integration Tests** - Run on PR and main branch
- **E2E Tests** - Run after staging deployment
- **Performance Tests** - Run on schedule and before production
- **Security Tests** - Run on every commit and daily schedule

### Coverage Requirements

- **Minimum Coverage** - 80% overall
- **Domain Layer** - 90% coverage required
- **Application Layer** - 85% coverage required
- **API Layer** - 80% coverage required

## 🔒 Security Best Practices

### Implemented Security Measures

1. **Secret Management** - All secrets stored in Azure Key Vault
2. **Least Privilege** - Minimal required permissions for service principals
3. **Network Security** - Private endpoints and VNet integration
4. **Data Encryption** - Encryption at rest and in transit
5. **Regular Scanning** - Automated vulnerability assessment
6. **Dependency Management** - Automated dependency updates

### Security Scanning Schedule

- **SAST** - Every commit
- **Dependency Scan** - Daily
- **Container Scan** - Every build
- **Infrastructure Scan** - Every infrastructure change
- **Secrets Scan** - Every commit

## 🚨 Troubleshooting

### Common Issues

#### Build Failures

```bash
# Check build logs
gh run list --limit 5
gh run view {run-id}

# Local build test
dotnet build Zeus.People.sln --configuration Release
```

#### Test Failures

```bash
# Run tests locally
dotnet test --configuration Release --logger "console;verbosity=detailed"

# Check test results in pipeline artifacts
```

#### Deployment Issues

```bash
# Check Azure resources
az group list --query "[?contains(name, 'zeus-people')]"
az webapp show --name app-zeus-people-staging --resource-group rg-zeus-people-staging

# Validate Bicep templates
az bicep build --file infra/main.bicep
az deployment sub validate --location eastus2 --template-file infra/main.bicep
```

#### Performance Issues

```bash
# Check Application Insights
az monitor app-insights query --app {app-insights-name} --analytics-query "requests | summarize avg(duration) by bin(timestamp, 5m)"

# Local performance test
artillery run .github/config/artillery-config.yml
```

### Emergency Contacts

- **DevOps Team** - devops@zeuspeople.com
- **On-Call Engineer** - Available 24/7 via Teams
- **Security Team** - security@zeuspeople.com

## 📈 Metrics & KPIs

### Deployment Metrics

- **Deployment Frequency** - Target: Daily to staging, Weekly to production
- **Lead Time** - Target: < 2 hours from commit to staging
- **MTTR** - Target: < 30 minutes for rollback
- **Change Failure Rate** - Target: < 5%

### Quality Metrics

- **Test Coverage** - Target: > 80%
- **Security Vulnerabilities** - Target: 0 critical, < 5 medium
- **Performance** - Target: < 2s response time
- **Availability** - Target: 99.9% uptime

## 🔄 Continuous Improvement

### Monthly Reviews

1. **Pipeline Performance** - Analyze build times and success rates
2. **Security Posture** - Review vulnerability trends
3. **Quality Metrics** - Assess test coverage and defect rates
4. **Cost Optimization** - Review Azure resource usage

### Quarterly Updates

1. **Tool Updates** - Update to latest versions of tools and actions
2. **Process Improvement** - Incorporate feedback and lessons learned
3. **Security Review** - Complete security audit
4. **Disaster Recovery Test** - Test backup and recovery procedures

---

## 📚 Additional Resources

- [Azure DevOps Documentation](https://docs.microsoft.com/en-us/azure/devops/)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Azure Bicep Documentation](https://docs.microsoft.com/en-us/azure/azure-resource-manager/bicep/)
- [Application Insights Documentation](https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview)

---

_This documentation is maintained by the Zeus.People DevOps team. For questions or contributions, please create an issue in this repository._
