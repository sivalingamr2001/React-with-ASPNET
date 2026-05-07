# API_IMPLEMENTATION.md
# Enterprise ASP.NET Core 9 Web API — Principal Architecture Implementation Guide

> **Document Classification:** Internal Engineering Reference — Principal Architecture  
> **Technology Stack:** ASP.NET Core 9 · C# 13 · EF Core 9 · SQL Server · Redis · Docker  
> **Architecture Pattern:** Clean Architecture + Vertical Slice + CQRS + DDD  
> **Authored For:** Hyperscale, Multi-Team, Cloud-Native Enterprise Environments

---

## TABLE OF CONTENTS

1. [System Architecture Overview](#1-system-architecture-overview)
2. [Complete Project Initialization](#2-complete-project-initialization)
3. [Enterprise Folder Structure](#3-enterprise-folder-structure)
4. [Clean Architecture Implementation](#4-clean-architecture-implementation)
5. [High-Performance API Design](#5-high-performance-api-design)
6. [CQRS + MediatR Architecture](#6-cqrs--mediatr-architecture)
7. [Database Architecture](#7-database-architecture)
8. [Redis Caching Strategy](#8-redis-caching-strategy)
9. [Authentication & Authorization](#9-authentication--authorization)
10. [Security Architecture](#10-security-architecture)
11. [Validation Architecture](#11-validation-architecture)
12. [Error Handling Strategy](#12-error-handling-strategy)
13. [Observability & Telemetry](#13-observability--telemetry)
14. [Performance Optimization](#14-performance-optimization)
15. [API Versioning Strategy](#15-api-versioning-strategy)
16. [Background Processing Architecture](#16-background-processing-architecture)
17. [Rate Limiting & Resilience](#17-rate-limiting--resilience)
18. [API Documentation Strategy](#18-api-documentation-strategy)
19. [Configuration Management](#19-configuration-management)
20. [Docker & Containerization](#20-docker--containerization)
21. [CI/CD Ready Foundation](#21-cicd-ready-foundation)
22. [Testing Architecture](#22-testing-architecture)
23. [Cloud-Native Readiness](#23-cloud-native-readiness)
24. [Enterprise Coding Standards](#24-enterprise-coding-standards)
25. [Final Enterprise Engineering Principles](#25-final-enterprise-engineering-principles)

---

## 1. SYSTEM ARCHITECTURE OVERVIEW

### Architectural Philosophy

This platform is engineered under a **Security-First, Performance-Optimized, Cloud-Native** philosophy. Every design decision is evaluated against three axes: **operational safety**, **throughput efficiency**, and **long-term maintainability at scale**. The architecture deliberately avoids premature abstraction while preserving all seams required for bounded context extraction into microservices.

The platform is designed to handle **tens of thousands of concurrent requests** without architectural rewrites. This means stateless request processing, async-first implementations, distributed caching at the query level, and zero shared mutable state across request pipelines.

### Clean Architecture Strategy

Clean Architecture is adopted to enforce **dependency inversion at every layer boundary**. The dependency rule is strict and mechanically enforced: outer layers depend inward, never the reverse. This produces a codebase where:

- The **Domain layer** contains enterprise business logic with zero infrastructure dependencies
- The **Application layer** orchestrates use cases using abstractions defined by the domain
- The **Infrastructure layer** implements those abstractions with concrete technology choices
- The **API layer** is a thin delivery mechanism, not a logic container

**Why Clean Architecture over a layered N-tier approach?**  
Layered architectures create implicit bidirectional coupling over time. Clean Architecture's concentric dependency rule makes this structurally impossible. When the team needs to replace EF Core with Dapper, or swap SQL Server for PostgreSQL, those changes are bounded to the Infrastructure layer only.

### Domain-Driven Design Considerations

DDD tactical patterns are applied selectively. Full DDD aggregate enforcement is reserved for core domains with complex invariants. Supporting domains use simpler CRUD-oriented patterns. The bounded context map defines:

- **Core Domain:** High-value business logic where competitive advantage lives. Full aggregate enforcement, domain events, rich entities.
- **Supporting Domain:** Value-adding non-core logic. Simplified entity models with validation enforced at the application layer.
- **Generic Subdomain:** Infrastructure concerns (emailing, file storage, notifications). Delegated to third-party services or simple services.

Value Objects are used wherever identity is not meaningful: `Money`, `Email`, `PhoneNumber`, `Address`. This prevents primitive obsession and centralizes validation logic.

### Vertical Slice Architecture Strategy

Within the Application layer, features are organized as **vertical slices** rather than horizontal layers of Commands, Queries, and Validators distributed across separate folders. Each feature slice owns its Command/Query, Handler, Validator, DTO, and mapping logic in a cohesive unit.

**Why Vertical Slices?**  
In horizontal layering, adding a new feature requires modifying 5–6 separate folders. In vertical slicing, a new feature is a self-contained directory. This dramatically reduces merge conflicts in multi-team environments, improves feature discoverability, and allows features to be individually migrated to microservices without code surgery.

The combination of Clean Architecture (for cross-cutting dependency management) and Vertical Slices (for feature cohesion) produces an architecture that is both **structurally sound** and **operationally ergonomic**.

### Scalability Philosophy

Horizontal scalability is a **first-class architectural constraint**, not an afterthought. This requires:

- All application state externalized to Redis or SQL Server — no in-process session state
- Idempotency keys on all mutating endpoints
- Distributed locking via Redis for operations requiring cross-node coordination
- Read replicas for query-heavy workloads
- Async non-blocking I/O at every I/O boundary without exception

The system is designed to scale from 1 to 1,000 nodes without application-layer changes.

### Security-First Principles

Security is not a feature layer — it is a structural property. The following principles are non-negotiable:

- **Defense in Depth:** Multiple independent security controls at each trust boundary
- **Least Privilege:** Every service identity, database connection, and API token has the minimum required permissions
- **Zero Trust:** Every request is authenticated and authorized regardless of origin
- **Secure by Default:** Insecure configurations require explicit opt-in and audit trail
- **Audit Everything:** All authentication events, authorization failures, and data mutations are immutably logged

### Cloud-Native Engineering Principles

The application follows the **Twelve-Factor App** methodology:

1. Codebase: Single source of truth in version control
2. Dependencies: Explicitly declared via NuGet and package lock files
3. Config: Environment variables and managed secrets (Azure Key Vault), never committed code
4. Backing Services: Databases, caches, and queues treated as attached resources
5. Build/Release/Run: Strictly separated stages via CI/CD pipeline
6. Processes: Stateless, share-nothing processes
7. Port Binding: Self-contained HTTP server, no external web server dependency
8. Concurrency: Scale out via process model
9. Disposability: Fast startup, graceful shutdown with `IHostApplicationLifetime`
10. Dev/Prod Parity: Docker Compose for local development mirrors production topology
11. Logs: Structured log streams via stdout/stderr to aggregation systems
12. Admin Processes: One-off tasks run as isolated processes (migrations, seed jobs)

### Performance Engineering Goals

| Metric | Target |
|---|---|
| p50 API Response Time | < 20ms |
| p95 API Response Time | < 100ms |
| p99 API Response Time | < 500ms |
| Throughput (single node) | > 10,000 req/s |
| Database Query p95 | < 50ms |
| Cache Hit Rate | > 85% for read-heavy endpoints |
| Error Rate | < 0.1% |
| Startup Time (cold) | < 3 seconds |

### Enterprise Maintainability Strategy

Maintainability at enterprise scale requires architectural enforcement mechanisms, not team discipline alone:

- **Architecture Unit Tests:** `NetArchTest` or `ArchUnitNET` enforces dependency rules as part of the CI pipeline — a domain entity importing an EF Core namespace causes a build failure
- **Feature Flags:** All new features deployed behind flags, decoupling deployment from release
- **Semantic Versioning:** API versions declared explicitly, deprecation notices published minimum 6 months in advance
- **ADR (Architecture Decision Records):** All significant decisions documented in `/docs/adr/`

---

## 2. COMPLETE PROJECT INITIALIZATION

### Solution and Project Creation

```bash
# Create solution root directory
mkdir EnterpriseApi && cd EnterpriseApi

# Initialize solution
dotnet new sln -n EnterpriseApi

# Create source projects
dotnet new classlib -n EnterpriseApi.Domain          -o src/EnterpriseApi.Domain          --framework net9.0
dotnet new classlib -n EnterpriseApi.Application     -o src/EnterpriseApi.Application     --framework net9.0
dotnet new classlib -n EnterpriseApi.Infrastructure  -o src/EnterpriseApi.Infrastructure  --framework net9.0
dotnet new classlib -n EnterpriseApi.Shared          -o src/EnterpriseApi.Shared          --framework net9.0
dotnet new webapi   -n EnterpriseApi.API             -o src/EnterpriseApi.API             --framework net9.0

# Create test projects
dotnet new xunit -n EnterpriseApi.UnitTests          -o tests/EnterpriseApi.UnitTests          --framework net9.0
dotnet new xunit -n EnterpriseApi.IntegrationTests   -o tests/EnterpriseApi.IntegrationTests   --framework net9.0
dotnet new xunit -n EnterpriseApi.ArchitectureTests  -o tests/EnterpriseApi.ArchitectureTests  --framework net9.0

# Add projects to solution
dotnet sln add src/EnterpriseApi.Domain/EnterpriseApi.Domain.csproj
dotnet sln add src/EnterpriseApi.Application/EnterpriseApi.Application.csproj
dotnet sln add src/EnterpriseApi.Infrastructure/EnterpriseApi.Infrastructure.csproj
dotnet sln add src/EnterpriseApi.Shared/EnterpriseApi.Shared.csproj
dotnet sln add src/EnterpriseApi.API/EnterpriseApi.API.csproj
dotnet sln add tests/EnterpriseApi.UnitTests/EnterpriseApi.UnitTests.csproj
dotnet sln add tests/EnterpriseApi.IntegrationTests/EnterpriseApi.IntegrationTests.csproj
dotnet sln add tests/EnterpriseApi.ArchitectureTests/EnterpriseApi.ArchitectureTests.csproj

# Wire project references (enforcing Clean Architecture dependency direction)
dotnet add src/EnterpriseApi.Application/EnterpriseApi.Application.csproj     reference src/EnterpriseApi.Domain/EnterpriseApi.Domain.csproj
dotnet add src/EnterpriseApi.Application/EnterpriseApi.Application.csproj     reference src/EnterpriseApi.Shared/EnterpriseApi.Shared.csproj
dotnet add src/EnterpriseApi.Infrastructure/EnterpriseApi.Infrastructure.csproj reference src/EnterpriseApi.Application/EnterpriseApi.Application.csproj
dotnet add src/EnterpriseApi.Infrastructure/EnterpriseApi.Infrastructure.csproj reference src/EnterpriseApi.Shared/EnterpriseApi.Shared.csproj
dotnet add src/EnterpriseApi.API/EnterpriseApi.API.csproj                      reference src/EnterpriseApi.Application/EnterpriseApi.Application.csproj
dotnet add src/EnterpriseApi.API/EnterpriseApi.API.csproj                      reference src/EnterpriseApi.Infrastructure/EnterpriseApi.Infrastructure.csproj
dotnet add src/EnterpriseApi.API/EnterpriseApi.API.csproj                      reference src/EnterpriseApi.Shared/EnterpriseApi.Shared.csproj

# Test project references
dotnet add tests/EnterpriseApi.UnitTests/EnterpriseApi.UnitTests.csproj               reference src/EnterpriseApi.Application/EnterpriseApi.Application.csproj
dotnet add tests/EnterpriseApi.UnitTests/EnterpriseApi.UnitTests.csproj               reference src/EnterpriseApi.Domain/EnterpriseApi.Domain.csproj
dotnet add tests/EnterpriseApi.IntegrationTests/EnterpriseApi.IntegrationTests.csproj reference src/EnterpriseApi.API/EnterpriseApi.API.csproj
dotnet add tests/EnterpriseApi.ArchitectureTests/EnterpriseApi.ArchitectureTests.csproj reference src/EnterpriseApi.Domain/EnterpriseApi.Domain.csproj
dotnet add tests/EnterpriseApi.ArchitectureTests/EnterpriseApi.ArchitectureTests.csproj reference src/EnterpriseApi.Application/EnterpriseApi.Application.csproj
dotnet add tests/EnterpriseApi.ArchitectureTests/EnterpriseApi.ArchitectureTests.csproj reference src/EnterpriseApi.Infrastructure/EnterpriseApi.Infrastructure.csproj
```

### Domain Layer Packages

```bash
cd src/EnterpriseApi.Domain

# No external dependencies by design — domain is pure C#
# Only optional: MediatR contracts for domain events (no runtime dependency on MediatR itself)
dotnet add package MediatR --version 12.*
```

### Application Layer Packages

```bash
cd src/EnterpriseApi.Application

dotnet add package MediatR                        --version 12.*
dotnet add package FluentValidation               --version 11.*
dotnet add package FluentValidation.DependencyInjectionExtensions --version 11.*
dotnet add package AutoMapper                     --version 13.*
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection --version 12.*
dotnet add package Microsoft.Extensions.Logging.Abstractions
dotnet add package Microsoft.Extensions.Options
```

### Infrastructure Layer Packages

```bash
cd src/EnterpriseApi.Infrastructure

dotnet add package Microsoft.EntityFrameworkCore.SqlServer    --version 9.*
dotnet add package Microsoft.EntityFrameworkCore.Tools        --version 9.*
dotnet add package Microsoft.EntityFrameworkCore.Design       --version 9.*
dotnet add package StackExchange.Redis                        --version 2.*
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis --version 9.*
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer   --version 9.*
dotnet add package Microsoft.IdentityModel.Tokens
dotnet add package System.IdentityModel.Tokens.Jwt
dotnet add package Polly                                       --version 8.*
dotnet add package Microsoft.Extensions.Http.Resilience       --version 9.*
dotnet add package Azure.Extensions.AspNetCore.Configuration.Secrets
dotnet add package Azure.Identity
dotnet add package Quartz                                      --version 3.*
dotnet add package Quartz.Extensions.Hosting                  --version 3.*
dotnet add package Quartz.Extensions.DependencyInjection      --version 3.*
```

### API Layer Packages

```bash
cd src/EnterpriseApi.API

dotnet add package Serilog.AspNetCore                         --version 8.*
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.Seq
dotnet add package Serilog.Enrichers.Environment
dotnet add package Serilog.Enrichers.Thread
dotnet add package Serilog.Enrichers.Process
dotnet add package Serilog.Enrichers.CorrelationId

dotnet add package OpenTelemetry.Extensions.Hosting           --version 1.*
dotnet add package OpenTelemetry.Instrumentation.AspNetCore   --version 1.*
dotnet add package OpenTelemetry.Instrumentation.Http         --version 1.*
dotnet add package OpenTelemetry.Instrumentation.EntityFrameworkCore --version 1.*
dotnet add package OpenTelemetry.Instrumentation.StackExchangeRedis  --version 1.*
dotnet add package OpenTelemetry.Exporter.Otlp               --version 1.*
dotnet add package OpenTelemetry.Exporter.Prometheus.AspNetCore --version 1.*

dotnet add package Swashbuckle.AspNetCore                     --version 6.*
dotnet add package Microsoft.AspNetCore.OpenApi               --version 9.*
dotnet add package Asp.Versioning.Mvc                         --version 8.*
dotnet add package Asp.Versioning.Mvc.ApiExplorer             --version 8.*

dotnet add package Microsoft.AspNetCore.RateLimiting
dotnet add package AspNetCoreRateLimit

dotnet add package Microsoft.AspNetCore.Diagnostics.HealthChecks
dotnet add package AspNetCore.HealthChecks.SqlServer
dotnet add package AspNetCore.HealthChecks.Redis
dotnet add package AspNetCore.HealthChecks.UI.Client
```

### Test Project Packages

```bash
cd tests/EnterpriseApi.UnitTests
dotnet add package xunit
dotnet add package xunit.runner.visualstudio
dotnet add package FluentAssertions           --version 6.*
dotnet add package NSubstitute               --version 5.*
dotnet add package AutoFixture               --version 4.*
dotnet add package AutoFixture.AutoNSubstitute
dotnet add package Bogus                     --version 35.*

cd tests/EnterpriseApi.IntegrationTests
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Testcontainers.SqlEdge    --version 3.*
dotnet add package Testcontainers.Redis      --version 3.*
dotnet add package FluentAssertions
dotnet add package Respawn                   --version 6.*

cd tests/EnterpriseApi.ArchitectureTests
dotnet add package NetArchTest.eNETCore      --version 1.*
dotnet add package FluentAssertions
```

### Docker Setup

```bash
# Create Docker network for local development
cat > docker-compose.yml << 'EOF'
version: '3.9'
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: "DevPassword123!"
      ACCEPT_EULA: "Y"
    ports:
      - "1433:1433"
    volumes:
      - sqldata:/var/opt/mssql
    healthcheck:
      test: ["CMD-SHELL", "/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P DevPassword123! -Q 'SELECT 1'"]
      interval: 10s
      timeout: 5s
      retries: 5

  redis:
    image: redis:7-alpine
    command: redis-server --requirepass DevRedisPassword123
    ports:
      - "6379:6379"
    volumes:
      - redisdata:/data
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s

  seq:
    image: datalust/seq:latest
    environment:
      ACCEPT_EULA: "Y"
    ports:
      - "5341:5341"
      - "8081:80"
    volumes:
      - seqdata:/data

  api:
    build:
      context: .
      dockerfile: src/EnterpriseApi.API/Dockerfile
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Server=sqlserver;Database=EnterpriseApi;User Id=sa;Password=DevPassword123!;TrustServerCertificate=True
      - ConnectionStrings__Redis=redis:6379,password=DevRedisPassword123
      - Serilog__WriteTo__1__Args__serverUrl=http://seq:5341
    depends_on:
      sqlserver:
        condition: service_healthy
      redis:
        condition: service_healthy

volumes:
  sqldata:
  redisdata:
  seqdata:
EOF
```

---

## 3. ENTERPRISE FOLDER STRUCTURE

```
EnterpriseApi/
├── .github/
│   └── workflows/
│       ├── ci.yml                          # Continuous integration pipeline
│       ├── cd-staging.yml                  # Staging deployment
│       └── cd-production.yml              # Production deployment
├── docs/
│   ├── adr/                               # Architecture Decision Records
│   │   ├── 001-clean-architecture.md
│   │   ├── 002-vertical-slices.md
│   │   └── 003-cqrs-mediatr.md
│   ├── architecture/                      # C4 model diagrams, architecture docs
│   └── runbooks/                          # Operational runbooks
├── deploy/
│   ├── k8s/                               # Kubernetes manifests
│   │   ├── deployment.yaml
│   │   ├── service.yaml
│   │   ├── ingress.yaml
│   │   ├── hpa.yaml
│   │   └── configmap.yaml
│   ├── helm/                              # Helm chart for parameterized k8s deploy
│   └── terraform/                         # IaC for cloud infrastructure
├── src/
│   ├── EnterpriseApi.Domain/              # ZERO external dependencies
│   │   ├── Common/
│   │   │   ├── Abstractions/
│   │   │   │   ├── IAggregateRoot.cs
│   │   │   │   ├── IEntity.cs
│   │   │   │   ├── IDomainEvent.cs
│   │   │   │   └── IValueObject.cs
│   │   │   ├── BaseEntity.cs              # Id, CreatedAt, UpdatedAt, domain events
│   │   │   ├── AggregateRoot.cs           # Extends BaseEntity with event dispatch
│   │   │   ├── ValueObject.cs             # Structural equality base
│   │   │   ├── DomainEvent.cs             # Base domain event record
│   │   │   └── Enumeration.cs             # Smart enum base class
│   │   ├── Entities/                      # Core domain entities per bounded context
│   │   │   └── Users/
│   │   │       ├── User.cs
│   │   │       ├── UserRole.cs
│   │   │       └── Events/
│   │   │           ├── UserCreatedEvent.cs
│   │   │           ├── UserPasswordChangedEvent.cs
│   │   │           └── UserDeactivatedEvent.cs
│   │   ├── ValueObjects/
│   │   │   ├── Email.cs
│   │   │   ├── Money.cs
│   │   │   ├── PhoneNumber.cs
│   │   │   └── Address.cs
│   │   ├── Repositories/                  # Interfaces only — no implementations
│   │   │   ├── IUserRepository.cs
│   │   │   └── IUnitOfWork.cs
│   │   ├── Services/                      # Domain service interfaces
│   │   │   └── IPasswordHashingService.cs
│   │   └── Exceptions/
│   │       ├── DomainException.cs
│   │       ├── EntityNotFoundException.cs
│   │       ├── BusinessRuleViolationException.cs
│   │       └── ConcurrencyException.cs
│   │
│   ├── EnterpriseApi.Application/         # Orchestration, use cases, CQRS
│   │   ├── Common/
│   │   │   ├── Abstractions/
│   │   │   │   ├── ICommand.cs
│   │   │   │   ├── IQuery.cs
│   │   │   │   ├── ICommandHandler.cs
│   │   │   │   └── IQueryHandler.cs
│   │   │   ├── Behaviors/                 # MediatR pipeline behaviors
│   │   │   │   ├── ValidationBehavior.cs
│   │   │   │   ├── LoggingBehavior.cs
│   │   │   │   ├── PerformanceBehavior.cs
│   │   │   │   ├── TransactionBehavior.cs
│   │   │   │   ├── CachingBehavior.cs
│   │   │   │   └── IdempotencyBehavior.cs
│   │   │   ├── Caching/
│   │   │   │   ├── ICacheService.cs
│   │   │   │   └── ICacheable.cs
│   │   │   ├── Mappings/
│   │   │   │   └── MappingProfile.cs      # AutoMapper profiles
│   │   │   └── Models/
│   │   │       ├── PagedResult.cs
│   │   │       ├── Result.cs              # Railway-oriented result type
│   │   │       └── Error.cs
│   │   ├── Features/                      # Vertical slices
│   │   │   ├── Users/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── CreateUser/
│   │   │   │   │   │   ├── CreateUserCommand.cs
│   │   │   │   │   │   ├── CreateUserCommandHandler.cs
│   │   │   │   │   │   ├── CreateUserCommandValidator.cs
│   │   │   │   │   │   └── CreateUserResponse.cs
│   │   │   │   │   ├── UpdateUser/
│   │   │   │   │   │   ├── UpdateUserCommand.cs
│   │   │   │   │   │   ├── UpdateUserCommandHandler.cs
│   │   │   │   │   │   └── UpdateUserCommandValidator.cs
│   │   │   │   │   └── DeactivateUser/
│   │   │   │   │       ├── DeactivateUserCommand.cs
│   │   │   │   │       └── DeactivateUserCommandHandler.cs
│   │   │   │   └── Queries/
│   │   │   │       ├── GetUserById/
│   │   │   │       │   ├── GetUserByIdQuery.cs
│   │   │   │       │   ├── GetUserByIdQueryHandler.cs
│   │   │   │       │   └── GetUserByIdResponse.cs
│   │   │   │       └── GetUsersPaged/
│   │   │   │           ├── GetUsersPagedQuery.cs
│   │   │   │           ├── GetUsersPagedQueryHandler.cs
│   │   │   │           └── GetUsersPagedResponse.cs
│   │   │   └── Auth/
│   │   │       ├── Commands/
│   │   │       │   ├── Login/
│   │   │       │   │   ├── LoginCommand.cs
│   │   │       │   │   ├── LoginCommandHandler.cs
│   │   │       │   │   └── LoginCommandValidator.cs
│   │   │       │   └── RefreshToken/
│   │   │       │       ├── RefreshTokenCommand.cs
│   │   │       │       └── RefreshTokenCommandHandler.cs
│   │   │       └── Queries/
│   │   ├── DomainEventHandlers/           # Application-level domain event consumers
│   │   │   └── Users/
│   │   │       ├── UserCreatedEventHandler.cs
│   │   │       └── UserDeactivatedEventHandler.cs
│   │   └── DependencyInjection.cs         # Application layer DI registration
│   │
│   ├── EnterpriseApi.Infrastructure/      # All external system implementations
│   │   ├── Persistence/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   ├── ApplicationDbContextFactory.cs
│   │   │   ├── UnitOfWork.cs
│   │   │   ├── Configurations/            # IEntityTypeConfiguration implementations
│   │   │   │   ├── UserConfiguration.cs
│   │   │   │   └── OutboxMessageConfiguration.cs
│   │   │   ├── Repositories/
│   │   │   │   └── UserRepository.cs
│   │   │   ├── Migrations/
│   │   │   ├── Interceptors/
│   │   │   │   ├── AuditInterceptor.cs
│   │   │   │   ├── SoftDeleteInterceptor.cs
│   │   │   │   └── DomainEventInterceptor.cs
│   │   │   ├── QueryObjects/              # Complex read-side query objects
│   │   │   │   └── UserQueryObject.cs
│   │   │   └── Seeding/
│   │   │       └── DatabaseSeeder.cs
│   │   ├── Caching/
│   │   │   ├── RedisCacheService.cs
│   │   │   └── CacheKeys.cs
│   │   ├── Authentication/
│   │   │   ├── JwtTokenService.cs
│   │   │   ├── RefreshTokenService.cs
│   │   │   └── TokenClaimsFactory.cs
│   │   ├── Services/
│   │   │   ├── PasswordHashingService.cs
│   │   │   ├── EmailService.cs
│   │   │   └── FileStorageService.cs
│   │   ├── Outbox/                        # Transactional outbox for domain events
│   │   │   ├── OutboxMessage.cs
│   │   │   └── OutboxProcessor.cs
│   │   ├── BackgroundJobs/
│   │   │   ├── OutboxProcessorJob.cs
│   │   │   └── TokenCleanupJob.cs
│   │   └── DependencyInjection.cs
│   │
│   ├── EnterpriseApi.Shared/              # Cross-cutting primitives — NO business logic
│   │   ├── Abstractions/
│   │   │   ├── ICurrentUserService.cs
│   │   │   ├── IDateTimeProvider.cs
│   │   │   └── ICorrelationIdProvider.cs
│   │   ├── Constants/
│   │   │   ├── ApiRoutes.cs
│   │   │   ├── CacheKeys.cs
│   │   │   ├── ClaimTypes.cs
│   │   │   └── PolicyNames.cs
│   │   └── Extensions/
│   │       ├── StringExtensions.cs
│   │       ├── EnumerableExtensions.cs
│   │       └── DateTimeExtensions.cs
│   │
│   └── EnterpriseApi.API/                 # Delivery mechanism only
│       ├── Program.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       ├── appsettings.Staging.json
│       ├── Dockerfile
│       ├── Endpoints/                     # Minimal API endpoint registration
│       │   ├── IEndpoint.cs
│       │   ├── EndpointExtensions.cs
│       │   ├── V1/
│       │   │   ├── Users/
│       │   │   │   ├── GetUserEndpoint.cs
│       │   │   │   ├── CreateUserEndpoint.cs
│       │   │   │   └── UpdateUserEndpoint.cs
│       │   │   └── Auth/
│       │   │       ├── LoginEndpoint.cs
│       │   │       └── RefreshTokenEndpoint.cs
│       │   └── V2/
│       │       └── Users/
│       ├── Middleware/
│       │   ├── ExceptionHandlingMiddleware.cs
│       │   ├── CorrelationIdMiddleware.cs
│       │   ├── SecurityHeadersMiddleware.cs
│       │   └── RequestLoggingMiddleware.cs
│       ├── Filters/
│       │   └── ApiKeyAuthFilter.cs
│       ├── Extensions/
│       │   ├── ServiceCollectionExtensions.cs  # DI wiring
│       │   ├── WebApplicationExtensions.cs     # Middleware pipeline
│       │   ├── SwaggerExtensions.cs
│       │   ├── AuthenticationExtensions.cs
│       │   ├── HealthCheckExtensions.cs
│       │   ├── RateLimitingExtensions.cs
│       │   ├── TelemetryExtensions.cs
│       │   └── CorsExtensions.cs
│       ├── Services/
│       │   ├── CurrentUserService.cs
│       │   ├── CorrelationIdProvider.cs
│       │   └── DateTimeProvider.cs
│       └── HealthChecks/
│           ├── SqlServerHealthCheck.cs
│           └── RedisHealthCheck.cs
│
└── tests/
    ├── EnterpriseApi.UnitTests/
    │   ├── Domain/
    │   │   └── Users/
    │   │       └── UserTests.cs
    │   ├── Application/
    │   │   ├── Features/
    │   │   │   └── Users/
    │   │   │       ├── CreateUserCommandHandlerTests.cs
    │   │   │       └── GetUserByIdQueryHandlerTests.cs
    │   │   └── Behaviors/
    │   │       └── ValidationBehaviorTests.cs
    │   └── Common/
    │       └── Builders/                  # Test data builders
    ├── EnterpriseApi.IntegrationTests/
    │   ├── Infrastructure/
    │   │   ├── ApiFactory.cs              # WebApplicationFactory
    │   │   ├── DatabaseFixture.cs         # Testcontainers SQL Server
    │   │   └── RedisFixture.cs
    │   └── Endpoints/
    │       ├── V1/
    │       │   └── Users/
    │       │       └── UsersEndpointTests.cs
    │       └── Auth/
    │           └── AuthEndpointTests.cs
    └── EnterpriseApi.ArchitectureTests/
        └── ArchitectureTests.cs           # NetArchTest dependency rules
```

---

## 4. CLEAN ARCHITECTURE IMPLEMENTATION

### Layer Responsibilities

**Domain Layer** — The heart of the system. Contains:
- Entities with encapsulated business logic
- Value Objects enforcing domain invariants
- Domain Events representing state changes
- Repository interfaces (contracts, not implementations)
- Domain Service interfaces
- Domain Exceptions

**Application Layer** — Use case orchestration. Contains:
- CQRS commands and queries
- Command and query handlers
- Application services (orchestration only)
- Validation rules (FluentValidation)
- DTOs and response models
- Pipeline behaviors (cross-cutting use case concerns)
- Domain event handlers at the application level

**Infrastructure Layer** — Technical implementations. Contains:
- EF Core DbContext and entity configurations
- Repository implementations
- Authentication/token services
- Caching implementations
- External service integrations
- Background job implementations

**API Layer** — HTTP delivery. Contains:
- Minimal API endpoint definitions
- Middleware pipeline
- DI configuration
- Swagger setup
- Configuration bootstrapping

### Dependency Rules (Enforced via Architecture Tests)

```
Domain      ──────► (nothing)
Application ──────► Domain, Shared
Infrastructure ───► Application, Domain, Shared
API         ──────► Application, Infrastructure, Shared
```

No reverse dependencies are permitted. This is mechanically enforced.

### Base Entity and Aggregate Root

```csharp
// src/EnterpriseApi.Domain/Common/BaseEntity.cs
namespace EnterpriseApi.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; protected set; }
    public DateTimeOffset? UpdatedAt { get; protected set; }
    public string? CreatedBy { get; protected set; }
    public string? UpdatedBy { get; protected set; }

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

```csharp
// src/EnterpriseApi.Domain/Common/IDomainEvent.cs
using MediatR;

namespace EnterpriseApi.Domain.Common;

public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    DateTimeOffset OccurredOn { get; }
}
```

```csharp
// src/EnterpriseApi.Domain/Common/DomainEvent.cs
namespace EnterpriseApi.Domain.Common;

public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}
```

### Value Object Base

```csharp
// src/EnterpriseApi.Domain/Common/ValueObject.cs
namespace EnterpriseApi.Domain.Common;

public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj) =>
        obj is ValueObject other && ValuesAreEqual(other);

    public bool Equals(ValueObject? other) =>
        other is not null && ValuesAreEqual(other);

    private bool ValuesAreEqual(ValueObject other) =>
        GetType() == other.GetType() &&
        GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());

    public override int GetHashCode() =>
        GetEqualityComponents()
            .Aggregate(default(int), HashCode.Combine);

    public static bool operator ==(ValueObject? left, ValueObject? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(ValueObject? left, ValueObject? right) =>
        !(left == right);
}
```

### Email Value Object Example

```csharp
// src/EnterpriseApi.Domain/ValueObjects/Email.cs
using System.Text.RegularExpressions;
using EnterpriseApi.Domain.Common;
using EnterpriseApi.Domain.Exceptions;

namespace EnterpriseApi.Domain.ValueObjects;

public sealed class Email : ValueObject
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(250));

    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var normalized = value.Trim().ToLowerInvariant();

        if (!EmailRegex.IsMatch(normalized))
            throw new BusinessRuleViolationException($"'{value}' is not a valid email address.");

        return new Email(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;
}
```

### User Aggregate

```csharp
// src/EnterpriseApi.Domain/Entities/Users/User.cs
using EnterpriseApi.Domain.Common;
using EnterpriseApi.Domain.Exceptions;
using EnterpriseApi.Domain.ValueObjects;

namespace EnterpriseApi.Domain.Entities.Users;

public sealed class User : BaseEntity
{
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public Email Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTimeOffset? RefreshTokenExpiresAt { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }

    // Private parameterless constructor for EF Core
    private User() { }

    public static User Create(
        string firstName,
        string lastName,
        Email email,
        string passwordHash,
        UserRole role = UserRole.User)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email,
            PasswordHash = passwordHash,
            Role = role,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        user.RaiseDomainEvent(new UserCreatedEvent(user.Id, user.Email.Value));

        return user;
    }

    public void SetRefreshToken(string token, DateTimeOffset expiresAt)
    {
        RefreshToken = token;
        RefreshTokenExpiresAt = expiresAt;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RevokeRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiresAt = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RecordFailedLoginAttempt()
    {
        FailedLoginAttempts++;

        if (FailedLoginAttempts >= 5)
        {
            LockedUntil = DateTimeOffset.UtcNow.AddMinutes(15);
            RaiseDomainEvent(new UserAccountLockedEvent(Id, LockedUntil.Value));
        }
    }

    public void ResetFailedLoginAttempts()
    {
        FailedLoginAttempts = 0;
        LockedUntil = null;
    }

    public bool IsLocked() => LockedUntil.HasValue && LockedUntil > DateTimeOffset.UtcNow;

    public void Deactivate()
    {
        if (!IsActive)
            throw new BusinessRuleViolationException("User is already deactivated.");

        IsActive = false;
        RevokeRefreshToken();
        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new UserDeactivatedEvent(Id));
    }

    public void UpdateProfile(string firstName, string lastName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
```

### Result Type (Railway-Oriented Error Handling)

```csharp
// src/EnterpriseApi.Application/Common/Models/Result.cs
namespace EnterpriseApi.Application.Common.Models;

public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public Error Error { get; }

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
        Error = Error.None;
    }

    private Result(Error error)
    {
        IsSuccess = false;
        Value = default;
        Error = error;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);

    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<Error, TResult> onFailure) =>
        IsSuccess ? onSuccess(Value!) : onFailure(Error);
}

public sealed record Error(string Code, string Description, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);

    public static Error NotFound(string code, string description) =>
        new(code, description, ErrorType.NotFound);

    public static Error Validation(string code, string description) =>
        new(code, description, ErrorType.Validation);

    public static Error Conflict(string code, string description) =>
        new(code, description, ErrorType.Conflict);

    public static Error Unauthorized(string code, string description) =>
        new(code, description, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string description) =>
        new(code, description, ErrorType.Forbidden);
}

public enum ErrorType { None, NotFound, Validation, Conflict, Unauthorized, Forbidden }
```

---

## 5. HIGH-PERFORMANCE API DESIGN

### Minimal APIs vs Controllers — Recommendation

**This platform uses Minimal APIs** for the following reasons:

1. **Lower overhead:** Minimal APIs bypass the MVC middleware pipeline (action filters, model binding overhead, action invoker chain). At hyperscale, this difference compounds significantly.
2. **Explicit routing:** No magic routing conventions — all routes are explicitly declared and version-scoped.
3. **Endpoint groups:** ASP.NET Core 9 `MapGroup` provides controller-equivalent organization without the performance cost.
4. **Testability:** Minimal API endpoints are easy to test via `WebApplicationFactory` without controller-specific scaffolding.
5. **Clean Architecture alignment:** Endpoints are thin delivery adapters — they dispatch to MediatR and return responses. The MVC pipeline adds no value for this pattern.

Traditional controllers are only appropriate when the team needs complex action filter pipelines, existing controller-heavy codebase migration, or heavy use of MVC-specific attributes. None of those apply here.

### Endpoint Interface and Registration Pattern

```csharp
// src/EnterpriseApi.API/Endpoints/IEndpoint.cs
namespace EnterpriseApi.API.Endpoints;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}
```

```csharp
// src/EnterpriseApi.API/Endpoints/EndpointExtensions.cs
using System.Reflection;

namespace EnterpriseApi.API.Endpoints;

public static class EndpointExtensions
{
    public static IServiceCollection AddEndpoints(
        this IServiceCollection services,
        Assembly assembly)
    {
        var endpointServiceDescriptors = assembly
            .GetExportedTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && t.IsAssignableTo(typeof(IEndpoint)))
            .Select(t => ServiceDescriptor.Transient(typeof(IEndpoint), t))
            .ToArray();

        services.TryAddEnumerable(endpointServiceDescriptors);

        return services;
    }

    public static IApplicationBuilder MapEndpoints(
        this WebApplication app,
        RouteGroupBuilder? routeGroupBuilder = null)
    {
        var endpoints = app.Services.GetRequiredService<IEnumerable<IEndpoint>>();

        IEndpointRouteBuilder builder = routeGroupBuilder ?? app;

        foreach (var endpoint in endpoints)
        {
            endpoint.MapEndpoint(builder);
        }

        return app;
    }
}
```

### Example Endpoint (Users)

```csharp
// src/EnterpriseApi.API/Endpoints/V1/Users/GetUserEndpoint.cs
using Asp.Versioning;
using EnterpriseApi.Application.Features.Users.Queries.GetUserById;
using EnterpriseApi.Shared.Constants;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EnterpriseApi.API.Endpoints.V1.Users;

public sealed class GetUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Users.GetById, HandleAsync)
           .WithName("GetUserById")
           .WithApiVersionSet(app.NewApiVersionSet().HasApiVersion(new ApiVersion(1, 0)).Build())
           .MapToApiVersion(1, 0)
           .WithTags("Users")
           .WithSummary("Get user by identifier")
           .Produces<GetUserByIdResponse>(StatusCodes.Status200OK)
           .ProducesProblem(StatusCodes.Status404NotFound)
           .ProducesProblem(StatusCodes.Status401Unauthorized)
           .RequireAuthorization(PolicyNames.RequireAuthenticatedUser)
           .WithOpenApi();
    }

    private static async Task<Results<Ok<GetUserByIdResponse>, NotFound<ProblemDetails>, UnauthorizedHttpResult>> HandleAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetUserByIdQuery(id);
        var result = await mediator.Send(query, cancellationToken);

        return result.Match<Results<Ok<GetUserByIdResponse>, NotFound<ProblemDetails>, UnauthorizedHttpResult>>(
            value => TypedResults.Ok(value),
            error => error.Type switch
            {
                ErrorType.NotFound => TypedResults.NotFound(
                    new ProblemDetails { Title = error.Description, Detail = error.Code }),
                _ => TypedResults.Unauthorized()
            });
    }
}
```

### Program.cs — Application Bootstrap

```csharp
// src/EnterpriseApi.API/Program.cs
using EnterpriseApi.API.Endpoints;
using EnterpriseApi.API.Extensions;
using EnterpriseApi.API.Middleware;
using EnterpriseApi.Application;
using EnterpriseApi.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog — configured before anything else to capture startup errors
builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig.ReadFrom.Configuration(context.Configuration));

// Service registrations
builder.Services
    .AddApplicationServices()                           // Application layer
    .AddInfrastructureServices(builder.Configuration)  // Infrastructure layer
    .AddApiServices(builder.Configuration)             // API layer (auth, versioning, swagger, etc.)
    .AddEndpoints(typeof(Program).Assembly);           // Minimal API endpoints

var app = builder.Build();

// Run database migrations and seed (development only)
if (app.Environment.IsDevelopment())
{
    await app.Services.InitializeDatabaseAsync();
}

// Middleware pipeline order matters — sequence is intentional
app.UseCorrelationId();
app.UseSecurityHeaders();
app.UseExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerWithVersioning();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("Default");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseRequestLogging();

// Map endpoints
app.MapEndpoints();
app.MapHealthChecks();
app.MapMetrics(); // Prometheus endpoint

await app.RunAsync();
```

### Async-First and CancellationToken Usage

```csharp
// All handlers must accept and forward CancellationToken
public sealed class GetUsersPagedQueryHandler
    : IRequestHandler<GetUsersPagedQuery, Result<PagedResult<UserSummaryResponse>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cache;

    public GetUsersPagedQueryHandler(IApplicationDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<Result<PagedResult<UserSummaryResponse>>> Handle(
        GetUsersPagedQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.Users.PagedList(request.Page, request.PageSize, request.SearchTerm);

        // Attempt cache-first retrieval
        var cached = await _cache.GetAsync<PagedResult<UserSummaryResponse>>(cacheKey, cancellationToken);
        if (cached is not null)
            return Result<PagedResult<UserSummaryResponse>>.Success(cached);

        var query = _context.Users
            .AsNoTracking()
            .Where(u => u.IsActive);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(u =>
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term) ||
                u.Email.Value.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserSummaryResponse(
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email.Value,
                u.Role.ToString(),
                u.CreatedAt))
            .ToListAsync(cancellationToken);

        var result = new PagedResult<UserSummaryResponse>(
            users, totalCount, request.Page, request.PageSize);

        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), cancellationToken);

        return Result<PagedResult<UserSummaryResponse>>.Success(result);
    }
}
```

### Pagination Standard

```csharp
// src/EnterpriseApi.Application/Common/Models/PagedResult.cs
namespace EnterpriseApi.Application.Common.Models;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    // Pagination metadata for Link header / HATEOAS
    public PaginationMetadata GetMetadata() => new(
        TotalCount, Page, PageSize, TotalPages, HasPreviousPage, HasNextPage);
}

public sealed record PaginationMetadata(
    int TotalCount,
    int CurrentPage,
    int PageSize,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);
```

### Response Compression

```csharp
// In ServiceCollectionExtensions.cs
services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        ["application/json", "application/problem+json"]);
});

services.Configure<BrotliCompressionProviderOptions>(options =>
    options.Level = CompressionLevel.Optimal);

services.Configure<GzipCompressionProviderOptions>(options =>
    options.Level = CompressionLevel.SmallestSize);
```

---

## 6. CQRS + MEDIATR ARCHITECTURE

### Command and Query Abstractions

```csharp
// src/EnterpriseApi.Application/Common/Abstractions/ICommand.cs
using MediatR;

namespace EnterpriseApi.Application.Common.Abstractions;

public interface ICommand : IRequest<Result<Unit>> { }
public interface ICommand<TResponse> : IRequest<Result<TResponse>> { }

public interface ICommandHandler<TCommand>
    : IRequestHandler<TCommand, Result<Unit>>
    where TCommand : ICommand { }

public interface ICommandHandler<TCommand, TResponse>
    : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse> { }
```

```csharp
// src/EnterpriseApi.Application/Common/Abstractions/IQuery.cs
using MediatR;

namespace EnterpriseApi.Application.Common.Abstractions;

public interface IQuery<TResponse> : IRequest<Result<TResponse>> { }

public interface IQueryHandler<TQuery, TResponse>
    : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse> { }
```

### Validation Pipeline Behavior

```csharp
// src/EnterpriseApi.Application/Common/Behaviors/ValidationBehavior.cs
using FluentValidation;
using MediatR;
using ValidationException = EnterpriseApi.Application.Common.Exceptions.ValidationException;

namespace EnterpriseApi.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) =>
        _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
```

### Logging Pipeline Behavior

```csharp
// src/EnterpriseApi.Application/Common/Behaviors/LoggingBehavior.cs
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace EnterpriseApi.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger) =>
        _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation(
            "Handling {RequestName} | Request: {@Request}",
            requestName, request);

        var sw = Stopwatch.StartNew();

        try
        {
            var response = await next();
            sw.Stop();

            _logger.LogInformation(
                "Handled {RequestName} in {ElapsedMilliseconds}ms",
                requestName, sw.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();

            _logger.LogError(ex,
                "Request {RequestName} failed after {ElapsedMilliseconds}ms",
                requestName, sw.ElapsedMilliseconds);

            throw;
        }
    }
}
```

### Performance Pipeline Behavior

```csharp
// src/EnterpriseApi.Application/Common/Behaviors/PerformanceBehavior.cs
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace EnterpriseApi.Application.Common.Behaviors;

