# JANATICS_DATAENGINE_IMPLEMENTATION.md
# Janatics.DataEngine — Enterprise Architecture & Implementation Blueprint
### Principal Platform Architecture Document | Revision 1.0.0 | .NET 9

---

## DOCUMENT AUTHORITY

| Attribute | Value |
|---|---|
| Platform Name | Janatics.DataEngine |
| Classification | Enterprise Engineering Blueprint |
| Architecture Tier | Principal / Distinguished |
| Primary Runtime | .NET 9 / C# 13 |
| Document Status | Authoritative Reference |
| Scope | Full-Stack Platform Architecture |

---

# 1. PLATFORM OVERVIEW

## 1.1 Platform Philosophy

Janatics.DataEngine is a metadata-driven, provider-agnostic, dynamic data execution platform built as a reusable .NET 9 class library ecosystem. Its philosophy is rooted in a single governing principle: **runtime configurability over compile-time rigidity**. Rather than requiring engineering teams to write bespoke data access layers, transaction handlers, or query processors for every business domain, the engine derives its entire execution model from structured metadata — allowing business logic, persistence behavior, relational hierarchies, and transformation pipelines to be defined as configuration rather than code.

The platform is designed to serve as a foundational data infrastructure layer for enterprise applications across industries — manufacturing, ERP, logistics, procurement, and industrial automation — where data complexity is high, schema evolution is frequent, and development agility is a competitive differentiator.

The engine operates on three governing philosophies:

**Metadata-First Execution**: Every query, every transaction, every mapping is resolved through a runtime metadata graph. The engine interprets intent from structured definitions and generates the optimal execution path without bespoke implementation.

**Abstraction Without Sacrifice**: The multi-database abstraction layer provides true provider independence — SQLite for development, MySQL for production, Oracle for enterprise — without imposing an ORM's limitations on query expressiveness or execution performance.

**Low-Code Backbone**: The platform is engineered to power no-code/low-code experiences. Business analysts and configurators should be able to define complex data flows, multi-level entity hierarchies, and cross-database processes through a governed UI — without writing a single line of C# or SQL directly.

## 1.2 Metadata-Driven Architecture

The metadata system is the engine's nervous system. Every executable operation in the platform — fetch queries, transaction nodes, field mappings, validation rules, process pipelines — resolves through a layered metadata graph loaded at startup and cached in a distributed cache layer.

Metadata is stored as relational records in a governed schema and surfaced through a typed metadata service layer. At runtime, the engine resolves execution plans from metadata rather than from hardcoded service implementations. This means adding a new entity, a new relationship, or a new process definition requires only metadata changes — no redeployment.

The metadata architecture supports:

- **Entity Definitions**: Columns, data types, primary keys, identity fields, concurrency tokens
- **Relationship Definitions**: Parent-child links, FK resolution, cardinality
- **Query Definitions**: Raw SQL templates, parameter bindings, pagination contracts
- **Process Definitions**: Ordered execution stages, pre/post hooks, validation chains
- **Mapping Definitions**: Source-to-target field transformation rules with computed support
- **Validation Definitions**: Rule-based validators bound to entities and fields

## 1.3 Dynamic Execution Strategy

Dynamic execution is the engine's most operationally significant capability. At no point does the engine require a statically compiled entity type to perform persistence, projection, or transformation. The execution pipeline operates on `IDictionary<string, object?>` graphs, dynamic result sets, and strongly-typed metadata contracts simultaneously.

The fetch engine accepts a query definition key, resolves the stored parameterized SQL from metadata, binds runtime parameters, executes via Dapper on the resolved database connection, and assembles a nested object graph — all without a single `typeof(T)` constraint in the hot path.

The transaction engine accepts a recursive node payload, resolves entity metadata, determines operation intent (insert/update/delete) via state detection, constructs parameterized DML statements, and executes within a distributed unit-of-work — recursively processing all child nodes before committing.

## 1.4 Enterprise Scalability Strategy

Janatics.DataEngine is designed with horizontal scale in mind from day one. The engine's execution model is stateless — all runtime context is carried in the execution request or resolved from distributed cache. This means every API host running the engine can scale independently without session affinity.

Key scalability vectors:

- **Metadata Cache**: Redis-backed, multi-tenant keyed, TTL-governed metadata graph eliminates per-request database roundtrips for schema resolution
- **Connection Pooling**: Provider-native connection pool management with configurable ceiling per tenant
- **Async-First Pipeline**: Every execution path is fully `async`/`await` from the API layer to the database command, using `ConfigureAwait(false)` discipline throughout
- **Bulk Transaction Engine**: Batch DML support for high-volume node processing scenarios
- **Read/Write Separation**: Provider abstraction supports routing reads to read replicas and writes to primary nodes
- **Streaming Results**: Large fetch operations support `IAsyncEnumerable<T>` streaming to eliminate memory pressure

## 1.5 Modular Extensibility

The platform is structured as a constellation of NuGet-publishable class libraries, each with a single responsibility boundary. Adding a new database provider, a new transformation function, or a new process stage type requires only implementing a defined interface in a new project — with zero modifications to the engine core.

Extension points are governed by:

- `IDbProvider` — database abstraction
- `IMetadataRepository` — metadata store abstraction
- `IFieldTransformer` — field transformation abstraction
- `IProcessStage` — pipeline stage abstraction
- `IQueryValidator` — query governance abstraction
- `IAuditSink` — audit output abstraction

## 1.6 Performance-First Architecture

Dapper is the exclusive query execution mechanism for all hot-path data access. EF Core is used only for metadata schema management and administrative operations where query expressiveness is not critical. All Dapper executions are parameterized with explicit type mappings. No string interpolation is permitted in the query execution layer. All result projections use typed column mapping via `SqlMapper.ITypeMap` implementations for zero-allocation field binding.

---

# 2. COMPLETE SOLUTION ARCHITECTURE

## 2.1 Solution Structure

```
Janatics.DataEngine.sln
│
├── src/
│   ├── Janatics.DataEngine.Domain
│   ├── Janatics.DataEngine.Application
│   ├── Janatics.DataEngine.Infrastructure
│   ├── Janatics.DataEngine.Providers.Sqlite
│   ├── Janatics.DataEngine.Providers.MySql
│   ├── Janatics.DataEngine.Providers.Oracle
│   ├── Janatics.DataEngine.Providers.SqlServer
│   ├── Janatics.DataEngine.QueryEngine
│   ├── Janatics.DataEngine.TransactionEngine
│   ├── Janatics.DataEngine.MappingEngine
│   ├── Janatics.DataEngine.ValidationEngine
│   ├── Janatics.DataEngine.OrchestrationEngine
│   ├── Janatics.DataEngine.CachingLayer
│   ├── Janatics.DataEngine.Telemetry
│   ├── Janatics.DataEngine.Security
│   ├── Janatics.DataEngine.Api
│   └── Janatics.DataEngine.Contracts
│
├── ui/
│   └── janatics-dataengine-console/
│
├── tests/
│   ├── Janatics.DataEngine.UnitTests
│   ├── Janatics.DataEngine.IntegrationTests
│   ├── Janatics.DataEngine.ProviderTests
│   └── Janatics.DataEngine.PerformanceTests
│
├── tools/
│   └── Janatics.DataEngine.Migrator
│
└── docs/
    └── architecture/
```

## 2.2 Dependency Flow

```
Api
 └── Application
      ├── Domain (no external dependencies)
      ├── Contracts (shared DTOs and interfaces)
      ├── QueryEngine → Infrastructure → Providers
      ├── TransactionEngine → Infrastructure → Providers
      ├── MappingEngine → Domain
      ├── ValidationEngine → Domain
      ├── OrchestrationEngine → QueryEngine + TransactionEngine + MappingEngine
      └── CachingLayer → Infrastructure
```

**Dependency inversion is absolute**: Domain and Contracts layers have zero dependencies on infrastructure, providers, or framework libraries. All dependency directions flow inward toward the domain.

## 2.3 Package Separation Strategy

| Project | NuGet Package | Purpose |
|---|---|---|
| Domain | `Janatics.DataEngine.Domain` | Core entities, interfaces, value objects |
| Contracts | `Janatics.DataEngine.Contracts` | Shared DTOs, request/response models |
| Application | `Janatics.DataEngine.Application` | CQRS handlers, use case orchestration |
| Infrastructure | `Janatics.DataEngine.Infrastructure` | EF Core context, metadata repo, migrations |
| QueryEngine | `Janatics.DataEngine.QueryEngine` | Dapper fetch pipeline |
| TransactionEngine | `Janatics.DataEngine.TransactionEngine` | DML orchestration |
| MappingEngine | `Janatics.DataEngine.MappingEngine` | Field transformation pipeline |
| ValidationEngine | `Janatics.DataEngine.ValidationEngine` | Dynamic FluentValidation |
| OrchestrationEngine | `Janatics.DataEngine.OrchestrationEngine` | Process pipeline coordination |
| CachingLayer | `Janatics.DataEngine.Caching` | Redis + in-memory metadata cache |
| Telemetry | `Janatics.DataEngine.Telemetry` | OpenTelemetry + Serilog integration |
| Security | `Janatics.DataEngine.Security` | SQL sanitization, permission gates |
| Providers.Sqlite | `Janatics.DataEngine.Providers.Sqlite` | SQLite provider |
| Providers.MySql | `Janatics.DataEngine.Providers.MySql` | MySQL provider |
| Providers.Oracle | `Janatics.DataEngine.Providers.Oracle` | Oracle provider |
| Providers.SqlServer | `Janatics.DataEngine.Providers.SqlServer` | SQL Server provider |

---

# 3. ENTERPRISE FOLDER STRUCTURE

