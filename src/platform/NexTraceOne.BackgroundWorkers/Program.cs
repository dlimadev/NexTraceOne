using NexTraceOne.BackgroundWorkers;
using NexTraceOne.BackgroundWorkers.Jobs;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.EventBus;
using NexTraceOne.Identity.Infrastructure;

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
builder.Services.AddHostedService<OutboxProcessorJob>();
builder.Services.AddHostedService<IdentityExpirationJob>();

var host = builder.Build();
host.Run();