public sealed class PerformanceBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private const int SlowRequestThresholdMs = 500;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger) =>
        _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        if (sw.ElapsedMilliseconds > SlowRequestThresholdMs)
        {
            _logger.LogWarning(
                "Slow request detected: {RequestName} took {ElapsedMilliseconds}ms. Request: {@Request}",
                typeof(TRequest).Name,
                sw.ElapsedMilliseconds,
                request);
        }

        return response;
    }
}
```

### Transaction Pipeline Behavior

```csharp
// src/EnterpriseApi.Application/Common/Behaviors/TransactionBehavior.cs
using EnterpriseApi.Application.Common.Abstractions;
using MediatR;

namespace EnterpriseApi.Application.Common.Behaviors;

// Only wraps commands in transactions — queries run without transaction overhead
public sealed class TransactionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand
    where TResponse : notnull
{
    private readonly IUnitOfWork _unitOfWork;

    public TransactionBehavior(IUnitOfWork unitOfWork) =>
        _unitOfWork = unitOfWork;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await next();
            await _unitOfWork.CommitTransactionAsync(transaction, cancellationToken);
            return response;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(transaction, cancellationToken);
            throw;
        }
    }
}
```

### Application Layer DI Registration

```csharp
// src/EnterpriseApi.Application/DependencyInjection.cs
using EnterpriseApi.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace EnterpriseApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddAutoMapper(typeof(DependencyInjection).Assembly);

        return services;
    }
}
```

---

## 7. DATABASE ARCHITECTURE

### DbContext Design

```csharp
// src/EnterpriseApi.Infrastructure/Persistence/ApplicationDbContext.cs
using System.Reflection;
using EnterpriseApi.Application.Common.Abstractions;
using EnterpriseApi.Domain.Common;
using EnterpriseApi.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseApi.Infrastructure.Persistence;

