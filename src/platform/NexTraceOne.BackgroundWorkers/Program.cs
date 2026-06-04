using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NexTraceOne.BackgroundWorkers;
using NexTraceOne.BackgroundWorkers.Backup;
using NexTraceOne.BackgroundWorkers.Configuration;
using NexTraceOne.BackgroundWorkers.Elasticsearch;
using NexTraceOne.BackgroundWorkers.Health;
using NexTraceOne.BackgroundWorkers.Jobs;
using NexTraceOne.BackgroundWorkers.Jobs.ExpirationHandlers;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.Cache;
using NexTraceOne.BuildingBlocks.Infrastructure.HealthChecks;
using NexTraceOne.BuildingBlocks.Observability;
using NexTraceOne.BuildingBlocks.Observability.HealthChecks;
using NexTraceOne.IdentityAccess.Infrastructure;
using NexTraceOne.IdentityAccess.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.API;

// Module infrastructure registrations for cross-module outbox processing
using NexTraceOne.Catalog.API;
using NexTraceOne.ChangeGovernance.API;
using NexTraceOne.AIKnowledge.API;
using NexTraceOne.Governance.API;
using NexTraceOne.Integrations.Infrastructure;
using NexTraceOne.Catalog.Infrastructure.Knowledge;
using NexTraceOne.Catalog.Infrastructure.ProductAnalytics;
using NexTraceOne.Notifications.Infrastructure;
using NexTraceOne.Configuration.Infrastructure;

// DbContext types for outbox processor registration
using NexTraceOne.Catalog.Infrastructure.Persistence;
using NexTraceOne.ChangeGovernance.Infrastructure.Persistence;
using NexTraceOne.AIKnowledge.Infrastructure.Persistence;
using NexTraceOne.Governance.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Persistence;
using NexTraceOne.Integrations.Infrastructure.Persistence;
using NexTraceOne.Configuration.Infrastructure.Persistence;

// ═══════════════════════════════════════════════════════════════════════════════
// NEXTRACEONE — Background Workers
// Processa: Outbox Messages (todos os módulos), Quartz Jobs, SLA Escalation, Cost Ingestion
// ═══════════════════════════════════════════════════════════════════════════════

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IPlatformHealthReader, DefaultPlatformHealthReader>();
builder.Services.AddSingleton<IBackupProcess, PgDumpBackupProcess>();
builder.Services.AddSingleton<IDateTimeProvider, WorkerDateTimeProvider>();
builder.Services.AddSingleton<ICurrentUser, WorkerCurrentUser>();
builder.Services.AddSingleton<ICurrentTenant, WorkerCurrentTenant>();
builder.Services.AddSingleton<WorkerJobHealthRegistry>();

builder.Services.AddBuildingBlocksEventBus(builder.Configuration);
builder.Services.AddBuildingBlocksDbContext(builder.Configuration);
builder.Services.AddDistributedCaching(builder.Configuration);
builder.Services.AddIngestionMetrics(builder.Configuration);

// ── Module infrastructure registration ──
// Cada módulo registra seu DbContext, repositórios e serviços necessários
// para que o outbox processor possa acessar as mensagens pendentes.
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddServiceCatalogModule(builder.Configuration);
builder.Services.AddChangeGovernanceModule(builder.Configuration);
builder.Services.AddAiHubModule(builder.Configuration);
builder.Services.AddPlatformGovernanceModule(builder.Configuration);
builder.Services.AddIncidentResponseModule(builder.Configuration);
builder.Services.AddIntegrationsInfrastructure(builder.Configuration);
builder.Services.AddKnowledgeInfrastructure(builder.Configuration);
builder.Services.AddProductAnalyticsInfrastructure(builder.Configuration);
builder.Services.AddNotificationsInfrastructure(builder.Configuration);
builder.Services.AddConfigurationInfrastructure(builder.Configuration);

builder.Services.Configure<DriftDetectionOptions>(
    builder.Configuration.GetSection(DriftDetectionOptions.SectionName));
builder.Services.Configure<OtelCatalogBridgeOptions>(
    builder.Configuration.GetSection(OtelCatalogBridgeOptions.SectionName));
builder.Services.Configure<ContractConsumerIngestionOptions>(
    builder.Configuration.GetSection(ContractConsumerIngestionOptions.SectionName));
