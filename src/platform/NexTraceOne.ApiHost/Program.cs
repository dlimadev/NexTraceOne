using NexTraceOne.BuildingBlocks.Security.Integrity;
using NexTraceOne.BuildingBlocks.EventBus;
using NexTraceOne.BuildingBlocks.Observability;
using NexTraceOne.BuildingBlocks.Observability.Logging;
using NexTraceOne.BuildingBlocks.Security;
using NexTraceOne.BuildingBlocks.Security.MultiTenancy;
using NexTraceOne.ApiHost;
using NexTraceOne.Identity.API;
using NexTraceOne.Licensing.API;
using NexTraceOne.EngineeringGraph.API;
using NexTraceOne.Contracts.API;
using NexTraceOne.ChangeIntelligence.API;
using NexTraceOne.RulesetGovernance.API;
using NexTraceOne.Workflow.API;
using NexTraceOne.Promotion.API;
using NexTraceOne.Audit.API;
using NexTraceOne.DeveloperPortal.API;
using Scalar.AspNetCore;
using System.Threading.RateLimiting;

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

// [4] Módulos — cada um registra sua Application + Infrastructure + DI
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddLicensingModule(builder.Configuration);
builder.Services.AddEngineeringGraphModule(builder.Configuration);
builder.Services.AddContractsModule(builder.Configuration);
builder.Services.AddChangeIntelligenceModule(builder.Configuration);
builder.Services.AddRulesetGovernanceModule(builder.Configuration);
builder.Services.AddWorkflowModule(builder.Configuration);
builder.Services.AddPromotionModule(builder.Configuration);
builder.Services.AddAuditModule(builder.Configuration);
builder.Services.AddDeveloperPortalModule(builder.Configuration);

// [5] OpenAPI
builder.Services.AddOpenApi();

// [6] CORS — validação de origens e configuração restritiva
builder.AddCorsConfiguration();

// [7] Rate Limiting — proteção contra abuso nas APIs públicas
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Política global: janela fixa de 60s com limite de 100 requisições por IP.
    // Clientes sem IP resolvido (atrás de proxy sem X-Forwarded-For)
    // recebem um limite mais restritivo para mitigar bypass de rate limiting.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();
        var permitLimit = remoteIp is not null ? 100 : 20;

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: remoteIp ?? "unresolved-ip",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 5
            });
    });
});

// [8] Tratamento de exceções não capturadas
builder.Services.AddExceptionHandler(_ => { });
builder.Services.AddProblemDetails();

var app = builder.Build();

// ── Auto-migrations ──
await app.ApplyDatabaseMigrationsAsync();

// ── Middlewares na ordem correta ──
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseSecurityHeaders();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseGlobalExceptionHandler();
app.UseAuthentication();
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

app.MapHealthChecks("/health").AllowAnonymous();

app.Run();
