# Development Setup Instructions

## Overview

Complete development environment setup for the Academic Management System CQRS application including tools, dependencies, and local development configuration.

## Prerequisites

### Required Software

- **Visual Studio 2022** (v17.8+) or **Visual Studio Code** with C# extensions
- **.NET 8.0 SDK** (latest version)
- **SQL Server 2022** or **SQL Server Express LocalDB**
- **Azure CLI** (latest version)
- **Git** (latest version)
- **Docker Desktop** (for containerized dependencies)
- **Postman** or **Thunder Client** (for API testing)

### Optional Tools

- **Azure Data Studio** (for database management)
- **Service Bus Explorer** (for message queue testing)
- **Azure Storage Explorer** (for blob storage management)
- **NuGet Package Manager CLI**

## Development Environment Setup

### 1. Clone Repository

```powershell
git clone https://github.com/your-org/Zeus.People.git
cd Zeus.People
```

### 2. Install .NET Dependencies

```powershell
# Restore NuGet packages
dotnet restore

# Install development tools
dotnet tool install --global dotnet-ef
dotnet tool install --global dotnet-user-secrets
dotnet tool install --global dotnet-aspnet-codegenerator
```

### 3. Database Setup

```powershell
# Create local database
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "CREATE DATABASE AcademicManagementDev"

# Apply migrations
dotnet ef database update --project src/Infrastructure --startup-project src/API
```

### 4. User Secrets Configuration

```powershell
# Initialize user secrets
dotnet user-secrets init --project src/API

# Set development secrets
dotnet user-secrets set "DatabaseSettings:WriteConnectionString" "Server=(localdb)\\MSSQLLocalDB;Database=AcademicManagementDev;Trusted_Connection=true;MultipleActiveResultSets=true" --project src/API
dotnet user-secrets set "DatabaseSettings:ReadConnectionString" "Server=(localdb)\\MSSQLLocalDB;Database=AcademicManagementDev;Trusted_Connection=true;MultipleActiveResultSets=true" --project src/API
dotnet user-secrets set "ServiceBusSettings:ConnectionString" "Endpoint=sb://localhost:5672;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE" --project src/API
```

## Project Structure Setup

### Solution Structure

```
Zeus.People.sln
├── src/
│   ├── Domain/
│   │   ├── Zeus.People.Domain.csproj
│   │   ├── Entities/
│   │   ├── ValueObjects/
│   │   ├── Events/
│   │   ├── Repositories/
│   │   └── Services/
│   ├── Application/
│   │   ├── Zeus.People.Application.csproj
│   │   ├── Commands/
│   │   ├── Queries/
│   │   ├── Handlers/
│   │   ├── DTOs/
│   │   └── Interfaces/
│   ├── Infrastructure/
│   │   ├── Zeus.People.Infrastructure.csproj
│   │   ├── Persistence/
│   │   ├── EventStore/
│   │   ├── Messaging/
│   │   └── Configuration/
│   └── API/
│       ├── Zeus.People.API.csproj
│       ├── Controllers/
│       ├── Middleware/
│       └── Configuration/
└── tests/
    ├── Zeus.People.Domain.Tests/
    ├── Zeus.People.Application.Tests/
    ├── Zeus.People.Infrastructure.Tests/
    └── Zeus.People.API.Tests/
```

### Create Project Files

```powershell
# Create solution
dotnet new sln -n Zeus.People

# Create projects
dotnet new classlib -n Zeus.People.Domain -o src/Domain
dotnet new classlib -n Zeus.People.Application -o src/Application
dotnet new classlib -n Zeus.People.Infrastructure -o src/Infrastructure
dotnet new webapi -n Zeus.People.API -o src/API

# Create test projects
dotnet new xunit -n Zeus.People.Domain.Tests -o tests/Zeus.People.Domain.Tests
dotnet new xunit -n Zeus.People.Application.Tests -o tests/Zeus.People.Application.Tests
dotnet new xunit -n Zeus.People.Infrastructure.Tests -o tests/Zeus.People.Infrastructure.Tests
dotnet new xunit -n Zeus.People.API.Tests -o tests/Zeus.People.API.Tests

# Add projects to solution
dotnet sln add src/Domain/Zeus.People.Domain.csproj
dotnet sln add src/Application/Zeus.People.Application.csproj
dotnet sln add src/Infrastructure/Zeus.People.Infrastructure.csproj
dotnet sln add src/API/Zeus.People.API.csproj
dotnet sln add tests/Zeus.People.Domain.Tests/Zeus.People.Domain.Tests.csproj
dotnet sln add tests/Zeus.People.Application.Tests/Zeus.People.Application.Tests.csproj
dotnet sln add tests/Zeus.People.Infrastructure.Tests/Zeus.People.Infrastructure.Tests.csproj
dotnet sln add tests/Zeus.People.API.Tests/Zeus.People.API.Tests.csproj
```

## NuGet Package Dependencies

### Domain Project Packages