```
Janatics.DataEngine/
│
├── src/
│   │
│   ├── core/
│   │   ├── Janatics.DataEngine.Domain/
│   │   │   ├── Entities/
│   │   │   │   ├── MetadataEntity.cs
│   │   │   │   ├── MetadataField.cs
│   │   │   │   ├── MetadataRelationship.cs
│   │   │   │   ├── QueryDefinition.cs
│   │   │   │   ├── QueryParameter.cs
│   │   │   │   ├── ProcessDefinition.cs
│   │   │   │   ├── ProcessStageDefinition.cs
│   │   │   │   ├── MappingProfile.cs
│   │   │   │   ├── MappingRule.cs
│   │   │   │   ├── ValidationRule.cs
│   │   │   │   ├── AuditRecord.cs
│   │   │   │   └── TransactionRecord.cs
│   │   │   ├── Enumerations/
│   │   │   │   ├── DataProviderType.cs
│   │   │   │   ├── FieldDataType.cs
│   │   │   │   ├── RelationshipCardinality.cs
│   │   │   │   ├── OperationType.cs
│   │   │   │   ├── ProcessStageType.cs
│   │   │   │   ├── MappingTransformType.cs
│   │   │   │   └── ValidationRuleType.cs
│   │   │   ├── ValueObjects/
│   │   │   │   ├── EntityKey.cs
│   │   │   │   ├── FieldPath.cs
│   │   │   │   ├── QuerySignature.cs
│   │   │   │   └── TenantContext.cs
│   │   │   ├── Interfaces/
│   │   │   │   ├── IDbProvider.cs
│   │   │   │   ├── IMetadataRepository.cs
│   │   │   │   ├── IQueryRepository.cs
│   │   │   │   ├── IProcessRepository.cs
│   │   │   │   ├── IMappingRepository.cs
│   │   │   │   ├── IAuditSink.cs
│   │   │   │   ├── IFieldTransformer.cs
│   │   │   │   ├── IProcessStage.cs
│   │   │   │   └── IQueryValidator.cs
│   │   │   ├── Exceptions/
│   │   │   │   ├── DataEngineException.cs
│   │   │   │   ├── MetadataNotFoundException.cs
│   │   │   │   ├── QueryExecutionException.cs
│   │   │   │   ├── TransactionOrchestrationException.cs
│   │   │   │   ├── MappingException.cs
│   │   │   │   └── ValidationFailureException.cs
│   │   │   └── Events/
│   │   │       ├── TransactionCompletedEvent.cs
│   │   │       ├── ProcessExecutedEvent.cs
│   │   │       └── QueryExecutedEvent.cs
│   │   │
│   │   └── Janatics.DataEngine.Contracts/
│   │       ├── Requests/
│   │       │   ├── FetchRequest.cs
│   │       │   ├── TransactionRequest.cs
│   │       │   ├── ProcessExecutionRequest.cs
│   │       │   └── MappingExecutionRequest.cs
│   │       ├── Responses/
│   │       │   ├── FetchResponse.cs
│   │       │   ├── TransactionResponse.cs
│   │       │   ├── ProcessExecutionResponse.cs
│   │       │   └── ApiResponse.cs
│   │       ├── Dtos/
│   │       │   ├── EntityNodeDto.cs
│   │       │   ├── QueryDefinitionDto.cs
│   │       │   ├── MetadataEntityDto.cs
│   │       │   ├── MappingProfileDto.cs
│   │       │   └── ProcessDefinitionDto.cs
│   │       └── Pagination/
│   │           ├── PagedRequest.cs
│   │           └── PagedResponse.cs
│   │
│   ├── application/
│   │   └── Janatics.DataEngine.Application/
│   │       ├── Commands/
│   │       │   ├── ExecuteTransaction/
│   │       │   │   ├── ExecuteTransactionCommand.cs
│   │       │   │   ├── ExecuteTransactionCommandHandler.cs
│   │       │   │   └── ExecuteTransactionCommandValidator.cs
│   │       │   ├── ExecuteProcess/
│   │       │   │   ├── ExecuteProcessCommand.cs
│   │       │   │   ├── ExecuteProcessCommandHandler.cs
│   │       │   │   └── ExecuteProcessCommandValidator.cs
│   │       │   ├── SaveQueryDefinition/
│   │       │   │   ├── SaveQueryDefinitionCommand.cs
│   │       │   │   └── SaveQueryDefinitionCommandHandler.cs
│   │       │   └── SaveMappingProfile/
│   │       │       ├── SaveMappingProfileCommand.cs
│   │       │       └── SaveMappingProfileCommandHandler.cs
│   │       ├── Queries/
│   │       │   ├── ExecuteFetch/
│   │       │   │   ├── ExecuteFetchQuery.cs
│   │       │   │   └── ExecuteFetchQueryHandler.cs
│   │       │   ├── GetMetadataEntity/
│   │       │   │   ├── GetMetadataEntityQuery.cs
│   │       │   │   └── GetMetadataEntityQueryHandler.cs
│   │       │   └── GetQueryDefinitions/
│   │       │       ├── GetQueryDefinitionsQuery.cs
│   │       │       └── GetQueryDefinitionsQueryHandler.cs
│   │       ├── Behaviors/
│   │       │   ├── LoggingBehavior.cs
│   │       │   ├── ValidationBehavior.cs
│   │       │   ├── TracingBehavior.cs
│   │       │   └── CachingBehavior.cs
│   │       └── DependencyInjection/
│   │           └── ApplicationServiceExtensions.cs
│   │
│   ├── infrastructure/
│   │   └── Janatics.DataEngine.Infrastructure/
│   │       ├── Persistence/
│   │       │   ├── DataEngineDbContext.cs
│   │       │   ├── Configurations/
│   │       │   │   ├── MetadataEntityConfiguration.cs
│   │       │   │   ├── QueryDefinitionConfiguration.cs
│   │       │   │   ├── ProcessDefinitionConfiguration.cs
│   │       │   │   └── MappingProfileConfiguration.cs
│   │       │   └── Migrations/
│   │       ├── Repositories/
│   │       │   ├── MetadataRepository.cs
│   │       │   ├── QueryRepository.cs
│   │       │   ├── ProcessRepository.cs
│   │       │   └── MappingRepository.cs
│   │       ├── Audit/
│   │       │   ├── DatabaseAuditSink.cs
│   │       │   └── AuditInterceptor.cs
│   │       └── DependencyInjection/
│   │           └── InfrastructureServiceExtensions.cs
│   │
│   ├── providers/
│   │   ├── Janatics.DataEngine.Providers.Abstractions/
│   │   │   ├── IDbProvider.cs
│   │   │   ├── IDbConnectionFactory.cs
│   │   │   ├── IDbDialect.cs
│   │   │   ├── IDbCapabilities.cs
│   │   │   └── DbProviderBase.cs
│   │   ├── Janatics.DataEngine.Providers.Sqlite/
│   │   │   ├── SqliteProvider.cs
│   │   │   ├── SqliteConnectionFactory.cs
│   │   │   ├── SqliteDialect.cs
│   │   │   └── SqliteCapabilities.cs
│   │   ├── Janatics.DataEngine.Providers.MySql/
│   │   │   ├── MySqlProvider.cs
│   │   │   ├── MySqlConnectionFactory.cs
│   │   │   ├── MySqlDialect.cs
│   │   │   └── MySqlCapabilities.cs
│   │   ├── Janatics.DataEngine.Providers.Oracle/
│   │   │   ├── OracleProvider.cs
│   │   │   ├── OracleConnectionFactory.cs
│   │   │   ├── OracleDialect.cs
│   │   │   └── OracleCapabilities.cs
│   │   └── Janatics.DataEngine.Providers.SqlServer/
│   │       ├── SqlServerProvider.cs
│   │       ├── SqlServerConnectionFactory.cs
│   │       ├── SqlServerDialect.cs
│   │       └── SqlServerCapabilities.cs
│   │
│   ├── queryengine/
│   │   └── Janatics.DataEngine.QueryEngine/
│   │       ├── Pipeline/
│   │       │   ├── FetchPipeline.cs
│   │       │   ├── FetchPipelineContext.cs
│   │       │   └── FetchPipelineResult.cs
│   │       ├── Stages/
│   │       │   ├── QueryResolutionStage.cs
│   │       │   ├── ParameterBindingStage.cs
│   │       │   ├── SqlValidationStage.cs
│   │       │   ├── ExecutionStage.cs
│   │       │   ├── ChildLoadingStage.cs
│   │       │   └── ProjectionStage.cs
│   │       ├── Execution/
│   │       │   ├── DapperQueryExecutor.cs
│   │       │   ├── StreamingQueryExecutor.cs
│   │       │   └── PagedQueryExecutor.cs
│   │       ├── Graph/
│   │       │   ├── ObjectGraphBuilder.cs
│   │       │   ├── HierarchyResolver.cs
│   │       │   └── ChildCollectionLoader.cs
│   │       └── DependencyInjection/
│   │           └── QueryEngineServiceExtensions.cs
│   │
│   ├── transactionengine/
│   │   └── Janatics.DataEngine.TransactionEngine/
│   │       ├── Orchestration/
│   │       │   ├── TransactionOrchestrator.cs
│   │       │   ├── NodeProcessor.cs
│   │       │   ├── OperationDetector.cs
│   │       │   └── RelationshipResolver.cs
│   │       ├── UnitOfWork/
│   │       │   ├── DataEngineUnitOfWork.cs
│   │       │   ├── IDataEngineUnitOfWork.cs
│   │       │   └── TransactionScope.cs
│   │       ├── Commands/
│   │       │   ├── InsertCommandBuilder.cs
│   │       │   ├── UpdateCommandBuilder.cs
│   │       │   ├── DeleteCommandBuilder.cs
│   │       │   └── BulkCommandBuilder.cs
│   │       ├── Concurrency/
│   │       │   ├── OptimisticConcurrencyHandler.cs
│   │       │   └── RowVersionResolver.cs
│   │       └── DependencyInjection/
│   │           └── TransactionEngineServiceExtensions.cs
│   │
│   ├── mapping/
│   │   └── Janatics.DataEngine.MappingEngine/
│   │       ├── Profiles/
│   │       │   ├── MappingProfileResolver.cs
│   │       │   └── MappingProfileCache.cs
│   │       ├── Transformers/
│   │       │   ├── StringTransformer.cs
│   │       │   ├── NumericTransformer.cs
│   │       │   ├── DateTimeTransformer.cs
│   │       │   ├── BooleanTransformer.cs
│   │       │   ├── ComputedFieldTransformer.cs
│   │       │   └── ConditionalTransformer.cs
│   │       ├── Pipeline/
│   │       │   ├── MappingPipeline.cs
│   │       │   └── TransformationContext.cs
│   │       └── DependencyInjection/
│   │           └── MappingEngineServiceExtensions.cs
│   │
│   ├── validation/
│   │   └── Janatics.DataEngine.ValidationEngine/
│   │       ├── Builders/
│   │       │   ├── DynamicValidatorBuilder.cs
│   │       │   └── HierarchicalValidatorBuilder.cs
│   │       ├── Rules/
│   │       │   ├── RequiredRuleFactory.cs
│   │       │   ├── RangeRuleFactory.cs
│   │       │   ├── RegexRuleFactory.cs
│   │       │   ├── UniqueRuleFactory.cs
│   │       │   └── CrossFieldRuleFactory.cs
│   │       ├── Execution/
│   │       │   ├── ValidationExecutor.cs
│   │       │   └── ValidationResultAggregator.cs
│   │       └── DependencyInjection/
│   │           └── ValidationEngineServiceExtensions.cs
│   │
│   ├── orchestration/
│   │   └── Janatics.DataEngine.OrchestrationEngine/
│   │       ├── Process/
│   │       │   ├── ProcessExecutor.cs
│   │       │   ├── ProcessContext.cs
│   │       │   └── ProcessLifecycleManager.cs
│   │       ├── Stages/
│   │       │   ├── ValidationStage.cs
│   │       │   ├── MappingStage.cs
│   │       │   ├── FetchStage.cs
│   │       │   ├── PersistenceStage.cs
│   │       │   └── PostProcessingStage.cs
│   │       ├── Hooks/
│   │       │   ├── IPreProcessHook.cs
│   │       │   ├── IPostProcessHook.cs
│   │       │   └── HookRegistry.cs
│   │       └── DependencyInjection/
│   │           └── OrchestrationServiceExtensions.cs
│   │
│   ├── caching/
│   │   └── Janatics.DataEngine.CachingLayer/
│   │       ├── Keys/
│   │       │   ├── MetadataCacheKey.cs
│   │       │   ├── QueryCacheKey.cs
│   │       │   └── ResultCacheKey.cs
│   │       ├── Providers/
│   │       │   ├── RedisCacheProvider.cs
│   │       │   └── InMemoryCacheProvider.cs
│   │       ├── Strategies/
│   │       │   ├── MetadataCacheStrategy.cs
│   │       │   ├── QueryResultCacheStrategy.cs
│   │       │   └── InvalidationStrategy.cs
│   │       └── DependencyInjection/
│   │           └── CachingServiceExtensions.cs
│   │
│   ├── telemetry/
│   │   └── Janatics.DataEngine.Telemetry/
│   │       ├── Tracing/
│   │       │   ├── DataEngineActivitySource.cs
│   │       │   ├── QueryTracer.cs
│   │       │   └── TransactionTracer.cs
│   │       ├── Metrics/
│   │       │   ├── FetchMetrics.cs
│   │       │   ├── TransactionMetrics.cs
│   │       │   └── ProcessMetrics.cs
│   │       ├── Logging/
│   │       │   ├── SerilogConfigurator.cs
│   │       │   ├── CorrelationEnricher.cs
│   │       │   └── TenantEnricher.cs
│   │       └── DependencyInjection/
│   │           └── TelemetryServiceExtensions.cs
│   │
│   ├── security/
│   │   └── Janatics.DataEngine.Security/
│   │       ├── Sanitization/
│   │       │   ├── SqlSanitizer.cs
│   │       │   └── AllowedOperationValidator.cs
│   │       ├── Permissions/
│   │       │   ├── QueryPermissionGate.cs
│   │       │   ├── ProcessPermissionGate.cs
│   │       │   └── MetadataPermissionGate.cs
│   │       └── DependencyInjection/
│   │           └── SecurityServiceExtensions.cs
│   │
│   └── api/
│       └── Janatics.DataEngine.Api/
│           ├── Controllers/
│           │   ├── FetchController.cs
│           │   ├── TransactionController.cs
│           │   ├── ProcessController.cs
│           │   ├── QueryDefinitionController.cs
│           │   ├── MetadataController.cs
│           │   └── MappingController.cs
│           ├── Middleware/
│           │   ├── GlobalExceptionMiddleware.cs
│           │   ├── CorrelationIdMiddleware.cs
│           │   └── TenantResolutionMiddleware.cs
│           ├── Filters/
│           │   ├── ApiValidationFilter.cs
│           │   └── ApiPermissionFilter.cs
│           └── DependencyInjection/
│               └── ApiServiceExtensions.cs
│
├── ui/
│   └── janatics-dataengine-console/
│       ├── src/
│       │   ├── features/
│       │   │   ├── query-builder/
│       │   │   ├── mapping-designer/
│       │   │   ├── process-designer/
│       │   │   ├── metadata-manager/
│       │   │   ├── execution-monitor/
│       │   │   └── audit-viewer/
│       │   ├── components/
│       │   ├── hooks/
│       │   ├── stores/
│       │   ├── api/
│       │   └── types/
│       └── package.json
│
└── tests/
    ├── Janatics.DataEngine.UnitTests/
    ├── Janatics.DataEngine.IntegrationTests/
    ├── Janatics.DataEngine.ProviderTests/
    └── Janatics.DataEngine.PerformanceTests/
```

### Folder Responsibilities

| Folder | Responsibility |
|---|---|
| `core/Domain` | Pure business rules, entities, interfaces — zero external dependencies |
| `core/Contracts` | Shared DTOs, API contracts, request/response models — no domain logic |
| `application` | CQRS commands/queries, use case orchestration, MediatR pipeline behaviors |
| `infrastructure` | EF Core context, concrete repository implementations, database migrations |
| `providers/*` | Database-specific connection factories, dialects, and capability declarations |
| `queryengine` | Dapper-backed fetch pipeline, hierarchy loading, object graph construction |
| `transactionengine` | DML orchestration, unit-of-work management, concurrency handling |
| `mapping` | Field transformation pipeline, type converters, computed field evaluation |
| `validation` | Dynamic FluentValidation builder, rule factories, hierarchical validation |
| `orchestration` | Process lifecycle, stage pipeline coordination, hook management |
| `caching` | Redis/in-memory abstraction, metadata and query result caching |
| `telemetry` | OpenTelemetry tracing, Serilog structured logging, custom metrics |
| `security` | SQL sanitization, operation allowlisting, permission gate enforcement |
| `api` | ASP.NET Core controllers, middleware, exception handling, Swagger configuration |
| `ui` | React TypeScript frontend — admin console, designers, monitors |

---

# 4. MULTI-DATABASE PROVIDER ARCHITECTURE

## 4.1 Provider Abstraction

```csharp
// Janatics.DataEngine.Providers.Abstractions/IDbProvider.cs
public interface IDbProvider
{
    DataProviderType ProviderType { get; }
    IDbDialect Dialect { get; }
    IDbCapabilities Capabilities { get; }
    Task<IDbConnection> OpenConnectionAsync(string connectionString, CancellationToken ct = default);
    Task<IDbConnection> OpenReadConnectionAsync(string connectionString, CancellationToken ct = default);
}

// Janatics.DataEngine.Providers.Abstractions/IDbDialect.cs
public interface IDbDialect
{
    string QuoteIdentifier(string name);
    string ParameterPrefix { get; }
    string PaginationClause(int offset, int limit);
    string ConcurrencyTokenClause(string columnName);
    string LastInsertedIdExpression { get; }
    string UtcNowExpression { get; }
    bool SupportsReturningClause { get; }
}

// Janatics.DataEngine.Providers.Abstractions/IDbCapabilities.cs
public interface IDbCapabilities
{
    bool SupportsBulkInsert { get; }
    bool SupportsWindowFunctions { get; }
    bool SupportsJsonColumns { get; }
    bool SupportsReadReplica { get; }
    int MaxParameterCount { get; }
    int DefaultCommandTimeoutSeconds { get; }
}

// Janatics.DataEngine.Providers.Abstractions/IDbConnectionFactory.cs
public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateAsync(string connectionString, CancellationToken ct = default);
    Task<IDbConnection> CreateReadAsync(string connectionString, CancellationToken ct = default);
}
```

## 4.2 Provider Base

```csharp
// Janatics.DataEngine.Providers.Abstractions/DbProviderBase.cs
public abstract class DbProviderBase : IDbProvider
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IDbConnectionFactory _readConnectionFactory;

    protected DbProviderBase(
        IDbConnectionFactory connectionFactory,
        IDbConnectionFactory? readConnectionFactory = null)
    {
        _connectionFactory = connectionFactory;
        _readConnectionFactory = readConnectionFactory ?? connectionFactory;
    }

    public abstract DataProviderType ProviderType { get; }
    public abstract IDbDialect Dialect { get; }
    public abstract IDbCapabilities Capabilities { get; }

    public Task<IDbConnection> OpenConnectionAsync(string connectionString, CancellationToken ct = default)
        => _connectionFactory.CreateAsync(connectionString, ct);

    public Task<IDbConnection> OpenReadConnectionAsync(string connectionString, CancellationToken ct = default)
        => _readConnectionFactory.CreateReadAsync(connectionString, ct);
}
```

## 4.3 Provider Factory

```csharp
// Janatics.DataEngine.Providers.Abstractions/DbProviderFactory.cs
public interface IDbProviderFactory
{
    IDbProvider Resolve(DataProviderType providerType);
    IDbProvider ResolveForTenant(TenantContext tenant);
}

public sealed class DbProviderFactory : IDbProviderFactory
{
    private readonly IReadOnlyDictionary<DataProviderType, IDbProvider> _providers;

    public DbProviderFactory(IEnumerable<IDbProvider> providers)
    {
        _providers = providers.ToDictionary(p => p.ProviderType, p => p);
    }

    public IDbProvider Resolve(DataProviderType providerType)
    {
        if (!_providers.TryGetValue(providerType, out var provider))
            throw new DataEngineException($"No provider registered for type: {providerType}");
        return provider;
    }

    public IDbProvider ResolveForTenant(TenantContext tenant)
        => Resolve(tenant.ProviderType);
}
```

## 4.4 MySQL Provider Implementation

```csharp
// Janatics.DataEngine.Providers.MySql/MySqlProvider.cs
public sealed class MySqlProvider : DbProviderBase
{
    public MySqlProvider(MySqlConnectionFactory factory)
        : base(factory) { }

    public override DataProviderType ProviderType => DataProviderType.MySql;
    public override IDbDialect Dialect { get; } = new MySqlDialect();
    public override IDbCapabilities Capabilities { get; } = new MySqlCapabilities();
}

// Janatics.DataEngine.Providers.MySql/MySqlDialect.cs
public sealed class MySqlDialect : IDbDialect
{
    public string QuoteIdentifier(string name) => $"`{name}`";
    public string ParameterPrefix => "@";
    public string PaginationClause(int offset, int limit) => $"LIMIT {limit} OFFSET {offset}";
    public string ConcurrencyTokenClause(string columnName) => $"{QuoteIdentifier(columnName)} = @_RowVersion";
    public string LastInsertedIdExpression => "SELECT LAST_INSERT_ID()";
    public string UtcNowExpression => "UTC_TIMESTAMP()";
    public bool SupportsReturningClause => false;
}

// Janatics.DataEngine.Providers.MySql/MySqlConnectionFactory.cs
public sealed class MySqlConnectionFactory : IDbConnectionFactory
{
    public async Task<IDbConnection> CreateAsync(string connectionString, CancellationToken ct = default)
    {
        var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(ct).ConfigureAwait(false);
        return connection;
    }

    public Task<IDbConnection> CreateReadAsync(string connectionString, CancellationToken ct = default)
        => CreateAsync(connectionString, ct);
}
```