public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ICurrentUserService _currentUserService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all IEntityTypeConfiguration implementations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Global query filters — applied to all queries unless explicitly disabled
        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);

        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.EnableSensitiveDataLogging(false); // Never in production
        optionsBuilder.EnableDetailedErrors(false);       // Never in production
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Automatically set audit fields before saving
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.SetCreatedAt(DateTimeOffset.UtcNow, _currentUserService.UserId);
                    break;
                case EntityState.Modified:
                    entry.Entity.SetUpdatedAt(DateTimeOffset.UtcNow, _currentUserService.UserId);
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
```

### Entity Configuration (Fluent API)

```csharp
// src/EnterpriseApi.Infrastructure/Persistence/Configurations/UserConfiguration.cs
using EnterpriseApi.Domain.Entities.Users;
using EnterpriseApi.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseApi.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", "dbo");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .ValueGeneratedNever(); // Guid generated in domain, not DB

        builder.Property(u => u.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.LastName)
            .HasMaxLength(100)
            .IsRequired();

        // Value Object owned entity mapping
        builder.OwnsOne(u => u.Email, emailBuilder =>
        {
            emailBuilder.Property(e => e.Value)
                .HasColumnName("Email")
                .HasMaxLength(256)
                .IsRequired();

            emailBuilder.HasIndex(e => e.Value)
                .IsUnique()
                .HasDatabaseName("IX_Users_Email");
        });

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(u => u.Role)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.RefreshToken)
            .HasMaxLength(500);

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.UpdatedAt);

        builder.Property(u => u.CreatedBy)
            .HasMaxLength(100);

        builder.Property(u => u.UpdatedBy)
            .HasMaxLength(100);

        builder.Property(u => u.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        // Soft delete column
        builder.Property(u => u.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();

        // Performance indexes
        builder.HasIndex(u => u.CreatedAt)
            .HasDatabaseName("IX_Users_CreatedAt");

        builder.HasIndex(u => u.IsActive)
            .HasDatabaseName("IX_Users_IsActive");

        builder.HasIndex(u => new { u.IsActive, u.IsDeleted })
            .HasDatabaseName("IX_Users_Active_NotDeleted");

        // Ignore domain events — not persisted
        builder.Ignore(u => u.DomainEvents);
    }
}
```

### Unit of Work

```csharp
// src/EnterpriseApi.Infrastructure/Persistence/UnitOfWork.cs
using EnterpriseApi.Domain.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace EnterpriseApi.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context) => _context = context;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await _context.SaveChangesAsync(cancellationToken);

    public async Task<IDbContextTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default) =>
        await _context.Database.BeginTransactionAsync(cancellationToken);

    public async Task CommitTransactionAsync(
        IDbContextTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RollbackTransactionAsync(
        IDbContextTransaction transaction,
        CancellationToken cancellationToken = default) =>
        await transaction.RollbackAsync(cancellationToken);
}
```

### Migration Strategy

```bash
# Add migration
dotnet ef migrations add InitialCreate \
  --project src/EnterpriseApi.Infrastructure \
  --startup-project src/EnterpriseApi.API \
  --output-dir Persistence/Migrations

