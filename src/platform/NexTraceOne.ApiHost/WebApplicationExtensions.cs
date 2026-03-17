using NexTraceOne.Catalog.Infrastructure.Graph.Persistence;
using NexTraceOne.Audit.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence;
using Microsoft.EntityFrameworkCore;

using NexTraceOne.AuditCompliance.Infrastructure.Persistence;
using NexTraceOne.Catalog.Infrastructure.Contracts.Persistence;
using NexTraceOne.Catalog.Infrastructure.Portal.Persistence;
using NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence;
using NexTraceOne.ChangeGovernance.Infrastructure.Promotion.Persistence;
using NexTraceOne.ChangeGovernance.Infrastructure.RulesetGovernance.Persistence;
using NexTraceOne.ChangeGovernance.Infrastructure.Workflow.Persistence;
using NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence;
using NexTraceOne.IdentityAccess.Infrastructure.Persistence;

namespace NexTraceOne.ApiHost;

/// <summary>
/// Extension methods para configuração do pipeline de middleware e inicialização do host.
/// Centralizam responsabilidades que antes estavam dispersas no Program.cs,
/// melhorando legibilidade e permitindo testes isolados de cada configuração.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Aplica migrações pendentes de todos os DbContexts de módulos registrados.
    /// Executado em ambiente Development ou quando NEXTRACE_AUTO_MIGRATE=true.
    /// Cada módulo possui seu próprio DbContext com migrações independentes,
    /// garantindo isolamento entre bounded contexts.
    /// </summary>
    public static async Task ApplyDatabaseMigrationsAsync(this WebApplication app)
    {
        var shouldMigrate = app.Environment.IsDevelopment()
            || string.Equals(
                Environment.GetEnvironmentVariable("NEXTRACE_AUTO_MIGRATE"),
                "true",
                StringComparison.OrdinalIgnoreCase);

        if (!shouldMigrate)
            return;

        using var migrationScope = app.Services.CreateScope();
        var logger = migrationScope.ServiceProvider
            .GetRequiredService<ILogger<Program>>();

        var pendingContexts = new List<string>();

        try
        {
            logger.LogInformation("Applying pending database migrations...");

            await MigrateContextAsync<IdentityDbContext>(migrationScope, pendingContexts);
            await MigrateContextAsync<CatalogGraphDbContext>(migrationScope, pendingContexts);
            await MigrateContextAsync<ContractsDbContext>(migrationScope, pendingContexts);
            await MigrateContextAsync<ChangeIntelligenceDbContext>(migrationScope, pendingContexts);
            await MigrateContextAsync<RulesetGovernanceDbContext>(migrationScope, pendingContexts);
            await MigrateContextAsync<WorkflowDbContext>(migrationScope, pendingContexts);
            await MigrateContextAsync<PromotionDbContext>(migrationScope, pendingContexts);
            await MigrateContextAsync<AuditDbContext>(migrationScope, pendingContexts);
            await MigrateContextAsync<DeveloperPortalDbContext>(migrationScope, pendingContexts);
            await MigrateContextAsync<IncidentDbContext>(migrationScope, pendingContexts);
            await MigrateContextAsync<AiGovernanceDbContext>(migrationScope, pendingContexts);

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

    /// <summary>
    /// Configura headers de segurança HTTP aplicados a todas as respostas.
    /// Protege contra XSS, clickjacking, MIME sniffing e downgrade de protocolo.
    /// O frontend SPA deve ter seu próprio CSP configurado no servidor/proxy que o serve.
    /// </summary>
    public static void UseSecurityHeaders(this WebApplication app)
    {
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
    }

    /// <summary>
    /// Configura o tratamento global de exceções não capturadas.
    /// Retorna ProblemDetails padronizado sem expor detalhes internos (stack traces, tipos).
    /// </summary>
    public static void UseGlobalExceptionHandler(this WebApplication app)
    {
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
    }

    /// <summary>
    /// Aplica migrações de um DbContext específico e registra o nome no rastreamento.
    /// </summary>
    private static async Task MigrateContextAsync<TContext>(
        IServiceScope scope,
        List<string> pendingContexts)
        where TContext : DbContext
    {
        var db = scope.ServiceProvider.GetRequiredService<TContext>();
        pendingContexts.Add(typeof(TContext).Name);
        await db.Database.MigrateAsync();
    }
}
