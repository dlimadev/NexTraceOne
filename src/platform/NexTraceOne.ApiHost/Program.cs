using NexTraceOne.BuildingBlocks.Security.Integrity;
using NexTraceOne.BuildingBlocks.EventBus;
using NexTraceOne.BuildingBlocks.Observability;
using NexTraceOne.BuildingBlocks.Observability.Logging;
using NexTraceOne.BuildingBlocks.Security;
using NexTraceOne.BuildingBlocks.Security.MultiTenancy;
using NexTraceOne.ApiHost;
using NexTraceOne.Identity.API;
using NexTraceOne.Identity.Infrastructure.Persistence;
using NexTraceOne.Licensing.API;
using NexTraceOne.Licensing.Infrastructure.Persistence;
using NexTraceOne.EngineeringGraph.API;
using NexTraceOne.EngineeringGraph.Infrastructure.Persistence;
using NexTraceOne.Contracts.API;
using NexTraceOne.Contracts.Infrastructure.Persistence;
using NexTraceOne.ChangeIntelligence.API;
using NexTraceOne.ChangeIntelligence.Infrastructure.Persistence;
using NexTraceOne.RulesetGovernance.API;
using NexTraceOne.RulesetGovernance.Infrastructure.Persistence;
using NexTraceOne.Workflow.API;
using NexTraceOne.Workflow.Infrastructure.Persistence;
using NexTraceOne.Promotion.API;
using NexTraceOne.Promotion.Infrastructure.Persistence;
using NexTraceOne.Audit.API;
using NexTraceOne.Audit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

// ═══════════════════════════════════════════════════════════════════════════════
// NEXTRACEONE — Sovereign Change Intelligence Platform
// Host de entrada: NexTraceOne.ApiHost
// Arquitetura v2: Archon Pattern — Modular Monolith + Building Blocks
// ═══════════════════════════════════════════════════════════════════════════════

// [1] Verificação de integridade dos assemblies antes de qualquer inicialização
// Habilitado em produção via variável de ambiente. Em dev, definir NEXTRACE_SKIP_INTEGRITY=true
if (!string.Equals(Environment.GetEnvironmentVariable("NEXTRACE_SKIP_INTEGRITY"), "true", StringComparison.OrdinalIgnoreCase))
{
    AssemblyIntegrityChecker.VerifyOrThrow();
}

var builder = WebApplication.CreateBuilder(args);

// [2] Serilog — logger estruturado da plataforma
builder.Host.ConfigureNexTraceSerilog(builder.Configuration);

// [3] Building Blocks transversais
// NOTA: AddBuildingBlocksApplication e AddBuildingBlocksInfrastructure são registrados
// por cada módulo via AddIdentityModule → AddIdentityApplication → AddBuildingBlocksApplication.
// O registro direto aqui garante disponibilidade quando módulos ainda não estão adicionados.
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

// [5] OpenAPI
builder.Services.AddOpenApi();

// [6] CORS para frontend — restringido a métodos e headers necessários
// Segurança: validar que nenhuma origem contém wildcard (*) quando AllowCredentials está ativo.
// Wildcard + Credentials é proibido pela spec CORS e pode causar falhas silenciosas.
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173", "http://localhost:3000"];

foreach (var origin in corsOrigins)
{
    if (origin.Contains('*'))
    {
        throw new InvalidOperationException(
            $"CORS origin '{origin}' contains a wildcard. Wildcards are not allowed when AllowCredentials is enabled. " +
            "Configure explicit origins in 'Cors:AllowedOrigins'.");
    }
}

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(corsOrigins)
            .WithHeaders("Content-Type", "Authorization", "X-Tenant-Id", "X-Requested-With")
            .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH")
            .AllowCredentials());
});

// [7] Tratamento de exceções não capturadas
builder.Services.AddExceptionHandler(_ => { });
builder.Services.AddProblemDetails();

var app = builder.Build();

// ── Auto-migrations: executa em Development ou quando NEXTRACE_AUTO_MIGRATE=true ──
var shouldMigrate = app.Environment.IsDevelopment()
    || string.Equals(
        Environment.GetEnvironmentVariable("NEXTRACE_AUTO_MIGRATE"),
        "true",
        StringComparison.OrdinalIgnoreCase);

