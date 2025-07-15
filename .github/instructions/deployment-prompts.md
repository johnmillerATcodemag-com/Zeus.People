# Deployment Prompts for Academic Management System

## Overview

Sequential prompts for creating, provisioning, deploying, configuring, and testing the Academic Management System CQRS application.

---

## Phase 1: Project Foundation and Setup

### Prompt 1.1: Project Structure Creation

```
Create a complete .NET 8 CQRS solution structure for the Academic Management System following the instructions in `.github/instructions/development-setup.md` and `.github/instructions/cqrs-architecture.md`.

Requirements:
1. Create the solution file and all project files as specified
2. Set up proper project references between layers
3. Add all required NuGet packages for each project
4. Create the basic folder structure within each project
5. Implement the base classes and interfaces for CQRS pattern

Testing Instructions:
- Verify all projects compile successfully: `dotnet build`
- Confirm project references are correct: `dotnet list reference`
- Check that all NuGet packages are properly restored: `dotnet restore`
- Validate solution structure matches the specifications
```

### Prompt 1.2: Domain Model Implementation

```
Implement the complete domain model for the Academic Management System based on the business rules in `business-rules.md`.

Requirements:
1. Create all entity classes (Academic, Department, Room, Extension, etc.)
2. Implement all value objects (EmpNr, EmpName, Rank, etc.)
3. Create domain events for all state changes
4. Implement all business rule validations in the domain layer
5. Create aggregate roots and ensure consistency boundaries
6. Add proper domain exceptions and error handling

Focus on these key entities and their relationships:
- Academic with EmpNr, EmpName, Rank, and contract details
- Department with professors, senior lecturers, and lecturers
- Room assignments and building relationships
- Extension and access level associations
- Chair and committee assignments
- Degree and university relationships

Testing Instructions:
- Run domain unit tests: `dotnet test tests/Zeus.People.Domain.Tests/`
- Verify all business rules are enforced
- Test entity creation with valid and invalid data
- Validate value object constraints
- Confirm domain events are raised correctly
```

### Prompt 1.3: Application Layer Implementation

```
Implement the application layer with CQRS commands, queries, and handlers following the architecture in `.github/instructions/cqrs-architecture.md`.

Requirements:
1. Create commands for all write operations (Create, Update, Delete academics, departments, etc.)
2. Create queries for all read operations with specific DTOs
3. Implement command handlers with business rule enforcement
4. Implement query handlers for read models
5. Add FluentValidation for input validation
6. Implement MediatR for command/query dispatching
7. Create DTOs for API responses
8. Add proper error handling and result patterns

Key Commands to implement:
- CreateAcademicCommand, UpdateAcademicCommand, DeleteAcademicCommand
- AssignAcademicToRoomCommand, AssignAcademicToExtensionCommand
- CreateDepartmentCommand, AssignDepartmentHeadCommand
- SetAcademicTenureCommand, SetContractEndCommand

Key Queries to implement:
- GetAcademicQuery, GetAcademicsByDepartmentQuery
- GetDepartmentQuery, GetDepartmentStaffCountQuery
- GetRoomOccupancyQuery, GetExtensionAccessLevelQuery

Testing Instructions:
- Run application unit tests: `dotnet test tests/Zeus.People.Application.Tests/`
- Verify command handlers enforce business rules
- Test query handlers return correct data
- Validate input validation works properly
- Confirm error handling produces appropriate results
```

---

## Phase 2: Infrastructure and Persistence

### Prompt 2.1: Infrastructure Layer Implementation

```
Implement the infrastructure layer including Entity Framework, event store, and message bus integration following `.github/instructions/cqrs-architecture.md`.

Requirements:
1. Create Entity Framework DbContext for write operations
2. Implement repository pattern for aggregate persistence
3. Create event store for domain event persistence
4. Implement Azure Service Bus integration for event publishing
5. Create read model projections for Cosmos DB
6. Add proper database migrations
7. Implement configuration providers and dependency injection
8. Add health checks for all external dependencies

Key Infrastructure Components:
- AcademicContext (EF Core DbContext)
- EventStore implementation with versioning
- ServiceBusEventPublisher for domain events
- CosmosDbReadModelRepository for queries
- DatabaseHealthCheck, ServiceBusHealthCheck

Database Schema:
- Tables for all entities with proper relationships
- Event store table for domain events
- Proper indexes for performance
- Foreign key constraints for referential integrity

Testing Instructions:
- Run infrastructure tests: `dotnet test tests/Zeus.People.Infrastructure.Tests/`
- Verify database migrations work: `dotnet ef database update`
- Test repository implementations with in-memory database
- Confirm event store persistence and retrieval
- Validate Service Bus message publishing
- Check health checks return appropriate status
```

### Prompt 2.2: API Layer Implementation

