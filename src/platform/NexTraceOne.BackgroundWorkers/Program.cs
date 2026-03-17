using NexTraceOne.BackgroundWorkers;
using NexTraceOne.BackgroundWorkers.Jobs;
using NexTraceOne.BackgroundWorkers.Jobs.ExpirationHandlers;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.Identity.Infrastructure;
using NexTraceOne.IdentityAccess.Infrastructure;

// ═══════════════════════════════════════════════════════════════════════════════
// NEXTRACEONE — Background Workers
// Processa: Outbox Messages, Quartz Jobs, SLA Escalation, Cost Ingestion
// ═══════════════════════════════════════════════════════════════════════════════

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IDateTimeProvider, WorkerDateTimeProvider>();
builder.Services.AddSingleton<ICurrentUser, WorkerCurrentUser>();
builder.Services.AddSingleton<ICurrentTenant, WorkerCurrentTenant>();

builder.Services.AddBuildingBlocksEventBus(builder.Configuration);
builder.Services.AddIdentityInfrastructure(builder.Configuration);

// Handlers de expiração — cada um processa um único tipo de entidade expirável.
// A ordem de registro define a ordem de execução no IdentityExpirationJob.
builder.Services.AddSingleton<IExpirationHandler, DelegationExpirationHandler>();
builder.Services.AddSingleton<IExpirationHandler, BreakGlassExpirationHandler>();
builder.Services.AddSingleton<IExpirationHandler, JitAccessExpirationHandler>();
builder.Services.AddSingleton<IExpirationHandler, AccessReviewExpirationHandler>();
builder.Services.AddSingleton<IExpirationHandler, EnvironmentAccessExpirationHandler>();

builder.Services.AddHostedService<OutboxProcessorJob>();
builder.Services.AddHostedService<IdentityExpirationJob>();

var host = builder.Build();
host.Run();