# Apply migrations (CI/CD pipeline uses this)
dotnet ef database update \
  --project src/EnterpriseApi.Infrastructure \
  --startup-project src/EnterpriseApi.API

# Generate idempotent SQL script for production deployments (preferred for prod)
dotnet ef migrations script --idempotent \
  --project src/EnterpriseApi.Infrastructure \
  --startup-project src/EnterpriseApi.API \
  --output migrations.sql
```

**Migration Policy:** Production databases are never updated via `dotnet ef database update`. An idempotent SQL script is generated as a CI artifact, reviewed, and applied by the DBA pipeline. This gives change control, rollback capability, and audit trail.

### Outbox Pattern for Domain Events

```csharp
// src/EnterpriseApi.Infrastructure/Outbox/OutboxMessage.cs
namespace EnterpriseApi.Infrastructure.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Type { get; private set; } = default!;
    public string Content { get; private set; } = default!;
    public DateTimeOffset OccurredOn { get; private set; }
    public DateTimeOffset? ProcessedOn { get; private set; }
    public string? Error { get; private set; }
    public int RetryCount { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage Create(IDomainEvent domainEvent)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = domainEvent.GetType().AssemblyQualifiedName!,
            Content = JsonSerializer.Serialize(domainEvent,
                new JsonSerializerOptions { WriteIndented = false }),
            OccurredOn = domainEvent.OccurredOn
        };
    }

    public void MarkProcessed() => ProcessedOn = DateTimeOffset.UtcNow;

    public void MarkFailed(string error)
    {
        Error = error;
        RetryCount++;
    }
}
```

### Domain Event Interceptor

```csharp
// src/EnterpriseApi.Infrastructure/Persistence/Interceptors/DomainEventInterceptor.cs
using EnterpriseApi.Domain.Common;
using EnterpriseApi.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EnterpriseApi.Infrastructure.Persistence.Interceptors;