```xml
<PackageReference Include="MediatR" Version="12.2.0" />
<PackageReference Include="FluentValidation" Version="11.8.1" />
<PackageReference Include="System.ComponentModel.Annotations" Version="5.0.0" />
```

### Application Project Packages

```xml
<PackageReference Include="MediatR" Version="12.2.0" />
<PackageReference Include="FluentValidation" Version="11.8.1" />
<PackageReference Include="AutoMapper" Version="12.0.1" />
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
```

### Infrastructure Project Packages

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.0" />
<PackageReference Include="Microsoft.Azure.Cosmos" Version="3.36.0" />
<PackageReference Include="Azure.Messaging.ServiceBus" Version="7.17.3" />
<PackageReference Include="Microsoft.Extensions.Configuration.AzureKeyVault" Version="6.0.1" />
<PackageReference Include="Azure.Identity" Version="1.10.4" />
```

### API Project Packages

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.0" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
<PackageReference Include="Microsoft.ApplicationInsights.AspNetCore" Version="2.21.0" />
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
<PackageReference Include="MediatR.Extensions.Microsoft.DependencyInjection" Version="11.1.0" />
```

### Test Project Packages

```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
<PackageReference Include="xunit" Version="2.6.2" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
<PackageReference Include="FluentAssertions" Version="6.12.0" />
<PackageReference Include="Moq" Version="4.20.69" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.0" />
<PackageReference Include="Testcontainers.SqlServer" Version="3.6.0" />
```

## Development Configuration

### appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "DatabaseSettings": {
    "CommandTimeoutSeconds": 30,
    "EnableSensitiveDataLogging": true,
    "EnableDetailedErrors": true
  },
  "ServiceBusSettings": {
    "MessageRetryCount": 3,
    "MessageTimeoutMinutes": 5,
    "MaxConcurrentCalls": 1
  },
  "CorsSettings": {
    "AllowedOrigins": [
      "https://localhost:3000",
      "http://localhost:3000",
      "https://localhost:5173",
      "http://localhost:5173"
    ]
  },
  "SwaggerSettings": {
    "Enabled": true,
    "RoutePrefix": "swagger"
  }
}
```

### launchSettings.json

```json
{
  "profiles": {
    "Zeus.People.API": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "https://localhost:7001;http://localhost:5001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "IIS Express": {
      "commandName": "IISExpress",
      "launchBrowser": true,
      "launchUrl": "swagger",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

## Local Development Services

### Docker Compose for Dependencies

```yaml
version: "3.8"
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=Dev123456!
    ports:
      - "1433:1433"
    volumes:
      - sqlserver_data:/var/opt/mssql

  cosmosdb:
    image: mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest
    ports:
      - "8081:8081"
      - "10251:10251"
      - "10252:10252"
      - "10253:10253"
      - "10254:10254"
    environment:
      - AZURE_COSMOS_EMULATOR_PARTITION_COUNT=10
      - AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE=true

  servicebus:
    image: mcr.microsoft.com/azure-service-bus-emulator:latest
    ports:
      - "5672:5672"
    environment:
      - ACCEPT_EULA=Y

volumes:
  sqlserver_data:
```

### Start Development Services

```powershell
# Start dependencies
docker-compose up -d

# Verify services are running
docker ps

# Check service health
curl http://localhost:8081/_explorer/index.html
```

## IDE Configuration

### Visual Studio 2022 Setup

1. Install required workloads:

   - ASP.NET and web development
   - Azure development
   - .NET desktop development

2. Install extensions:
   - Azure Service Bus Explorer
   - Entity Framework Core Power Tools
   - REST Client

### Visual Studio Code Setup

1. Install extensions:
   - C# for Visual Studio Code
   - Azure Tools
   - REST Client
   - Docker
   - SQL Server (mssql)

## Build and Run Instructions

### Build Solution

```powershell
# Clean and build
dotnet clean
dotnet build

# Run tests
dotnet test

# Run API
dotnet run --project src/API
```

### Development Workflow

1. Start Docker dependencies
2. Run database migrations
3. Start the API project
4. Open Swagger UI at https://localhost:7001/swagger
5. Test API endpoints with sample data

## Debugging Setup

### Debug Configuration

- Set breakpoints in command/query handlers
- Use Entity Framework logging for database queries
- Monitor Service Bus messages in development
- Use Application Insights for local testing

### Common Development Issues

1. **Database Connection**: Verify LocalDB is running
2. **Port Conflicts**: Check if ports 7001, 5001 are available
3. **Docker Issues**: Ensure Docker Desktop is running
4. **User Secrets**: Verify secrets are properly configured

## Code Quality Tools

### EditorConfig (.editorconfig)

```ini
root = true

[*]
charset = utf-8
insert_final_newline = true
trim_trailing_whitespace = true

[*.cs]
indent_style = space
indent_size = 4
```

### Development Scripts

Create development helper scripts in `/scripts` folder:

- `setup-dev.ps1`: Complete development setup
- `reset-database.ps1`: Reset local database
- `run-tests.ps1`: Run all tests with coverage
- `docker-start.ps1`: Start development dependencies
