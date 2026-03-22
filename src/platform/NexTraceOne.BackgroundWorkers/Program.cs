using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NexTraceOne.BackgroundWorkers;
using NexTraceOne.BackgroundWorkers.Configuration;
using NexTraceOne.BackgroundWorkers.Jobs;
using NexTraceOne.BackgroundWorkers.Jobs.ExpirationHandlers;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.HealthChecks;
using NexTraceOne.BuildingBlocks.Observability.HealthChecks;
using NexTraceOne.IdentityAccess.Infrastructure;
using NexTraceOne.IdentityAccess.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.API.Runtime.Endpoints;

// Module infrastructure registrations for cross-module outbox processing
using NexTraceOne.Catalog.API.Graph.Endpoints;
using NexTraceOne.Catalog.API.Contracts.Endpoints;
using NexTraceOne.Catalog.API.Portal.Endpoints;
using NexTraceOne.ChangeGovernance.API.ChangeIntelligence.Endpoints;
using NexTraceOne.ChangeGovernance.API.RulesetGovernance.Endpoints;
using NexTraceOne.ChangeGovernance.API.Workflow.Endpoints;
using NexTraceOne.ChangeGovernance.API.Promotion.Endpoints;
using NexTraceOne.AIKnowledge.API.Governance.Endpoints;
using NexTraceOne.AIKnowledge.API.ExternalAI.Endpoints;
using NexTraceOne.AIKnowledge.API.Orchestration.Endpoints;
using NexTraceOne.AuditCompliance.API.Endpoints;
using NexTraceOne.Governance.API;
using NexTraceOne.OperationalIntelligence.API.Reliability.Endpoints;
using NexTraceOne.OperationalIntelligence.API.Cost.Endpoints;

// DbContext types for outbox processor registration
using NexTraceOne.Catalog.Infrastructure.Graph.Persistence;
using NexTraceOne.Catalog.Infrastructure.Contracts.Persistence;
using NexTraceOne.Catalog.Infrastructure.Portal.Persistence;
using NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence;
using NexTraceOne.ChangeGovernance.Infrastructure.RulesetGovernance.Persistence;
using NexTraceOne.ChangeGovernance.Infrastructure.Workflow.Persistence;
using NexTraceOne.ChangeGovernance.Infrastructure.Promotion.Persistence;
using NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence;
using NexTraceOne.AIKnowledge.Infrastructure.ExternalAI.Persistence;
using NexTraceOne.AIKnowledge.Infrastructure.Orchestration.Persistence;
using NexTraceOne.AuditCompliance.Infrastructure.Persistence;
using NexTraceOne.Governance.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Automation.Persistence;

// ═══════════════════════════════════════════════════════════════════════════════
// NEXTRACEONE — Background Workers
// Processa: Outbox Messages (todos os módulos), Quartz Jobs, SLA Escalation, Cost Ingestion
// ═══════════════════════════════════════════════════════════════════════════════

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IDateTimeProvider, WorkerDateTimeProvider>();
builder.Services.AddSingleton<ICurrentUser, WorkerCurrentUser>();
builder.Services.AddSingleton<ICurrentTenant, WorkerCurrentTenant>();
builder.Services.AddSingleton<WorkerJobHealthRegistry>();

builder.Services.AddBuildingBlocksEventBus(builder.Configuration);

// ── Module infrastructure registration ──
// Cada módulo registra seu DbContext, repositórios e serviços necessários
// para que o outbox processor possa acessar as mensagens pendentes.
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddCatalogGraphModule(builder.Configuration);
builder.Services.AddContractsModule(builder.Configuration);
builder.Services.AddDeveloperPortalModule(builder.Configuration);
builder.Services.AddChangeIntelligenceModule(builder.Configuration);
builder.Services.AddRulesetGovernanceModule(builder.Configuration);
builder.Services.AddWorkflowModule(builder.Configuration);
builder.Services.AddPromotionModule(builder.Configuration);
builder.Services.AddAiGovernanceModule(builder.Configuration);
builder.Services.AddExternalAiModule(builder.Configuration);
builder.Services.AddAiOrchestrationModule(builder.Configuration);
builder.Services.AddAuditModule(builder.Configuration);
builder.Services.AddGovernanceModule(builder.Configuration);
builder.Services.AddRuntimeIntelligenceModule(builder.Configuration);
builder.Services.AddReliabilityModule(builder.Configuration);
builder.Services.AddCostIntelligenceModule(builder.Configuration);

builder.Services.Configure<DriftDetectionOptions>(
    builder.Configuration.GetSection(DriftDetectionOptions.SectionName));
