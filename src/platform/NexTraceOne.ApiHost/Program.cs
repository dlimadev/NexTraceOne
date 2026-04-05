using NexTraceOne.BuildingBlocks.Security.Integrity;
using NexTraceOne.BuildingBlocks.Infrastructure;
using NexTraceOne.BuildingBlocks.Observability;
using NexTraceOne.BuildingBlocks.Observability.Logging;
using NexTraceOne.BuildingBlocks.Observability.HealthChecks;
using NexTraceOne.BuildingBlocks.Security;
using NexTraceOne.BuildingBlocks.Security.MultiTenancy;
using NexTraceOne.IdentityAccess.Infrastructure.Context;
using NexTraceOne.ApiHost;
using NexTraceOne.Catalog.API.Graph;
using NexTraceOne.Governance.API;
using NexTraceOne.Integrations.API.Endpoints;
using NexTraceOne.ProductAnalytics.API;
using NexTraceOne.Configuration.API;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using System.Threading.RateLimiting;

using NexTraceOne.Knowledge.API.Endpoints;
using NexTraceOne.AIKnowledge.API.ExternalAI.Endpoints;
using NexTraceOne.AIKnowledge.API.Governance.Endpoints;
using NexTraceOne.AIKnowledge.API.Orchestration.Endpoints;
using NexTraceOne.AIKnowledge.API.Runtime.Endpoints;
using NexTraceOne.AuditCompliance.API.Endpoints;
using NexTraceOne.Catalog.API.Contracts.Endpoints;
using NexTraceOne.Catalog.API.Graph.Endpoints;
using NexTraceOne.Catalog.API.LegacyAssets.Endpoints;
using NexTraceOne.Catalog.API.Portal.Endpoints;
using NexTraceOne.Catalog.API.Templates;
using NexTraceOne.ChangeGovernance.API.ChangeIntelligence.Endpoints;
using NexTraceOne.ChangeGovernance.API.Promotion.Endpoints;
using NexTraceOne.ChangeGovernance.API.RulesetGovernance.Endpoints;
using NexTraceOne.ChangeGovernance.API.Workflow.Endpoints;
using NexTraceOne.IdentityAccess.API.Endpoints;
using NexTraceOne.Notifications.API.Endpoints;
using NexTraceOne.OperationalIntelligence.API.Cost.Endpoints;
using NexTraceOne.OperationalIntelligence.API.Reliability.Endpoints;
using NexTraceOne.OperationalIntelligence.API.Runtime.Endpoints;

// ═══════════════════════════════════════════════════════════════════════════════
// NEXTRACEONE — Sovereign Change Intelligence Platform
// Host de entrada: NexTraceOne.ApiHost
// Arquitetura v2: Archon Pattern — Modular Monolith + Building Blocks
// ═══════════════════════════════════════════════════════════════════════════════

// [1] Verificação de integridade dos assemblies antes de qualquer inicialização
if (!string.Equals(Environment.GetEnvironmentVariable("NEXTRACE_SKIP_INTEGRITY"), "true", StringComparison.OrdinalIgnoreCase))
{
    AssemblyIntegrityChecker.VerifyOrThrow();
}

var builder = WebApplication.CreateBuilder(args);

// [2] Serilog — logger estruturado da plataforma
builder.Host.ConfigureNexTraceSerilog(builder.Configuration);

// [3] Building Blocks transversais
builder.Services.AddBuildingBlocksEventBus(builder.Configuration);
builder.Services.AddBuildingBlocksObservability(builder.Configuration);
builder.Services.AddBuildingBlocksSecurity(builder.Configuration);
builder.Services.AddApiHostOperationalHealthChecks();

// [3.0] In-process caching — required by ConfigurationCacheService and other modules
builder.Services.AddMemoryCache();

// [3.1] Platform health provider — liga GetPlatformHealth a health checks reais
builder.Services.AddSingleton<NexTraceOne.Governance.Application.Abstractions.IPlatformHealthProvider,
    NexTraceOne.ApiHost.HealthCheckPlatformHealthProvider>();

// [4] Módulos — cada um registra sua Application + Infrastructure + DI
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddCatalogGraphModule(builder.Configuration);
builder.Services.AddCatalogLegacyAssetsModule(builder.Configuration);
builder.Services.AddContractsModule(builder.Configuration);
builder.Services.AddChangeIntelligenceModule(builder.Configuration);
builder.Services.AddRulesetGovernanceModule(builder.Configuration);
builder.Services.AddWorkflowModule(builder.Configuration);
builder.Services.AddPromotionModule(builder.Configuration);
builder.Services.AddAuditModule(builder.Configuration);
builder.Services.AddDeveloperPortalModule(builder.Configuration);
builder.Services.AddCatalogTemplatesModule(builder.Configuration);
builder.Services.AddGovernanceModule(builder.Configuration);
builder.Services.AddIntegrationsModule(builder.Configuration);
builder.Services.AddProductAnalyticsModule(builder.Configuration);
builder.Services.AddRuntimeIntelligenceModule(builder.Configuration);
builder.Services.AddReliabilityModule(builder.Configuration);
builder.Services.AddCostIntelligenceModule(builder.Configuration);
builder.Services.AddAiGovernanceModule(builder.Configuration);
builder.Services.AddExternalAiModule(builder.Configuration);
builder.Services.AddAiOrchestrationModule(builder.Configuration);
builder.Services.AddAiRuntimeModule(builder.Configuration);
builder.Services.AddNotificationsModule(builder.Configuration);
builder.Services.AddConfigurationModule(builder.Configuration);
builder.Services.AddKnowledgeModule(builder.Configuration);

