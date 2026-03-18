using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.Governance.Infrastructure;
using Serilog;

/// <summary>
/// Ponto de entrada do NexTraceOne Ingestion API.
///
/// Este serviço é o entry point oficial para integrações externas:
/// - Eventos de deployment (GitHub, GitLab, Jenkins, Azure DevOps)
/// - Eventos de promoção entre ambientes
/// - Atualizações de consumidores e dependências
/// - Sinais de runtime e marcadores operacionais
/// - Sincronização de contratos de fontes externas
///
/// Separado do ApiHost principal para:
/// 1. Isolamento de carga — integrações externas não afetam o portal interno
/// 2. Políticas de rate-limiting diferenciadas por origem
/// 3. Autenticação via API Key (sem sessão de usuário)
/// 4. Preparação para futura extração como serviço independente
/// </summary>
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

// Add Governance Infrastructure for persistence
builder.Services.AddGovernanceInfrastructure(builder.Configuration);

var app = builder.Build();

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ingestion-api" }))
    .WithTags("Health");

// ============================================================
// Deployment Events — recebe notificações de CI/CD
// ============================================================
var deployments = app.MapGroup("/api/v1/deployments")
    .WithTags("Deployments");

deployments.MapPost("/events", async (
    DeploymentEventRequest request,
    IIntegrationConnectorRepository connectorRepo,
    IIngestionExecutionRepository executionRepo,
    IIngestionSourceRepository sourceRepo,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock,
    CancellationToken ct) =>
{
    // Find or create connector
    var connector = await connectorRepo.GetByNameAsync(request.Provider, ct);
    if (connector is null)
    {
        connector = IntegrationConnector.Create(
            name: request.Provider.ToLowerInvariant().Replace(" ", "-"),
            connectorType: "CI/CD",
            description: $"Auto-registered {request.Provider} connector",
            provider: request.Provider,
            endpoint: null,
            utcNow: clock.UtcNow);
        await connectorRepo.AddAsync(connector, ct);
    }

    // Find or create source
    var source = await sourceRepo.GetByConnectorAndNameAsync(connector.Id, request.Source ?? "default", ct);
    if (source is null)
    {
        source = IngestionSource.Create(
            connectorId: connector.Id,
            name: request.Source ?? "default",
            sourceType: "Webhook",
            description: $"Deployment events from {request.Provider}",
            endpoint: null,
            expectedIntervalMinutes: 30,
            utcNow: clock.UtcNow);
        await sourceRepo.AddAsync(source, ct);
    }

    // Create execution
    var execution = IngestionExecution.Start(
        connectorId: connector.Id,
        sourceId: source.Id,
        correlationId: request.CorrelationId ?? Guid.NewGuid().ToString("N"),
        utcNow: clock.UtcNow);

    // Mark completion
    execution.CompleteSuccess(itemsProcessed: 1, itemsSucceeded: 1, utcNow: clock.UtcNow);

    // Update source freshness
    source.RecordDataReceived(itemCount: 1, utcNow: clock.UtcNow);

    // Update connector stats
    connector.RecordSuccess(clock.UtcNow);

    await executionRepo.AddAsync(execution, ct);
    await sourceRepo.UpdateAsync(source, ct);
    await connectorRepo.UpdateAsync(connector, ct);
    await unitOfWork.CommitAsync(ct);

    return Results.Accepted(null, new
    {
        message = "Deployment event received",
        status = "processed",
        executionId = execution.Id.Value
    });
})
.WithDescription("Receives deployment event notifications from CI/CD platforms");

// ============================================================
// Promotion Events — recebe eventos de promoção entre ambientes
// ============================================================
var promotions = app.MapGroup("/api/v1/promotions")
    .WithTags("Promotions");

promotions.MapPost("/events", async (
    PromotionEventRequest request,
    IIntegrationConnectorRepository connectorRepo,
    IIngestionExecutionRepository executionRepo,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock,
    CancellationToken ct) =>
{
    var connector = await connectorRepo.GetByNameAsync("promotions", ct);
    if (connector is null)
    {
        connector = IntegrationConnector.Create(
            name: "promotions",
            connectorType: "Promotions",
            description: "Environment promotion events",
            provider: "Internal",
            endpoint: null,
            utcNow: clock.UtcNow);
        await connectorRepo.AddAsync(connector, ct);
    }

    var execution = IngestionExecution.Start(
        connectorId: connector.Id,
        sourceId: null,
        correlationId: request.CorrelationId ?? Guid.NewGuid().ToString("N"),
        utcNow: clock.UtcNow);

    execution.CompleteSuccess(itemsProcessed: 1, itemsSucceeded: 1, utcNow: clock.UtcNow);
    connector.RecordSuccess(clock.UtcNow);

    await executionRepo.AddAsync(execution, ct);
    await connectorRepo.UpdateAsync(connector, ct);
    await unitOfWork.CommitAsync(ct);

    return Results.Accepted(null, new
    {
        message = "Promotion event received",
        status = "processed",
        executionId = execution.Id.Value
    });
})
.WithDescription("Receives promotion event notifications");

// ============================================================
// Runtime Signals — recebe sinais operacionais
// ============================================================
var runtime = app.MapGroup("/api/v1/runtime")
    .WithTags("Runtime");

