using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NexTraceOne.BuildingBlocks.Application;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.HealthChecks;
using NexTraceOne.BuildingBlocks.Observability.HealthChecks;
using NexTraceOne.BuildingBlocks.Security;
using NexTraceOne.BuildingBlocks.Security.Authentication;
using NexTraceOne.BuildingBlocks.Security.MultiTenancy;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;
using NexTraceOne.Governance.Infrastructure;
using NexTraceOne.Governance.Infrastructure.Persistence;
using Serilog;
using System.Diagnostics;

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

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler(_ => { });
builder.Services.AddBuildingBlocksApplication(builder.Configuration);
builder.Services.AddNexTraceHealthChecks();
builder.Services.AddHealthChecks()
    .AddCheck<DbContextConnectivityHealthCheck<GovernanceDbContext>>(
        "governance-db",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready", "health"]);
builder.Services.AddBuildingBlocksSecurity(builder.Configuration);
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(IngestionApiSecurity.PolicyName, policy =>
    {
        policy.AuthenticationSchemes.Add(ApiKeyAuthenticationOptions.SchemeName);
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("auth_method", "api_key");
        policy.RequireClaim("permissions", IngestionApiSecurity.RequiredPermission);
    });
});

// Add Governance Infrastructure for persistence
builder.Services.AddGovernanceInfrastructure(builder.Configuration);

var app = builder.Build();

ValidateIngestionSecurityConfiguration(app);

app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["X-XSS-Protection"] = "0";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
    headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
    headers["Cache-Control"] = "no-store";
    headers["Pragma"] = "no-cache";

    if (!app.Environment.IsDevelopment())
    {
        headers["Strict-Transport-Security"] = "max-age=63072000; includeSubDomains; preload";
    }

    await next();
});
app.UseExceptionHandler();
app.UseStatusCodePages(async statusCodeContext =>
{
    var httpContext = statusCodeContext.HttpContext;
    if (httpContext.Response.HasStarted)
    {
        return;
    }

    var statusCode = httpContext.Response.StatusCode;
    if (statusCode is not (StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden))
    {
        return;
    }

    var detail = statusCode == StatusCodes.Status401Unauthorized
        ? "A valid ingestion API key is required."
        : "The authenticated ingestion client is not authorized for this operation.";

    await Results.Problem(
        statusCode: statusCode,
        title: statusCode == StatusCodes.Status401Unauthorized ? "Unauthorized" : "Forbidden",
        detail: detail,
        extensions: new Dictionary<string, object?>
        {
            ["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier
        })
        .ExecuteAsync(httpContext);
});
app.Use(async (context, next) =>
{
    var correlationId = ResolveCorrelationId(context);
    context.Response.Headers[IngestionApiSecurity.CorrelationHeaderName] = correlationId;
    await next();
});
app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

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

// ============================================================
// Deployment Events — recebe notificações de CI/CD
// ============================================================
var deployments = app.MapGroup("/api/v1/deployments")
    .WithTags("Deployments")
    .RequireAuthorization(IngestionApiSecurity.PolicyName);

deployments.MapPost("/events", async (
    HttpContext httpContext,
    DeploymentEventRequest request,
    IIntegrationConnectorRepository connectorRepo,
    IIngestionExecutionRepository executionRepo,
    IIngestionSourceRepository sourceRepo,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock,
    CancellationToken ct) =>
{
    var correlationId = ResolveCorrelationId(httpContext, request.CorrelationId);

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
        correlationId: correlationId,
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
        status = "accepted",
        processingStatus = "metadata_recorded",
        note = "Event metadata and execution tracked. Payload processing into domain entities is planned for a future release.",
        correlationId,
        executionId = execution.Id.Value
    });
})
.WithDescription("Receives deployment event notifications from CI/CD platforms");

// ============================================================
// Promotion Events — recebe eventos de promoção entre ambientes
// ============================================================
var promotions = app.MapGroup("/api/v1/promotions")
    .WithTags("Promotions")
    .RequireAuthorization(IngestionApiSecurity.PolicyName);

promotions.MapPost("/events", async (
    HttpContext httpContext,
    PromotionEventRequest request,
    IIntegrationConnectorRepository connectorRepo,
    IIngestionExecutionRepository executionRepo,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock,
    CancellationToken ct) =>
{
    var correlationId = ResolveCorrelationId(httpContext, request.CorrelationId);

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
        correlationId: correlationId,
        utcNow: clock.UtcNow);

    execution.CompleteSuccess(itemsProcessed: 1, itemsSucceeded: 1, utcNow: clock.UtcNow);
    connector.RecordSuccess(clock.UtcNow);

    await executionRepo.AddAsync(execution, ct);
    await connectorRepo.UpdateAsync(connector, ct);
    await unitOfWork.CommitAsync(ct);

    return Results.Accepted(null, new
    {
        message = "Promotion event received",
        status = "accepted",
        processingStatus = "metadata_recorded",
        note = "Event metadata and execution tracked. Payload processing into domain entities is planned for a future release.",
        correlationId,
        executionId = execution.Id.Value
    });
})
.WithDescription("Receives promotion event notifications");

