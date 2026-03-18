using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;
using Xunit;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Catalog.Infrastructure.Contracts.Persistence;
using NexTraceOne.Catalog.Infrastructure.Graph.Persistence;
using NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Persistence;
using NexTraceOne.IdentityAccess.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Infrastructure.Incidents.Persistence;

namespace NexTraceOne.IntegrationTests.Infrastructure;

public sealed class PostgreSqlIntegrationFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("postgres")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly Dictionary<string, string> _connectionStrings = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Respawner> _respawners = new(StringComparer.OrdinalIgnoreCase);

    private readonly ICurrentTenant _tenant = new TestCurrentTenant();
    private readonly ICurrentUser _user = new TestCurrentUser();
    private readonly IDateTimeProvider _clock = new TestDateTimeProvider();

    public string CatalogConnectionString => _connectionStrings["catalog"];
    public string ContractsConnectionString => _connectionStrings["contracts"];
    public string ChangeGovernanceConnectionString => _connectionStrings["change-governance"];
    public string IdentityConnectionString => _connectionStrings["identity"];
    public string IncidentsConnectionString => _connectionStrings["incidents"];

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        _connectionStrings["catalog"] = await CreateDatabaseAsync("nextrace_it_catalog");
        _connectionStrings["contracts"] = await CreateDatabaseAsync("nextrace_it_contracts");
        _connectionStrings["change-governance"] = await CreateDatabaseAsync("nextrace_it_change_governance");
        _connectionStrings["identity"] = await CreateDatabaseAsync("nextrace_it_identity");
        _connectionStrings["incidents"] = await CreateDatabaseAsync("nextrace_it_incidents");

        await ApplyMigrationsAsync();
        await InitializeRespawnersAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public CatalogGraphDbContext CreateCatalogGraphDbContext()
    {
        var options = new DbContextOptionsBuilder<CatalogGraphDbContext>()
            .UseNpgsql(CatalogConnectionString)
            .Options;

        return new CatalogGraphDbContext(options, _tenant, _user, _clock);
    }

    public ContractsDbContext CreateContractsDbContext()
    {
        var options = new DbContextOptionsBuilder<ContractsDbContext>()
            .UseNpgsql(ContractsConnectionString)
            .Options;

        return new ContractsDbContext(options, _tenant, _user, _clock);
    }

    public ChangeIntelligenceDbContext CreateChangeIntelligenceDbContext()
    {
        var options = new DbContextOptionsBuilder<ChangeIntelligenceDbContext>()
            .UseNpgsql(ChangeGovernanceConnectionString)
            .Options;

        return new ChangeIntelligenceDbContext(options, _tenant, _user, _clock);
    }

    public IdentityDbContext CreateIdentityDbContext()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(IdentityConnectionString)
            .Options;

        return new IdentityDbContext(options, _tenant, _user, _clock);
    }

    public IncidentDbContext CreateIncidentDbContext()
    {
        var options = new DbContextOptionsBuilder<IncidentDbContext>()
            .UseNpgsql(IncidentsConnectionString)
            .Options;

        return new IncidentDbContext(options, _tenant, _user, _clock);
    }

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
        await using var catalogContext = CreateCatalogGraphDbContext();
        await catalogContext.Database.MigrateAsync();

        await using var contractsContext = CreateContractsDbContext();
        await contractsContext.Database.MigrateAsync();

        await using var changeIntelligenceContext = CreateChangeIntelligenceDbContext();
        await changeIntelligenceContext.Database.MigrateAsync();

        await using var identityContext = CreateIdentityDbContext();
        await identityContext.Database.MigrateAsync();

        await using var incidentsContext = CreateIncidentDbContext();
        await incidentsContext.Database.MigrateAsync();
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
