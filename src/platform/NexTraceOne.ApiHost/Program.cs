using NexTraceOne.BuildingBlocks.Security.Integrity;

// ═══════════════════════════════════════════════════════════════════════════════
// NEXTRACEONE — Sovereign Change Intelligence Platform
// Host de entrada: NexTraceOne.ApiHost
// Arquitetura v2: Archon Pattern — Modular Monolith + Building Blocks
// ═══════════════════════════════════════════════════════════════════════════════

// [1] Verificação de integridade dos assemblies antes de qualquer inicialização
// AssemblyIntegrityChecker.VerifyOrThrow(); // TODO: Habilitar em produção

var builder = WebApplication.CreateBuilder(args);

// [2] Serilog
// TODO: builder.Host.UseSerilog(...)

// [3] Building Blocks
// TODO: builder.Services.AddBuildingBlocksApplication(builder.Configuration);
// TODO: builder.Services.AddBuildingBlocksInfrastructure(builder.Configuration);
// TODO: builder.Services.AddBuildingBlocksEventBus(builder.Configuration);
// TODO: builder.Services.AddBuildingBlocksObservability(builder.Configuration);
// TODO: builder.Services.AddBuildingBlocksSecurity(builder.Configuration);

// [4] Módulos — cada um registra sua Application + Infrastructure
// TODO: builder.Services.AddIdentityModule(builder.Configuration);
// TODO: builder.Services.AddLicensingModule(builder.Configuration);
// TODO: ... (todos os módulos)

// [5] OpenAPI / Swagger
builder.Services.AddOpenApi();

// [6] Rate Limiting, CORS, Health Checks
// TODO: builder.Services.AddNexTraceRateLimiting(...);

// [7] Quartz.NET (Outbox Processor, SLA Escalation)
// TODO: builder.Services.AddNexTraceJobs(...);

var app = builder.Build();

// ── Middlewares ──
app.UseHttpsRedirection();
// TODO: app.UseMiddleware<TenantResolutionMiddleware>();
// TODO: app.UseNexTraceExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

// ── Endpoints de módulos (assembly scanning) ──
// TODO: app.MapAllModuleEndpoints();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

// Health check público
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Version = "2.0.0" }))
   .WithTags("Platform")
   .AllowAnonymous();

app.Run();