// ============================================================
// Runtime Signals — recebe sinais operacionais
// ============================================================
var runtime = app.MapGroup("/api/v1/runtime")
    .WithTags("Runtime")
    .RequireAuthorization(IngestionApiSecurity.PolicyName);

runtime.MapPost("/signals", async (
    HttpContext httpContext,
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

    var correlationId = ResolveCorrelationId(httpContext);
    var execution = IngestionExecution.Start(
        connectorId: connector.Id,
        sourceId: null,
        correlationId: correlationId,
        utcNow: clock.UtcNow);

    execution.CompleteSuccess(itemsProcessed: 1, itemsSucceeded: 1, utcNow: clock.UtcNow);
    connector.RecordSuccess(clock.UtcNow);

    await executionRepo.AddAsync(execution, ct);
    await connectorRepo.UpdateAsync(connector, ct);
    await unitOfWork.CommitAsync(ct);

    return Results.Accepted(null, new
    {
        message = "Runtime signal received",
        status = "accepted",
        processingStatus = "metadata_recorded",
        note = "Signal metadata and execution tracked. Payload processing into domain entities is planned for a future release.",
        correlationId,
        executionId = execution.Id.Value
    });
})
.WithDescription("Receives runtime signals and markers from monitored services");


// ============================================================
// Consumer Updates — recebe atualizações de dependências
// ============================================================
var consumers = app.MapGroup("/api/v1/consumers")
    .WithTags("Consumers")
    .RequireAuthorization(IngestionApiSecurity.PolicyName);

consumers.MapPost("/sync", async (
    HttpContext httpContext,
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

    var correlationId = ResolveCorrelationId(httpContext);
    var execution = IngestionExecution.Start(
        connectorId: connector.Id,
        sourceId: null,
        correlationId: correlationId,
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
        status = "accepted",
        processingStatus = "metadata_recorded",
        note = "Update metadata and execution tracked. Payload processing into domain entities is planned for a future release.",
        correlationId,
        executionId = execution.Id.Value
    });
})
.WithDescription("Receives consumer/dependency update notifications");

// ============================================================
// Contract Sync — recebe contratos de fontes externas
// ============================================================
var contracts = app.MapGroup("/api/v1/contracts")
    .WithTags("Contracts")
    .RequireAuthorization(IngestionApiSecurity.PolicyName);

contracts.MapPost("/sync", async (
    HttpContext httpContext,
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

    var correlationId = ResolveCorrelationId(httpContext);
    var execution = IngestionExecution.Start(
        connectorId: connector.Id,
        sourceId: null,
        correlationId: correlationId,
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
        status = "accepted",
        processingStatus = "metadata_recorded",
        note = "Sync metadata and execution tracked. Payload processing into domain entities is planned for a future release.",
        correlationId,
        executionId = execution.Id.Value
    });
})
.WithDescription("Receives contract synchronization from external sources");

app.Run();

static string ResolveCorrelationId(HttpContext httpContext, string? requestCorrelationId = null)
{
    var correlationId = !string.IsNullOrWhiteSpace(requestCorrelationId)
        ? requestCorrelationId
        : httpContext.Request.Headers[IngestionApiSecurity.CorrelationHeaderName].FirstOrDefault();

    correlationId = string.IsNullOrWhiteSpace(correlationId)
        ? Activity.Current?.Id ?? httpContext.TraceIdentifier
        : correlationId;

    httpContext.Response.Headers[IngestionApiSecurity.CorrelationHeaderName] = correlationId;
    return correlationId;
}

static void ValidateIngestionSecurityConfiguration(WebApplication app)
{
    var configuredKeys = app.Configuration
        .GetSection("Security:ApiKeys")
        .Get<List<ApiKeyConfiguration>>() ?? [];

    var validKeys = configuredKeys
        .Where(IsValidIngestionApiKey)
        .ToList();

    if (validKeys.Count == 0)
    {
        const string message = "Ingestion.Api requires at least one API key with a valid tenant and 'integrations:write' permission configured under 'Security:ApiKeys'.";

        if (app.Environment.IsDevelopment())
        {
            app.Logger.LogWarning("{Message} Requests will be rejected until a valid API key is configured via appsettings, secrets or environment variables.", message);
            return;
        }

        throw new InvalidOperationException(message);
    }

    app.Logger.LogInformation(
        "Ingestion.Api security initialized with {ApiKeyCount} API key client(s): {ClientIds}",
        validKeys.Count,
        string.Join(", ", validKeys.Select(key => key.ClientId)));
}

static bool IsValidIngestionApiKey(ApiKeyConfiguration apiKey)
    => !string.IsNullOrWhiteSpace(apiKey.Key)
        && !string.IsNullOrWhiteSpace(apiKey.ClientId)
        && !string.IsNullOrWhiteSpace(apiKey.ClientName)
        && Guid.TryParse(apiKey.TenantId, out _)
        && apiKey.Permissions.Any(permission => string.Equals(permission, IngestionApiSecurity.RequiredPermission, StringComparison.OrdinalIgnoreCase));

internal static class IngestionApiSecurity
{
    internal const string PolicyName = "IngestionApiKeyWrite";
    internal const string RequiredPermission = "integrations:write";
    internal const string CorrelationHeaderName = "X-Correlation-Id";
}

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