```
Implement the Web API layer with controllers, middleware, and configuration following REST principles and OpenAPI standards.

Requirements:
1. Create controllers for all CQRS operations
2. Implement proper HTTP status codes and responses
3. Add comprehensive input validation and error handling
4. Configure OpenAPI/Swagger documentation
5. Add authentication and authorization
6. Implement proper logging and monitoring
7. Add CORS configuration for frontend integration
8. Create health check endpoints

Controllers to implement:
- AcademicsController (CRUD operations)
- DepartmentsController (department management)
- RoomsController (room assignments)
- ExtensionsController (extension management)
- ReportsController (queries and statistics)

API Features:
- RESTful endpoints with proper HTTP verbs
- Consistent error response format
- Request/response DTOs with validation
- Swagger documentation with examples
- Health check endpoint at /health

Testing Instructions:
- Run API tests: `dotnet test tests/Zeus.People.API.Tests/`
- Start the API: `dotnet run --project src/API`
- Test Swagger UI at https://localhost:7001/swagger
- Verify all endpoints return expected responses
- Test error handling with invalid inputs
- Confirm authentication works (if implemented)
- Check health endpoint responds correctly
```

---

## Phase 3: Azure Infrastructure Provisioning

### Prompt 3.1: Azure Bicep Templates Creation

```
Create comprehensive Azure Bicep templates for provisioning all required Azure resources following `.github/instructions/azure-infrastructure.md`.

Requirements:
1. Create main.bicep with all required Azure resources
2. Create separate modules for each service (database, service bus, app service, etc.)
3. Add main.parameters.json files for each environment (dev, staging, prod)
4. Implement proper naming conventions and tagging
5. Configure security settings and access policies
6. Add outputs for connection strings and endpoints
7. Include monitoring and alerting configuration

Resources to provision:
- Resource Group with proper tags
- Azure SQL Database with elastic pool
- Azure Cosmos DB with SQL API
- Azure Service Bus with premium tier
- Azure App Service with managed identity
- Azure Key Vault with access policies
- Application Insights for monitoring
- Log Analytics workspace

Security Configuration:
- Managed Identity for App Service
- Private endpoints for databases
- Key Vault access policies
- Network security groups
- Azure AD B2C integration

Testing Instructions:
- Validate Bicep templates: `az bicep build --file main.bicep`
- Deploy to development: `az deployment group create --resource-group rg-academic-dev --template-file main.bicep --parameters main.parameters.dev.json`
- Verify all resources are created successfully
- Test connectivity to databases and services
- Confirm managed identity has proper permissions
- Validate Key Vault access and secret storage
```

### Prompt 3.2: Configuration and Secrets Management

```
Implement comprehensive configuration management and secrets handling using Azure Key Vault following `.github/instructions/configuration-management.md`.

Requirements:
1. Configure Key Vault secrets for all environments
2. Update application configuration to use Key Vault
3. Implement proper configuration validation
4. Add health checks for configuration dependencies
5. Create deployment scripts for secret management
6. Configure managed identity access to Key Vault
7. Add configuration documentation and troubleshooting guides

Key Vault Secrets to configure:
- Database connection strings (write and read)
- Service Bus connection string
- Azure AD B2C configuration
- Application Insights instrumentation key
- JWT signing keys
- External service API keys

Configuration Classes:
- DatabaseConfiguration with validation
- ServiceBusConfiguration with timeouts
- AzureAdConfiguration with authentication
- ApplicationConfiguration with feature flags

Testing Instructions:
- Deploy configuration to Azure: Use deployment scripts
- Test application startup with Azure configuration
- Verify Key Vault access works with managed identity
- Confirm all secrets are properly retrieved
- Test configuration validation catches invalid values
- Check health checks report configuration status correctly
```

---

## Phase 4: Deployment and DevOps

### Prompt 4.1: CI/CD Pipeline Creation

```
Create comprehensive CI/CD pipelines for automated building, testing, and deployment using Azure DevOps or GitHub Actions.

Requirements:
1. Create build pipeline for continuous integration
2. Implement automated testing (unit, integration, E2E)
3. Add code quality gates and security scanning
4. Create deployment pipeline for multiple environments
5. Implement database migration automation
6. Add monitoring and alerting for deployments
7. Configure blue-green deployment strategy
8. Add rollback procedures and disaster recovery

Pipeline Stages:
- Build and compile all projects
- Run unit tests with coverage reporting
- Run integration tests with test databases
- Perform security and dependency scanning
- Build and push container images (if applicable)
- Deploy to staging environment
- Run E2E tests against staging
- Deploy to production with blue-green strategy

Infrastructure as Code:
- Deploy Bicep templates as part of pipeline
- Update Key Vault secrets securely
- Run database migrations automatically
- Configure monitoring and alerting

Testing Instructions:
- Trigger pipeline with code commit
- Verify all build stages complete successfully
- Confirm tests run and pass in pipeline
- Test deployment to staging environment
- Validate E2E tests pass against deployed application
- Test rollback procedures work correctly
- Monitor deployment metrics and logs
```

### Prompt 4.2: Monitoring and Observability Setup