builder.Services.AddNexTraceHealthChecks();
builder.Services.AddHealthChecks()
    .AddCheck<DbContextConnectivityHealthCheck<IdentityDbContext>>(
        "identity-db",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready", "health"])
    .AddTypeActivatedCheck<BackgroundWorkerJobHealthCheck>(
        "outbox-processor-identity",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["health"],
        args: [ModuleOutboxProcessorJob<IdentityDbContext>.HealthCheckName, TimeSpan.FromMinutes(2)])
    .AddTypeActivatedCheck<BackgroundWorkerJobHealthCheck>(
        "identity-expiration-job",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["health"],
        args: [IdentityExpirationJob.HealthCheckName, TimeSpan.FromMinutes(5)])
    .AddTypeActivatedCheck<BackgroundWorkerJobHealthCheck>(
        "drift-detection-job",
        failureStatus: HealthStatus.Degraded,
        tags: ["health"],
        args: [DriftDetectionJob.HealthCheckName, TimeSpan.FromMinutes(10)]);

// Handlers de expiração — cada um processa um único tipo de entidade expirável.
// A ordem de registro define a ordem de execução no IdentityExpirationJob.
builder.Services.AddSingleton<IExpirationHandler, DelegationExpirationHandler>();
builder.Services.AddSingleton<IExpirationHandler, BreakGlassExpirationHandler>();
builder.Services.AddSingleton<IExpirationHandler, JitAccessExpirationHandler>();
builder.Services.AddSingleton<IExpirationHandler, AccessReviewExpirationHandler>();
builder.Services.AddSingleton<IExpirationHandler, EnvironmentAccessExpirationHandler>();

// ── Outbox Processors — um por DbContext de cada módulo ──
// Cada processador opera de forma independente, garantindo que falhas em um módulo
// não afetem o processamento de outbox de outros módulos.
// Todos compartilham o mesmo padrão de processamento: lote de 50, ciclo de 5s, max 5 retries.

// Identity (database: nextraceone_identity)
builder.Services.AddHostedService<ModuleOutboxProcessorJob<IdentityDbContext>>();

// Catalog (database: nextraceone_catalog)
builder.Services.AddHostedService<ModuleOutboxProcessorJob<CatalogGraphDbContext>>();
builder.Services.AddHostedService<ModuleOutboxProcessorJob<ContractsDbContext>>();
builder.Services.AddHostedService<ModuleOutboxProcessorJob<DeveloperPortalDbContext>>();

// ChangeGovernance (database: nextraceone_catalog — shares with Catalog via separate schemas)
builder.Services.AddHostedService<ModuleOutboxProcessorJob<ChangeIntelligenceDbContext>>();
builder.Services.AddHostedService<ModuleOutboxProcessorJob<RulesetGovernanceDbContext>>();
builder.Services.AddHostedService<ModuleOutboxProcessorJob<WorkflowDbContext>>();
builder.Services.AddHostedService<ModuleOutboxProcessorJob<PromotionDbContext>>();

// AIKnowledge (database: nextraceone_ai)
builder.Services.AddHostedService<ModuleOutboxProcessorJob<AiGovernanceDbContext>>();
builder.Services.AddHostedService<ModuleOutboxProcessorJob<ExternalAiDbContext>>();
builder.Services.AddHostedService<ModuleOutboxProcessorJob<AiOrchestrationDbContext>>();

// Governance (database: nextraceone_operations)
builder.Services.AddHostedService<ModuleOutboxProcessorJob<GovernanceDbContext>>();

// AuditCompliance (database: nextraceone_operations)
builder.Services.AddHostedService<ModuleOutboxProcessorJob<AuditDbContext>>();

// OperationalIntelligence (database: nextraceone_operations)
builder.Services.AddHostedService<ModuleOutboxProcessorJob<RuntimeIntelligenceDbContext>>();
builder.Services.AddHostedService<ModuleOutboxProcessorJob<ReliabilityDbContext>>();
builder.Services.AddHostedService<ModuleOutboxProcessorJob<CostIntelligenceDbContext>>();
builder.Services.AddHostedService<ModuleOutboxProcessorJob<IncidentDbContext>>();
builder.Services.AddHostedService<ModuleOutboxProcessorJob<AutomationDbContext>>();

builder.Services.AddHostedService<IdentityExpirationJob>();
builder.Services.AddHostedService<DriftDetectionJob>();

var app = builder.Build();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteResponse
}).AllowAnonymous();

app.MapHealthChecks("/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteResponse
}).AllowAnonymous();

app.MapHealthChecks("/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = HealthCheckResponseWriter.WriteResponse
}).AllowAnonymous();

app.Run();