## 4.5 Oracle Provider Implementation

```csharp
// Janatics.DataEngine.Providers.Oracle/OracleDialect.cs
public sealed class OracleDialect : IDbDialect
{
    public string QuoteIdentifier(string name) => $"\"{name.ToUpperInvariant()}\"";
    public string ParameterPrefix => ":";
    public string PaginationClause(int offset, int limit)
        => $"OFFSET {offset} ROWS FETCH NEXT {limit} ROWS ONLY";
    public string ConcurrencyTokenClause(string columnName)
        => $"{QuoteIdentifier(columnName)} = :_RowVersion";
    public string LastInsertedIdExpression => "SELECT {sequence}.CURRVAL FROM DUAL";
    public string UtcNowExpression => "SYS_EXTRACT_UTC(SYSTIMESTAMP)";
    public bool SupportsReturningClause => true;
}
```

## 4.6 Provider Registration

```csharp
// DI registration pattern
services.AddSingleton<IDbProvider, SqliteProvider>();
services.AddSingleton<IDbProvider, MySqlProvider>();
services.AddSingleton<IDbProvider, OracleProvider>();
services.AddSingleton<IDbProvider, SqlServerProvider>();
services.AddSingleton<IDbProviderFactory, DbProviderFactory>();
```

---

# 5. METADATA-DRIVEN ENGINE ARCHITECTURE

## 5.1 Core Metadata Domain Models

```csharp
// Janatics.DataEngine.Domain/Entities/MetadataEntity.cs
public sealed class MetadataEntity
{
    public Guid Id { get; init; }
    public string TenantCode { get; init; } = default!;
    public string EntityKey { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
    public string TableName { get; init; } = default!;
    public string? SchemaName { get; init; }
    public string PrimaryKeyField { get; init; } = default!;
    public bool IsIdentityPk { get; init; }
    public string? ConcurrencyField { get; init; }
    public string? SoftDeleteField { get; init; }
    public string? CreatedAtField { get; init; }
    public string? UpdatedAtField { get; init; }
    public string? CreatedByField { get; init; }
    public string? UpdatedByField { get; init; }
    public bool IsAuditEnabled { get; init; }
    public DataProviderType ProviderType { get; init; }
    public string ConnectionName { get; init; } = default!;

    public IReadOnlyList<MetadataField> Fields { get; init; } = [];
    public IReadOnlyList<MetadataRelationship> Relationships { get; init; } = [];
}

// Janatics.DataEngine.Domain/Entities/MetadataField.cs
public sealed class MetadataField
{
    public Guid Id { get; init; }
    public Guid EntityId { get; init; }
    public string FieldKey { get; init; } = default!;
    public string ColumnName { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
    public FieldDataType DataType { get; init; }
    public int? MaxLength { get; init; }
    public int? Precision { get; init; }
    public int? Scale { get; init; }
    public bool IsNullable { get; init; }
    public bool IsReadOnly { get; init; }
    public bool IsSystemField { get; init; }
    public string? DefaultValue { get; init; }
    public int SortOrder { get; init; }
}

// Janatics.DataEngine.Domain/Entities/MetadataRelationship.cs
public sealed class MetadataRelationship
{
    public Guid Id { get; init; }
    public Guid ParentEntityId { get; init; }
    public Guid ChildEntityId { get; init; }
    public string RelationshipKey { get; init; } = default!;
    public string ParentKeyField { get; init; } = default!;
    public string ChildForeignKeyField { get; init; } = default!;
    public RelationshipCardinality Cardinality { get; init; }
    public bool CascadeDelete { get; init; }
    public bool IsLazyLoadEnabled { get; init; }
    public string? ChildCollectionAlias { get; init; }
}

// Janatics.DataEngine.Domain/Entities/QueryDefinition.cs
public sealed class QueryDefinition
{
    public Guid Id { get; init; }
    public string TenantCode { get; init; } = default!;
    public string QueryKey { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
    public string SqlTemplate { get; init; } = default!;
    public string RootEntityKey { get; init; } = default!;
    public DataProviderType ProviderType { get; init; }
    public string ConnectionName { get; init; } = default!;
    public bool IsPaginationEnabled { get; init; }
    public bool IsCacheable { get; init; }
    public int CacheTtlSeconds { get; init; }
    public int Version { get; init; }
    public bool IsActive { get; init; }

    public IReadOnlyList<QueryParameter> Parameters { get; init; } = [];
    public IReadOnlyList<QueryChildDefinition> Children { get; init; } = [];
}
```

## 5.2 Metadata Repository Interface

```csharp
// Janatics.DataEngine.Domain/Interfaces/IMetadataRepository.cs
public interface IMetadataRepository
{
    Task<MetadataEntity?> GetEntityAsync(string tenantCode, string entityKey, CancellationToken ct = default);
    Task<IReadOnlyList<MetadataEntity>> GetAllEntitiesAsync(string tenantCode, CancellationToken ct = default);
    Task<QueryDefinition?> GetQueryDefinitionAsync(string tenantCode, string queryKey, CancellationToken ct = default);
    Task<IReadOnlyList<QueryDefinition>> GetQueryDefinitionsAsync(string tenantCode, CancellationToken ct = default);
    Task<MappingProfile?> GetMappingProfileAsync(string tenantCode, string profileKey, CancellationToken ct = default);
    Task<ProcessDefinition?> GetProcessDefinitionAsync(string tenantCode, string processKey, CancellationToken ct = default);
    Task UpsertEntityAsync(MetadataEntity entity, CancellationToken ct = default);
    Task UpsertQueryDefinitionAsync(QueryDefinition definition, CancellationToken ct = default);
}
```

## 5.3 Metadata Cache Strategy

```csharp
// Janatics.DataEngine.CachingLayer/Strategies/MetadataCacheStrategy.cs
public sealed class MetadataCacheStrategy : IMetadataRepository
{
    private readonly IMetadataRepository _inner;
    private readonly ICacheProvider _cache;
    private readonly ILogger<MetadataCacheStrategy> _logger;

    public MetadataCacheStrategy(
        IMetadataRepository inner,
        ICacheProvider cache,
        ILogger<MetadataCacheStrategy> logger)
    {
        _inner = inner;
        _cache = cache;
        _logger = logger;
    }

    public async Task<MetadataEntity?> GetEntityAsync(
        string tenantCode, string entityKey, CancellationToken ct = default)
    {
        var cacheKey = MetadataCacheKey.ForEntity(tenantCode, entityKey);
        var cached = await _cache.GetAsync<MetadataEntity>(cacheKey, ct).ConfigureAwait(false);
        if (cached is not null)
            return cached;

        var entity = await _inner.GetEntityAsync(tenantCode, entityKey, ct).ConfigureAwait(false);
        if (entity is not null)
        {
            await _cache.SetAsync(cacheKey, entity, TimeSpan.FromMinutes(30), ct).ConfigureAwait(false);
        }

        return entity;
    }

    public async Task<QueryDefinition?> GetQueryDefinitionAsync(
        string tenantCode, string queryKey, CancellationToken ct = default)
    {
        var cacheKey = MetadataCacheKey.ForQuery(tenantCode, queryKey);
        var cached = await _cache.GetAsync<QueryDefinition>(cacheKey, ct).ConfigureAwait(false);
        if (cached is not null)
            return cached;

        var definition = await _inner.GetQueryDefinitionAsync(tenantCode, queryKey, ct).ConfigureAwait(false);
        if (definition is not null)
        {
            await _cache.SetAsync(cacheKey, definition, TimeSpan.FromMinutes(15), ct).ConfigureAwait(false);
        }

        return definition;
    }

    // Additional methods follow same cache-aside pattern
}
```

---

# 6. DYNAMIC FETCH ENGINE

## 6.1 Fetch Pipeline

```csharp
// Janatics.DataEngine.QueryEngine/Pipeline/FetchPipeline.cs
public sealed class FetchPipeline : IFetchPipeline
{
    private readonly IReadOnlyList<IFetchPipelineStage> _stages;
    private readonly ILogger<FetchPipeline> _logger;

    public FetchPipeline(
        IEnumerable<IFetchPipelineStage> stages,
        ILogger<FetchPipeline> logger)
    {
        _stages = stages.OrderBy(s => s.Order).ToList().AsReadOnly();
        _logger = logger;
    }

    public async Task<FetchPipelineResult> ExecuteAsync(
        FetchPipelineContext context, CancellationToken ct = default)
    {
        using var activity = DataEngineActivitySource.StartFetch(context.QueryKey, context.TenantCode);

        foreach (var stage in _stages)
        {
            _logger.LogDebug("Executing fetch stage {Stage} for query {QueryKey}",
                stage.GetType().Name, context.QueryKey);

            await stage.ExecuteAsync(context, ct).ConfigureAwait(false);

            if (context.IsTerminated)
                break;
        }

        return context.Result;
    }
}

// Janatics.DataEngine.QueryEngine/Pipeline/FetchPipelineContext.cs
public sealed class FetchPipelineContext
{
    public required string TenantCode { get; init; }
    public required string QueryKey { get; init; }
    public required IReadOnlyDictionary<string, object?> Parameters { get; init; }
    public PaginationOptions? Pagination { get; set; }
    public IReadOnlyList<SortOption>? Sorting { get; set; }
    public IReadOnlyList<FilterOption>? Filters { get; set; }

    // Resolved during pipeline
    public QueryDefinition? QueryDefinition { get; set; }
    public string? ResolvedSql { get; set; }
    public IReadOnlyDictionary<string, object?>? BoundParameters { get; set; }
    public IEnumerable<IDictionary<string, object?>>? RootResults { get; set; }
    public FetchPipelineResult Result { get; set; } = new();
    public bool IsTerminated { get; private set; }

    public void Terminate(string reason)
    {
        IsTerminated = true;
        Result.TerminationReason = reason;
    }
}
```

## 6.2 Query Resolution Stage

```csharp
// Janatics.DataEngine.QueryEngine/Stages/QueryResolutionStage.cs
public sealed class QueryResolutionStage : IFetchPipelineStage
{
    public int Order => 10;

    private readonly IMetadataRepository _metadata;

    public QueryResolutionStage(IMetadataRepository metadata)
        => _metadata = metadata;

    public async Task ExecuteAsync(FetchPipelineContext context, CancellationToken ct)
    {
        var definition = await _metadata.GetQueryDefinitionAsync(
            context.TenantCode, context.QueryKey, ct).ConfigureAwait(false);

        if (definition is null)
        {
            context.Terminate($"Query definition not found: {context.QueryKey}");
            return;
        }

        if (!definition.IsActive)
        {
            context.Terminate($"Query definition is inactive: {context.QueryKey}");
            return;
        }

        context.QueryDefinition = definition;
    }
}
```

## 6.3 Parameter Binding Stage

```csharp
// Janatics.DataEngine.QueryEngine/Stages/ParameterBindingStage.cs
public sealed class ParameterBindingStage : IFetchPipelineStage
{
    public int Order => 20;

    private readonly ISqlSanitizer _sanitizer;

    public ParameterBindingStage(ISqlSanitizer sanitizer) => _sanitizer = sanitizer;

    public Task ExecuteAsync(FetchPipelineContext context, CancellationToken ct)
    {
        var definition = context.QueryDefinition!;
        var boundParams = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var paramDef in definition.Parameters)
        {
            if (context.Parameters.TryGetValue(paramDef.ParameterKey, out var rawValue))
            {
                boundParams[paramDef.ParameterKey] = CoerceValue(rawValue, paramDef.DataType);
            }
            else if (paramDef.IsRequired)
            {
                context.Terminate($"Required parameter missing: {paramDef.ParameterKey}");
                return Task.CompletedTask;
            }
            else if (paramDef.DefaultValue is not null)
            {
                boundParams[paramDef.ParameterKey] = CoerceValue(paramDef.DefaultValue, paramDef.DataType);
            }
        }

        // Inject pagination as parameters if enabled
        if (definition.IsPaginationEnabled && context.Pagination is not null)
        {
            boundParams["__offset"] = context.Pagination.Offset;
            boundParams["__limit"] = context.Pagination.PageSize;
        }

        context.BoundParameters = boundParams.AsReadOnly();
        return Task.CompletedTask;
    }

    private static object? CoerceValue(object? value, FieldDataType targetType) => targetType switch
    {
        FieldDataType.Integer => value is null ? null : Convert.ToInt64(value),
        FieldDataType.Decimal => value is null ? null : Convert.ToDecimal(value),
        FieldDataType.Boolean => value is null ? null : Convert.ToBoolean(value),
        FieldDataType.DateTime => value is null ? null : Convert.ToDateTime(value),
        FieldDataType.String => value?.ToString(),
        FieldDataType.Guid => value is null ? null : Guid.Parse(value.ToString()!),
        _ => value
    };
}
```

## 6.4 Execution Stage

```csharp
// Janatics.DataEngine.QueryEngine/Execution/DapperQueryExecutor.cs
public sealed class DapperQueryExecutor : IDapperQueryExecutor
{
    private readonly IDbProviderFactory _providerFactory;
    private readonly IConnectionStringResolver _connectionResolver;
    private readonly ILogger<DapperQueryExecutor> _logger;

    public DapperQueryExecutor(
        IDbProviderFactory providerFactory,
        IConnectionStringResolver connectionResolver,
        ILogger<DapperQueryExecutor> logger)
    {
        _providerFactory = providerFactory;
        _connectionResolver = connectionResolver;
        _logger = logger;
    }

    public async Task<IEnumerable<IDictionary<string, object?>>> ExecuteAsync(
        QueryDefinition definition,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken ct = default)
    {
        var provider = _providerFactory.Resolve(definition.ProviderType);
        var connectionString = _connectionResolver.Resolve(definition.ConnectionName);

        await using var connection = (DbConnection)await provider
            .OpenReadConnectionAsync(connectionString, ct)
            .ConfigureAwait(false);

        var dynamicParams = new DynamicParameters();
        foreach (var (key, value) in parameters)
            dynamicParams.Add($"{provider.Dialect.ParameterPrefix}{key}", value);

        _logger.LogDebug("Executing query {QueryKey} on {Provider}",
            definition.QueryKey, definition.ProviderType);

        var results = await connection.QueryAsync(
            new CommandDefinition(
                definition.SqlTemplate,
                dynamicParams,
                commandTimeout: provider.Capabilities.DefaultCommandTimeoutSeconds,
                cancellationToken: ct))
            .ConfigureAwait(false);

        return results.Cast<IDictionary<string, object?>>().ToList();
    }
}
```

## 6.5 Child Collection Loader