public sealed class DomainEventInterceptor : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
            return await base.SavingChangesAsync(eventData, result, cancellationToken);

        // Convert domain events to outbox messages for reliable delivery
        var outboxMessages = eventData.Context.ChangeTracker
            .Entries<BaseEntity>()
            .Select(e => e.Entity)
            .SelectMany(entity =>
            {
                var events = entity.DomainEvents.ToList();
                entity.ClearDomainEvents();
                return events;
            })
            .Select(OutboxMessage.Create)
            .ToList();

        if (outboxMessages.Count > 0)
        {
            await eventData.Context.Set<OutboxMessage>().AddRangeAsync(outboxMessages, cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
```

---

## 8. REDIS CACHING STRATEGY

### Cache Service Abstraction

```csharp
// src/EnterpriseApi.Application/Common/Caching/ICacheService.cs
namespace EnterpriseApi.Application.Common.Caching;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? absoluteExpiration = null,
        CancellationToken cancellationToken = default);

    Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? absoluteExpiration = null,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}
```

### Redis Cache Service Implementation

```csharp
// src/EnterpriseApi.Infrastructure/Caching/RedisCacheService.cs
using EnterpriseApi.Application.Common.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace EnterpriseApi.Infrastructure.Caching;

public sealed class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public RedisCacheService(
        IDistributedCache distributedCache,
        IConnectionMultiplexer redis,
        ILogger<RedisCacheService> logger)
    {
        _distributedCache = distributedCache;
        _redis = redis;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = await _distributedCache.GetStringAsync(key, cancellationToken);

            if (data is null)
                return default;

            return JsonSerializer.Deserialize<T>(data, SerializerOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache GET failed for key {CacheKey}. Returning null.", key);
            return default; // Fail open — cache miss is acceptable
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? absoluteExpiration = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new DistributedCacheEntryOptions();

            if (absoluteExpiration.HasValue)
                options.SetAbsoluteExpiration(absoluteExpiration.Value);
            else
                options.SetAbsoluteExpiration(TimeSpan.FromMinutes(15)); // Sensible default

            var data = JsonSerializer.Serialize(value, SerializerOptions);
            await _distributedCache.SetStringAsync(key, data, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache SET failed for key {CacheKey}. Continuing without cache.", key);
            // Fail open — continue without caching
        }
    }

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? absoluteExpiration = null,
        CancellationToken cancellationToken = default)
    {
        var cached = await GetAsync<T>(key, cancellationToken);

        if (cached is not null)
            return cached;

        var result = await factory(cancellationToken);

        if (result is not null)
            await SetAsync(key, result, absoluteExpiration, cancellationToken);

        return result;
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache REMOVE failed for key {CacheKey}.", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoints = _redis.GetEndPoints();
            var server = _redis.GetServer(endpoints.First());

            var keys = server.Keys(pattern: $"*{pattern}*").ToList();

            if (keys.Count > 0)
            {
                var db = _redis.GetDatabase();
                await db.KeyDeleteAsync(keys.ToArray());
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache pattern REMOVE failed for pattern {Pattern}.", pattern);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            return await db.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache EXISTS check failed for key {CacheKey}.", key);
            return false;
        }
    }
}
```

### Cache Keys Centralization

```csharp
// src/EnterpriseApi.Shared/Constants/CacheKeys.cs
namespace EnterpriseApi.Shared.Constants;

public static class CacheKeys
{
    public static class Users
    {
        public static string ById(Guid id) => $"users:id:{id}";
        public static string ByEmail(string email) => $"users:email:{email.ToLower()}";
        public static string PagedList(int page, int size, string? search) =>
            $"users:list:p{page}:s{size}:q{search ?? "all"}";
        public const string Pattern = "users:";
    }

    public static class Auth
    {
        public static string RevokedToken(string jti) => $"auth:revoked:{jti}";
        public static string UserPermissions(Guid userId) => $"auth:perms:{userId}";
    }
}
```

### Cache Invalidation on Command Completion

```csharp
// Example: Invalidate user cache after UpdateUserCommandHandler succeeds
public sealed class UpdateUserCommandHandler : ICommandHandler<UpdateUserCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public async Task<Result<Unit>> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            return Result<Unit>.Failure(Error.NotFound("User.NotFound", $"User {request.UserId} not found."));

        user.UpdateProfile(request.FirstName, request.LastName);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Targeted invalidation — remove specific keys and list patterns
        await Task.WhenAll(
            _cache.RemoveAsync(CacheKeys.Users.ById(request.UserId), cancellationToken),
            _cache.RemoveByPatternAsync(CacheKeys.Users.Pattern, cancellationToken));

        return Result<Unit>.Success(Unit.Value);
    }
}
```

---

## 9. AUTHENTICATION & AUTHORIZATION

### JWT Token Service

```csharp
// src/EnterpriseApi.Infrastructure/Authentication/JwtTokenService.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EnterpriseApi.Domain.Entities.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EnterpriseApi.Infrastructure.Authentication;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> settings) =>
        _settings = settings.Value;

    public (string accessToken, string jti) GenerateAccessToken(User user)
    {
        var jti = Guid.NewGuid().ToString();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email.Value),
            new(JwtRegisteredClaimNames.Jti, jti),
            new(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(AppClaimTypes.UserId, user.Id.ToString()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

        var tokenDescriptor = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpirationMinutes),
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(tokenDescriptor), jti);
    }

    public (string token, DateTimeOffset expiresAt) GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(randomBytes);
        var expiresAt = DateTimeOffset.UtcNow.AddDays(_settings.RefreshTokenExpirationDays);

        return (token, expiresAt);
    }

    public ClaimsPrincipal? ValidateExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey)),
            ValidateIssuer = true,
            ValidIssuer = _settings.Issuer,
            ValidateAudience = true,
            ValidAudience = _settings.Audience,
            ValidateLifetime = false, // Allow expired tokens during refresh
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, tokenValidationParameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha512,
                    StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
```

### Login Command Handler

```csharp
// src/EnterpriseApi.Application/Features/Auth/Commands/Login/LoginCommandHandler.cs
public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, LoginResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHashingService _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly ILogger<LoginCommandHandler> _logger;

    public async Task<Result<LoginResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null || !user.IsActive)
        {
            _logger.LogWarning("Failed login attempt for email {Email}", request.Email);
            return Result<LoginResponse>.Failure(
                Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password."));
        }

        if (user.IsLocked())
        {
            _logger.LogWarning("Locked account login attempt for user {UserId}", user.Id);
            return Result<LoginResponse>.Failure(
                Error.Unauthorized("Auth.AccountLocked", "Account is temporarily locked."));
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.RecordFailedLoginAttempt();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<LoginResponse>.Failure(
                Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password."));
        }

        user.ResetFailedLoginAttempts();

        var (accessToken, jti) = _jwtTokenService.GenerateAccessToken(user);
        var (refreshToken, refreshTokenExpiresAt) = _jwtTokenService.GenerateRefreshToken();

        user.SetRefreshToken(refreshToken, refreshTokenExpiresAt);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<LoginResponse>.Success(new LoginResponse(
            accessToken,
            refreshToken,
            refreshTokenExpiresAt,
            user.Id,
            user.Email.Value,
            user.Role.ToString()));
    }
}
```

### Policy-Based Authorization Setup

```csharp
// src/EnterpriseApi.API/Extensions/AuthenticationExtensions.cs
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace EnterpriseApi.API.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>()!;

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = true;
            options.SaveToken = false; // Don't save token in HttpContext — use claims
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero, // Zero tolerance for clock drift
                RequireExpirationTime = true,
                RequireSignedTokens = true
            };

            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    // Check token revocation list in Redis
                    var jti = context.Principal?.FindFirst("jti")?.Value;
                    if (jti is not null)
                    {
                        var cache = context.HttpContext.RequestServices.GetRequiredService<ICacheService>();
                        var isRevoked = await cache.ExistsAsync(CacheKeys.Auth.RevokedToken(jti));

                        if (isRevoked)
                            context.Fail("Token has been revoked.");
                    }
                },
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILogger<JwtBearerEvents>>();
                    logger.LogWarning("JWT authentication failed: {Error}", context.Exception.Message);
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(PolicyNames.RequireAuthenticatedUser, policy =>
                policy.RequireAuthenticatedUser());

            options.AddPolicy(PolicyNames.RequireAdminRole, policy =>
                policy.RequireRole("Admin"));

            options.AddPolicy(PolicyNames.RequireManagerOrAdmin, policy =>
                policy.RequireRole("Manager", "Admin"));

            // Permission-based policy example
            options.AddPolicy(PolicyNames.CanManageUsers, policy =>
                policy.RequireAssertion(ctx =>
                    ctx.User.IsInRole("Admin") ||
                    ctx.User.HasClaim(AppClaimTypes.Permission, "users:write")));
        });

        return services;
    }
}
```

---

## 10. SECURITY ARCHITECTURE

### Security Headers Middleware

```csharp
// src/EnterpriseApi.API/Middleware/SecurityHeadersMiddleware.cs
namespace EnterpriseApi.API.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["X-XSS-Protection"] = "1; mode=block";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "script-src 'self'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: https:; " +
            "font-src 'self'; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none'";
        headers["Strict-Transport-Security"] = "max-age=63072000; includeSubDomains; preload";

        // Remove server fingerprint headers
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");
        context.Response.Headers.Remove("X-AspNet-Version");

        await _next(context);
    }
}
```

### CORS Configuration

```csharp
// src/EnterpriseApi.API/Extensions/CorsExtensions.cs
public static class CorsExtensions
{
    public static IServiceCollection AddApiCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy("Default", policy =>
            {
                if (allowedOrigins.Length > 0)
                    policy.WithOrigins(allowedOrigins);
                else
                    policy.AllowAnyOrigin(); // Development only

                policy
                    .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                    .WithHeaders("Authorization", "Content-Type", "X-Correlation-ID")
                    .WithExposedHeaders("X-Correlation-ID", "X-Total-Count", "Link");
            });
        });

        return services;
    }
}
```

### Data Protection for Sensitive Fields

```csharp
// Configuration in DI
services.AddDataProtection()
    .SetApplicationName("EnterpriseApi")
    .PersistKeysToStackExchangeRedis(connectionMultiplexer, "DataProtection-Keys")
    .ProtectKeysWithAzureKeyVault(
        new Uri(configuration["AzureKeyVault:KeyIdentifier"]!),
        new DefaultAzureCredential());
```

### SQL Injection Prevention

EF Core's parameterized queries prevent SQL injection by default. Raw SQL is forbidden unless using `FromSqlRaw` with parameters:

```csharp
// NEVER do this:
var users = _context.Users.FromSqlRaw($"SELECT * FROM Users WHERE Email = '{email}'");

// Always do this:
var users = _context.Users.FromSqlRaw(
    "SELECT * FROM Users WHERE Email = {0}", email);

// Preferred: use LINQ, which is always parameterized:
var users = _context.Users.Where(u => u.Email.Value == email);
```

### Input Sanitization

```csharp
// FluentValidation handles input sanitization at command level
public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100)
            .Matches(@"^[a-zA-Z\s\-']+$")
            .WithMessage("First name contains invalid characters.");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100)
            .Matches(@"^[a-zA-Z\s\-']+$");

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(12)
            .MaximumLength(128)
            .Matches(@"[A-Z]").WithMessage("Must contain uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Must contain lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Must contain digit.")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("Must contain special character.");
    }
}
```

---

## 11. VALIDATION ARCHITECTURE

### Validation Exception

```csharp
// src/EnterpriseApi.Application/Common/Exceptions/ValidationException.cs
using FluentValidation.Results;

namespace EnterpriseApi.Application.Common.Exceptions;

public sealed class ValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : base("One or more validation failures occurred.")
    {
        Errors = failures
            .GroupBy(f => f.PropertyName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Select(f => f.ErrorMessage).ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }
}
```

### Example Validator with Custom Rules

```csharp
// src/EnterpriseApi.Application/Features/Users/Commands/CreateUser/CreateUserCommandValidator.cs
using EnterpriseApi.Domain.Repositories;
using FluentValidation;