runtime.MapPost("/signals", async (
    RuntimeSignalRequest request,
    IIntegrationConnectorRepository connectorRepo,
    IIngestionExecutionRepository executionRepo,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock,
    CancellationToken ct) =>
{
    var connector = await connectorRepo.GetByNameAsync("runtime-signals", ct);
    if (connector is null)
    {
        connector = IntegrationConnector.Create(
            name: "runtime-signals",
            connectorType: "Runtime",
            description: "Runtime signals and markers",
            provider: "Internal",
            endpoint: null,
            utcNow: clock.UtcNow);
        await connectorRepo.AddAsync(connector, ct);
    }

    var execution = IngestionExecution.Start(
        connectorId: connector.Id,
        sourceId: null,
        correlationId: Guid.NewGuid().ToString("N"),
        utcNow: clock.UtcNow);

    execution.CompleteSuccess(itemsProcessed: 1, itemsSucceeded: 1, utcNow: clock.UtcNow);
    connector.RecordSuccess(clock.UtcNow);

    await executionRepo.AddAsync(execution, ct);
    await connectorRepo.UpdateAsync(connector, ct);
    await unitOfWork.CommitAsync(ct);

    return Results.Accepted(null, new
    {
        message = "Runtime signal received",
        status = "processed",
        executionId = execution.Id.Value
    });
})
.WithDescription("Receives runtime signals and markers from monitored services");

// ============================================================
// Consumer Updates — recebe atualizações de dependências
// ============================================================
var consumers = app.MapGroup("/api/v1/consumers")
    .WithTags("Consumers");

consumers.MapPost("/sync", async (
    ConsumerSyncRequest request,
    IIntegrationConnectorRepository connectorRepo,
    IIngestionExecutionRepository executionRepo,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock,
    CancellationToken ct) =>
{
    var connector = await connectorRepo.GetByNameAsync("consumer-sync", ct);
    if (connector is null)
    {
        connector = IntegrationConnector.Create(
            name: "consumer-sync",
            connectorType: "Dependencies",
            description: "Consumer and dependency updates",
            provider: "Internal",
            endpoint: null,
            utcNow: clock.UtcNow);
        await connectorRepo.AddAsync(connector, ct);
    }

    var execution = IngestionExecution.Start(
        connectorId: connector.Id,
        sourceId: null,
        correlationId: Guid.NewGuid().ToString("N"),
        utcNow: clock.UtcNow);

    execution.CompleteSuccess(
        itemsProcessed: request.Consumers?.Count ?? 1,
        itemsSucceeded: request.Consumers?.Count ?? 1,
        utcNow: clock.UtcNow);
    connector.RecordSuccess(clock.UtcNow);

    await executionRepo.AddAsync(execution, ct);
    await connectorRepo.UpdateAsync(connector, ct);
    await unitOfWork.CommitAsync(ct);

    return Results.Accepted(null, new
    {
        message = "Consumer update received",
        status = "processed",
        executionId = execution.Id.Value
    });
})
.WithDescription("Receives consumer/dependency update notifications");

// ============================================================
// Contract Sync — recebe contratos de fontes externas
// ============================================================
var contracts = app.MapGroup("/api/v1/contracts")
    .WithTags("Contracts");

contracts.MapPost("/sync", async (
    ContractSyncRequest request,
    IIntegrationConnectorRepository connectorRepo,
    IIngestionExecutionRepository executionRepo,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock,
    CancellationToken ct) =>
{
    var connector = await connectorRepo.GetByNameAsync("contract-sync", ct);
    if (connector is null)
    {
        connector = IntegrationConnector.Create(
            name: "contract-sync",
            connectorType: "ContractImport",
            description: "External contract synchronization",
            provider: request.Provider ?? "External",
            endpoint: null,
            utcNow: clock.UtcNow);
        await connectorRepo.AddAsync(connector, ct);
    }

    var execution = IngestionExecution.Start(
        connectorId: connector.Id,
        sourceId: null,
        correlationId: Guid.NewGuid().ToString("N"),
        utcNow: clock.UtcNow);

    execution.CompleteSuccess(
        itemsProcessed: request.Contracts?.Count ?? 1,
        itemsSucceeded: request.Contracts?.Count ?? 1,
        utcNow: clock.UtcNow);
    connector.RecordSuccess(clock.UtcNow);

    await executionRepo.AddAsync(execution, ct);
    await connectorRepo.UpdateAsync(connector, ct);
    await unitOfWork.CommitAsync(ct);

    return Results.Accepted(null, new
    {
        message = "Contract sync received",
        status = "processed",
        executionId = execution.Id.Value
    });
})
.WithDescription("Receives contract synchronization from external sources");

app.Run();

// ============================================================
// Request DTOs
// ============================================================

/// <summary>Request for deployment events from CI/CD platforms.</summary>
public sealed record DeploymentEventRequest(
    string Provider,
    string? Source,
    string? CorrelationId,
    string? ServiceName,
    string? Environment,
    string? Version,
    string? CommitSha);

/// <summary>Request for promotion events between environments.</summary>
public sealed record PromotionEventRequest(
    string? CorrelationId,
    string? ServiceName,
    string? FromEnvironment,
    string? ToEnvironment,
    string? Version);

/// <summary>Request for runtime signals from monitored services.</summary>
public sealed record RuntimeSignalRequest(
    string? ServiceName,
    string? SignalType,
    string? Message,
    Dictionary<string, string>? Tags);

/// <summary>Request for consumer/dependency sync.</summary>
public sealed record ConsumerSyncRequest(
    string? ServiceName,
    List<string>? Consumers,
    List<string>? Dependencies);

/// <summary>Request for contract synchronization.</summary>
public sealed record ContractSyncRequest(
    string? Provider,
    List<ContractItem>? Contracts);

/// <summary>Contract item for sync.</summary>
public sealed record ContractItem(
    string Name,
    string Type,
    string? Version,
    string? Content);