```csharp
// Janatics.DataEngine.QueryEngine/Graph/ChildCollectionLoader.cs
public sealed class ChildCollectionLoader : IChildCollectionLoader
{
    private readonly IDapperQueryExecutor _executor;
    private readonly IMetadataRepository _metadata;

    public ChildCollectionLoader(IDapperQueryExecutor executor, IMetadataRepository metadata)
    {
        _executor = executor;
        _metadata = metadata;
    }

    public async Task<IReadOnlyDictionary<string, IEnumerable<IDictionary<string, object?>>>> LoadChildrenAsync(
        string tenantCode,
        QueryDefinition parentQuery,
        IEnumerable<IDictionary<string, object?>> rootRows,
        CancellationToken ct = default)
    {
        var result = new Dictionary<string, IEnumerable<IDictionary<string, object?>>>();

        foreach (var childDef in parentQuery.Children)
        {
            var childQueryDef = await _metadata.GetQueryDefinitionAsync(
                tenantCode, childDef.QueryKey, ct).ConfigureAwait(false);

            if (childQueryDef is null) continue;

            var parentIds = rootRows
                .Select(r => r.TryGetValue(childDef.ParentKeyField, out var v) ? v : null)
                .Where(v => v is not null)
                .Distinct()
                .ToList();

            var parameters = new Dictionary<string, object?>
            {
                [childDef.ChildForeignKeyParam] = parentIds
            };

            var childRows = await _executor.ExecuteAsync(childQueryDef, parameters, ct)
                .ConfigureAwait(false);

            result[childDef.CollectionAlias] = childRows;
        }

        return result.AsReadOnly();
    }
}
```

## 6.6 Object Graph Builder

```csharp
// Janatics.DataEngine.QueryEngine/Graph/ObjectGraphBuilder.cs
public sealed class ObjectGraphBuilder : IObjectGraphBuilder
{
    public IReadOnlyList<IDictionary<string, object?>> BuildGraph(
        IEnumerable<IDictionary<string, object?>> rootRows,
        IReadOnlyDictionary<string, IEnumerable<IDictionary<string, object?>>> childCollections,
        QueryDefinition definition)
    {
        var rootList = rootRows.ToList();

        foreach (var childDef in definition.Children)
        {
            if (!childCollections.TryGetValue(childDef.CollectionAlias, out var childRows))
                continue;

            var grouped = childRows
                .GroupBy(r => r.TryGetValue(childDef.ChildForeignKeyField, out var fk) ? fk : null)
                .Where(g => g.Key is not null)
                .ToDictionary(g => g.Key!, g => g.AsEnumerable());

            foreach (var root in rootList)
            {
                if (!root.TryGetValue(childDef.ParentKeyField, out var parentId))
                    continue;

                root[childDef.CollectionAlias] = grouped.TryGetValue(parentId!, out var children)
                    ? children.ToList()
                    : new List<IDictionary<string, object?>>();
            }
        }

        return rootList.AsReadOnly();
    }
}
```

---

# 7. QUERY STORAGE & EXECUTION MODEL

## 7.1 Query Definition Schema

```sql
-- EF Core-managed schema (simplified DDL representation)
CREATE TABLE de_query_definitions (
    id              CHAR(36)     NOT NULL PRIMARY KEY,
    tenant_code     VARCHAR(50)  NOT NULL,
    query_key       VARCHAR(200) NOT NULL,
    display_name    VARCHAR(500) NOT NULL,
    sql_template    TEXT         NOT NULL,
    root_entity_key VARCHAR(200) NOT NULL,
    provider_type   TINYINT      NOT NULL,
    connection_name VARCHAR(200) NOT NULL,
    is_pagination_enabled BOOLEAN NOT NULL DEFAULT FALSE,
    is_cacheable    BOOLEAN      NOT NULL DEFAULT FALSE,
    cache_ttl_seconds INT        NOT NULL DEFAULT 0,
    version         INT          NOT NULL DEFAULT 1,
    is_active       BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at      DATETIME     NOT NULL,
    created_by      VARCHAR(200) NOT NULL,
    updated_at      DATETIME     NULL,
    updated_by      VARCHAR(200) NULL,
    CONSTRAINT uq_query_tenant_key UNIQUE (tenant_code, query_key)
);

CREATE TABLE de_query_parameters (
    id              CHAR(36)     NOT NULL PRIMARY KEY,
    query_id        CHAR(36)     NOT NULL,
    parameter_key   VARCHAR(200) NOT NULL,
    display_name    VARCHAR(500) NOT NULL,
    data_type       TINYINT      NOT NULL,
    is_required     BOOLEAN      NOT NULL DEFAULT FALSE,
    default_value   VARCHAR(1000) NULL,
    sort_order      INT          NOT NULL DEFAULT 0,
    CONSTRAINT fk_qp_query FOREIGN KEY (query_id) REFERENCES de_query_definitions(id)
);

CREATE TABLE de_query_children (
    id                      CHAR(36)     NOT NULL PRIMARY KEY,
    parent_query_id         CHAR(36)     NOT NULL,
    child_query_key         VARCHAR(200) NOT NULL,
    collection_alias        VARCHAR(200) NOT NULL,
    parent_key_field        VARCHAR(200) NOT NULL,
    child_foreign_key_field VARCHAR(200) NOT NULL,
    child_fk_param_name     VARCHAR(200) NOT NULL,
    CONSTRAINT fk_qc_parent FOREIGN KEY (parent_query_id) REFERENCES de_query_definitions(id)
);
```

## 7.2 SQL Validation Service

```csharp
// Janatics.DataEngine.Security/Sanitization/AllowedOperationValidator.cs
public sealed class AllowedOperationValidator : IQueryValidator
{
    private static readonly HashSet<string> DeniedKeywords =
    [
        "DROP", "TRUNCATE", "DELETE", "UPDATE", "INSERT",
        "ALTER", "CREATE", "GRANT", "REVOKE", "EXEC",
        "EXECUTE", "sp_", "xp_", "--", "/*", "*/"
    ];

    private static readonly string[] AllowedStartKeywords = ["SELECT", "WITH"];

    public ValidationResult Validate(string sql)
    {
        var normalizedSql = sql.Trim().ToUpperInvariant();

        if (!AllowedStartKeywords.Any(kw => normalizedSql.StartsWith(kw, StringComparison.Ordinal)))
        {
            return ValidationResult.Failure("Query must start with SELECT or WITH clause.");
        }

        foreach (var denied in DeniedKeywords)
        {
            if (normalizedSql.Contains(denied, StringComparison.Ordinal))
            {
                return ValidationResult.Failure($"Disallowed keyword detected: {denied}");
            }
        }

        return ValidationResult.Success();
    }
}
```

## 7.3 Query Versioning Strategy

Every `QueryDefinition` carries an integer `Version` field. When a query is modified through the UI or API:

1. The current active version is soft-archived (IsActive = false)
2. A new record is inserted with Version incremented and IsActive = true
3. Execution always resolves the highest-version active definition per `QueryKey`
4. The cache key includes version, ensuring stale definitions are never served post-update

```csharp
// Version-aware cache key
public static string ForQuery(string tenantCode, string queryKey)
    => $"de:meta:query:{tenantCode}:{queryKey}:latest";

// On definition save, invalidate the cache entry
await _cache.DeleteAsync(MetadataCacheKey.ForQuery(tenantCode, queryKey), ct);
```

---

# 8. DYNAMIC TRANSACTION ENGINE

## 8.1 Transaction Payload Contract

```csharp
// Janatics.DataEngine.Contracts/Requests/TransactionRequest.cs
public sealed record TransactionRequest
{
    public required string TenantCode { get; init; }
    public required string RootEntity { get; init; }
    public object? RootId { get; init; }
    public required IDictionary<string, object?> Content { get; init; }
    public IReadOnlyList<TransactionNodeRequest> Nodes { get; init; } = [];
    public string? CorrelationId { get; init; }
    public string? InitiatedBy { get; init; }
}

public sealed record TransactionNodeRequest
{
    public required string NodeEntity { get; init; }
    public required string ParentLink { get; init; }
    public object? NodeId { get; init; }
    public bool IsDeleted { get; init; }
    public required IDictionary<string, object?> Content { get; init; }
    public IReadOnlyList<TransactionNodeRequest> Nodes { get; init; } = [];
}
```

## 8.2 Transaction Orchestrator

```csharp
// Janatics.DataEngine.TransactionEngine/Orchestration/TransactionOrchestrator.cs
public sealed class TransactionOrchestrator : ITransactionOrchestrator
{
    private readonly IMetadataRepository _metadata;
    private readonly INodeProcessor _nodeProcessor;
    private readonly IDataEngineUnitOfWork _unitOfWork;
    private readonly IAuditSink _auditSink;
    private readonly ILogger<TransactionOrchestrator> _logger;

    public TransactionOrchestrator(
        IMetadataRepository metadata,
        INodeProcessor nodeProcessor,
        IDataEngineUnitOfWork unitOfWork,
        IAuditSink auditSink,
        ILogger<TransactionOrchestrator> logger)
    {
        _metadata = metadata;
        _nodeProcessor = nodeProcessor;
        _unitOfWork = unitOfWork;
        _auditSink = auditSink;
        _logger = logger;
    }

    public async Task<TransactionResult> OrchestrateAsync(
        TransactionRequest request, CancellationToken ct = default)
    {
        using var activity = DataEngineActivitySource.StartTransaction(
            request.RootEntity, request.TenantCode);

        var entity = await _metadata.GetEntityAsync(
            request.TenantCode, request.RootEntity, ct).ConfigureAwait(false)
            ?? throw new MetadataNotFoundException(request.RootEntity);

        var transactionId = Guid.NewGuid();
        var results = new List<NodeProcessingResult>();

        try
        {
            await _unitOfWork.BeginAsync(ct).ConfigureAwait(false);

            // Process root node
            var rootResult = await _nodeProcessor.ProcessAsync(
                new NodeProcessingContext
                {
                    TenantCode = request.TenantCode,
                    EntityMetadata = entity,
                    NodeId = request.RootId,
                    Content = request.Content,
                    IsDeleted = false,
                    ParentKeyField = null,
                    ParentKeyValue = null,
                    TransactionId = transactionId,
                    InitiatedBy = request.InitiatedBy
                }, ct).ConfigureAwait(false);

            results.Add(rootResult);

            // Recursively process child nodes
            await ProcessChildNodesAsync(
                request.TenantCode,
                request.Nodes,
                rootResult.GeneratedId ?? request.RootId,
                transactionId,
                request.InitiatedBy,
                results, ct).ConfigureAwait(false);

            await _unitOfWork.CommitAsync(ct).ConfigureAwait(false);

            await _auditSink.RecordTransactionAsync(
                transactionId, request, results, ct).ConfigureAwait(false);

            return TransactionResult.Succeeded(transactionId, results);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync(ct).ConfigureAwait(false);
            _logger.LogError(ex, "Transaction orchestration failed for {Entity} [{TransactionId}]",
                request.RootEntity, transactionId);
            return TransactionResult.Failed(transactionId, ex.Message);
        }
    }

    private async Task ProcessChildNodesAsync(
        string tenantCode,
        IReadOnlyList<TransactionNodeRequest> nodes,
        object? parentId,
        Guid transactionId,
        string? initiatedBy,
        List<NodeProcessingResult> results,
        CancellationToken ct)
    {
        foreach (var node in nodes)
        {
            var childEntity = await _metadata.GetEntityAsync(
                tenantCode, node.NodeEntity, ct).ConfigureAwait(false)
                ?? throw new MetadataNotFoundException(node.NodeEntity);

            var childResult = await _nodeProcessor.ProcessAsync(
                new NodeProcessingContext
                {
                    TenantCode = tenantCode,
                    EntityMetadata = childEntity,
                    NodeId = node.NodeId,
                    Content = node.Content,
                    IsDeleted = node.IsDeleted,
                    ParentKeyField = node.ParentLink,
                    ParentKeyValue = parentId,
                    TransactionId = transactionId,
                    InitiatedBy = initiatedBy
                }, ct).ConfigureAwait(false);

            results.Add(childResult);

            if (node.Nodes.Count > 0)
            {
                await ProcessChildNodesAsync(
                    tenantCode, node.Nodes,
                    childResult.GeneratedId ?? node.NodeId,
                    transactionId, initiatedBy, results, ct).ConfigureAwait(false);
            }
        }
    }
}
```

## 8.3 Node Processor

```csharp
// Janatics.DataEngine.TransactionEngine/Orchestration/NodeProcessor.cs
public sealed class NodeProcessor : INodeProcessor
{
    private readonly IOperationDetector _operationDetector;
    private readonly IInsertCommandBuilder _insertBuilder;
    private readonly IUpdateCommandBuilder _updateBuilder;
    private readonly IDeleteCommandBuilder _deleteBuilder;
    private readonly IDbProviderFactory _providerFactory;
    private readonly IConnectionStringResolver _connectionResolver;
    private readonly IOptimisticConcurrencyHandler _concurrencyHandler;

    public NodeProcessor(
        IOperationDetector operationDetector,
        IInsertCommandBuilder insertBuilder,
        IUpdateCommandBuilder updateBuilder,
        IDeleteCommandBuilder deleteBuilder,
        IDbProviderFactory providerFactory,
        IConnectionStringResolver connectionResolver,
        IOptimisticConcurrencyHandler concurrencyHandler)
    {
        _operationDetector = operationDetector;
        _insertBuilder = insertBuilder;
        _updateBuilder = updateBuilder;
        _deleteBuilder = deleteBuilder;
        _providerFactory = providerFactory;
        _connectionResolver = connectionResolver;
        _concurrencyHandler = concurrencyHandler;
    }

    public async Task<NodeProcessingResult> ProcessAsync(
        NodeProcessingContext context, CancellationToken ct)
    {
        var operation = _operationDetector.Detect(context);
        var entity = context.EntityMetadata;
        var provider = _providerFactory.Resolve(entity.ProviderType);
        var connectionString = _connectionResolver.Resolve(entity.ConnectionName);

        var content = new Dictionary<string, object?>(context.Content,
            StringComparer.OrdinalIgnoreCase);

        // Inject parent FK if this is a child node
        if (context.ParentKeyField is not null && context.ParentKeyValue is not null)
            content[context.ParentKeyField] = context.ParentKeyValue;

        // Inject audit fields
        if (entity.UpdatedAtField is not null)
            content[entity.UpdatedAtField] = DateTime.UtcNow;

        if (operation == OperationType.Insert && entity.CreatedAtField is not null)
            content[entity.CreatedAtField] = DateTime.UtcNow;

        if (context.InitiatedBy is not null)
        {
            if (operation == OperationType.Insert && entity.CreatedByField is not null)
                content[entity.CreatedByField] = context.InitiatedBy;
            if (entity.UpdatedByField is not null)
                content[entity.UpdatedByField] = context.InitiatedBy;
        }

        await using var connection = (DbConnection)await provider
            .OpenConnectionAsync(connectionString, ct).ConfigureAwait(false);

        return operation switch
        {
            OperationType.Insert => await _insertBuilder.BuildAndExecuteAsync(
                entity, content, provider, connection, ct),
            OperationType.Update => await _updateBuilder.BuildAndExecuteAsync(
                entity, context.NodeId!, content, provider, connection,
                _concurrencyHandler, ct),
            OperationType.Delete => await _deleteBuilder.BuildAndExecuteAsync(
                entity, context.NodeId!, provider, connection, ct),
            _ => throw new InvalidOperationException($"Unknown operation type: {operation}")
        };
    }
}
```

## 8.4 Operation Detector

```csharp
// Janatics.DataEngine.TransactionEngine/Orchestration/OperationDetector.cs
public sealed class OperationDetector : IOperationDetector
{
    public OperationType Detect(NodeProcessingContext context)
    {
        if (context.IsDeleted)
            return OperationType.Delete;

        return context.NodeId is null || IsNullOrDefaultId(context.NodeId)
            ? OperationType.Insert
            : OperationType.Update;
    }

    private static bool IsNullOrDefaultId(object? id) => id switch
    {
        null => true,
        int i => i == 0,
        long l => l == 0,
        Guid g => g == Guid.Empty,
        string s => string.IsNullOrWhiteSpace(s),
        _ => false
    };
}
```