namespace EnterpriseApi.Application.Features.Users.Commands.CreateUser;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    private readonly IUserRepository _userRepository;

    public CreateUserCommandValidator(IUserRepository userRepository)
    {
        _userRepository = userRepository;

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256)
            .MustAsync(BeUniqueEmailAsync)
            .WithMessage("An account with this email address already exists.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(12)
            .MaximumLength(128);
    }

    private async Task<bool> BeUniqueEmailAsync(
        string email,
        CancellationToken cancellationToken)
    {
        return !await _userRepository.ExistsByEmailAsync(email, cancellationToken);
    }
}
```

---

## 12. ERROR HANDLING STRATEGY

### Global Exception Handling Middleware

```csharp
// src/EnterpriseApi.API/Middleware/ExceptionHandlingMiddleware.cs
using EnterpriseApi.Application.Common.Exceptions;
using EnterpriseApi.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace EnterpriseApi.API.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            var correlationId = context.TraceIdentifier;

            _logger.LogError(
                exception,
                "Unhandled exception. CorrelationId: {CorrelationId} | Path: {Path} | Method: {Method}",
                correlationId,
                context.Request.Path,
                context.Request.Method);

            await HandleExceptionAsync(context, exception, correlationId);
        }
    }

    private static async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception,
        string correlationId)
    {
        context.Response.ContentType = "application/problem+json";

        var (statusCode, problemDetails) = exception switch
        {
            ValidationException validationEx => (
                StatusCodes.Status422UnprocessableEntity,
                new ValidationProblemDetails(validationEx.Errors)
                {
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Title = "Validation Failed",
                    Detail = "One or more validation errors occurred.",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = context.Request.Path
                }),

            EntityNotFoundException notFoundEx => (
                StatusCodes.Status404NotFound,
                new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Resource Not Found",
                    Detail = notFoundEx.Message,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    Instance = context.Request.Path
                }),

            BusinessRuleViolationException businessEx => (
                StatusCodes.Status409Conflict,
                new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Business Rule Violation",
                    Detail = businessEx.Message,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                    Instance = context.Request.Path
                }),

            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Detail = "Authentication is required.",
                    Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                    Instance = context.Request.Path
                }),

            _ => (
                StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Internal Server Error",
                    Detail = "An unexpected error occurred. Please try again later.",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    Instance = context.Request.Path
                })
        };

        // Inject correlation ID into problem details
        problemDetails.Extensions["correlationId"] = correlationId;
        problemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;

        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json");
    }
}
```

---

## 13. OBSERVABILITY & TELEMETRY

### Serilog Configuration

```json
// appsettings.json — Serilog configuration
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "Microsoft.EntityFrameworkCore.Database.Command": "Warning",
        "System": "Warning",
        "StackExchange.Redis": "Warning"
      }
    },
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId", "WithProcessId"],
    "Properties": {
      "Application": "EnterpriseApi",
      "Environment": "Production"
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}",
          "theme": "Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Code, Serilog.Sinks.Console"
        }
      },
      {
        "Name": "Seq",
        "Args": {
          "serverUrl": "http://localhost:5341",
          "restrictedToMinimumLevel": "Information"
        }
      }
    ]
  }
}
```

### OpenTelemetry Configuration

```csharp
// src/EnterpriseApi.API/Extensions/TelemetryExtensions.cs
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace EnterpriseApi.API.Extensions;

public static class TelemetryExtensions
{
    public static IServiceCollection AddApiTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var serviceName = configuration["OpenTelemetry:ServiceName"] ?? "EnterpriseApi";
        var serviceVersion = configuration["OpenTelemetry:ServiceVersion"] ?? "1.0.0";
        var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317";

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName, serviceVersion: serviceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production",
                    ["host.name"] = Environment.MachineName
                }))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.Filter = ctx =>
                        !ctx.Request.Path.StartsWithSegments("/health") &&
                        !ctx.Request.Path.StartsWithSegments("/metrics");
                })
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation(options =>
                {
                    options.SetDbStatementForText = false; // Prevent query logging PII
                    options.SetDbStatementForStoredProcedures = false;
                })
                .AddRedisInstrumentation()
                .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint)))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddProcessInstrumentation()
                .AddMeter("EnterpriseApi.Application")
                .AddPrometheusExporter());

        return services;
    }
}
```

### Correlation ID Middleware

```csharp
// src/EnterpriseApi.API/Middleware/CorrelationIdMiddleware.cs
namespace EnterpriseApi.API.Middleware;

public sealed class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
                            ?? Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
```

### Health Checks

```csharp
// src/EnterpriseApi.API/Extensions/HealthCheckExtensions.cs
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace EnterpriseApi.API.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddApiHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddSqlServer(
                connectionString: configuration.GetConnectionString("DefaultConnection")!,
                healthQuery: "SELECT 1;",
                name: "sqlserver",
                tags: ["database", "critical"])
            .AddRedis(
                redisConnectionString: configuration.GetConnectionString("Redis")!,
                name: "redis",
                tags: ["cache", "critical"])
            .AddCheck<CustomApplicationHealthCheck>(
                "application",
                tags: ["application"]);

        return services;
    }

    public static WebApplication MapHealthChecks(this WebApplication app)
    {
        // Detailed health for internal monitoring
        app.MapHealthChecks("/health/detail", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
            AllowCachingResponses = false
        }).RequireAuthorization(PolicyNames.RequireAdminRole);

        // Simple liveness probe for Kubernetes
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false // Just check the application is running
        });

        // Readiness probe for Kubernetes — checks external dependencies
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("critical")
        });

        return app;
    }
}
```

---

## 14. PERFORMANCE OPTIMIZATION

### Query Optimization

```csharp
// Efficient paged query with compiled query for hot paths
public static class CompiledQueries
{
    // Compiled queries avoid repeated expression tree compilation on hot paths
    public static readonly Func<ApplicationDbContext, Guid, CancellationToken, Task<User?>> GetUserById =
        EF.CompileAsyncQuery((ApplicationDbContext ctx, Guid id, CancellationToken _) =>
            ctx.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.Id == id));
}

// Usage in repository
public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    => await CompiledQueries.GetUserById(_context, id, cancellationToken);
```

### Projection Over Full Entity Loading

```csharp
// Never load full entities for read-only queries
// BAD — loads all columns, materializes full object graph
var users = await _context.Users.ToListAsync(cancellationToken);
var dtos = users.Select(u => new UserDto(u.Id, u.FirstName, u.Email.Value)).ToList();

// GOOD — projects to DTO directly in SQL, only fetches required columns
var dtos = await _context.Users
    .AsNoTracking()
    .Where(u => u.IsActive)
    .Select(u => new UserDto(u.Id, u.FirstName, u.Email.Value))
    .ToListAsync(cancellationToken);
```

### Thread Pool and Async Configuration

```csharp
// In Program.cs — configure thread pool for I/O-bound workloads
// Increase minimum threads to avoid starvation under burst load
ThreadPool.SetMinThreads(
    workerThreads: Environment.ProcessorCount * 4,
    completionPortThreads: Environment.ProcessorCount * 4);
```

### HTTP Client Configuration

```csharp
// Properly configured HttpClient with connection pooling
services.AddHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri(configuration["ExternalApi:BaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Accept.Add(
        new MediaTypeWithQualityHeaderValue("application/json"));
})
.SetHandlerLifetime(TimeSpan.FromMinutes(5)) // Respect DNS TTL
.AddStandardResilienceHandler(options =>
{
    options.Retry.MaxRetryAttempts = 3;
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
    options.CircuitBreaker.FailureRatio = 0.5;
});
```

### Memory Allocation Reduction

```csharp
// Use ArrayPool to avoid large heap allocations in hot paths
private static readonly ArrayPool<byte> BytePool = ArrayPool<byte>.Shared;

public async Task<byte[]> ProcessLargePayload(Stream inputStream, CancellationToken ct)
{
    var buffer = BytePool.Rent(8192);
    try
    {
        // process...
        return [];
    }
    finally
    {
        BytePool.Return(buffer, clearArray: true);
    }
}

// Use Span<T> and Memory<T> for zero-allocation string operations
public static bool ContainsSensitiveData(ReadOnlySpan<char> input)
{
    // Operates on stack memory — no heap allocation
    return input.Contains("password", StringComparison.OrdinalIgnoreCase);
}
```

### JSON Serialization Optimization

```csharp
// Source-generated serializers for zero-reflection overhead
[JsonSerializable(typeof(UserResponse))]
[JsonSerializable(typeof(PagedResult<UserSummaryResponse>))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
internal sealed partial class AppJsonSerializerContext : JsonSerializerContext { }

// Register in DI
services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});
```

---

## 15. API VERSIONING STRATEGY

### Versioning Configuration

```csharp
// src/EnterpriseApi.API/Extensions/ServiceCollectionExtensions.cs
using Asp.Versioning;

services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;

    // Support multiple versioning strategies simultaneously
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),        // /api/v1/users
        new HeaderApiVersionReader("X-API-Version"), // X-API-Version: 1.0
        new MediaTypeApiVersionReader("ver"));   // Accept: application/json;ver=1.0
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
```

### Version Deprecation Strategy

```csharp
// Mark endpoints as deprecated with sunset date
app.MapGet("/api/v1/users", HandleV1)
   .WithApiVersionSet(versionSet)
   .MapToApiVersion(1, 0)
   .WithMetadata(new ApiVersionMetadata(
       new ApiVersionModel(
           declaredVersions: [new ApiVersion(1, 0)],
           implementedVersions: [new ApiVersion(1, 0), new ApiVersion(2, 0)],
           deprecatedVersions: [new ApiVersion(1, 0)],
           supportedVersions: [new ApiVersion(2, 0)])));

// Add Sunset response header for deprecated versions
app.Use(async (context, next) =>
{
    await next();

    var apiVersion = context.GetRequestedApiVersion();
    if (apiVersion is not null && deprecatedVersions.Contains(apiVersion))
    {
        context.Response.Headers["Sunset"] = "Sat, 31 Dec 2025 23:59:59 GMT";
        context.Response.Headers["Deprecation"] = "true";
        context.Response.Headers["Link"] = "</api/v2/users>; rel=\"successor-version\"";
    }
});
```

---

## 16. BACKGROUND PROCESSING ARCHITECTURE

### Outbox Processor Background Job

```csharp
// src/EnterpriseApi.Infrastructure/BackgroundJobs/OutboxProcessorJob.cs
using EnterpriseApi.Infrastructure.Outbox;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnterpriseApi.Infrastructure.BackgroundJobs;

public sealed class OutboxProcessorJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessorJob> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(10);

    public OutboxProcessorJob(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessorJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox processor started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessages(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error processing outbox messages.");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("Outbox processor stopped.");
    }

    private async Task ProcessOutboxMessages(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var messages = await db.OutboxMessages
            .Where(m => m.ProcessedOn == null && m.RetryCount < 5)
            .OrderBy(m => m.OccurredOn)
            .Take(20) // Process in batches
            .ToListAsync(ct);

        foreach (var message in messages)
        {
            try
            {
                var eventType = Type.GetType(message.Type);

                if (eventType is null)
                {
                    _logger.LogWarning("Unknown outbox message type: {Type}", message.Type);
                    message.MarkFailed("Unknown event type.");
                    continue;
                }

                var domainEvent = (IDomainEvent?)JsonSerializer.Deserialize(message.Content, eventType);

                if (domainEvent is null)
                {
                    message.MarkFailed("Failed to deserialize event.");
                    continue;
                }

                await publisher.Publish(domainEvent, ct);
                message.MarkProcessed();

                _logger.LogInformation(
                    "Processed outbox message {MessageId} of type {Type}",
                    message.Id, message.Type);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox message {MessageId}", message.Id);
                message.MarkFailed(ex.Message);
            }
        }

        if (messages.Count > 0)
            await db.SaveChangesAsync(ct);
    }
}
```

### Quartz.NET Scheduled Job Example

```csharp
// src/EnterpriseApi.Infrastructure/BackgroundJobs/TokenCleanupJob.cs
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace EnterpriseApi.Infrastructure.BackgroundJobs;

[DisallowConcurrentExecution]
public sealed class TokenCleanupJob : IJob
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TokenCleanupJob> _logger;

    public TokenCleanupJob(ApplicationDbContext context, ILogger<TokenCleanupJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Token cleanup job starting...");

        var cutoff = DateTimeOffset.UtcNow;

        var deletedCount = await _context.Users
            .Where(u => u.RefreshTokenExpiresAt < cutoff)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(u => u.RefreshToken, (string?)null)
                 .SetProperty(u => u.RefreshTokenExpiresAt, (DateTimeOffset?)null),
                context.CancellationToken);

        _logger.LogInformation(
            "Token cleanup complete. Cleared {Count} expired refresh tokens.", deletedCount);
    }
}

