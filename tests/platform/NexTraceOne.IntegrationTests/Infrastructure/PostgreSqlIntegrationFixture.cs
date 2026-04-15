using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;
using Xunit;
using NexTraceOne.AIKnowledge.Infrastructure.ExternalAI.Persistence;
using NexTraceOne.AIKnowledge.Infrastructure.Governance.Persistence;
using NexTraceOne.AIKnowledge.Infrastructure.Orchestration.Persistence;
using NexTraceOne.AuditCompliance.Infrastructure.Persistence;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Infrastructure.Contracts.Persistence;
using NexTraceOne.Catalog.Infrastructure.Graph.Persistence;
using NexTraceOne.Catalog.Infrastructure.Portal.Persistence;
using NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence;
using NexTraceOne.ChangeGovernance.Infrastructure.Promotion.Persistence;
using NexTraceOne.ChangeGovernance.Infrastructure.RulesetGovernance.Persistence;
using NexTraceOne.ChangeGovernance.Infrastructure.Workflow.Persistence;
using NexTraceOne.Governance.Infrastructure.Persistence;
using NexTraceOne.IdentityAccess.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Cost.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence;

namespace NexTraceOne.IntegrationTests.Infrastructure;

/// <summary>
/// Fixture de integração com PostgreSQL real via Testcontainers.
/// Gerencia 4 bancos de dados consolidados cobrindo todos os 16 DbContexts da plataforma (ADR-001).
/// Todas as tabelas usam prefixos de módulo únicos — incluindo a tabela outbox de cada módulo
/// (ex: wf_outbox_messages, ci_outbox_messages) — permitindo múltiplos DbContexts por database sem conflitos.
///
/// nextrace_it_identity   → IdentityDbContext, AuditDbContext
/// nextrace_it_catalog    → CatalogGraphDbContext, ContractsDbContext, DeveloperPortalDbContext
/// nextrace_it_operations → ChangeIntelligenceDbContext, WorkflowDbContext, PromotionDbContext,
///                          RulesetGovernanceDbContext, IncidentDbContext, RuntimeIntelligenceDbContext,
///                          CostIntelligenceDbContext, GovernanceDbContext
/// nextrace_it_ai         → AiGovernanceDbContext, ExternalAiDbContext, AiOrchestrationDbContext
/// </summary>
public sealed class PostgreSqlIntegrationFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("postgres")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly Dictionary<string, string> _connectionStrings = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Respawner> _respawners = new(StringComparer.OrdinalIgnoreCase);

    private readonly ICurrentTenant _tenant = new TestCurrentTenant();
    private readonly ICurrentUser _user = new TestCurrentUser();
    private readonly IDateTimeProvider _clock = new TestDateTimeProvider();

    // ── 4 consolidated databases (ADR-001) ────────────────────────────────────

    public string IdentityConnectionString => _connectionStrings["identity"];
    public string AuditConnectionString => _connectionStrings["identity"];

    public string CatalogConnectionString => _connectionStrings["catalog"];
    public string ContractsConnectionString => _connectionStrings["catalog"];
    public string DeveloperPortalConnectionString => _connectionStrings["catalog"];

    public string ChangeGovernanceConnectionString => _connectionStrings["operations"];
    public string WorkflowConnectionString => _connectionStrings["operations"];
    public string PromotionConnectionString => _connectionStrings["operations"];
    public string RulesetGovernanceConnectionString => _connectionStrings["operations"];
    public string IncidentsConnectionString => _connectionStrings["operations"];
    public string RuntimeIntelligenceConnectionString => _connectionStrings["operations"];
    public string CostIntelligenceConnectionString => _connectionStrings["operations"];
    public string GovernanceConnectionString => _connectionStrings["operations"];

    public string AiKnowledgeConnectionString => _connectionStrings["ai"];
    public string ExternalAiConnectionString => _connectionStrings["ai"];
    public string AiOrchestrationConnectionString => _connectionStrings["ai"];

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        _connectionStrings["identity"] = await CreateDatabaseAsync("nextrace_it_identity");
        _connectionStrings["catalog"] = await CreateDatabaseAsync("nextrace_it_catalog");
        _connectionStrings["operations"] = await CreateDatabaseAsync("nextrace_it_operations");
        _connectionStrings["ai"] = await CreateDatabaseAsync("nextrace_it_ai");

        await ApplyMigrationsAsync();
        await InitializeRespawnersAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    // ── DbContext factories — Core ────────────────────────────────────────────

    public CatalogGraphDbContext CreateCatalogGraphDbContext()
    {
        return new CatalogGraphDbContext(BuildOptions<CatalogGraphDbContext>(CatalogConnectionString), _tenant, _user, _clock);
    }

    public ContractsDbContext CreateContractsDbContext()
    {
        return new ContractsDbContext(BuildOptions<ContractsDbContext>(ContractsConnectionString), _tenant, _user, _clock);
    }

    public ChangeIntelligenceDbContext CreateChangeIntelligenceDbContext()
    {
        return new ChangeIntelligenceDbContext(BuildOptions<ChangeIntelligenceDbContext>(ChangeGovernanceConnectionString), _tenant, _user, _clock);
    }

    public IdentityDbContext CreateIdentityDbContext()
    {
        return new IdentityDbContext(BuildOptions<IdentityDbContext>(IdentityConnectionString), _tenant, _user, _clock);
    }

    public IncidentDbContext CreateIncidentDbContext()
    {
        return new IncidentDbContext(BuildOptions<IncidentDbContext>(IncidentsConnectionString), _tenant, _user, _clock);
    }

    // ── DbContext factories — Change Governance extensions ───────────────────

    public WorkflowDbContext CreateWorkflowDbContext()
    {
        return new WorkflowDbContext(BuildOptions<WorkflowDbContext>(WorkflowConnectionString), _tenant, _user, _clock);
    }

    public PromotionDbContext CreatePromotionDbContext()
    {
        return new PromotionDbContext(BuildOptions<PromotionDbContext>(PromotionConnectionString), _tenant, _user, _clock);
    }

    public RulesetGovernanceDbContext CreateRulesetGovernanceDbContext()
    {
        return new RulesetGovernanceDbContext(BuildOptions<RulesetGovernanceDbContext>(RulesetGovernanceConnectionString), _tenant, _user, _clock);
    }

    // ── DbContext factories — Catalog extensions ─────────────────────────────

    public DeveloperPortalDbContext CreateDeveloperPortalDbContext()
    {
        return new DeveloperPortalDbContext(BuildOptions<DeveloperPortalDbContext>(DeveloperPortalConnectionString), _tenant, _user, _clock);
    }

    // ── DbContext factories — OperationalIntelligence extensions ─────────────

    public RuntimeIntelligenceDbContext CreateRuntimeIntelligenceDbContext()
    {
        return new RuntimeIntelligenceDbContext(BuildOptions<RuntimeIntelligenceDbContext>(RuntimeIntelligenceConnectionString), _tenant, _user, _clock);
    }

    public CostIntelligenceDbContext CreateCostIntelligenceDbContext()
    {
        return new CostIntelligenceDbContext(BuildOptions<CostIntelligenceDbContext>(CostIntelligenceConnectionString), _tenant, _user, _clock);
    }

    // ── DbContext factories — AIKnowledge ─────────────────────────────────────

    public AiGovernanceDbContext CreateAiGovernanceDbContext()
    {
        return new AiGovernanceDbContext(BuildOptions<AiGovernanceDbContext>(AiKnowledgeConnectionString), _tenant, _user, _clock);
    }

    public ExternalAiDbContext CreateExternalAiDbContext()
    {
        return new ExternalAiDbContext(BuildOptions<ExternalAiDbContext>(ExternalAiConnectionString), _tenant, _user, _clock);
    }

    public AiOrchestrationDbContext CreateAiOrchestrationDbContext()
    {
        return new AiOrchestrationDbContext(BuildOptions<AiOrchestrationDbContext>(AiOrchestrationConnectionString), _tenant, _user, _clock);
    }

    // ── DbContext factories — Governance ──────────────────────────────────────

    public GovernanceDbContext CreateGovernanceDbContext()
    {
        return new GovernanceDbContext(BuildOptions<GovernanceDbContext>(GovernanceConnectionString), _tenant, _user, _clock);
    }

    // ── DbContext factories — Audit ───────────────────────────────────────────

    public AuditDbContext CreateAuditDbContext()
    {
        return new AuditDbContext(BuildOptions<AuditDbContext>(AuditConnectionString), _tenant, _user, _clock);
    }

    // ── Options builder helper ────────────────────────────────────────────────

    /// <summary>
    /// Constrói DbContextOptions suprimindo PendingModelChangesWarning.
    /// No EF Core 10 este warning é elevado a erro, mas em testes de integração
    /// queremos exercitar as migrations existentes independentemente de gaps de modelo.
    /// </summary>
    private static DbContextOptions<TContext> BuildOptions<TContext>(string connectionString)
        where TContext : DbContext
    {
        return new DbContextOptionsBuilder<TContext>()
            .UseNpgsql(connectionString)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;
    }

    // ── Reset & Utilities ─────────────────────────────────────────────────────

    public async Task ResetDatabasesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var item in _connectionStrings)
        {
            await using var connection = new NpgsqlConnection(item.Value);
            await connection.OpenAsync(cancellationToken);
            await _respawners[item.Key].ResetAsync(connection);
        }
    }

    public async Task<int> GetAppliedMigrationsCountAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM \"__EFMigrationsHistory\"";

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    public async Task<string?> GetColumnDataTypeAsync(
        string connectionString,
        string tableName,
        string columnName,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
                              SELECT data_type
                              FROM information_schema.columns
                              WHERE table_schema = 'public'
                                AND table_name = @tableName
                                AND column_name = @columnName
                              LIMIT 1
                              """;
        command.Parameters.AddWithValue("tableName", tableName);
        command.Parameters.AddWithValue("columnName", columnName);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result?.ToString();
    }

    public async Task<bool> TableExistsAsync(
        string connectionString,
        string tableName,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
                              SELECT COUNT(1)
                              FROM information_schema.tables
                              WHERE table_schema = 'public'
                                AND table_name = @tableName
                              """;
        command.Parameters.AddWithValue("tableName", tableName);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result) > 0;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<string> CreateDatabaseAsync(string databaseName)
    {
        var adminConnectionString = new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Database = "postgres"
        }.ToString();

        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();

        await using (var existsCommand = connection.CreateCommand())
        {
            existsCommand.CommandText = "SELECT 1 FROM pg_database WHERE datname = @databaseName";
            existsCommand.Parameters.AddWithValue("databaseName", databaseName);
            var exists = await existsCommand.ExecuteScalarAsync();
            if (exists is null)
            {
                await using var createCommand = connection.CreateCommand();
                createCommand.CommandText = $"CREATE DATABASE \"{databaseName.Replace("\"", "\"\"")}\"";
                await createCommand.ExecuteNonQueryAsync();
            }
        }

        return new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Database = databaseName
        }.ToString();
    }

    private async Task ApplyMigrationsAsync()
    {
        // ── Catalog database ─────────────────────────────────────────────────
        await using var catalogContext = CreateCatalogGraphDbContext();
        await catalogContext.Database.MigrateAsync();

        await using var contractsContext = CreateContractsDbContext();
        await contractsContext.Database.MigrateAsync();

        await using var portalContext = CreateDeveloperPortalDbContext();
        await portalContext.Database.MigrateAsync();

        // ── Change Governance database ───────────────────────────────────────
        await using var changeIntelligenceContext = CreateChangeIntelligenceDbContext();
        await changeIntelligenceContext.Database.MigrateAsync();

        await using var workflowContext = CreateWorkflowDbContext();
        await workflowContext.Database.MigrateAsync();

        await using var promotionContext = CreatePromotionDbContext();
        await promotionContext.Database.MigrateAsync();

        await using var rulesetContext = CreateRulesetGovernanceDbContext();
        await rulesetContext.Database.MigrateAsync();

        // ── Identity database ────────────────────────────────────────────────
        await using var identityContext = CreateIdentityDbContext();
        await identityContext.Database.MigrateAsync();

        // ── Operations database (ChangeGov + OI + Governance) ────────────────
        await using var incidentsContext = CreateIncidentDbContext();
        await incidentsContext.Database.MigrateAsync();

        await using var runtimeContext = CreateRuntimeIntelligenceDbContext();
        await runtimeContext.Database.MigrateAsync();

        await using var costContext = CreateCostIntelligenceDbContext();
        await costContext.Database.MigrateAsync();

        await using var governanceContext = CreateGovernanceDbContext();
        await governanceContext.Database.MigrateAsync();

        // ── Identity database (Identity + Audit) ─────────────────────────────
        await using var auditContext = CreateAuditDbContext();
        await auditContext.Database.MigrateAsync();

        // ── AI database ──────────────────────────────────────────────────────
        await using var aiGovContext = CreateAiGovernanceDbContext();
        await aiGovContext.Database.MigrateAsync();

        await using var externalAiContext = CreateExternalAiDbContext();
        await externalAiContext.Database.MigrateAsync();

        await using var aiOrchContext = CreateAiOrchestrationDbContext();
        await aiOrchContext.Database.MigrateAsync();
    }

    private async Task InitializeRespawnersAsync()
    {
        foreach (var item in _connectionStrings)
        {
            await using var connection = new NpgsqlConnection(item.Value);
            await connection.OpenAsync();

            _respawners[item.Key] = await Respawner.CreateAsync(
                connection,
                new RespawnerOptions
                {
                    DbAdapter = DbAdapter.Postgres,
                    SchemasToInclude = ["public"],
                    TablesToIgnore = [new Table("__EFMigrationsHistory")]
                });
        }
    }

    private sealed class TestCurrentTenant : ICurrentTenant
    {
        public Guid Id { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public string Slug { get; } = "integration-tests";
        public string Name { get; } = "Integration Tests";
        public bool IsActive { get; } = true;

        public bool HasCapability(string capability) => true;
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public string Id { get; } = "integration-user";
        public string Name { get; } = "Integration User";
        public string Email { get; } = "integration.user@nextraceone.local";
        public string? Persona { get; } = null;
        public bool IsAuthenticated { get; } = true;

        public bool HasPermission(string permission) => true;
    }

    private sealed class TestDateTimeProvider : IDateTimeProvider
    {
        private static readonly DateTimeOffset FixedNow = new(2026, 03, 18, 12, 00, 00, TimeSpan.Zero);

        public DateTimeOffset UtcNow => FixedNow;

        public DateOnly UtcToday => DateOnly.FromDateTime(FixedNow.UtcDateTime);
    }
}
