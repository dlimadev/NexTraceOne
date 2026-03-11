using NexTraceOne.BuildingBlocks.Security.Integrity;
using NexTraceOne.BuildingBlocks.EventBus;
using NexTraceOne.BuildingBlocks.Observability;
using NexTraceOne.BuildingBlocks.Observability.Logging;
using NexTraceOne.BuildingBlocks.Security;
using NexTraceOne.BuildingBlocks.Security.MultiTenancy;
using NexTraceOne.ApiHost;
using NexTraceOne.Identity.API;
using NexTraceOne.Licensing.API;

// ═══════════════════════════════════════════════════════════════════════════════
// NEXTRACEONE — Sovereign Change Intelligence Platform
// Host de entrada: NexTraceOne.ApiHost
// Arquitetura v2: Archon Pattern — Modular Monolith + Building Blocks
// ═══════════════════════════════════════════════════════════════════════════════

// [1] Verificação de integridade dos assemblies antes de qualquer inicialização
// AssemblyIntegrityChecker.VerifyOrThrow(); // TODO: Habilitar em produção

var builder = WebApplication.CreateBuilder(args);

// [2] Serilog
builder.Host.ConfigureNexTraceSerilog(builder.Configuration);

// [3] Building Blocks
// TODO: builder.Services.AddBuildingBlocksApplication(builder.Configuration);
// TODO: builder.Services.AddBuildingBlocksInfrastructure(builder.Configuration);
builder.Services.AddBuildingBlocksEventBus(builder.Configuration);
builder.Services.AddBuildingBlocksObservability(builder.Configuration);
builder.Services.AddBuildingBlocksSecurity(builder.Configuration);

// [4] Módulos — cada um registra sua Application + Infrastructure
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddLicensingModule(builder.Configuration);
// TODO: ... (todos os módulos)

// [5] OpenAPI / Swagger
builder.Services.AddExceptionHandler(_ => { });
builder.Services.AddOpenApi();

// [6] Rate Limiting, CORS, Health Checks
// TODO: builder.Services.AddNexTraceRateLimiting(...);

// [7] Quartz.NET (Outbox Processor, SLA Escalation)
// TODO: builder.Services.AddNexTraceJobs(...);

var app = builder.Build();

// ── Middlewares ──
app.UseHttpsRedirection();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(new
        {
            title = "Unexpected error",
            detail = "An unexpected error occurred while processing the request.",
            status = StatusCodes.Status500InternalServerError
        });
    });
});
app.UseAuthentication();
app.UseAuthorization();

// ── Endpoints de módulos (assembly scanning) ──
app.MapAllModuleEndpoints();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapHealthChecks("/health").AllowAnonymous();

app.Run();