builder.Services.Configure<BackupOptions>(
    builder.Configuration.GetSection(BackupOptions.SectionName));
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
        "outbox-processor-integrations",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["health"],
        args: [ModuleOutboxProcessorJob<IntegrationsDbContext>.HealthCheckName, TimeSpan.FromMinutes(2)])
    .AddTypeActivatedCheck<BackgroundWorkerJobHealthCheck>(
        "outbox-processor-configuration",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["health"],
        args: [ModuleOutboxProcessorJob<ConfigurationDbContext>.HealthCheckName, TimeSpan.FromMinutes(2)])
    // Catalog module (consolidated ServiceCatalogDbContext)
    .AddTypeActivatedCheck<BackgroundWorkerJobHealthCheck>(
        "outbox-processor-service-catalog",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["health"],
        args: [ModuleOutboxProcessorJob<ServiceCatalogDbContext>.HealthCheckName, TimeSpan.FromMinutes(2)])
    // ChangeGovernance module (consolidated DbContext)
    .AddTypeActivatedCheck<BackgroundWorkerJobHealthCheck>(
        "outbox-processor-change-governance",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["health"],
        args: [ModuleOutboxProcessorJob<ChangeGovernanceDbContext>.HealthCheckName, TimeSpan.FromMinutes(2)])
    // AIKnowledge module (consolidated DbContext)
    .AddTypeActivatedCheck<BackgroundWorkerJobHealthCheck>(
        "outbox-processor-ai-hub",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["health"],
        args: [ModuleOutboxProcessorJob<AiHubDbContext>.HealthCheckName, TimeSpan.FromMinutes(2)])
    // PlatformGovernance module (consolidated: Governance + AuditCompliance)
    .AddTypeActivatedCheck<BackgroundWorkerJobHealthCheck>(
        "outbox-processor-governance",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["health"],
        args: [ModuleOutboxProcessorJob<PlatformGovernanceDbContext>.HealthCheckName, TimeSpan.FromMinutes(2)])
    // OperationalIntelligence module (IncidentResponse consolidated DbContext)
    .AddTypeActivatedCheck<BackgroundWorkerJobHealthCheck>(
        "outbox-processor-incident-response",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["health"],
        args: [ModuleOutboxProcessorJob<IncidentResponseDbContext>.HealthCheckName, TimeSpan.FromMinutes(2)])
    .AddTypeActivatedCheck<BackgroundWorkerJobHealthCheck>(
        "identity-expiration-job",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["health"],
        args: [IdentityExpirationJob.HealthCheckName, TimeSpan.FromMinutes(5)])
    .AddTypeActivatedCheck<BackgroundWorkerJobHealthCheck>(
        "drift-detection-job",
        failureStatus: HealthStatus.Degraded,
        tags: ["health"],
        args: [DriftDetectionJob.HealthCheckName, TimeSpan.FromMinutes(10)])
    .AddTypeActivatedCheck<BackgroundWorkerJobHealthCheck>(
        "otel-catalog-bridge-job",
        failureStatus: HealthStatus.Degraded,
        tags: ["health"],
        args: [OtelCatalogBridgeJob.HealthCheckName, TimeSpan.FromMinutes(60)])
    .AddTypeActivatedCheck<BackgroundWorkerJobHealthCheck>(
        "contract-consumer-ingestion-job",
        failureStatus: HealthStatus.Degraded,
        tags: ["health"],
        args: [ContractConsumerIngestionJob.HealthCheckName, TimeSpan.FromMinutes(30)])
    .AddTypeActivatedCheck<BackgroundWorkerJobHealthCheck>(
        "license-recalculation-job",
        failureStatus: HealthStatus.Degraded,
        tags: ["health"],
        args: [LicenseRecalculationJob.HealthCheckName, TimeSpan.FromMinutes(30)])
    .AddTypeActivatedCheck<BackgroundWorkerJobHealthCheck>(
        "platform-health-monitor-job",
        failureStatus: HealthStatus.Degraded,
        tags: ["health"],
        args: [PlatformHealthMonitorJob.HealthCheckName, TimeSpan.FromMinutes(15)])
    .AddTypeActivatedCheck<BackgroundWorkerJobHealthCheck>(
        "dependency-scan-job",
        failureStatus: HealthStatus.Degraded,
        tags: ["health"],
        args: [DependencyScanJob.HealthCheckName, TimeSpan.FromHours(8)])
    .AddTypeActivatedCheck<BackgroundWorkerJobHealthCheck>(
        "backup-coordinator-job",
        failureStatus: HealthStatus.Degraded,
        tags: ["health"],
        args: [BackupCoordinatorJob.HealthCheckName, TimeSpan.FromHours(25)])
    .AddTypeActivatedCheck<BackgroundWorkerJobHealthCheck>(
        "waste-detection-job",
        failureStatus: HealthStatus.Degraded,
        tags: ["health"],
        args: [WasteDetectionJob.HealthCheckName, TimeSpan.FromHours(25)])
    .AddTypeActivatedCheck<BackgroundWorkerJobHealthCheck>(
        "elasticsearch-index-maintenance-job",
        failureStatus: HealthStatus.Degraded,
        tags: ["health"],
        args: [ElasticsearchIndexMaintenanceJob.HealthCheckName, TimeSpan.FromHours(7)])
    .AddTypeActivatedCheck<BackgroundWorkerJobHealthCheck>(
        "carbon-score-calculation-job",
        failureStatus: HealthStatus.Degraded,
        tags: ["health"],
        args: [CarbonScoreCalculationJob.HealthCheckName, TimeSpan.FromHours(25)])
    .AddTypeActivatedCheck<BackgroundWorkerJobHealthCheck>(
        "incident-probability-refresh-job",
        failureStatus: HealthStatus.Degraded,
        tags: ["health"],
        args: [IncidentProbabilityRefreshJob.HealthCheckName, TimeSpan.FromMinutes(60)])
    .AddTypeActivatedCheck<BackgroundWorkerJobHealthCheck>(
        "cloud-billing-ingestion-job",
        failureStatus: HealthStatus.Degraded,
        tags: ["health"],
        args: [CloudBillingIngestionJob.HealthCheckName, TimeSpan.FromHours(48)]);

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