## 8.5 Insert Command Builder

```csharp
// Janatics.DataEngine.TransactionEngine/Commands/InsertCommandBuilder.cs
public sealed class InsertCommandBuilder : IInsertCommandBuilder
{
    public async Task<NodeProcessingResult> BuildAndExecuteAsync(
        MetadataEntity entity,
        IDictionary<string, object?> content,
        IDbProvider provider,
        IDbConnection connection,
        CancellationToken ct)
    {
        var dialect = provider.Dialect;
        var fieldMap = entity.Fields.ToDictionary(
            f => f.FieldKey, f => f, StringComparer.OrdinalIgnoreCase);

        // Filter to writable fields only
        var writableFields = content
            .Where(kv => fieldMap.TryGetValue(kv.Key, out var f)
                         && !f.IsReadOnly
                         && !f.IsSystemField
                         && kv.Key != entity.PrimaryKeyField)
            .ToList();

        var columnList = string.Join(", ",
            writableFields.Select(kv =>
                dialect.QuoteIdentifier(fieldMap[kv.Key].ColumnName)));

        var paramList = string.Join(", ",
            writableFields.Select(kv =>
                $"{dialect.ParameterPrefix}{kv.Key}"));

        var sql = $"INSERT INTO {dialect.QuoteIdentifier(entity.TableName)} ({columnList}) VALUES ({paramList})";

        var dynamicParams = new DynamicParameters();
        foreach (var (key, value) in writableFields)
            dynamicParams.Add($"{dialect.ParameterPrefix}{key}", value);

        var generatedId = await connection.ExecuteScalarAsync<object?>(
            new CommandDefinition(
                $"{sql}; {dialect.LastInsertedIdExpression}",
                dynamicParams,
                cancellationToken: ct)).ConfigureAwait(false);

        return NodeProcessingResult.Inserted(entity.EntityKey, generatedId);
    }
}
```

## 8.6 Unit of Work

```csharp
// Janatics.DataEngine.TransactionEngine/UnitOfWork/DataEngineUnitOfWork.cs
public sealed class DataEngineUnitOfWork : IDataEngineUnitOfWork, IAsyncDisposable
{
    private readonly IDbConnection _connection;
    private IDbTransaction? _transaction;

    public DataEngineUnitOfWork(IDbConnection connection)
        => _connection = connection;

    public async Task BeginAsync(CancellationToken ct = default)
    {
        if (_connection is DbConnection dbConn)
        {
            _transaction = await dbConn.BeginTransactionAsync(ct).ConfigureAwait(false);
        }
        else
        {
            _transaction = _connection.BeginTransaction();
        }
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction is DbTransaction dbTx)
            await dbTx.CommitAsync(ct).ConfigureAwait(false);
        else
            _transaction?.Commit();
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction is DbTransaction dbTx)
            await dbTx.RollbackAsync(ct).ConfigureAwait(false);
        else
            _transaction?.Rollback();
    }

    public IDbTransaction? CurrentTransaction => _transaction;

    public async ValueTask DisposeAsync()
    {
        if (_transaction is IAsyncDisposable adt)
            await adt.DisposeAsync().ConfigureAwait(false);
        else
            _transaction?.Dispose();

        if (_connection is IAsyncDisposable adc)
            await adc.DisposeAsync().ConfigureAwait(false);
        else
            _connection.Dispose();
    }
}
```

---

# 9. PROCESS ORCHESTRATION ENGINE

## 9.1 Process Definition Model

```csharp
// Janatics.DataEngine.Domain/Entities/ProcessDefinition.cs
public sealed class ProcessDefinition
{
    public Guid Id { get; init; }
    public string TenantCode { get; init; } = default!;
    public string ProcessKey { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public bool IsTransactional { get; init; }
    public string? InputEntityKey { get; init; }
    public string? OutputEntityKey { get; init; }

    public IReadOnlyList<ProcessStageDefinition> Stages { get; init; } = [];
    public IReadOnlyList<ProcessHookDefinition> Hooks { get; init; } = [];
}

public sealed class ProcessStageDefinition
{
    public Guid Id { get; init; }
    public Guid ProcessId { get; init; }
    public ProcessStageType StageType { get; init; }
    public int ExecutionOrder { get; init; }
    public string? StageKey { get; init; }
    public string? QueryKey { get; init; }
    public string? MappingProfileKey { get; init; }
    public string? ValidationSetKey { get; init; }
    public bool ContinueOnFailure { get; init; }
    public IDictionary<string, string> Configuration { get; init; } = new Dictionary<string, string>();
}
```

## 9.2 Process Executor

```csharp
// Janatics.DataEngine.OrchestrationEngine/Process/ProcessExecutor.cs
public sealed class ProcessExecutor : IProcessExecutor
{
    private readonly IMetadataRepository _metadata;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHookRegistry _hookRegistry;
    private readonly ILogger<ProcessExecutor> _logger;

    public ProcessExecutor(
        IMetadataRepository metadata,
        IServiceProvider serviceProvider,
        IHookRegistry hookRegistry,
        ILogger<ProcessExecutor> logger)
    {
        _metadata = metadata;
        _serviceProvider = serviceProvider;
        _hookRegistry = hookRegistry;
        _logger = logger;
    }

    public async Task<ProcessExecutionResult> ExecuteAsync(
        ProcessExecutionRequest request, CancellationToken ct = default)
    {
        var definition = await _metadata.GetProcessDefinitionAsync(
            request.TenantCode, request.ProcessKey, ct).ConfigureAwait(false)
            ?? throw new MetadataNotFoundException(request.ProcessKey);

        var context = new ProcessContext
        {
            TenantCode = request.TenantCode,
            ProcessKey = request.ProcessKey,
            ExecutionId = Guid.NewGuid(),
            InputPayload = request.Payload,
            InitiatedBy = request.InitiatedBy,
            StartedAt = DateTime.UtcNow
        };

        // Execute pre-process hooks
        foreach (var hook in _hookRegistry.GetPreHooks(request.ProcessKey))
        {
            await hook.ExecuteAsync(context, ct).ConfigureAwait(false);
            if (context.IsAborted) return ProcessExecutionResult.Aborted(context);
        }

        // Execute stages in order
        foreach (var stageDef in definition.Stages.OrderBy(s => s.ExecutionOrder))
        {
            var stage = ResolveStage(stageDef.StageType);

            _logger.LogInformation(
                "Executing process stage {StageType} [{Order}] for process {ProcessKey}",
                stageDef.StageType, stageDef.ExecutionOrder, request.ProcessKey);

            var stageResult = await stage.ExecuteAsync(context, stageDef, ct).ConfigureAwait(false);

            context.StageResults.Add(stageDef.StageType, stageResult);

            if (!stageResult.IsSuccess && !stageDef.ContinueOnFailure)
            {
                context.Abort($"Stage {stageDef.StageType} failed: {stageResult.ErrorMessage}");
                break;
            }
        }

        // Execute post-process hooks
        if (!context.IsAborted)
        {
            foreach (var hook in _hookRegistry.GetPostHooks(request.ProcessKey))
                await hook.ExecuteAsync(context, ct).ConfigureAwait(false);
        }

        context.CompletedAt = DateTime.UtcNow;
        return ProcessExecutionResult.FromContext(context);
    }

    private IProcessStage ResolveStage(ProcessStageType type) =>
        _serviceProvider.GetRequiredKeyedService<IProcessStage>(type.ToString());
}
```

---

# 10. FIELD MAPPING ENGINE

## 10.1 Mapping Profile Model

```csharp
// Janatics.DataEngine.Domain/Entities/MappingProfile.cs
public sealed class MappingProfile
{
    public Guid Id { get; init; }
    public string TenantCode { get; init; } = default!;
    public string ProfileKey { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
    public string SourceEntityKey { get; init; } = default!;
    public string TargetEntityKey { get; init; } = default!;
    public bool IsActive { get; init; }

    public IReadOnlyList<MappingRule> Rules { get; init; } = [];
}

public sealed class MappingRule
{
    public Guid Id { get; init; }
    public Guid ProfileId { get; init; }
    public string SourceFieldKey { get; init; } = default!;
    public string TargetFieldKey { get; init; } = default!;
    public MappingTransformType TransformType { get; init; }
    public string? TransformExpression { get; init; }
    public string? ConditionExpression { get; init; }
    public bool IsRequired { get; init; }
    public string? DefaultValue { get; init; }
    public int SortOrder { get; init; }
}
```

## 10.2 Mapping Pipeline

```csharp
// Janatics.DataEngine.MappingEngine/Pipeline/MappingPipeline.cs
public sealed class MappingPipeline : IMappingPipeline
{
    private readonly IEnumerable<IFieldTransformer> _transformers;
    private readonly IMappingProfileResolver _profileResolver;
    private readonly ILogger<MappingPipeline> _logger;

    public MappingPipeline(
        IEnumerable<IFieldTransformer> transformers,
        IMappingProfileResolver profileResolver,
        ILogger<MappingPipeline> logger)
    {
        _transformers = transformers;
        _profileResolver = profileResolver;
        _logger = logger;
    }

    public async Task<IDictionary<string, object?>> TransformAsync(
        string tenantCode,
        string profileKey,
        IDictionary<string, object?> source,
        CancellationToken ct = default)
    {
        var profile = await _profileResolver.ResolveAsync(tenantCode, profileKey, ct)
            .ConfigureAwait(false);

        var target = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var context = new TransformationContext { Source = source, Target = target };

        foreach (var rule in profile.Rules.OrderBy(r => r.SortOrder))
        {
            if (!EvaluateCondition(rule, context)) continue;

            object? sourceValue = null;
            if (!source.TryGetValue(rule.SourceFieldKey, out sourceValue) || sourceValue is null)
            {
                if (rule.IsRequired)
                    throw new MappingException($"Required source field missing: {rule.SourceFieldKey}");

                if (rule.DefaultValue is not null)
                    target[rule.TargetFieldKey] = rule.DefaultValue;

                continue;
            }

            var transformer = _transformers.FirstOrDefault(
                t => t.CanHandle(rule.TransformType));

            target[rule.TargetFieldKey] = transformer is not null
                ? await transformer.TransformAsync(sourceValue, rule, context, ct).ConfigureAwait(false)
                : sourceValue;
        }

        return target;
    }

    private static bool EvaluateCondition(MappingRule rule, TransformationContext context)
    {
        if (string.IsNullOrWhiteSpace(rule.ConditionExpression)) return true;
        // Expression evaluation via compiled expression cache (SimpleExpressionEvaluator)
        return SimpleExpressionEvaluator.Evaluate(rule.ConditionExpression, context.Source);
    }
}
```

## 10.3 Transformer Implementations

```csharp
// Janatics.DataEngine.MappingEngine/Transformers/StringTransformer.cs
public sealed class StringTransformer : IFieldTransformer
{
    public bool CanHandle(MappingTransformType type) =>
        type is MappingTransformType.ToUpperCase
            or MappingTransformType.ToLowerCase
            or MappingTransformType.Trim
            or MappingTransformType.StringFormat;

    public ValueTask<object?> TransformAsync(
        object? value, MappingRule rule, TransformationContext context, CancellationToken ct)
    {
        var str = value?.ToString();
        if (str is null) return ValueTask.FromResult<object?>(null);

        var result = rule.TransformType switch
        {
            MappingTransformType.ToUpperCase => str.ToUpperInvariant(),
            MappingTransformType.ToLowerCase => str.ToLowerInvariant(),
            MappingTransformType.Trim => str.Trim(),
            MappingTransformType.StringFormat when rule.TransformExpression is not null
                => string.Format(rule.TransformExpression, str),
            _ => str
        };

        return ValueTask.FromResult<object?>(result);
    }
}

// Janatics.DataEngine.MappingEngine/Transformers/ComputedFieldTransformer.cs
public sealed class ComputedFieldTransformer : IFieldTransformer
{
    private readonly IComputedFieldEvaluator _evaluator;

    public ComputedFieldTransformer(IComputedFieldEvaluator evaluator)
        => _evaluator = evaluator;

    public bool CanHandle(MappingTransformType type) =>
        type == MappingTransformType.Computed;

    public async ValueTask<object?> TransformAsync(
        object? value, MappingRule rule, TransformationContext context, CancellationToken ct)
    {
        if (rule.TransformExpression is null) return value;
        return await _evaluator.EvaluateAsync(rule.TransformExpression, context, ct)
            .ConfigureAwait(false);
    }
}
```

---

# 11. SQL QUERY BUILDER UI ARCHITECTURE

## 11.1 React Architecture Overview

The Query Builder UI is a standalone React TypeScript feature module within the `janatics-dataengine-console` application. It implements a visual, drag-and-drop SQL construction experience backed by a live preview panel and real-time API execution capability.

## 11.2 Component Architecture

```
features/query-builder/
├── QueryBuilderPage.tsx           # Feature root, layout orchestration
├── components/
│   ├── TableSelector/
│   │   ├── TableSelector.tsx      # Schema browser with search
│   │   └── TableSelectorItem.tsx  # Individual table entry
│   ├── JoinBuilder/
│   │   ├── JoinBuilder.tsx        # Join definition canvas
│   │   ├── JoinLine.tsx           # Visual join connector
│   │   └── JoinConditionEditor.tsx
│   ├── FieldSelector/
│   │   ├── FieldSelector.tsx      # Available fields panel
│   │   └── SelectedField.tsx      # Draggable selected field token
│   ├── FilterBuilder/
│   │   ├── FilterBuilder.tsx      # WHERE clause builder
│   │   ├── FilterGroup.tsx        # AND/OR grouped conditions
│   │   └── FilterCondition.tsx    # Individual condition row
│   ├── SortBuilder/
│   │   └── SortBuilder.tsx        # ORDER BY clause builder
│   ├── AggregationBuilder/
│   │   └── AggregationBuilder.tsx # GROUP BY + aggregate functions
│   ├── ParameterBuilder/
│   │   ├── ParameterBuilder.tsx   # Named parameter definition
│   │   └── ParameterRow.tsx
│   ├── PaginationConfig/
│   │   └── PaginationConfig.tsx   # Limit/offset configuration
│   ├── SqlPreview/
│   │   ├── SqlPreview.tsx         # Generated SQL with syntax highlighting
│   │   └── SqlCopyButton.tsx
│   ├── ResultPreview/
│   │   ├── ResultPreview.tsx      # Live execution result grid
│   │   └── ResultGrid.tsx
│   └── QuerySaveDialog/
│       └── QuerySaveDialog.tsx    # Save/version dialog
├── hooks/
│   ├── useQueryBuilder.ts         # Core builder state machine
│   ├── useSchemaIntrospection.ts  # API hook for table/field discovery
│   ├── useSqlPreview.ts           # Debounced SQL generation
│   └── useQueryExecution.ts      # TanStack Query for live execution
├── stores/
│   └── queryBuilderStore.ts      # Zustand store for builder state
├── types/
│   ├── QueryBuilderState.ts
│   ├── JoinDefinition.ts
│   ├── FilterDefinition.ts
│   └── FieldSelection.ts
└── api/
    └── queryBuilderApi.ts         # API client functions
```

## 11.3 Query Builder State Management

