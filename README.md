# Zeus.People

This repo contains the files that accompany the 2025 NextGen/AI session: AI Assisted Software Development

## Domain

The domain is a People Management System, which includes features for managing employees, departments, and roles.

The model is taken from the ORM white paper, which can be found here: https://orm.net/pdf/ORMwhitePaper.pdf

![Object Role Model diagram showing an academic system with Academic as the central entity connected to various components including Building, Room, Extension, Rank, Teacher, Professor, Department, and other academic entities. The diagram uses standard ER notation with rectangles for entities, diamonds for relationships, and ovals for attributes. Key relationships include teaches connecting Academic to Subject, occupies linking Academic to Room, and hierarchical connections between Teacher and Professor entities. The diagram includes cardinality notations and constraint symbols throughout, representing a comprehensive university personnel and facility management system with interconnected academic roles, physical spaces, and organizational structures.](Academic-Model.png)

## 🚀 CI/CD Pipeline & Deployment

This project includes a comprehensive CI/CD pipeline with automated deployment to Azure staging environment.

### Pipeline Features

- **Automated Testing**: 293 unit tests with comprehensive coverage
- **Security Scanning**: CodeQL analysis and vulnerability scanning  
- **Infrastructure Deployment**: Automated Azure resource provisioning with Bicep
- **Application Deployment**: Azure App Service deployment with configuration management
- **Monitoring Setup**: Comprehensive alert rules and monitoring dashboard deployment

### Monitoring & Alerts

The application includes sophisticated monitoring with:

- **Performance Monitoring**: Response time, CPU, memory, and throughput metrics
- **Error Tracking**: Automated error rate monitoring and alerting  
- **Database Monitoring**: SQL Database DTU consumption and performance alerts
- **Availability Monitoring**: Application availability and health check monitoring
- **Auto-scaling**: Automatic scaling based on CPU and memory usage

#### Alert Rules Deployed

1. **High Error Rate Alert** - Triggers when error rate > 5% for 5 minutes
2. **High Response Time Alert** - Triggers when response time > 5 seconds for 5 minutes  
3. **High CPU Usage Alert** - Triggers when CPU > 80% for 15 minutes
4. **High Memory Usage Alert** - Triggers when memory > 85% for 15 minutes
5. **Database High DTU Alert** - Triggers when DTU > 80% for 10 minutes
6. **Application Availability Alert** - Triggers when availability < 99% for 5 minutes

### Quick Start

#### Prerequisites
- Azure CLI installed and authenticated
- .NET 8.0 SDK
- PowerShell (for scripts)

#### Deploy Monitoring (Optional)
```powershell
# Deploy comprehensive alert rules to Azure
.\scripts\deploy-monitoring.ps1 -ResourceGroupName "your-rg" -AlertEmailAddress "your-email@company.com"

# Validate monitoring setup
.\scripts\validate-monitoring.ps1 -ResourceGroupName "your-rg"
```

#### Run End-to-End Tests
```powershell
# Test deployed application comprehensively  
.\scripts\end-to-end-validation.ps1 -AppUrl "https://your-app.azurewebsites.net"
```

### GitHub Secrets Required

The CI/CD pipeline requires these secrets to be configured:

- `AZURE_CLIENT_ID` - Service Principal Application ID
- `AZURE_CLIENT_SECRET` - Service Principal Password/Secret
- `AZURE_TENANT_ID` - Azure Active Directory Tenant ID  
- `AZURE_SUBSCRIPTION_ID` - Target Azure Subscription ID
- `MANAGED_IDENTITY_CLIENT_ID` - User-Assigned Managed Identity Client ID
- `APP_INSIGHTS_CONNECTION_STRING` - Application Insights connection string
- `ALERT_EMAIL_ADDRESS` - Email address for monitoring alerts

See [GITHUB_ENVIRONMENTS_SECRETS_GUIDE.md](GITHUB_ENVIRONMENTS_SECRETS_GUIDE.md) for detailed setup instructions.

### Documentation

- [📊 Monitoring Strategy](MONITORING.md) - Detailed monitoring documentation
- [🧪 Testing Strategy](TESTING_STRATEGY.md) - Comprehensive testing framework
- [🏗️ Deployment Guide](DEPLOYMENT_GUIDE.md) - Infrastructure and deployment guide
- [🔧 Configuration](GITHUB_ENVIRONMENTS_SECRETS_GUIDE.md) - GitHub secrets and environment setup
