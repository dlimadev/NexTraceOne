using NexTraceOne.Catalog.Infrastructure.Graph.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Diagnostics;

using NexTraceOne.AIKnowledge.Infrastructure.ExternalAI.Persistence;
using NexTraceOne.AIKnowledge.Infrastructure.Orchestration.Persistence;
using NexTraceOne.AuditCompliance.Infrastructure.Persistence;
using NexTraceOne.BuildingBlocks.Security.CookieSession;
using NexTraceOne.Catalog.Infrastructure.Contracts.Persistence;
using NexTraceOne.Catalog.Infrastructure.DependencyGovernance.Persistence;
using NexTraceOne.Catalog.Infrastructure.DeveloperExperience.Persistence;
using NexTraceOne.Catalog.Infrastructure.LegacyAssets.Persistence;
using NexTraceOne.Catalog.Infrastructure.Portal.Persistence;
using NexTraceOne.Catalog.Infrastructure.Templates.Persistence;
using NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence;
using NexTraceOne.ChangeGovernance.Infrastructure.Promotion.Persistence;
using NexTraceOne.ChangeGovernance.Infrastructure.RulesetGovernance.Persistence;
using NexTraceOne.ChangeGovernance.Infrastructure.Workflow.Persistence;
using NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence;
using NexTraceOne.Configuration.Infrastructure.Persistence;
using NexTraceOne.Governance.Infrastructure.Persistence;
using NexTraceOne.IdentityAccess.Infrastructure.Persistence;
using NexTraceOne.Integrations.Infrastructure.Persistence;
using NexTraceOne.Knowledge.Infrastructure.Persistence;
using NexTraceOne.Notifications.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Automation.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Reliability.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.TelemetryStore.Persistence;
using NexTraceOne.ProductAnalytics.Infrastructure.Persistence;
using System.Diagnostics;

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
    /// Executado apenas em ambiente Development ou quando NEXTRACE_AUTO_MIGRATE=true
    /// em ambientes não-Production. Bloqueado incondicionalmente em Production.
    ///
    /// Architecture: All DbContexts use a single physical PostgreSQL database `nextraceone`.
    /// Module isolation is enforced by table prefix per module (iam_, env_, cat_, etc.)
    /// and by independent DbContext per module (or sub-domain).
    ///
    /// All 27 DbContexts are registered across 7 waves ordered by dependency priority.
    /// </summary>
    public static async Task ApplyDatabaseMigrationsAsync(this WebApplication app)
    {
        var isProduction = app.Environment.IsProduction();
        var autoMigrateEnv = string.Equals(
            Environment.GetEnvironmentVariable("NEXTRACE_AUTO_MIGRATE"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (isProduction && autoMigrateEnv)
        {
            var migrationLogger = app.Services.GetRequiredService<ILogger<Program>>();
            migrationLogger.LogCritical(
                "NEXTRACE_AUTO_MIGRATE=true is set in Production environment. " +
                "Auto-migrations are blocked in Production to prevent data loss. " +
                "Use a CI/CD migration pipeline instead. Aborting startup.");
            throw new InvalidOperationException(
                "Auto-migrations are not allowed in Production. " +
                "Use a CI/CD pipeline with 'dotnet ef database update' or a migration runner.");
        }

        var shouldMigrate = app.Environment.IsDevelopment() || autoMigrateEnv;

        if (!shouldMigrate)
            return;

        if (!app.Environment.IsDevelopment())
        {
            var migrationLogger = app.Services.GetRequiredService<ILogger<Program>>();
            migrationLogger.LogWarning(
                "Auto-migrations are running in non-Development environment '{Environment}'. " +
                "This is acceptable for Staging/QA but should not be used in Production. " +
                "Consider using a CI/CD migration pipeline for production deployments.",
                app.Environment.EnvironmentName);
        }

        using var migrationScope = app.Services.CreateScope();
        var logger = migrationScope.ServiceProvider
            .GetRequiredService<ILogger<Program>>();

        var pendingContexts = new List<string>();

        try
        {
            logger.LogInformation(
                "Applying pending database migrations for all 27 DbContexts...");

            // Wave 1 — Foundation (highest priority, all other modules depend on these)
            await MigrateContextAsync<ConfigurationDbContext>(migrationScope, pendingContexts);
            await MigrateContextAsync<IdentityDbContext>(migrationScope, pendingContexts);

            // Wave 2 — Catalog & Contracts (includes sub-domain contexts)
            await MigrateContextAsync<CatalogGraphDbContext>(migrationScope, pendingContexts);
            await MigrateContextAsync<DeveloperPortalDbContext>(migrationScope, pendingContexts);
            await MigrateContextAsync<ContractsDbContext>(migrationScope, pendingContexts);
            await MigrateContextAsync<DeveloperExperienceDbContext>(migrationScope, pendingContexts);
            await MigrateContextAsync<LegacyAssetsDbContext>(migrationScope, pendingContexts);
            await MigrateContextAsync<TemplatesDbContext>(migrationScope, pendingContexts);
            await MigrateContextAsync<DependencyGovernanceDbContext>(migrationScope, pendingContexts);

            // Wave 3 — Change Governance & Operational Intelligence (includes sub-domain contexts)
            await MigrateContextAsync<ChangeIntelligenceDbContext>(migrationScope, pendingContexts);
            await MigrateContextAsync<RulesetGovernanceDbContext>(migrationScope, pendingContexts);
            await MigrateContextAsync<WorkflowDbContext>(migrationScope, pendingContexts);
            await MigrateContextAsync<PromotionDbContext>(migrationScope, pendingContexts);
            await MigrateContextAsync<IncidentDbContext>(migrationScope, pendingContexts);
            await MigrateContextAsync<RuntimeIntelligenceDbContext>(migrationScope, pendingContexts);
            await MigrateContextAsync<CostIntelligenceDbContext>(migrationScope, pendingContexts);
            await MigrateContextAsync<ReliabilityDbContext>(migrationScope, pendingContexts);
            await MigrateContextAsync<AutomationDbContext>(migrationScope, pendingContexts);
            await MigrateContextAsync<TelemetryStoreDbContext>(migrationScope, pendingContexts);

            // Wave 4 — Audit & Governance
            await MigrateContextAsync<AuditDbContext>(migrationScope, pendingContexts);
            await MigrateContextAsync<GovernanceDbContext>(migrationScope, pendingContexts);

            // Wave 5 — Integrations & Product Analytics
            await MigrateContextAsync<IntegrationsDbContext>(migrationScope, pendingContexts);
            await MigrateContextAsync<ProductAnalyticsDbContext>(migrationScope, pendingContexts);

            // Wave 6 — Notifications, Messaging & Knowledge
            await MigrateContextAsync<NotificationsDbContext>(migrationScope, pendingContexts);
            await MigrateContextAsync<KnowledgeDbContext>(migrationScope, pendingContexts);

            // Wave 7 — AI & Knowledge (highest complexity, lowest maturity)
            await MigrateContextAsync<AiGovernanceDbContext>(migrationScope, pendingContexts);
            await MigrateContextAsync<ExternalAiDbContext>(migrationScope, pendingContexts);
            await MigrateContextAsync<AiOrchestrationDbContext>(migrationScope, pendingContexts);

            if (pendingContexts.Count > 0)
            {
                logger.LogInformation(
                    "Migrations applied successfully for: {Contexts}",
                    string.Join(", ", pendingContexts));

                // Apply RLS policies after migrations (idempotent — safe to re-run).
                await ApplyRlsPoliciesAsync(migrationScope, logger);
            }
            else
            {
                logger.LogInformation(
                    "No pending migrations found. All 27 DbContexts are up-to-date.");
            }
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
    /// Seeds mandatory configuration definitions in ALL environments.
    /// The configuration module requires a baseline set of 533+ definitions
    /// to function correctly. This seeder is idempotent — it only inserts
    /// definitions that do not yet exist (checked by key).
    /// Unlike development seed data, this runs in every environment including Production.
    /// </summary>
    public static async Task SeedConfigurationDefinitionsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            var seeder = scope.ServiceProvider
                .GetRequiredService<NexTraceOne.Configuration.Application.Abstractions.IConfigurationDefinitionSeeder>();

            var result = await seeder.SeedAsync();

            if (result.IsNoOp)
            {
                logger.LogInformation(
                    "Configuration definitions already up-to-date. " +
                    "{Skipped} definitions verified (no changes).",
                    result.Skipped);
            }
            else
            {
                logger.LogInformation(
                    "Configuration definitions seeded. " +
                    "Added: {Added}, Already existing: {Skipped}, Total: {Total}.",
                    result.Added, result.Skipped, result.Total);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Configuration definitions seeding failed. " +
                "This may be expected if the Configuration database schema has not been created yet. " +
                "The application will start without baseline configuration definitions.");
        }
    }

    /// <summary>
    /// Seeds mandatory feature flag definitions in ALL environments.
    /// Provides all platform feature flags for runtime toggle and governance.
    /// This seeder is idempotent — only inserts flags that do not yet exist.
    /// Legacy flags (legacy.*) are inserted via migration W00 and are skipped here.
    /// </summary>
    public static async Task SeedFeatureFlagDefinitionsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            var seeder = scope.ServiceProvider
                .GetRequiredService<NexTraceOne.Configuration.Application.Abstractions.IFeatureFlagDefinitionSeeder>();

            var result = await seeder.SeedAsync();

            if (result.IsNoOp)
            {
                logger.LogInformation(
                    "Feature flag definitions already up-to-date. " +
                    "{Skipped} definitions verified (no changes).",
                    result.Skipped);
            }
            else
            {
                logger.LogInformation(
                    "Feature flag definitions seeded. " +
                    "Added: {Added}, Already existing: {Skipped}, Total: {Total}.",
                    result.Added, result.Skipped, result.Total);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Feature flag definitions seeding failed. " +
                "This may be expected if the Configuration database schema has not been created yet. " +
                "The application will start without baseline feature flag definitions.");
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
    /// Delega no GlobalExceptionHandler (IExceptionHandler) registado em DI.
    /// Quando TryHandleAsync retorna true, o ExceptionHandlerMiddleware não emite
    /// o LogError automático — o logging fica sob controlo total do GlobalExceptionHandler.
    /// </summary>
    public static void UseGlobalExceptionHandler(this WebApplication app)
    {
        app.UseExceptionHandler();
    }

    /// <summary>
    /// Aplica validação CSRF global para requests mutáveis autenticados por cookie httpOnly.
    /// Requests Bearer continuam isentos porque não transportam credenciais automáticas do browser.
    /// </summary>
    public static void UseCookieSessionCsrfProtection(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            var options = context.RequestServices
                .GetRequiredService<IOptions<CookieSessionOptions>>()
                .Value;

            if (!options.Enabled || CsrfTokenValidator.IsValid(context, options))
            {
                await next();
                return;
            }

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(new
            {
                title = "Forbidden",
                detail = "CSRF token is missing or invalid.",
                status = StatusCodes.Status403Forbidden,
                code = "csrf_token_invalid",
                traceId = Activity.Current?.Id ?? context.TraceIdentifier
            });
        });
    }

    /// <summary>
    /// Aplica migrações pendentes de um DbContext específico e registra o nome no rastreamento.
    /// Apenas executa MigrateAsync se existirem migrações pendentes.
    /// </summary>
    private static async Task MigrateContextAsync<TContext>(
        IServiceScope scope,
        List<string> pendingContexts,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        var db = scope.ServiceProvider.GetRequiredService<TContext>();
        var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count == 0) return;

        pendingContexts.Add($"{typeof(TContext).Name}({pending.Count})");
        await db.Database.MigrateAsync(cancellationToken);
    }

    /// <summary>
    /// Aplica políticas de Row-Level Security (RLS) após migrações.
    /// Executa o script SQL idempotente que cria/actualiza as políticas RLS
    /// para isolamento por tenant em todas as tabelas elegíveis.
    /// Só é executado quando existem migrações pendentes aplicadas nesta sessão.
    /// Em ambientes containerizados, configura o caminho via NEXTRACE_RLS_SCRIPT_PATH.
    /// </summary>
    private static async Task ApplyRlsPoliciesAsync(
        IServiceScope scope,
        ILogger logger)
    {
        const string rlsScriptRelativePath = "infra/postgres/apply-rls.sql";

        // 1. Check explicit environment variable (works in containers and non-standard deployments).
        var explicitPath = Environment.GetEnvironmentVariable("NEXTRACE_RLS_SCRIPT_PATH");

        // 2. Fall back to solution root discovery (works in development).
        string? rlsScriptPath;
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            rlsScriptPath = explicitPath;
        }
        else
        {
            var contentRoot = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>().ContentRootPath;
            var solutionRoot = FindSolutionRootFrom(contentRoot);
            rlsScriptPath = solutionRoot is not null ? Path.Combine(solutionRoot, rlsScriptRelativePath) : null;
        }

        if (rlsScriptPath is null || !File.Exists(rlsScriptPath))
        {
            logger.LogInformation(
                "RLS script not found (path: '{Path}'). " +
                "RLS policies will not be auto-applied. " +
                "Set NEXTRACE_RLS_SCRIPT_PATH or run 'psql -f {Script}' manually after migrations.",
                rlsScriptPath ?? "(unresolved)", rlsScriptRelativePath);
            return;
        }

        try
        {
            var rlsSql = await File.ReadAllTextAsync(rlsScriptPath);

            // Use the ConfigurationDbContext (Wave 1) to execute RLS — it shares the same database.
            var db = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
            await db.Database.ExecuteSqlRawAsync(rlsSql);

            logger.LogInformation(
                "RLS policies applied successfully from '{ScriptPath}'.",
                rlsScriptPath);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to apply RLS policies from '{ScriptPath}'. " +
                "The application will continue without RLS enforcement. " +
                "Run the script manually: psql -f {Script}",
                rlsScriptPath, rlsScriptRelativePath);
        }
    }

    /// <summary>
    /// Procura a raiz da solução (NexTraceOne.sln) a partir de um caminho.
    /// </summary>
    private static string? FindSolutionRootFrom(string startPath)
    {
        var dir = new DirectoryInfo(startPath);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "NexTraceOne.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        return null;
    }
}