// Registration in DI
services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjectionJobFactory();

    var jobKey = new JobKey("token-cleanup");

    q.AddJob<TokenCleanupJob>(opts => opts.WithIdentity(jobKey));
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("token-cleanup-trigger")
        .WithCronSchedule("0 0 2 * * ?") // Daily at 2 AM UTC
        .StartNow());
});

services.AddQuartzHostedService(options =>
    options.WaitForJobsToComplete = true);
```

---

## 17. RATE LIMITING & RESILIENCE

### Rate Limiting Configuration

```csharp
// src/EnterpriseApi.API/Extensions/RateLimitingExtensions.cs
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace EnterpriseApi.API.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, ct) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.Headers.RetryAfter =
                    context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                        ? ((int)retryAfter.TotalSeconds).ToString()
                        : "60";

                await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = 429,
                    Title = "Too Many Requests",
                    Detail = "Rate limit exceeded. Please retry after the indicated delay."
                }, ct);
            };

            // Per-IP global rate limit
            options.AddPolicy("PerIp", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10
                    }));

            // Per-user rate limit for authenticated requests (higher limits for trusted users)
            options.AddPolicy("PerUser", context =>
            {
                var userId = context.User?.FindFirst("sub")?.Value ?? "anonymous";
                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: userId,
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 300,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 20
                    });
            });

            // Strict rate limit for authentication endpoints
            options.AddPolicy("AuthEndpoints", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(15),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));
        });

        return services;
    }
}
```

### Polly Resilience Pipelines

```csharp
// Resilience pipeline for external HTTP calls
services.AddResiliencePipeline("external-api", builder =>
{
    builder
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            Delay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromSeconds(30),
            UseJitter = true,
            ShouldHandle = new PredicateBuilder()
                .Handle<HttpRequestException>()
                .HandleResult<HttpResponseMessage>(r =>
                    r.StatusCode is >= System.Net.HttpStatusCode.InternalServerError
                    or System.Net.HttpStatusCode.RequestTimeout)
        })
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            SamplingDuration = TimeSpan.FromSeconds(30),
            MinimumThroughput = 10,
            BreakDuration = TimeSpan.FromSeconds(60),
            OnOpened = args =>
            {
                logger.LogWarning("Circuit breaker opened for external API.");
                return ValueTask.CompletedTask;
            }
        })
        .AddTimeout(TimeSpan.FromSeconds(10));
});
```

---

## 18. API DOCUMENTATION STRATEGY

### Swagger Configuration

```csharp
// src/EnterpriseApi.API/Extensions/SwaggerExtensions.cs
using Asp.Versioning.ApiExplorer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EnterpriseApi.API.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddApiSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            // Document each API version separately
            var provider = services.BuildServiceProvider()
                .GetRequiredService<IApiVersionDescriptionProvider>();

            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(description.GroupName, new OpenApiInfo
                {
                    Title = $"Enterprise API {description.GroupName.ToUpperInvariant()}",
                    Version = description.ApiVersion.ToString(),
                    Description = description.IsDeprecated
                        ? "**DEPRECATED** — This API version is deprecated. Migrate to the latest version."
                        : "Enterprise-grade ASP.NET Core Web API",
                    Contact = new OpenApiContact
                    {
                        Name = "API Engineering Team",
                        Email = "api-team@enterprise.com"
                    }
                });
            }

            // JWT security definition
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter JWT token. Example: Bearer {token}"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    []
                }
            });

            // Include XML documentation comments
            var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                options.IncludeXmlComments(xmlPath);

            options.EnableAnnotations();
            options.UseInlineDefinitionsForEnums();
        });

        return services;
    }

    public static WebApplication UseSwaggerWithVersioning(this WebApplication app)
    {
        app.UseSwagger(options =>
        {
            options.RouteTemplate = "api-docs/{documentName}/openapi.json";
        });

        app.UseSwaggerUI(options =>
        {
            var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerEndpoint(
                    $"/api-docs/{description.GroupName}/openapi.json",
                    $"Enterprise API {description.GroupName.ToUpperInvariant()}");
            }

            options.RoutePrefix = "api-docs";
            options.DisplayRequestDuration();
            options.EnableDeepLinking();
            options.EnableFilter();
        });

        return app;
    }
}
```

---

## 19. CONFIGURATION MANAGEMENT

### Strongly Typed Settings

```csharp
// src/EnterpriseApi.Shared/Settings/JwtSettings.cs
using System.ComponentModel.DataAnnotations;

namespace EnterpriseApi.Shared.Settings;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    [Required]
    [MinLength(32)]
    public string SecretKey { get; init; } = default!;

    [Required]
    public string Issuer { get; init; } = default!;

    [Required]
    public string Audience { get; init; } = default!;

    [Range(1, 60)]
    public int AccessTokenExpirationMinutes { get; init; } = 15;

    [Range(1, 90)]
    public int RefreshTokenExpirationDays { get; init; } = 30;
}
```

### Configuration Registration with Validation

```csharp
// In DI setup — fail fast on invalid configuration at startup
services
    .AddOptions<JwtSettings>()
    .BindConfiguration(JwtSettings.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart(); // Fail immediately if config is invalid

services
    .AddOptions<DatabaseSettings>()
    .BindConfiguration(DatabaseSettings.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

### appsettings.json Structure

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=EnterpriseApi;Trusted_Connection=True;TrustServerCertificate=True",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "SecretKey": "REPLACE_WITH_ENVIRONMENT_VARIABLE_OR_KEY_VAULT_SECRET",
    "Issuer": "EnterpriseApi",
    "Audience": "EnterpriseApi.Clients",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 30
  },
  "OpenTelemetry": {
    "ServiceName": "EnterpriseApi",
    "ServiceVersion": "1.0.0",
    "OtlpEndpoint": "http://localhost:4317"
  },
  "Cors": {
    "AllowedOrigins": []
  },
  "RateLimiting": {
    "Enabled": true
  },
  "FeatureFlags": {
    "EnableNewUserProfileV2": false
  }
}
```

### Azure Key Vault Integration

```csharp
// In Program.cs — add Key Vault as configuration provider
if (!builder.Environment.IsDevelopment())
{
    var keyVaultUri = builder.Configuration["AzureKeyVault:VaultUri"]!;

    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = builder.Configuration["AzureKeyVault:ManagedIdentityClientId"]
        }),
        new AzureKeyVaultConfigurationOptions
        {
            ReloadInterval = TimeSpan.FromHours(1) // Refresh secrets hourly
        });
}
```

---

## 20. DOCKER & CONTAINERIZATION

### Multi-Stage Dockerfile

```dockerfile
# src/EnterpriseApi.API/Dockerfile

# ─── Stage 1: Restore ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS restore
WORKDIR /src

# Copy only project files first for optimal layer caching
COPY ["src/EnterpriseApi.Domain/EnterpriseApi.Domain.csproj", "EnterpriseApi.Domain/"]
COPY ["src/EnterpriseApi.Application/EnterpriseApi.Application.csproj", "EnterpriseApi.Application/"]
COPY ["src/EnterpriseApi.Infrastructure/EnterpriseApi.Infrastructure.csproj", "EnterpriseApi.Infrastructure/"]
COPY ["src/EnterpriseApi.Shared/EnterpriseApi.Shared.csproj", "EnterpriseApi.Shared/"]
COPY ["src/EnterpriseApi.API/EnterpriseApi.API.csproj", "EnterpriseApi.API/"]

RUN dotnet restore "EnterpriseApi.API/EnterpriseApi.API.csproj" \
    --runtime linux-musl-x64

# ─── Stage 2: Build ──────────────────────────────────────────────────────────
FROM restore AS build
WORKDIR /src

COPY src/ .

RUN dotnet build "EnterpriseApi.API/EnterpriseApi.API.csproj" \
    --configuration Release \
    --no-restore \
    --runtime linux-musl-x64 \
    --self-contained false

# ─── Stage 3: Test ────────────────────────────────────────────────────────────
FROM build AS test
COPY tests/ ../tests/

RUN dotnet test "../tests/EnterpriseApi.UnitTests/EnterpriseApi.UnitTests.csproj" \
    --configuration Release \
    --no-build \
    --logger "console;verbosity=minimal"

# ─── Stage 4: Publish ─────────────────────────────────────────────────────────
FROM build AS publish

RUN dotnet publish "EnterpriseApi.API/EnterpriseApi.API.csproj" \
    --configuration Release \
    --no-restore \
    --runtime linux-musl-x64 \
    --self-contained false \
    --output /app/publish \
    -p:PublishSingleFile=false \
    -p:PublishTrimmed=false \
    -p:DebugType=None \
    -p:DebugSymbols=false

# ─── Stage 5: Final Runtime Image ─────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final

# Security: Run as non-root user
RUN addgroup --system --gid 1001 appgroup && \
    adduser --system --uid 1001 --ingroup appgroup appuser

WORKDIR /app

# Copy published output
COPY --from=publish --chown=appuser:appgroup /app/publish .

# Switch to non-root user
USER appuser

# Expose HTTP port only — TLS termination handled by ingress/load balancer
EXPOSE 8080

# Health check for container orchestrators
HEALTHCHECK --interval=30s --timeout=10s --retries=3 --start-period=30s \
    CMD wget -qO- http://localhost:8080/health/live || exit 1

# Environment defaults — overridden via Kubernetes ConfigMap/Secrets
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_USE_POLLING_FILE_WATCHER=false \
    DOTNET_GCConserveMemory=5

ENTRYPOINT ["dotnet", "EnterpriseApi.API.dll"]
```

### .dockerignore

```
**/.git
**/.gitignore
**/bin/
**/obj/
**/*.user
**/*.suo
**/node_modules/
**/TestResults/
**/.vs/
**/coverage/
**/*.md
**/Dockerfile*
**/docker-compose*
```

---

## 21. CI/CD READY FOUNDATION

### GitHub Actions CI Pipeline

```yaml
# .github/workflows/ci.yml
name: Continuous Integration

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main, develop ]

env:
  DOTNET_VERSION: '9.0.x'
  DOTNET_NOLOGO: true
  DOTNET_CLI_TELEMETRY_OPTOUT: true

jobs:
  build-and-test:
    name: Build, Test & Analyze
    runs-on: ubuntu-latest

    services:
      sqlserver:
        image: mcr.microsoft.com/mssql/server:2022-latest
        env:
          SA_PASSWORD: TestPassword123!
          ACCEPT_EULA: Y
        ports:
          - 1433:1433
        options: >-
          --health-cmd "/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P TestPassword123! -Q 'SELECT 1'"
          --health-interval 10s
          --health-retries 5

      redis:
        image: redis:7-alpine
        ports:
          - 6379:6379
        options: >-
          --health-cmd "redis-cli ping"
          --health-interval 10s

    steps:
      - name: Checkout
        uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Restore dependencies
        run: dotnet restore EnterpriseApi.sln

      - name: Build
        run: dotnet build EnterpriseApi.sln --configuration Release --no-restore

      - name: Run unit tests
        run: |
          dotnet test tests/EnterpriseApi.UnitTests/EnterpriseApi.UnitTests.csproj \
            --configuration Release \
            --no-build \
            --collect:"XPlat Code Coverage" \
            --results-directory TestResults/Unit \
            --logger "trx;LogFileName=unit-tests.trx"

      - name: Run architecture tests
        run: |
          dotnet test tests/EnterpriseApi.ArchitectureTests/EnterpriseApi.ArchitectureTests.csproj \
            --configuration Release \
            --no-build \
            --logger "trx;LogFileName=arch-tests.trx"

      - name: Run integration tests
        env:
          ConnectionStrings__DefaultConnection: "Server=localhost,1433;Database=EnterpriseApiTest;User Id=sa;Password=TestPassword123!;TrustServerCertificate=True"
          ConnectionStrings__Redis: "localhost:6379"
        run: |
          dotnet test tests/EnterpriseApi.IntegrationTests/EnterpriseApi.IntegrationTests.csproj \
            --configuration Release \
            --no-build \
            --logger "trx;LogFileName=integration-tests.trx"

      - name: Publish test results
        uses: dorny/test-reporter@v1
        if: always()
        with:
          name: Test Results
          path: 'TestResults/**/*.trx'
          reporter: dotnet-trx

      - name: Build Docker image
        run: |
          docker build \
            --file src/EnterpriseApi.API/Dockerfile \
            --tag enterpriseapi:${{ github.sha }} \
            .

      - name: Run Trivy vulnerability scan
        uses: aquasecurity/trivy-action@master
        with:
          image-ref: enterpriseapi:${{ github.sha }}
          format: 'sarif'
          output: 'trivy-results.sarif'
          severity: 'CRITICAL,HIGH'
          exit-code: '1'

      - name: Upload SARIF to GitHub Security
        uses: github/codeql-action/upload-sarif@v3
        if: always()
        with:
          sarif_file: 'trivy-results.sarif'