```typescript
// features/query-builder/stores/queryBuilderStore.ts
import { create } from 'zustand';
import { immer } from 'zustand/middleware/immer';

interface QueryBuilderState {
  selectedTables: SelectedTable[];
  joins: JoinDefinition[];
  selectedFields: FieldSelection[];
  filters: FilterGroup[];
  sorts: SortDefinition[];
  aggregations: AggregationDefinition[];
  parameters: ParameterDefinition[];
  pagination: PaginationConfig;
  generatedSql: string;
  isDirty: boolean;
}

interface QueryBuilderActions {
  addTable: (table: SchemaTable) => void;
  removeTable: (tableAlias: string) => void;
  addJoin: (join: JoinDefinition) => void;
  updateJoin: (index: number, join: Partial<JoinDefinition>) => void;
  removeJoin: (index: number) => void;
  addField: (field: FieldSelection) => void;
  removeField: (fieldAlias: string) => void;
  reorderFields: (from: number, to: number) => void;
  addFilter: (filter: FilterCondition) => void;
  updateFilter: (path: FilterPath, condition: Partial<FilterCondition>) => void;
  removeFilter: (path: FilterPath) => void;
  addSort: (sort: SortDefinition) => void;
  updateSort: (index: number, sort: Partial<SortDefinition>) => void;
  removeSort: (index: number) => void;
  addParameter: (param: ParameterDefinition) => void;
  removeParameter: (key: string) => void;
  setPagination: (config: Partial<PaginationConfig>) => void;
  setGeneratedSql: (sql: string) => void;
  reset: () => void;
}

export const useQueryBuilderStore = create<QueryBuilderState & QueryBuilderActions>()(
  immer((set) => ({
    selectedTables: [],
    joins: [],
    selectedFields: [],
    filters: [],
    sorts: [],
    aggregations: [],
    parameters: [],
    pagination: { enabled: false, pageSize: 50, offset: 0 },
    generatedSql: '',
    isDirty: false,

    addTable: (table) => set((state) => {
      state.selectedTables.push({
        ...table,
        alias: generateUniqueAlias(table.tableName, state.selectedTables)
      });
      state.isDirty = true;
    }),

    removeTable: (tableAlias) => set((state) => {
      state.selectedTables = state.selectedTables.filter(t => t.alias !== tableAlias);
      state.joins = state.joins.filter(
        j => j.leftTableAlias !== tableAlias && j.rightTableAlias !== tableAlias
      );
      state.selectedFields = state.selectedFields.filter(f => f.tableAlias !== tableAlias);
      state.isDirty = true;
    }),

    addJoin: (join) => set((state) => {
      state.joins.push(join);
      state.isDirty = true;
    }),

    // ... additional implementations
    reset: () => set(() => ({
      selectedTables: [], joins: [], selectedFields: [], filters: [],
      sorts: [], aggregations: [], parameters: [],
      pagination: { enabled: false, pageSize: 50, offset: 0 },
      generatedSql: '', isDirty: false
    }))
  }))
);
```

## 11.4 SQL Preview Hook

```typescript
// features/query-builder/hooks/useSqlPreview.ts
import { useEffect, useDeferredValue } from 'react';
import { useMutation } from '@tanstack/react-query';
import { useQueryBuilderStore } from '../stores/queryBuilderStore';
import { queryBuilderApi } from '../api/queryBuilderApi';

export function useSqlPreview() {
  const state = useQueryBuilderStore();
  const setGeneratedSql = useQueryBuilderStore(s => s.setGeneratedSql);

  const deferredState = useDeferredValue({
    tables: state.selectedTables,
    joins: state.joins,
    fields: state.selectedFields,
    filters: state.filters,
    sorts: state.sorts,
    aggregations: state.aggregations,
    pagination: state.pagination,
  });

  const { mutate, isPending } = useMutation({
    mutationFn: queryBuilderApi.generateSql,
    onSuccess: (result) => setGeneratedSql(result.sql),
  });

  useEffect(() => {
    if (deferredState.tables.length === 0) {
      setGeneratedSql('');
      return;
    }
    mutate(deferredState);
  }, [deferredState, mutate, setGeneratedSql]);

  return { isGenerating: isPending };
}
```

---

# 12. FIELD MAPPING UI

## 12.1 Mapping Designer Architecture

```
features/mapping-designer/
├── MappingDesignerPage.tsx
├── components/
│   ├── SourcePanel/
│   │   ├── SourcePanel.tsx            # Left panel: source entity fields
│   │   ├── SourceEntitySelector.tsx   # Entity picker
│   │   └── SourceFieldList.tsx        # Draggable field list
│   ├── TargetPanel/
│   │   ├── TargetPanel.tsx            # Right panel: target entity fields
│   │   ├── TargetEntitySelector.tsx
│   │   └── TargetFieldSlot.tsx        # Drop target for field binding
│   ├── MappingCanvas/
│   │   ├── MappingCanvas.tsx          # Center: visual connector canvas
│   │   ├── MappingLine.tsx            # SVG connector between fields
│   │   └── MappingLineDraggable.tsx
│   ├── RuleEditor/
│   │   ├── RuleEditor.tsx             # Transformation rule editor panel
│   │   ├── TransformTypeSelector.tsx
│   │   └── ExpressionEditor.tsx       # Monaco editor for expressions
│   ├── AutoMapSuggestions/
│   │   └── AutoMapSuggestions.tsx     # AI-assisted auto-mapping panel
│   ├── MappingPreview/
│   │   └── MappingPreview.tsx         # Live transformation preview
│   └── ValidationConfig/
│       └── ValidationConfig.tsx       # Per-mapping validation rules
├── hooks/
│   ├── useMappingDesigner.ts
│   ├── useSchemaIntrospection.ts
│   ├── useAutoMapSuggestions.ts
│   └── useMappingPreview.ts
└── stores/
    └── mappingDesignerStore.ts
```

## 12.2 Auto-Mapping Suggestion Engine

```typescript
// features/mapping-designer/hooks/useAutoMapSuggestions.ts
import { useMutation } from '@tanstack/react-query';
import { mappingApi } from '../api/mappingApi';

export function useAutoMapSuggestions() {
  const { mutate, data, isPending } = useMutation({
    mutationFn: ({ sourceEntityKey, targetEntityKey }: AutoMapRequest) =>
      mappingApi.suggestAutoMappings(sourceEntityKey, targetEntityKey),
  });

  return {
    suggest: mutate,
    suggestions: data?.suggestions ?? [],
    isLoading: isPending,
  };
}
```

---

# 13. SECURITY ARCHITECTURE

## 13.1 SQL Sanitizer

```csharp
// Janatics.DataEngine.Security/Sanitization/SqlSanitizer.cs
public sealed class SqlSanitizer : ISqlSanitizer
{
    private static readonly Regex CommentPattern =
        new(@"(--[^\r\n]*|/\*[\s\S]*?\*/)", RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Regex MultipleStatementsPattern =
        new(@";\s*(?!$)", RegexOptions.Compiled);

    public SanitizationResult Sanitize(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return SanitizationResult.Invalid("SQL template is empty.");

        var stripped = CommentPattern.Replace(sql, " ").Trim();

        if (MultipleStatementsPattern.IsMatch(stripped))
            return SanitizationResult.Invalid(
                "Multiple SQL statements detected. Only single-statement queries are permitted.");

        return SanitizationResult.Valid(stripped);
    }
}
```

## 13.2 Query Permission Gate

```csharp
// Janatics.DataEngine.Security/Permissions/QueryPermissionGate.cs
public sealed class QueryPermissionGate : IQueryPermissionGate
{
    private readonly IPermissionService _permissionService;

    public QueryPermissionGate(IPermissionService permissionService)
        => _permissionService = permissionService;

    public async Task<PermissionResult> AuthorizeAsync(
        string tenantCode, string queryKey, string userId, CancellationToken ct = default)
    {
        var permission = await _permissionService.GetQueryPermissionAsync(
            tenantCode, queryKey, userId, ct).ConfigureAwait(false);

        return permission switch
        {
            QueryPermission.Allowed => PermissionResult.Granted(),
            QueryPermission.Denied => PermissionResult.Denied($"User {userId} is not authorized to execute query: {queryKey}"),
            QueryPermission.NotFound => PermissionResult.Denied($"Query {queryKey} not found or not accessible."),
            _ => PermissionResult.Denied("Unknown permission state.")
        };
    }
}
```

## 13.3 API Security Configuration

```csharp
// API layer security registration
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = configuration["Auth:Authority"];
        options.Audience = configuration["Auth:Audience"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

services.AddAuthorization(options =>
{
    options.AddPolicy("QueryExecution", policy =>
        policy.RequireClaim("de:permission", "query:execute"));

    options.AddPolicy("TransactionWrite", policy =>
        policy.RequireClaim("de:permission", "transaction:write"));

    options.AddPolicy("MetadataAdmin", policy =>
        policy.RequireClaim("de:permission", "metadata:admin"));
});
```

---

# 14. HIGH-PERFORMANCE EXECUTION STRATEGY

## 14.1 Async Execution Discipline

Every operation in the execution path must comply with the following rules:

- All I/O operations use `async`/`await` with `ConfigureAwait(false)` in library code
- No blocking calls (`Task.Result`, `.Wait()`, `.GetAwaiter().GetResult()`) are permitted
- `CancellationToken` is threaded through every async method signature
- Database commands use `CommandDefinition` with `CancellationToken` binding
- Connection disposal uses `await using` to ensure proper async cleanup

## 14.2 Dapper Performance Optimizations

```csharp
// Typed column mapping via SqlMapper for zero-reflection field binding
public static class DapperTypeMapRegistration
{
    public static void RegisterMappings(IEnumerable<MetadataEntity> entities)
    {
        foreach (var entity in entities)
        {
            SqlMapper.SetTypeMap(
                typeof(IDictionary<string, object?>),
                new DynamicTypeMap());
        }
    }
}

// Custom type handler for JSON columns
public sealed class JsonTypeHandler<T> : SqlMapper.TypeHandler<T>
{
    public override void SetValue(IDbDataParameter parameter, T? value)
        => parameter.Value = value is null
            ? DBNull.Value
            : JsonSerializer.Serialize(value, JsonDefaults.Options);

    public override T? Parse(object value)
        => value is DBNull or null
            ? default
            : JsonSerializer.Deserialize<T>(value.ToString()!, JsonDefaults.Options);
}
```

## 14.3 Connection Pool Configuration

```csharp
// MySQL connection pool configuration
var mySqlConnectionString = new MySqlConnectionStringBuilder(rawConnectionString)
{
    MaximumPoolSize = 100,
    MinimumPoolSize = 5,
    ConnectionTimeout = 15,
    DefaultCommandTimeout = 30,
    AllowPublicKeyRetrieval = true,
    SslMode = MySqlSslMode.Required,
    Pooling = true
}.ToString();
```

## 14.4 Streaming Large Result Sets

```csharp
// Janatics.DataEngine.QueryEngine/Execution/StreamingQueryExecutor.cs
public sealed class StreamingQueryExecutor : IStreamingQueryExecutor
{
    private readonly IDbProviderFactory _providerFactory;
    private readonly IConnectionStringResolver _connectionResolver;

    public async IAsyncEnumerable<IDictionary<string, object?>> StreamAsync(
        QueryDefinition definition,
        IReadOnlyDictionary<string, object?> parameters,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var provider = _providerFactory.Resolve(definition.ProviderType);
        var connectionString = _connectionResolver.Resolve(definition.ConnectionName);

        await using var connection = (DbConnection)await provider
            .OpenReadConnectionAsync(connectionString, ct)
            .ConfigureAwait(false);

        var dynamicParams = new DynamicParameters();
        foreach (var (key, value) in parameters)
            dynamicParams.Add(key, value);

        var reader = await connection.ExecuteReaderAsync(
            new CommandDefinition(definition.SqlTemplate, dynamicParams, cancellationToken: ct))
            .ConfigureAwait(false);

        await using (reader)
        {
            while (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                var row = new Dictionary<string, object?>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < reader.FieldCount; i++)
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);

                yield return row;
            }
        }
    }
}
```

---

# 15. CACHING ARCHITECTURE

## 15.1 Cache Provider Abstraction

```csharp
// Janatics.DataEngine.CachingLayer/ICacheProvider.cs
public interface ICacheProvider
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
    Task DeleteByPatternAsync(string pattern, CancellationToken ct = default);
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
}

// Janatics.DataEngine.CachingLayer/Providers/RedisCacheProvider.cs
public sealed class RedisCacheProvider : ICacheProvider
{
    private readonly IDatabase _database;
    private readonly IServer _server;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisCacheProvider(IConnectionMultiplexer redis, JsonSerializerOptions jsonOptions)
    {
        _database = redis.GetDatabase();
        _server = redis.GetServer(redis.GetEndPoints().First());
        _jsonOptions = jsonOptions;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var value = await _database.StringGetAsync(key).ConfigureAwait(false);
        if (value.IsNullOrEmpty) return default;
        return JsonSerializer.Deserialize<T>(value!, _jsonOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
    {
        var serialized = JsonSerializer.Serialize(value, _jsonOptions);
        await _database.StringSetAsync(key, serialized, ttl).ConfigureAwait(false);
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
        => _database.KeyDeleteAsync(key).AsTask();

    public async Task DeleteByPatternAsync(string pattern, CancellationToken ct = default)
    {
        var keys = _server.Keys(pattern: pattern).ToArray();
        if (keys.Length > 0)
            await _database.KeyDeleteAsync(keys).ConfigureAwait(false);
    }

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
        => _database.KeyExistsAsync(key).AsTask();
}
```

## 15.2 Cache Key Conventions

```csharp
// Janatics.DataEngine.CachingLayer/Keys/MetadataCacheKey.cs
public static class MetadataCacheKey
{
    private const string Prefix = "de:meta";

    public static string ForEntity(string tenant, string entityKey)
        => $"{Prefix}:entity:{tenant}:{entityKey}";

    public static string ForAllEntities(string tenant)
        => $"{Prefix}:entities:{tenant}:all";

    public static string ForQuery(string tenant, string queryKey)
        => $"{Prefix}:query:{tenant}:{queryKey}";

    public static string ForProcess(string tenant, string processKey)
        => $"{Prefix}:process:{tenant}:{processKey}";

    public static string ForMappingProfile(string tenant, string profileKey)
        => $"{Prefix}:mapping:{tenant}:{profileKey}";

    public static string TenantPattern(string tenant)
        => $"{Prefix}:*:{tenant}:*";
}
```

## 15.3 Multi-Tenant Cache Invalidation

```csharp
// Janatics.DataEngine.CachingLayer/Strategies/InvalidationStrategy.cs
public sealed class InvalidationStrategy : IInvalidationStrategy
{
    private readonly ICacheProvider _cache;

    public InvalidationStrategy(ICacheProvider cache) => _cache = cache;

    public Task InvalidateTenantMetadataAsync(string tenantCode, CancellationToken ct = default)
        => _cache.DeleteByPatternAsync(MetadataCacheKey.TenantPattern(tenantCode), ct);

    public Task InvalidateEntityAsync(string tenantCode, string entityKey, CancellationToken ct = default)
        => _cache.DeleteAsync(MetadataCacheKey.ForEntity(tenantCode, entityKey), ct);

    public Task InvalidateQueryAsync(string tenantCode, string queryKey, CancellationToken ct = default)
        => _cache.DeleteAsync(MetadataCacheKey.ForQuery(tenantCode, queryKey), ct);
}
```

---

# 16. VALIDATION ENGINE

## 16.1 Dynamic Validator Builder