```
Implement comprehensive monitoring, logging, and observability for the Academic Management System using Application Insights and Azure Monitor.

Requirements:
1. Configure Application Insights telemetry
2. Implement structured logging with Serilog
3. Add custom metrics for business events
4. Create dashboards for system health
5. Set up alerting for critical issues
6. Implement distributed tracing
7. Add performance monitoring
8. Create runbooks for incident response

Monitoring Components:
- Application Insights for application telemetry
- Log Analytics for centralized logging
- Azure Monitor for infrastructure metrics
- Custom dashboards for business metrics
- Alert rules for SLA violations
- Performance counters for optimization

Key Metrics to Monitor:
- API response times and error rates
- Database performance and connection health
- Service Bus message processing rates
- Business rule violation counts
- User authentication and authorization
- System resource utilization

Alerting Rules:
- High error rate (>5% in 5 minutes)
- Slow response times (>2 seconds 95th percentile)
- Database connection failures
- Service Bus message backlog
- Authentication failures
- Infrastructure resource issues

Testing Instructions:
- Deploy monitoring configuration to Azure
- Generate test traffic to validate telemetry
- Verify custom metrics appear in dashboards
- Test alert rules trigger correctly
- Confirm logs are structured and searchable
- Validate distributed tracing works across services
- Test incident response procedures
```

---

## Phase 5: Testing and Validation

### Prompt 5.1: Comprehensive Testing Implementation

```
Implement comprehensive testing suite following `.github/instructions/testing-strategy.md` including unit, integration, and E2E tests.

Requirements:
1. Create complete unit test coverage for domain logic
2. Implement integration tests for all handlers
3. Add E2E tests for critical business scenarios
4. Create performance and load tests
5. Implement test data management
6. Add contract testing for APIs
7. Create automated test reporting
8. Implement test environment management

Test Categories:
- Unit Tests: Domain entities, value objects, business rules
- Integration Tests: Command/query handlers, repositories
- E2E Tests: Complete workflows via API
- Performance Tests: Load testing with realistic data
- Contract Tests: API endpoint contracts
- Security Tests: Authentication and authorization

Key Test Scenarios:
- Academic lifecycle (create, update, tenure, contract)
- Department management and staff assignments
- Room and extension assignments
- Business rule enforcement
- Error handling and edge cases
- Concurrent operations and data consistency

Testing Infrastructure:
- Test containers for database testing
- In-memory databases for unit tests
- Test data builders and factories
- Mock services for external dependencies
- Load testing tools and scripts

Testing Instructions:
- Run all tests: `dotnet test --collect:"XPlat Code Coverage"`
- Execute load tests: Run performance test suite
- Verify test coverage meets requirements (>80%)
- Test all business rules are enforced
- Confirm error scenarios are handled properly
- Validate concurrent operations work correctly
- Generate test reports and coverage analysis
```

### Prompt 5.2: End-to-End Validation and Go-Live

```
Perform comprehensive end-to-end validation of the complete Academic Management System before production go-live.

Requirements:
1. Execute full system validation tests
2. Perform user acceptance testing scenarios
3. Validate all business rules work correctly
4. Test system performance under load
5. Verify security and compliance requirements
6. Conduct disaster recovery testing
7. Validate monitoring and alerting
8. Perform production readiness review

Validation Scenarios:
- Complete academic onboarding workflow
- Department staff management operations
- Room and resource assignment processes
- Tenure and contract management
- Reporting and query operations
- System administration functions

Production Readiness Checklist:
□ All business rules implemented and tested
□ Performance requirements met
□ Security controls in place
□ Monitoring and alerting configured
□ Backup and recovery procedures tested
□ Documentation complete and current
□ Support procedures established
□ User training completed

Go-Live Activities:
1. Final deployment to production
2. Data migration (if applicable)
3. DNS and routing configuration
4. Monitoring validation
5. Smoke tests execution
6. User communication and training
7. Support team activation
8. Go/no-go decision and sign-off

Post Go-Live Monitoring:
- Monitor system performance for 48 hours
- Track error rates and response times
- Validate user activity and adoption
- Monitor infrastructure resources
- Collect user feedback
- Address any immediate issues

Testing Instructions:
- Execute complete validation test suite
- Perform load testing with production-like data
- Test all critical business scenarios
- Validate monitoring and alerting systems
- Confirm backup and recovery procedures
- Test incident response procedures
- Verify system meets all acceptance criteria
- Document any issues and resolutions
```

---

## Summary

These prompts provide a comprehensive guide for creating, deploying, and validating the Academic Management System. Each prompt includes:

1. **Clear Requirements**: Specific deliverables and acceptance criteria
2. **Testing Instructions**: How to validate each phase is completed correctly
3. **Dependencies**: What must be completed before starting each phase
4. **Success Criteria**: How to determine if the phase is successful

Execute these prompts in sequence, ensuring each phase is fully tested and validated before proceeding to the next phase.