// [5] JSON — serialização de enums como strings para Minimal API
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// [6] OpenAPI
builder.Services.AddOpenApi();

// [7] CORS — validação de origens e configuração restritiva
builder.AddCorsConfiguration();

// [8] Rate Limiting — proteção contra abuso nas APIs públicas
var rateLimitingOptions = builder.Configuration
    .GetSection(RateLimitingOptions.SectionName)
    .Get<RateLimitingOptions>() ?? new RateLimitingOptions();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Política global: janela fixa por IP, com limite mais restritivo para IPs não resolvidos
    // (clientes atrás de proxy sem X-Forwarded-For), para mitigar bypass de rate limiting.
    var globalOpts = rateLimitingOptions.Global;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();
        var permitLimit = remoteIp is not null ? globalOpts.PermitLimit : globalOpts.UnresolvedIpPermitLimit;

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: remoteIp ?? "unresolved-ip",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(globalOpts.WindowMinutes),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = globalOpts.QueueLimit
            });
    });

    // Política "auth" — endpoints de autenticação (login, refresh, federated, oidc).
    // Protege contra brute force e credential stuffing.
    var authOpts = rateLimitingOptions.Auth;
    options.AddPolicy("auth", context =>
    {
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"auth:{remoteIp ?? "unresolved-ip"}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authOpts.PermitLimit,
                Window = TimeSpan.FromMinutes(authOpts.WindowMinutes),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = authOpts.QueueLimit
            });
    });

    // Política "auth-sensitive" — operações sensíveis (register, OIDC start, cookie session).
    var authSensitiveOpts = rateLimitingOptions.AuthSensitive;
    options.AddPolicy("auth-sensitive", context =>
    {
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"auth-sensitive:{remoteIp ?? "unresolved-ip"}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authSensitiveOpts.PermitLimit,
                Window = TimeSpan.FromMinutes(authSensitiveOpts.WindowMinutes),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = authSensitiveOpts.QueueLimit
            });
    });

    // Política "ai" — endpoints de IA (chat, geração, retrieval, análise).
    var aiOpts = rateLimitingOptions.Ai;
    options.AddPolicy("ai", context =>
    {
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"ai:{remoteIp ?? "unresolved-ip"}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = aiOpts.PermitLimit,
                Window = TimeSpan.FromMinutes(aiOpts.WindowMinutes),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = aiOpts.QueueLimit
            });
    });

    // Política "data-intensive" — dados intensivos (catálogo, analytics, runtime queries, relatórios).
    var dataIntensiveOpts = rateLimitingOptions.DataIntensive;
    options.AddPolicy("data-intensive", context =>
    {
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"data-intensive:{remoteIp ?? "unresolved-ip"}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = dataIntensiveOpts.PermitLimit,
                Window = TimeSpan.FromMinutes(dataIntensiveOpts.WindowMinutes),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = dataIntensiveOpts.QueueLimit
            });
    });

    // Política "operations" — endpoints operacionais (incidentes, automação, observabilidade, health).
    var operationsOpts = rateLimitingOptions.Operations;
    options.AddPolicy("operations", context =>
    {
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"operations:{remoteIp ?? "unresolved-ip"}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = operationsOpts.PermitLimit,
                Window = TimeSpan.FromMinutes(operationsOpts.WindowMinutes),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = operationsOpts.QueueLimit
            });
    });
});

// [9] Tratamento de exceções não capturadas
builder.Services.AddExceptionHandler(_ => { });
builder.Services.AddProblemDetails();

// [10] Compressão de respostas — melhoria de performance nas APIs
builder.Services.AddResponseCompression();

var app = builder.Build();

// ── Validação de configuração crítica ──
app.ValidateStartupConfiguration();

// ── Lifecycle logging — registo de arranque e encerramento gracioso ──
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStarted.Register(() => app.Logger.LogInformation("NexTraceOne API host started successfully. Environment: {Environment}", app.Environment.EnvironmentName));
lifetime.ApplicationStopping.Register(() => app.Logger.LogInformation("NexTraceOne API host is shutting down gracefully..."));

// ── Auto-migrations ──
await app.ApplyDatabaseMigrationsAsync();

// ── Seed de definições de configuração (idempotente, todos os ambientes) ──
await app.SeedConfigurationDefinitionsAsync();

// ── Seed de dados de autorização (idempotente, todos os ambientes) ──
// Popula iam_role_permissions e iam_module_access_policies a partir dos catálogos.
// Necessário para que o pipeline de autorização (cascata JWT → DB → ModuleAccess → JIT)
// funcione com dados persistidos em vez de depender apenas do fallback estático.
await app.SeedAuthorizationDataAsync();

// ── Seed data de desenvolvimento (idempotente, apenas em Development) ──
await app.SeedDevelopmentDataAsync();

// ── Middlewares na ordem correta ──
app.UseResponseCompression();
app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiter();
app.UseSecurityHeaders();
app.UseGlobalExceptionHandler();
app.UseCookieSessionCsrfProtection();
app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<EnvironmentResolutionMiddleware>();
app.UseAuthorization();

// ── Endpoints ──
app.MapAllModuleEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "NexTraceOne API";
        options.Theme = ScalarTheme.BluePlanet;
    });
}

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

// Expose Program for WebApplicationFactory in E2E and integration tests
public partial class Program { }