```csharp
// Janatics.DataEngine.ValidationEngine/Builders/DynamicValidatorBuilder.cs
public sealed class DynamicValidatorBuilder : IDynamicValidatorBuilder
{
    private readonly IMetadataRepository _metadata;
    private readonly IRuleFactoryRegistry _ruleRegistry;

    public DynamicValidatorBuilder(
        IMetadataRepository metadata,
        IRuleFactoryRegistry ruleRegistry)
    {
        _metadata = metadata;
        _ruleRegistry = ruleRegistry;
    }

    public async Task<IValidator<IDictionary<string, object?>>> BuildAsync(
        string tenantCode, string entityKey, string? validationSetKey = null,
        CancellationToken ct = default)
    {
        var entity = await _metadata.GetEntityAsync(tenantCode, entityKey, ct)
            .ConfigureAwait(false)
            ?? throw new MetadataNotFoundException(entityKey);

        var rules = await _metadata.GetValidationRulesAsync(
            tenantCode, entityKey, validationSetKey, ct)
            .ConfigureAwait(false);

        return new DynamicEntityValidator(entity, rules, _ruleRegistry);
    }
}

// Dynamic validator implementation
public sealed class DynamicEntityValidator : AbstractValidator<IDictionary<string, object?>>
{
    public DynamicEntityValidator(
        MetadataEntity entity,
        IReadOnlyList<ValidationRule> rules,
        IRuleFactoryRegistry ruleRegistry)
    {
        foreach (var rule in rules.OrderBy(r => r.SortOrder))
        {
            var field = entity.Fields.FirstOrDefault(
                f => f.FieldKey.Equals(rule.FieldKey, StringComparison.OrdinalIgnoreCase));

            if (field is null) continue;

            var factory = ruleRegistry.Resolve(rule.RuleType);
            factory?.ApplyRule(this, rule, field);
        }
    }
}
```

## 16.2 Rule Factory Registry

```csharp
// Rule factory for Required validation
public sealed class RequiredRuleFactory : IValidationRuleFactory
{
    public ValidationRuleType RuleType => ValidationRuleType.Required;

    public void ApplyRule(
        AbstractValidator<IDictionary<string, object?>> validator,
        ValidationRule rule,
        MetadataField field)
    {
        validator.RuleFor(d => d.GetValueOrDefault(field.FieldKey))
            .NotNull()
            .WithMessage(rule.ErrorMessage ?? $"{field.DisplayName} is required.")
            .WithName(field.FieldKey);
    }
}

// Rule factory for MaxLength validation
public sealed class MaxLengthRuleFactory : IValidationRuleFactory
{
    public ValidationRuleType RuleType => ValidationRuleType.MaxLength;

    public void ApplyRule(
        AbstractValidator<IDictionary<string, object?>> validator,
        ValidationRule rule,
        MetadataField field)
    {
        var maxLength = field.MaxLength ?? int.Parse(rule.RuleValue ?? "255");

        validator.RuleFor(d => d.GetValueOrDefault(field.FieldKey)?.ToString())
            .MaximumLength(maxLength)
            .When(d => d.ContainsKey(field.FieldKey) && d[field.FieldKey] is not null)
            .WithMessage(rule.ErrorMessage ?? $"{field.DisplayName} must not exceed {maxLength} characters.")
            .WithName(field.FieldKey);
    }
}
```

---

# 17. OBSERVABILITY & TELEMETRY

## 17.1 Activity Source

```csharp
// Janatics.DataEngine.Telemetry/Tracing/DataEngineActivitySource.cs
public static class DataEngineActivitySource
{
    public static readonly ActivitySource Source =
        new("Janatics.DataEngine", Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0");

    public static Activity? StartFetch(string queryKey, string tenantCode)
        => Source.StartActivity("DataEngine.Fetch", ActivityKind.Internal)
            ?.SetTag("de.query.key", queryKey)
            ?.SetTag("de.tenant.code", tenantCode)
            ?.SetTag("de.operation.type", "fetch");

    public static Activity? StartTransaction(string entityKey, string tenantCode)
        => Source.StartActivity("DataEngine.Transaction", ActivityKind.Internal)
            ?.SetTag("de.entity.key", entityKey)
            ?.SetTag("de.tenant.code", tenantCode)
            ?.SetTag("de.operation.type", "transaction");

    public static Activity? StartProcess(string processKey, string tenantCode)
        => Source.StartActivity("DataEngine.Process", ActivityKind.Internal)
            ?.SetTag("de.process.key", processKey)
            ?.SetTag("de.tenant.code", tenantCode)
            ?.SetTag("de.operation.type", "process");
}
```

## 17.2 OpenTelemetry Registration

```csharp
// Telemetry service registration
services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(DataEngineActivitySource.Source.Name)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(opts =>
        {
            opts.Endpoint = new Uri(configuration["Telemetry:OtlpEndpoint"]!);
        }))
    .WithMetrics(metrics => metrics
        .AddMeter("Janatics.DataEngine")
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter());
```

## 17.3 Serilog Structured Logging

```csharp
// Janatics.DataEngine.Telemetry/Logging/SerilogConfigurator.cs
public static class SerilogConfigurator
{
    public static LoggerConfiguration ConfigureForDataEngine(
        this LoggerConfiguration config,
        IConfiguration appConfig)
    {
        return config
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.With<CorrelationEnricher>()
            .Enrich.With<TenantEnricher>()
            .WriteTo.Console(new JsonFormatter())
            .WriteTo.Seq(appConfig["Logging:SeqEndpoint"] ?? "http://localhost:5341");
    }
}
```

## 17.4 MediatR Logging Behavior

```csharp
// Janatics.DataEngine.Application/Behaviors/LoggingBehavior.cs
public sealed class LoggingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;
        var correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString();

        _logger.LogInformation(
            "[DE] Handling {RequestName} | CorrelationId: {CorrelationId}",
            requestName, correlationId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next().ConfigureAwait(false);
            stopwatch.Stop();

            _logger.LogInformation(
                "[DE] Handled {RequestName} in {ElapsedMs}ms | CorrelationId: {CorrelationId}",
                requestName, stopwatch.ElapsedMilliseconds, correlationId);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "[DE] Failed {RequestName} after {ElapsedMs}ms | CorrelationId: {CorrelationId}",
                requestName, stopwatch.ElapsedMilliseconds, correlationId);
            throw;
        }
    }
}
```

---

# 18. API LAYER ARCHITECTURE

## 18.1 Controller Implementations

```csharp
// Janatics.DataEngine.Api/Controllers/FetchController.cs
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/fetch")]
[Authorize(Policy = "QueryExecution")]
public sealed class FetchController : ControllerBase
{
    private readonly IMediator _mediator;

    public FetchController(IMediator mediator) => _mediator = mediator;

    [HttpPost("{queryKey}")]
    [ProducesResponseType(typeof(ApiResponse<FetchResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExecuteFetch(
        [FromRoute] string queryKey,
        [FromBody] FetchRequest request,
        CancellationToken ct)
    {
        var query = new ExecuteFetchQuery
        {
            TenantCode = HttpContext.GetTenantCode(),
            QueryKey = queryKey,
            Parameters = request.Parameters,
            Pagination = request.Pagination,
            Sorting = request.Sorting,
            Filters = request.Filters
        };

        var result = await _mediator.Send(query, ct);

        return result.IsSuccess
            ? Ok(ApiResponse<FetchResponse>.Succeed(result.Data))
            : BadRequest(ApiResponse.Fail(result.Errors));
    }
}

// Janatics.DataEngine.Api/Controllers/TransactionController.cs
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/transaction")]
[Authorize(Policy = "TransactionWrite")]
public sealed class TransactionController : ControllerBase
{
    private readonly IMediator _mediator;

    public TransactionController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TransactionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExecuteTransaction(
        [FromBody] TransactionRequest request,
        CancellationToken ct)
    {
        var command = new ExecuteTransactionCommand
        {
            TenantCode = HttpContext.GetTenantCode(),
            Request = request with { TenantCode = HttpContext.GetTenantCode() },
            InitiatedBy = HttpContext.GetUserId()
        };

        var result = await _mediator.Send(command, ct);

        return result.IsSuccess
            ? Ok(ApiResponse<TransactionResponse>.Succeed(result.Data))
            : UnprocessableEntity(ApiResponse.Fail(result.Errors));
    }
}
```

## 18.2 API Response Standard

```csharp
// Janatics.DataEngine.Contracts/Responses/ApiResponse.cs
public sealed record ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public IReadOnlyList<string>? Errors { get; init; }
    public string? TraceId { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public static ApiResponse<T> Succeed(T data, string? traceId = null) =>
        new() { Success = true, Data = data, TraceId = traceId };

    public static ApiResponse<T> Fail(IReadOnlyList<string> errors, string? traceId = null) =>
        new() { Success = false, Errors = errors, TraceId = traceId };
}
```

## 18.3 Versioning Strategy

```csharp
// API versioning configuration
services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"));
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
```

---

# 19. ERROR HANDLING ARCHITECTURE

## 19.1 Global Exception Middleware

```csharp
// Janatics.DataEngine.Api/Middleware/GlobalExceptionMiddleware.cs
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.TraceId.ToString()
            ?? context.TraceIdentifier;

        var (statusCode, message, errors) = exception switch
        {
            MetadataNotFoundException mnfe =>
                (StatusCodes.Status404NotFound, mnfe.Message, new[] { mnfe.Message }),

            ValidationFailureException vfe =>
                (StatusCodes.Status422UnprocessableEntity, "Validation failed",
                 vfe.Failures.Select(f => f.ErrorMessage).ToArray()),

            QueryExecutionException qee =>
                (StatusCodes.Status500InternalServerError, "Query execution failed",
                 new[] { qee.Message }),

            TransactionOrchestrationException toe =>
                (StatusCodes.Status500InternalServerError, "Transaction failed",
                 new[] { toe.Message }),

            UnauthorizedAccessException =>
                (StatusCodes.Status403Forbidden, "Access denied", new[] { "Insufficient permissions." }),

            OperationCanceledException =>
                (StatusCodes.Status499ClientClosedRequest, "Request cancelled", Array.Empty<string>()),

            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred",
                  new[] { "Please contact support with trace ID: " + traceId })
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception,
                "Unhandled exception | TraceId: {TraceId} | Path: {Path}",
                traceId, context.Request.Path);
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new ApiResponse<object>
        {
            Success = false,
            Errors = errors,
            TraceId = traceId
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response, JsonDefaults.Options));
    }
}
```

---

# 20. AUDIT & HISTORY SYSTEM

## 20.1 Audit Schema

```sql
CREATE TABLE de_audit_records (
    id              CHAR(36)     NOT NULL PRIMARY KEY,
    tenant_code     VARCHAR(50)  NOT NULL,
    transaction_id  CHAR(36)     NULL,
    process_id      CHAR(36)     NULL,
    entity_key      VARCHAR(200) NOT NULL,
    operation_type  TINYINT      NOT NULL, -- 1=Insert, 2=Update, 3=Delete
    record_id       VARCHAR(500) NOT NULL,
    previous_values TEXT         NULL,     -- JSON snapshot
    new_values      TEXT         NULL,     -- JSON snapshot
    changed_fields  TEXT         NULL,     -- JSON array of changed field keys
    initiated_by    VARCHAR(200) NOT NULL,
    ip_address      VARCHAR(50)  NULL,
    correlation_id  CHAR(36)     NULL,
    created_at      DATETIME     NOT NULL,
    INDEX idx_audit_tenant_entity (tenant_code, entity_key),
    INDEX idx_audit_record (tenant_code, entity_key, record_id),
    INDEX idx_audit_transaction (transaction_id),
    INDEX idx_audit_created (created_at)
);
```

## 20.2 Audit Sink

```csharp
// Janatics.DataEngine.Infrastructure/Audit/DatabaseAuditSink.cs
public sealed class DatabaseAuditSink : IAuditSink
{
    private readonly IDbProviderFactory _providerFactory;
    private readonly IConnectionStringResolver _connectionResolver;
    private readonly ILogger<DatabaseAuditSink> _logger;

    public DatabaseAuditSink(
        IDbProviderFactory providerFactory,
        IConnectionStringResolver connectionResolver,
        ILogger<DatabaseAuditSink> logger)
    {
        _providerFactory = providerFactory;
        _connectionResolver = connectionResolver;
        _logger = logger;
    }

    public async Task RecordTransactionAsync(
        Guid transactionId,
        TransactionRequest request,
        IReadOnlyList<NodeProcessingResult> results,
        CancellationToken ct = default)
    {
        var records = results.Select(r => new AuditRecord
        {
            Id = Guid.NewGuid(),
            TenantCode = request.TenantCode,
            TransactionId = transactionId,
            EntityKey = r.EntityKey,
            OperationType = r.OperationType,
            RecordId = r.RecordId?.ToString() ?? string.Empty,
            NewValues = r.NewValues is not null
                ? JsonSerializer.Serialize(r.NewValues, JsonDefaults.Options)
                : null,
            PreviousValues = r.PreviousValues is not null
                ? JsonSerializer.Serialize(r.PreviousValues, JsonDefaults.Options)
                : null,
            InitiatedBy = request.InitiatedBy ?? "system",
            CreatedAt = DateTime.UtcNow
        }).ToList();

        // Fire-and-forget with explicit error logging — audit must never block the main flow
        _ = Task.Run(async () =>
        {
            try
            {
                var provider = _providerFactory.Resolve(DataProviderType.MySql);
                var cs = _connectionResolver.Resolve("audit");
                await using var connection = (DbConnection)await provider
                    .OpenConnectionAsync(cs, CancellationToken.None)
                    .ConfigureAwait(false);

                await connection.ExecuteAsync(
                    AuditSql.BulkInsert,
                    records).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audit record persistence failed for transaction {TransactionId}",
                    transactionId);
            }
        }, ct);
    }
}
```

---

# 21. UI/UX PLATFORM STRATEGY

## 21.1 Admin Console Architecture

The Janatics DataEngine Admin Console is a React 19 + TypeScript application structured as a feature-based monorepo module. It provides a full enterprise administrative experience for platform operators and configurators.

### Core Feature Modules

| Feature | Purpose |
|---|---|
| Query Manager | Create, test, version, and publish SQL query definitions |
| Mapping Designer | Visual field mapping between source and target entities |
| Process Designer | Drag-and-drop process stage pipeline builder |
| Metadata Manager | Entity schema browser and definition editor |
| Execution Monitor | Live transaction and process execution dashboard |
| Audit Viewer | Searchable audit trail with field-level change diff |
| Provider Config | Database connection configuration and health checks |

## 21.2 Technology Stack

```typescript
// package.json (core dependencies)
{
  "dependencies": {
    "react": "^19.0.0",
    "react-dom": "^19.0.0",
    "typescript": "^5.7.0",
    "@tanstack/react-query": "^5.0.0",
    "zustand": "^5.0.0",
    "react-hook-form": "^7.53.0",
    "zod": "^3.23.0",
    "@dnd-kit/core": "^6.0.0",
    "@dnd-kit/sortable": "^8.0.0",
    "@radix-ui/react-*": "latest",
    "class-variance-authority": "^0.7.0",
    "tailwind-merge": "^2.5.0",
    "lucide-react": "^0.460.0",
    "monaco-editor": "^0.52.0",
    "@monaco-editor/react": "^4.6.0",
    "recharts": "^2.13.0",
    "axios": "^1.7.0",
    "date-fns": "^4.1.0"
  }
}
```

---

# 22. ENTERPRISE TYPESCRIPT FRONTEND ARCHITECTURE

## 22.1 Folder Structure