```

---

## 22. TESTING ARCHITECTURE

### Integration Test Factory

```csharp
// tests/EnterpriseApi.IntegrationTests/Infrastructure/ApiFactory.cs
using EnterpriseApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Testcontainers.Redis;

namespace EnterpriseApi.IntegrationTests.Infrastructure;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("TestPassword123!")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace DbContext with test database
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(_sqlContainer.GetConnectionString()));

            // Replace Redis connection
            services.AddStackExchangeRedisCache(options =>
                options.Configuration = _redisContainer.GetConnectionString());
        });

        builder.UseEnvironment("Testing");
    }

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();
        await _redisContainer.StartAsync();

        // Apply migrations to test database
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _sqlContainer.StopAsync();
        await _redisContainer.StopAsync();
    }
}
```

### Architecture Tests

```csharp
// tests/EnterpriseApi.ArchitectureTests/ArchitectureTests.cs
using NetArchTest.Rules;

namespace EnterpriseApi.ArchitectureTests;

public sealed class ArchitectureTests
{
    private static readonly Assembly DomainAssembly =
        typeof(EnterpriseApi.Domain.Common.BaseEntity).Assembly;

    private static readonly Assembly ApplicationAssembly =
        typeof(EnterpriseApi.Application.DependencyInjection).Assembly;

    private static readonly Assembly InfrastructureAssembly =
        typeof(EnterpriseApi.Infrastructure.DependencyInjection).Assembly;

    [Fact]
    public void Domain_Should_Not_HaveDependency_On_Application()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot().HaveDependencyOn("EnterpriseApi.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Domain layer must not depend on Application layer.");
    }

    [Fact]
    public void Domain_Should_Not_HaveDependency_On_Infrastructure()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot().HaveDependencyOn("EnterpriseApi.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_Should_Not_HaveDependency_On_Infrastructure()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot().HaveDependencyOn("EnterpriseApi.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            because: "Application layer must not depend on Infrastructure layer.");
    }

    [Fact]
    public void CommandHandlers_Should_BeSealed()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That().ImplementInterface(typeof(ICommandHandler<,>))
            .Should().BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Domain_Entities_Should_HavePrivateConstructors()
    {
        var entityTypes = Types.InAssembly(DomainAssembly)
            .That().Inherit(typeof(BaseEntity))
            .GetTypes();

        foreach (var type in entityTypes)
        {
            var hasPublicConstructor = type.GetConstructors(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance).Length > 0;

            hasPublicConstructor.Should().BeFalse(
                because: $"{type.Name} should use factory methods, not public constructors.");
        }
    }
}
```

### Unit Test Example

```csharp
// tests/EnterpriseApi.UnitTests/Domain/Users/UserTests.cs
using EnterpriseApi.Domain.Entities.Users;
using EnterpriseApi.Domain.Exceptions;
using EnterpriseApi.Domain.ValueObjects;

namespace EnterpriseApi.UnitTests.Domain.Users;

public sealed class UserTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldRaiseDomainEvent()
    {
        // Arrange
        var email = Email.Create("test@example.com");

        // Act
        var user = User.Create("John", "Doe", email, "hashedPassword");

        // Assert
        user.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UserCreatedEvent>();
    }

    [Fact]
    public void Deactivate_AlreadyDeactivatedUser_ShouldThrowBusinessRuleViolation()
    {
        // Arrange
        var user = CreateTestUser();
        user.Deactivate();

        // Act
        var act = () => user.Deactivate();

        // Assert
        act.Should().Throw<BusinessRuleViolationException>()
            .WithMessage("*already deactivated*");
    }

    [Theory]
    [InlineData(4, false)] // 4 failed attempts — not locked yet
    [InlineData(5, true)]  // 5 failed attempts — locked
    public void RecordFailedLoginAttempt_ShouldLockAfterFiveAttempts(
        int attempts, bool expectedLocked)
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        for (var i = 0; i < attempts; i++)
            user.RecordFailedLoginAttempt();

        // Assert
        user.IsLocked().Should().Be(expectedLocked);
    }

    private static User CreateTestUser() =>
        User.Create("John", "Doe", Email.Create("test@example.com"), "hash");
}
```

---

## 23. CLOUD-NATIVE READINESS

### Kubernetes Manifests

```yaml
# deploy/k8s/deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: enterprise-api
  labels:
    app: enterprise-api
    version: v1
spec:
  replicas: 3
  selector:
    matchLabels:
      app: enterprise-api
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 0  # Zero-downtime deployments
  template:
    metadata:
      labels:
        app: enterprise-api
    spec:
      serviceAccountName: enterprise-api
      securityContext:
        runAsNonRoot: true
        runAsUser: 1001
        fsGroup: 1001
      containers:
        - name: enterprise-api
          image: your-registry/enterprise-api:latest
          ports:
            - containerPort: 8080
              name: http
          env:
            - name: ASPNETCORE_ENVIRONMENT
              value: "Production"
            - name: ConnectionStrings__DefaultConnection
              valueFrom:
                secretKeyRef:
                  name: enterprise-api-secrets
                  key: db-connection-string
          resources:
            requests:
              cpu: "100m"
              memory: "128Mi"
            limits:
              cpu: "500m"
              memory: "512Mi"
          livenessProbe:
            httpGet:
              path: /health/live
              port: 8080
            initialDelaySeconds: 30
            periodSeconds: 15
            failureThreshold: 3
          readinessProbe:
            httpGet:
              path: /health/ready
              port: 8080
            initialDelaySeconds: 10
            periodSeconds: 10
            failureThreshold: 3
          securityContext:
            allowPrivilegeEscalation: false
            readOnlyRootFilesystem: true
            capabilities:
              drop: ["ALL"]
      terminationGracePeriodSeconds: 30

---
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: enterprise-api-hpa
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: enterprise-api
  minReplicas: 3
  maxReplicas: 50
  metrics:
    - type: Resource
      resource:
        name: cpu
        target:
          type: Utilization
          averageUtilization: 70
    - type: Resource
      resource:
        name: memory
        target:
          type: Utilization
          averageUtilization: 75
```

### Graceful Shutdown Implementation

```csharp
// In Program.cs
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

lifetime.ApplicationStopping.Register(() =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation(
        "Application is shutting down. Waiting for in-flight requests to complete...");
});

// Configure graceful shutdown timeout
builder.Services.Configure<HostOptions>(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});
```

---

## 24. ENTERPRISE CODING STANDARDS

### Naming Conventions

| Element | Convention | Example |
|---|---|---|
| Classes | PascalCase | `UserCommandHandler` |
| Interfaces | `I` prefix + PascalCase | `IUserRepository` |
| Commands | `{Action}{Entity}Command` | `CreateUserCommand` |
| Queries | `Get{Entity}By{Field}Query` | `GetUserByIdQuery` |
| Handlers | `{Command|Query}Handler` | `CreateUserCommandHandler` |
| DTOs/Responses | `{Entity}{Action}Response` | `UserCreatedResponse` |
| Validators | `{Command}Validator` | `CreateUserCommandValidator` |
| Endpoints | `{Action}{Entity}Endpoint` | `CreateUserEndpoint` |
| Private fields | `_camelCase` | `_userRepository` |
| Constants | `UPPER_SNAKE_CASE` | *(avoid; prefer static readonly)* |
| Static readonly | PascalCase | `MaxPageSize` |
| Enums | PascalCase members | `UserRole.Administrator` |
| Async methods | `{Name}Async` suffix | `GetUserByIdAsync` |

### API Response Standards

All API responses follow RFC 7807 Problem Details for errors:

```json
// Success — 200 OK
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "firstName": "John",
  "lastName": "Doe",
  "email": "john.doe@example.com",
  "role": "User",
  "createdAt": "2025-01-15T10:30:00+00:00"
}

// Validation Error — 422 Unprocessable Entity
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Failed",
  "status": 422,
  "detail": "One or more validation errors occurred.",
  "instance": "/api/v1/users",
  "errors": {
    "Email": ["'Email' must not be empty.", "An account with this email already exists."],
    "Password": ["Must contain uppercase letter."]
  },
  "correlationId": "abc123def456",
  "timestamp": "2025-01-15T10:30:00+00:00"
}

// Paginated Response — with HTTP Link headers
// Headers: X-Total-Count: 1250, Link: </api/v1/users?page=2>; rel="next"
{
  "items": [...],
  "totalCount": 1250,
  "page": 1,
  "pageSize": 25,
  "totalPages": 50,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

### Architecture Governance Rules

1. **No business logic in controllers or endpoints.** Endpoints dispatch to MediatR and return HTTP responses only.
2. **No infrastructure imports in Domain or Application layers.** Verified by architecture tests in CI.
3. **No `new` keyword for services.** All dependencies injected via constructor.
4. **No `DateTime.Now`.** Always use `IDateTimeProvider.UtcNow` for testability.
5. **No `catch (Exception)` without logging and rethrowing or converting to domain exception.**
6. **No magic strings.** All string constants extracted to static classes.
7. **No synchronous blocking on async code.** `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` are forbidden.
8. **All public APIs must have XML documentation comments.**
9. **All database schema changes require a reviewed migration script before merge.**
10. **Feature flags required for all features that change existing behavior.**

---

## 25. FINAL ENTERPRISE ENGINEERING PRINCIPLES

### Long-Term Scalability Strategy

The architecture's scalability is enabled by three structural properties that cannot be retrofitted:

**1. Stateless Processes**  
No request-scoped or application-scoped in-memory state. All state lives in SQL Server or Redis. This makes horizontal scaling a Kubernetes configuration change, not an engineering effort.

**2. Async I/O Everywhere**  
The async pipeline from endpoint to database has no synchronous blocking points. Under high concurrency, this allows a single node to handle thousands of concurrent requests with the default .NET thread pool.

**3. Read/Write Separation**  
CQRS enforces that read models (queries) can evolve independently of write models (commands). As load increases on reads, a dedicated read replica can be added by changing a single configuration key in the Infrastructure layer — the application code remains untouched.

### Multi-Team Collaboration Strategy

Feature slices in `Application/Features/` create **natural team ownership boundaries**. A team owning the `Users` bounded context owns all files under `Features/Users/`. Merge conflicts across team boundaries are structurally eliminated.

**Tooling Conventions:**
- Branch strategy: `feature/{ticket-number}/{short-description}` — no long-lived feature branches
- PR size: Target < 400 lines changed. Large PRs are broken into infrastructure PRs and feature PRs.
- Architecture Decision Records: Any change affecting more than one layer requires an ADR in `/docs/adr/`
- Code ownership: `CODEOWNERS` file in `.github/` maps folders to team GitHub identifiers

### Monolith-to-Microservice Migration Readiness

The vertical slice architecture means extracting a microservice is a **mechanical process**, not an architectural redesign. For any bounded context:

1. The feature slice in `Application/Features/{BoundedContext}/` becomes the application layer of the new service
2. The infrastructure implementations already implement interfaces — they move as-is
3. Domain entities move to the new service's domain project
4. Inter-service communication is introduced at the event handler level (replacing in-process `INotification` handlers with service bus consumers)
5. The original monolith removes the extracted feature and calls the service via HTTP or messaging

The outbox pattern for domain events ensures that when this extraction happens, event delivery guarantees remain intact — events are not lost during the transition from in-process to distributed.

### Enterprise Maintainability Philosophy

**The best architecture is the one the team can maintain under pressure at 2 AM during an incident.**

This means:

- **Observability is not optional.** Distributed tracing, structured logs with correlation IDs, and health check endpoints are mandatory — not performance enhancements. When a production incident occurs, the question is always "which service, which request, which query, which user?" The telemetry in this architecture answers all four.

- **Fail fast and loudly.** Configuration validation at startup, architecture tests in CI, type-safe result types, and domain exceptions make wrong states impossible or immediately visible.

- **Boring is good.** The architecture uses well-established, widely-understood patterns (Clean Architecture, CQRS, Outbox Pattern). A new senior engineer hired tomorrow can be productive in their first week because the patterns are familiar. Clever architecture is a liability.

- **Every abstraction must pay rent.** The Repository pattern is included because it enables testability and insulates the domain from EF Core's API surface. AutoMapper is included because manual mapping at scale is error-prone and verbose — but it is constrained to explicit profiles, never magic conventions. Abstractions that add indirection without measurable benefit are removed.

- **The measure of good architecture is the cost of change.** Every design decision in this document is evaluated by the question: "When requirements change — and they always change — how much of this do we need to touch?" The answer in a well-structured Clean Architecture codebase is: usually one feature slice, occasionally one infrastructure implementation, rarely one shared abstraction.

---

*Document version: 1.0.0 | Technology baseline: ASP.NET Core 9 / .NET 9 / C# 13*  
*Review schedule: Quarterly or upon major .NET version release*  
*Owner: Platform Engineering — Principal Architecture Team*