if (shouldMigrate)
{
    using var migrationScope = app.Services.CreateScope();
    var logger = migrationScope.ServiceProvider
        .GetRequiredService<ILogger<Program>>();

    var pendingContexts = new List<string>();

    try
    {
        logger.LogInformation("Applying pending database migrations...");

        var identityDb = migrationScope.ServiceProvider
            .GetRequiredService<IdentityDbContext>();
        pendingContexts.Add(nameof(IdentityDbContext));
        await identityDb.Database.MigrateAsync();

        var licensingDb = migrationScope.ServiceProvider
            .GetRequiredService<LicensingDbContext>();
        pendingContexts.Add(nameof(LicensingDbContext));
        await licensingDb.Database.MigrateAsync();

        var engineeringGraphDb = migrationScope.ServiceProvider
            .GetRequiredService<EngineeringGraphDbContext>();
        pendingContexts.Add(nameof(EngineeringGraphDbContext));
        await engineeringGraphDb.Database.MigrateAsync();

        var contractsDb = migrationScope.ServiceProvider
            .GetRequiredService<ContractsDbContext>();
        pendingContexts.Add(nameof(ContractsDbContext));
        await contractsDb.Database.MigrateAsync();

        var changeIntelligenceDb = migrationScope.ServiceProvider
            .GetRequiredService<ChangeIntelligenceDbContext>();
        pendingContexts.Add(nameof(ChangeIntelligenceDbContext));
        await changeIntelligenceDb.Database.MigrateAsync();

        var rulesetGovernanceDb = migrationScope.ServiceProvider
            .GetRequiredService<RulesetGovernanceDbContext>();
        pendingContexts.Add(nameof(RulesetGovernanceDbContext));
        await rulesetGovernanceDb.Database.MigrateAsync();

        var workflowDb = migrationScope.ServiceProvider
            .GetRequiredService<WorkflowDbContext>();
        pendingContexts.Add(nameof(WorkflowDbContext));
        await workflowDb.Database.MigrateAsync();

        var promotionDb = migrationScope.ServiceProvider
            .GetRequiredService<PromotionDbContext>();
        pendingContexts.Add(nameof(PromotionDbContext));
        await promotionDb.Database.MigrateAsync();

        var auditDb = migrationScope.ServiceProvider
            .GetRequiredService<AuditDbContext>();
        pendingContexts.Add(nameof(AuditDbContext));
        await auditDb.Database.MigrateAsync();

        logger.LogInformation(
            "Migrations applied successfully for: {Contexts}",
            string.Join(", ", pendingContexts));
    }
    catch (Exception ex)
    {
        logger.LogError(
            ex,
            "Error applying migrations for contexts: {Contexts}",
            string.Join(", ", pendingContexts));
        throw;
    }
}

// ── Middlewares na ordem correta ──
app.UseHttpsRedirection();

// [Security] Headers de segurança — aplicados antes de qualquer conteúdo
// Estes headers reduzem superfície de ataque contra XSS, clickjacking, MIME sniffing e downgrade de protocolo.
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["X-XSS-Protection"] = "0";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
    // Content-Security-Policy para endpoints API: restringe ao mínimo necessário.
    // O frontend SPA deve ter seu próprio CSP configurado no servidor/proxy que o serve.
    headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
    // Impede cache de respostas sensíveis (API responses com dados de autenticação/tenant)
    headers["Cache-Control"] = "no-store";
    headers["Pragma"] = "no-cache";

    if (!app.Environment.IsDevelopment())
    {
        // HSTS com preload: indica ao navegador que o site deve ser acessado apenas via HTTPS.
        // includeSubDomains garante cobertura para subdomínios de tenant.
        // preload permite inclusão em listas de pré-carregamento dos navegadores.
        headers["Strict-Transport-Security"] = "max-age=63072000; includeSubDomains; preload";
    }

    await next();
});

// Resolve o tenant antes da autenticação/autorização
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