```
janatics-dataengine-console/
└── src/
    ├── app/
    │   ├── App.tsx
    │   ├── Router.tsx
    │   ├── providers/
    │   │   ├── QueryClientProvider.tsx
    │   │   ├── AuthProvider.tsx
    │   │   └── ThemeProvider.tsx
    │   └── layouts/
    │       ├── AdminLayout.tsx
    │       └── AuthLayout.tsx
    │
    ├── features/
    │   ├── query-builder/
    │   ├── mapping-designer/
    │   ├── process-designer/
    │   ├── metadata-manager/
    │   ├── execution-monitor/
    │   └── audit-viewer/
    │
    ├── components/
    │   ├── ui/                    # shadcn/ui re-exports
    │   ├── data-grid/
    │   │   ├── DataGrid.tsx
    │   │   └── DataGridPagination.tsx
    │   ├── monaco/
    │   │   └── SqlEditor.tsx
    │   ├── drag-and-drop/
    │   │   ├── DraggableItem.tsx
    │   │   └── DropZone.tsx
    │   └── layout/
    │       ├── PageHeader.tsx
    │       ├── SideNav.tsx
    │       └── Breadcrumbs.tsx
    │
    ├── hooks/
    │   ├── useDebounce.ts
    │   ├── useTenant.ts
    │   ├── usePermissions.ts
    │   └── useConfirm.ts
    │
    ├── api/
    │   ├── client.ts              # Axios instance with interceptors
    │   ├── queryKeys.ts           # TanStack Query key factories
    │   ├── endpoints/
    │   │   ├── fetchApi.ts
    │   │   ├── transactionApi.ts
    │   │   ├── metadataApi.ts
    │   │   ├── queryDefinitionApi.ts
    │   │   ├── mappingApi.ts
    │   │   └── processApi.ts
    │   └── types/
    │       └── api.types.ts
    │
    ├── stores/
    │   ├── authStore.ts
    │   └── tenantStore.ts
    │
    ├── types/
    │   ├── metadata.types.ts
    │   ├── query.types.ts
    │   ├── mapping.types.ts
    │   └── process.types.ts
    │
    └── utils/
        ├── formatters.ts
        ├── validators.ts
        └── sqlUtils.ts
```

## 22.2 API Client Configuration

```typescript
// src/api/client.ts
import axios, { AxiosInstance } from 'axios';
import { useAuthStore } from '../stores/authStore';
import { useTenantStore } from '../stores/tenantStore';

export const createApiClient = (): AxiosInstance => {
  const client = axios.create({
    baseURL: import.meta.env.VITE_API_BASE_URL,
    timeout: 30_000,
    headers: { 'Content-Type': 'application/json' },
  });

  client.interceptors.request.use((config) => {
    const token = useAuthStore.getState().accessToken;
    const tenantCode = useTenantStore.getState().tenantCode;
    const correlationId = crypto.randomUUID();

    if (token) config.headers.Authorization = `Bearer ${token}`;
    if (tenantCode) config.headers['X-Tenant-Code'] = tenantCode;
    config.headers['X-Correlation-Id'] = correlationId;

    return config;
  });

  client.interceptors.response.use(
    (response) => response,
    async (error) => {
      if (error.response?.status === 401) {
        useAuthStore.getState().clearSession();
        window.location.href = '/auth/login';
      }
      return Promise.reject(error);
    }
  );

  return client;
};

export const apiClient = createApiClient();
```

---

# 23. TESTING STRATEGY

## 23.1 Unit Testing

**Library**: xUnit + FluentAssertions + NSubstitute + AutoFixture

```csharp
// Janatics.DataEngine.UnitTests/TransactionEngine/OperationDetectorTests.cs
public sealed class OperationDetectorTests
{
    private readonly OperationDetector _sut = new();

    [Fact]
    public void Detect_WhenIsDeletedTrue_ReturnsDelete()
    {
        var context = new NodeProcessingContext { IsDeleted = true, NodeId = 42 };
        _sut.Detect(context).Should().Be(OperationType.Delete);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(0L)]
    public void Detect_WhenNodeIdIsNullOrDefault_ReturnsInsert(object? nodeId)
    {
        var context = new NodeProcessingContext { IsDeleted = false, NodeId = nodeId };
        _sut.Detect(context).Should().Be(OperationType.Insert);
    }

    [Fact]
    public void Detect_WhenNodeIdIsProvided_ReturnsUpdate()
    {
        var context = new NodeProcessingContext { IsDeleted = false, NodeId = 42 };
        _sut.Detect(context).Should().Be(OperationType.Update);
    }
}
```

## 23.2 Integration Testing

**Library**: xUnit + Testcontainers for .NET + Respawn

```csharp
// Janatics.DataEngine.IntegrationTests/FetchPipelineIntegrationTests.cs
public sealed class FetchPipelineIntegrationTests : IAsyncLifetime
{
    private readonly MySqlContainer _mySqlContainer = new MySqlBuilder()
        .WithDatabase("de_test")
        .WithUsername("root")
        .WithPassword("test")
        .Build();

    public async Task InitializeAsync()
    {
        await _mySqlContainer.StartAsync();
        await SeedTestDataAsync();
    }

    [Fact]
    public async Task ExecuteFetch_WithValidQueryKey_ReturnsPopulatedGraph()
    {
        // Arrange
        var services = BuildTestServices(_mySqlContainer.GetConnectionString());
        var pipeline = services.GetRequiredService<IFetchPipeline>();

        var context = new FetchPipelineContext
        {
            TenantCode = "TEST",
            QueryKey = "purchase_orders_list",
            Parameters = new Dictionary<string, object?> { ["Status"] = "Draft" }
        };

        // Act
        var result = await pipeline.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.RootResults.Should().NotBeEmpty();
        result.RootResults.First().Should().ContainKey("PONumber");
    }

    public async Task DisposeAsync() => await _mySqlContainer.DisposeAsync();
}
```

## 23.3 Provider Tests

Each database provider is tested against a Testcontainers instance:

- `MySqlProviderTests` — MySQL 8.x container
- `OracleProviderTests` — Oracle XE container
- `SqlServerProviderTests` — SQL Server container
- `SqliteProviderTests` — In-memory SQLite

## 23.4 Performance Testing

**Library**: NBomber

```csharp
// Janatics.DataEngine.PerformanceTests/FetchThroughputTests.cs
var fetchScenario = Scenario.Create("fetch_throughput", async context =>
{
    var response = await httpClient.PostAsJsonAsync(
        "/api/v1/fetch/purchase_orders_list",
        new { Parameters = new { Status = "Draft" } });

    return response.IsSuccessStatusCode
        ? Response.Ok()
        : Response.Fail(response.StatusCode.ToString());
})
.WithLoadSimulations(
    Simulation.Inject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(2))
);

NBomberRunner
    .RegisterScenarios(fetchScenario)
    .WithReportFormats(ReportFormat.Html, ReportFormat.Csv)
    .Run();
```

---

# 24. CI/CD & DEPLOYMENT STRATEGY

## 24.1 Docker Configuration

```dockerfile
# Janatics.DataEngine.Api/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/api/Janatics.DataEngine.Api/Janatics.DataEngine.Api.csproj", "api/"]
COPY ["src/application/Janatics.DataEngine.Application/Janatics.DataEngine.Application.csproj", "application/"]
COPY ["src/core/Janatics.DataEngine.Domain/Janatics.DataEngine.Domain.csproj", "domain/"]
RUN dotnet restore "api/Janatics.DataEngine.Api.csproj"
COPY . .
WORKDIR "/src/src/api/Janatics.DataEngine.Api"
RUN dotnet build "Janatics.DataEngine.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
RUN dotnet publish "Janatics.DataEngine.Api.csproj" -c $BUILD_CONFIGURATION \
    -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Janatics.DataEngine.Api.dll"]
```

## 24.2 Kubernetes Readiness

```yaml
# k8s/deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: janatics-dataengine-api
  namespace: janatics
spec:
  replicas: 3
  selector:
    matchLabels:
      app: janatics-dataengine-api
  template:
    metadata:
      labels:
        app: janatics-dataengine-api
    spec:
      containers:
        - name: dataengine-api
          image: janatics/dataengine-api:latest
          ports:
            - containerPort: 8080
          env:
            - name: ASPNETCORE_ENVIRONMENT
              value: Production
            - name: ConnectionStrings__Default
              valueFrom:
                secretKeyRef:
                  name: janatics-secrets
                  key: mysql-connection-string
            - name: Redis__ConnectionString
              valueFrom:
                secretKeyRef:
                  name: janatics-secrets
                  key: redis-connection-string
          readinessProbe:
            httpGet:
              path: /health/ready
              port: 8080
            initialDelaySeconds: 10
            periodSeconds: 5
          livenessProbe:
            httpGet:
              path: /health/live
              port: 8080
            initialDelaySeconds: 30
            periodSeconds: 15
          resources:
            requests:
              memory: "256Mi"
              cpu: "250m"
            limits:
              memory: "1Gi"
              cpu: "1000m"
---
apiVersion: v1
kind: Service
metadata:
  name: janatics-dataengine-api
  namespace: janatics
spec:
  selector:
    app: janatics-dataengine-api
  ports:
    - port: 80
      targetPort: 8080
  type: ClusterIP
```

## 24.3 Environment Strategy

| Environment | Database | Cache | Log Level | Config Source |
|---|---|---|---|---|
| Development | SQLite | In-Memory | Debug | appsettings.Development.json |
| Staging | MySQL | Redis | Information | Kubernetes ConfigMap |
| Production | MySQL (RDS) + Oracle | Redis Cluster | Warning | AWS Secrets Manager |

---

# 25. ENTERPRISE SCALING STRATEGY

## 25.1 Scaling to Millions of Transactions

The engine's stateless execution model means each API instance processes requests independently with no shared in-process state. At scale:

**Database Layer**:
- Read replicas absorb all fetch traffic; writes go exclusively to the primary
- Connection pooling is tuned per pod — typically 20–50 connections per instance
- Bulk transaction batching reduces round-trips for high-volume processing
- Async I/O ensures no thread blocking under concurrent load

**Cache Layer**:
- Redis Cluster with three primaries and three replicas handles metadata with microsecond latency
- Hot metadata (entity definitions, query templates) is cached indefinitely and invalidated on change
- Query results with deterministic parameters are cached with appropriate TTLs

**Horizontal Scale**:
- Kubernetes HPA triggers on CPU >60% and memory >70%
- At peak load, the engine scales from 3 to 30 pods in under 60 seconds

## 25.2 Multi-Tenant Scaling

All cache keys, metadata queries, and audit records are scoped by `TenantCode`. Tenant isolation is enforced at:

- Cache key level (tenant prefix in all cache keys)
- Metadata resolution level (all metadata queries filter by tenant)
- Connection level (tenants can have separate connection strings)
- Audit level (all audit records carry tenant context)

## 25.3 Microservices Readiness

The engine is architected to split into discrete services along these boundaries when the need arises:

- `janatics-fetch-service` — handles all read/query operations
- `janatics-transaction-service` — handles all write/DML operations
- `janatics-metadata-service` — serves metadata graphs to other services
- `janatics-orchestration-service` — manages process pipeline execution
- `janatics-audit-service` — dedicated audit ingestion and querying

Communication between services would use gRPC (synchronous) or RabbitMQ/Kafka (event-driven) patterns, with shared contracts defined in the `Janatics.DataEngine.Contracts` package.

## 25.4 Event-Driven Evolution

`TransactionCompletedEvent`, `ProcessExecutedEvent`, and `QueryExecutedEvent` are domain events already defined in the `Domain` project. Adding an outbox pattern or a message broker publisher (MassTransit over RabbitMQ or Azure Service Bus) would enable full event-driven downstream processing without architectural changes to the engine core.

---

# 26. ENGINEERING GOVERNANCE STANDARDS

## 26.1 Naming Conventions

| Artifact | Convention | Example |
|---|---|---|
| Interfaces | `I` prefix + PascalCase | `IDbProvider`, `IFetchPipeline` |
| Implementations | PascalCase, no suffix | `MySqlProvider`, `FetchPipeline` |
| Abstract Base | Suffix `Base` | `DbProviderBase`, `ProcessStageBase` |
| Domain Entities | PascalCase | `MetadataEntity`, `QueryDefinition` |
| DTOs | Suffix `Dto` | `MetadataEntityDto`, `QueryDefinitionDto` |
| Commands | Suffix `Command` | `ExecuteTransactionCommand` |
| Queries | Suffix `Query` | `ExecuteFetchQuery` |
| Handlers | Suffix `Handler` | `ExecuteTransactionCommandHandler` |
| Validators | Suffix `Validator` | `ExecuteTransactionCommandValidator` |
| Exceptions | Suffix `Exception` | `QueryExecutionException` |
| Constants | `PascalCase` in `static class` | `CacheKeyConstants.MetadataPrefix` |
| Async methods | Suffix `Async` | `ExecuteAsync`, `LoadChildrenAsync` |
| Test classes | Suffix `Tests` | `OperationDetectorTests` |
| Test methods | `Method_WhenCondition_ExpectedResult` | `Detect_WhenIsDeletedTrue_ReturnsDelete` |

## 26.2 Metadata Standards

- All metadata keys use `snake_case` for `EntityKey`, `QueryKey`, `ProcessKey`, `ProfileKey`
- Tenant codes are uppercase, max 50 characters, alphanumeric + hyphen only
- Entity table names follow the consuming database's conventions; schema names are optional
- Query definitions must declare all runtime parameters explicitly — implicit parameter injection is prohibited
- All metadata records must carry `created_at`, `created_by`, `updated_at`, `updated_by` audit columns

## 26.3 API Standards

- All API routes use `kebab-case`: `/api/v1/query-definitions/{queryKey}`
- All API versions are declared in the route prefix: `v1`, `v2`
- All responses conform to `ApiResponse<T>` envelope format
- All 4xx responses include `TraceId` and structured `Errors` array
- No endpoint returns raw model types directly — always wrapped in `ApiResponse<T>`
- Idempotency keys are supported via `X-Idempotency-Key` header on all write endpoints

## 26.4 Query Standards

- No raw string concatenation in SQL construction — all values must be parameterized
- All query definitions must pass through `AllowedOperationValidator` before persistence
- Query keys must be globally unique within a tenant scope
- Maximum SQL template length: 64,000 characters
- Parameter keys must match the regex: `^[a-zA-Z][a-zA-Z0-9_]{0,99}$`

## 26.5 Logging Standards

Structured log fields used consistently across all engine components:

| Field | Type | Description |
|---|---|---|
| `de.tenant.code` | string | Tenant identifier |
| `de.query.key` | string | Query definition key |
| `de.entity.key` | string | Entity metadata key |
| `de.process.key` | string | Process definition key |
| `de.transaction.id` | Guid | Transaction correlation ID |
| `de.operation.type` | string | `fetch`, `transaction`, `process` |
| `de.duration.ms` | long | Operation duration in milliseconds |
| `de.record.count` | int | Number of records affected or returned |

## 26.6 PR and Review Governance

- No PR may reduce test coverage below 80% in modified packages
- All database queries in new code must use parameterized execution — reviewer blocks any string interpolation in SQL context
- Every new provider must implement all `IDbDialect` and `IDbCapabilities` members — partial implementations do not merge
- Every new IProcessStage implementation must have unit tests covering success, failure, and cancellation paths
- Security-sensitive changes (SQL sanitization, permission gates, authentication middleware) require two senior engineer approvals

---

*End of JANATICS_DATAENGINE_IMPLEMENTATION.md*
*Document Authority: Janatics Platform Engineering | Principal Architecture Office*
*Revision 1.0.0 — .NET 9 / C# 13 / React 19*
