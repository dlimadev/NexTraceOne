using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NexTraceOne.BackgroundWorkers;
using NexTraceOne.BackgroundWorkers.Jobs;
using NexTraceOne.BackgroundWorkers.Jobs.ExpirationHandlers;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Infrastructure.HealthChecks;
using NexTraceOne.BuildingBlocks.Observability.HealthChecks;
using NexTraceOne.IdentityAccess.Infrastructure;
using NexTraceOne.IdentityAccess.Infrastructure.Persistence;

// ═══════════════════════════════════════════════════════════════════════════════
// NEXTRACEONE — Background Workers
// Processa: Outbox Messages, Quartz Jobs, SLA Escalation, Cost Ingestion
// ═══════════════════════════════════════════════════════════════════════════════

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IDateTimeProvider, WorkerDateTimeProvider>();
builder.Services.AddSingleton<ICurrentUser, WorkerCurrentUser>();
builder.Services.AddSingleton<ICurrentTenant, WorkerCurrentTenant>();
builder.Services.AddSingleton<WorkerJobHealthRegistry>();

builder.Services.AddBuildingBlocksEventBus(builder.Configuration);
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddNexTraceHealthChecks();
builder.Services.AddHealthChecks()
    .AddCheck<DbContextConnectivityHealthCheck<IdentityDbContext>>(
        "identity-db",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready", "health"])
    .AddTypeActivatedCheck<BackgroundWorkerJobHealthCheck>(
        "outbox-processor-job",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["health"],
        args: [OutboxProcessorJob.HealthCheckName, TimeSpan.FromMinutes(2)])
    .AddTypeActivatedCheck<BackgroundWorkerJobHealthCheck>(
        "identity-expiration-job",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["health"],
        args: [IdentityExpirationJob.HealthCheckName, TimeSpan.FromMinutes(5)]);

// Handlers de expiração — cada um processa um único tipo de entidade expirável.
// A ordem de registro define a ordem de execução no IdentityExpirationJob.
builder.Services.AddSingleton<IExpirationHandler, DelegationExpirationHandler>();
builder.Services.AddSingleton<IExpirationHandler, BreakGlassExpirationHandler>();
builder.Services.AddSingleton<IExpirationHandler, JitAccessExpirationHandler>();
builder.Services.AddSingleton<IExpirationHandler, AccessReviewExpirationHandler>();
builder.Services.AddSingleton<IExpirationHandler, EnvironmentAccessExpirationHandler>();

builder.Services.AddHostedService<OutboxProcessorJob>();
builder.Services.AddHostedService<IdentityExpirationJob>();

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
