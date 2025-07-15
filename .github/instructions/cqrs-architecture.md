# CQRS Architecture Instructions for Academic Management System

## Overview

Create a CQRS (Command Query Responsibility Segregation) application architecture for the Academic Management System that separates read and write operations while maintaining strict adherence to the business rules defined in `business-rules.md`.

## Architecture Principles

### Command Side (Write)

- **Commands**: Handle all write operations (Create, Update, Delete)
- **Command Handlers**: Process commands and enforce business rules
- **Domain Models**: Rich domain objects that encapsulate business logic
- **Event Store**: Store domain events for auditability and reconstruction
- **Write Database**: Optimized for transactional operations

### Query Side (Read)

- **Queries**: Handle all read operations with specific DTOs
- **Query Handlers**: Process queries against read models
- **Read Models**: Denormalized views optimized for specific queries
- **Read Database**: Optimized for query performance
- **Projections**: Transform domain events into read models

## Technology Stack

- **Framework**: .NET 8 with C#
- **Database**:
  - Write Side: Azure SQL Database for transactional data
  - Read Side: Azure Cosmos DB for read models
- **Message Bus**: Azure Service Bus for event publishing
- **Event Store**: Custom implementation with Azure SQL Database
- **API**: ASP.NET Core Web API with OpenAPI/Swagger
- **Authentication**: Azure AD B2C
- **Logging**: Application Insights

## Folder Structure

```
src/
├── Domain/
│   ├── Entities/
│   ├── ValueObjects/
│   ├── Events/
│   ├── Repositories/
│   └── Services/
├── Application/
│   ├── Commands/
│   ├── Queries/
│   ├── Handlers/
│   ├── DTOs/
│   └── Interfaces/
├── Infrastructure/
│   ├── Persistence/
│   ├── EventStore/
│   ├── Messaging/
│   └── Configuration/
├── API/
│   ├── Controllers/
│   ├── Middleware/
│   └── Configuration/
└── Tests/
    ├── Unit/
    ├── Integration/
    └── E2E/
```

## Key Implementation Guidelines

### Domain Events

- Create events for all state changes
- Include all necessary data for projections
- Use immutable event objects
- Version events for backward compatibility

### Business Rule Enforcement

- Implement all rules from `business-rules.md` in domain entities
- Use value objects for validation (e.g., EmpNr, RoomNr, ExtNr)
- Enforce constraints at the domain level
- Validate derived rules through event projections

### Error Handling

- Use Result patterns for command operations
- Implement comprehensive logging
- Create domain-specific exceptions
- Handle concurrency conflicts gracefully

### Performance Considerations

- Implement caching for read models
- Use async/await throughout
- Optimize database queries
- Implement proper indexing strategies

### Security

- Authenticate all API endpoints
- Authorize based on academic roles
- Protect sensitive data (contracts, phone numbers)
- Implement audit logging

## Testing Strategy

- Unit tests for domain logic and business rules
- Integration tests for command/query handlers
- End-to-end tests for complete workflows
- Performance tests for high-load scenarios
- Contract tests for API endpoints

## Compliance Requirements

- Ensure all business rules are implemented and tested
- Maintain data consistency between read and write sides
- Implement proper error handling and logging
- Follow security best practices for academic data