// Catalog (consolidated ServiceCatalogDbContext)
builder.Services.AddHostedService<ModuleOutboxProcessorJob<ServiceCatalogDbContext>>();

// ChangeGovernance (consolidated DbContext)
builder.Services.AddHostedService<ModuleOutboxProcessorJob<ChangeGovernanceDbContext>>();

// AIKnowledge (consolidated DbContext)
builder.Services.AddHostedService<ModuleOutboxProcessorJob<AiHubDbContext>>();

// PlatformGovernance consolidated (Governance + AuditCompliance)
builder.Services.AddHostedService<ModuleOutboxProcessorJob<PlatformGovernanceDbContext>>();

// OperationalIntelligence (fully consolidated: incidents, reliability, automation, runtime, cost, telemetry)
builder.Services.AddHostedService<ModuleOutboxProcessorJob<IncidentResponseDbContext>>();

// Integrations / Configuration (Notifications consolidated into ConfigurationDbContext)
builder.Services.AddHostedService<ModuleOutboxProcessorJob<IntegrationsDbContext>>();
builder.Services.AddHostedService<ModuleOutboxProcessorJob<ConfigurationDbContext>>();

builder.Services.AddHostedService<IdentityExpirationJob>();
builder.Services.AddHostedService<DriftDetectionJob>();
builder.Services.AddHostedService<OtelCatalogBridgeJob>();
builder.Services.AddHostedService<CloudBillingIngestionJob>();
builder.Services.AddHostedService<IncidentProbabilityRefreshJob>();
builder.Services.AddHostedService<ContractConsumerIngestionJob>();
builder.Services.AddHostedService<LicenseRecalculationJob>();
builder.Services.AddHostedService<PlatformHealthMonitorJob>();
builder.Services.AddHostedService<BackupCoordinatorJob>();
builder.Services.AddHostedService<WasteDetectionJob>();
builder.Services.AddHostedService<ElasticsearchIndexMaintenanceJob>();
builder.Services.AddHostedService<CarbonScoreCalculationJob>();
builder.Services.AddHostedService<DependencyScanJob>();

// W7-01: ES index manager — resolve via scoped scope in job
var esUrl = builder.Configuration["Elasticsearch:Url"] ?? builder.Configuration["Elasticsearch:Uri"] ?? "http://localhost:9200";
builder.Services.AddHttpClient<ElasticsearchIndexManagerService>(client =>
{
    client.BaseAddress = new Uri(esUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});
builder.Services.AddScoped<IElasticsearchIndexManager, ElasticsearchIndexManagerService>();

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
