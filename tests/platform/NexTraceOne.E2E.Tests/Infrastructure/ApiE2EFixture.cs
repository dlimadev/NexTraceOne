using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Respawn;
using Respawn.Graph;
using System.Text.Json;
using Testcontainers.PostgreSql;
using Xunit;

namespace NexTraceOne.E2E.Tests.Infrastructure;

/// <summary>
/// Fixture E2E que inicializa um container PostgreSQL real e uma instância completa
/// do NexTraceOne.ApiHost via WebApplicationFactory, permitindo testes HTTP reais
/// contra toda a stack: Minimal API → MediatR → Application → EF Core → PostgreSQL.
///
/// Estratégia:
/// - Um container PostgreSQL 16 é iniciado para todos os testes da sessão
/// - 11 bases de dados são criadas correspondendo aos agrupamentos reais dos contextos
/// - Todas as migrations são aplicadas automaticamente pelo mecanismo do ApiHost
/// - Os testes HTTP fazem chamadas reais ao HttpClient do WebApplicationFactory
/// - O estado é resetado via Respawn entre grupos de testes quando necessário
///
/// Configuração de ambiente:
/// - ASPNETCORE_ENVIRONMENT=Development (permite fallback JWT + migrations automáticas)
/// - NEXTRACE_SKIP_INTEGRITY=true (ignora verificação de integridade de assemblies)
/// - Todas as connection strings apontam para o container de teste
/// </summary>
public sealed class ApiE2EFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private string? _adminConnectionString;
    private WebApplicationFactory<Program>? _factory;
    private readonly Dictionary<string, string> _connectionStrings = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Respawner> _respawners = new(StringComparer.OrdinalIgnoreCase);

    private static readonly string[] LocalAdminConnectionCandidates =
    [
        Environment.GetEnvironmentVariable("NEXTRACE_TEST_ADMIN_CONNECTION_STRING") ?? string.Empty,
        "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres;Include Error Detail=true",
        "Host=localhost;Port=5432;Database=postgres;Username=nextraceone;Password=ouro18;Include Error Detail=true",
        "Host=localhost;Port=5432;Database=nextraceone;Username=nextraceone;Password=ouro18;Include Error Detail=true",
    ];

    // ── Test user credentials ─────────────────────────────────────────────────

    /// <summary>Email do utilizador de teste admin para fluxos E2E.</summary>
    public const string E2EAdminEmail = "e2e.admin@nextraceone.test";

    /// <summary>Senha do utilizador de teste admin.</summary>
    public const string E2EAdminPassword = "Admin@123";

    /// <summary>Email do utilizador de teste viewer para fluxos E2E.</summary>
    public const string E2EViewerEmail = "e2e.viewer@nextraceone.test";

    /// <summary>Senha do utilizador de teste viewer.</summary>
    public const string E2EViewerPassword = "Viewer@123";

    // ── Database grouping matching production configuration ───────────────────

    private static readonly (string Key, string DbName)[] DatabaseTargets =
    [
        ("catalog", "e2e_catalog"),
        ("change-governance", "e2e_change_governance"),
        ("identity", "e2e_identity"),
        ("incidents", "e2e_incidents"),
        ("runtime", "e2e_runtime"),
        ("cost", "e2e_cost"),
        ("aiknowledge", "e2e_aiknowledge"),
        ("external-ai", "e2e_external_ai"),
        ("ai-orchestration", "e2e_ai_orchestration"),
        ("governance", "e2e_governance"),
        ("audit", "e2e_audit"),
    ];

    public HttpClient Client { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        // Set environment variables before starting the factory
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("NEXTRACE_SKIP_INTEGRITY", "true");

        _adminConnectionString = await StartPostgreSqlAsync();

        foreach (var (key, dbName) in DatabaseTargets)
        {
            _connectionStrings[key] = await CreateDatabaseAsync(dbName);
        }

        ApplyEnvironmentOverrides();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");

                builder.ConfigureAppConfiguration((context, config) =>
                {
                    // Override all connection strings to point to the test container
                    config.AddInMemoryCollection(BuildConnectionStringOverrides());

                    // Provide a known JWT secret for tests
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Jwt:Secret"] = "e2e-testing-jwt-secret-min-256-bits-nextraceone-platform",
                        ["Jwt:Issuer"] = "NexTraceOne",
                        ["Jwt:Audience"] = "nextraceone-api",
                        ["Jwt:AccessTokenExpirationMinutes"] = "60",
                        ["NexTraceOne:IntegrityCheck"] = "false",
                    });
                });
            });

        // Trigger app startup which applies migrations automatically (Development env)
        Client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        // Wait for migrations to be applied by making a health check request
        await WaitForApplicationReadyAsync();

        // Seed minimal test data needed for E2E authentication flows
        await SeedMinimalTestDataAsync();

        await InitializeRespawnersAsync();
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (_factory is not null)
            await _factory.DisposeAsync();
        if (_container is not null)
            await _container.DisposeAsync();
    }

    /// <summary>Resets all test databases to their post-migration clean state.</summary>
    public async Task ResetDatabasesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var (key, _) in DatabaseTargets)
        {
            if (!_connectionStrings.TryGetValue(key, out var connectionString))
                continue;
            if (!_respawners.TryGetValue(key, out var respawner))
                continue;

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await respawner.ResetAsync(connection);
        }
    }

    /// <summary>
    /// Performs real login and returns the JWT access token for authenticated test flows.
    /// Returns null if login fails (test should handle or skip).
    /// </summary>
    public async Task<string?> GetAuthTokenAsync(
        string email = E2EAdminEmail,
        string password = E2EAdminPassword,
        CancellationToken cancellationToken = default)
    {
        var loginResponse = await Client.PostAsJsonAsync(
            "/api/v1/identity/auth/login",
            new { Email = email, Password = password },
            cancellationToken);

        if (!loginResponse.IsSuccessStatusCode)
            return null;

        var content = await loginResponse.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(content);

        if (doc.RootElement.TryGetProperty("accessToken", out var tokenProp))
            return tokenProp.GetString();

        // Try nested format
        if (doc.RootElement.TryGetProperty("data", out var data)
            && data.TryGetProperty("accessToken", out var nestedToken))
            return nestedToken.GetString();

        return null;
    }

    /// <summary>Creates an authenticated HttpClient with Bearer token for E2E tests.</summary>
    public async Task<HttpClient> CreateAuthenticatedClientAsync(
        string email = E2EAdminEmail,
        string password = E2EAdminPassword)
    {
        var token = await GetAuthTokenAsync(email, password);

        var client = _factory!.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        if (token is not null)
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    /// <summary>Returns the base URL of the test server for diagnostic purposes.</summary>
    public string BaseAddress => _factory?.Server.BaseAddress.ToString() ?? string.Empty;

    /// <summary>Creates a new HttpClient for the test server (unauthenticated).</summary>
    public HttpClient CreateClient() => _factory!.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = false
    });

    // ── Private helpers ───────────────────────────────────────────────────────

    private IEnumerable<KeyValuePair<string, string?>> BuildConnectionStringOverrides()
    {
        var catalog = _connectionStrings["catalog"];
        var changeGov = _connectionStrings["change-governance"];
        var identity = _connectionStrings["identity"];
        var incidents = _connectionStrings["incidents"];
        var runtime = _connectionStrings["runtime"];
        var cost = _connectionStrings["cost"];
        var aiKnowledge = _connectionStrings["aiknowledge"];
        var externalAi = _connectionStrings["external-ai"];
        var aiOrchestration = _connectionStrings["ai-orchestration"];
        var governance = _connectionStrings["governance"];
        var audit = _connectionStrings["audit"];

        return new Dictionary<string, string?>
        {
            // Core connection strings matching appsettings.json structure
            ["ConnectionStrings:NexTraceOne"] = catalog,
            ["ConnectionStrings:IdentityDatabase"] = identity,
            ["ConnectionStrings:CatalogDatabase"] = catalog,
            ["ConnectionStrings:ContractsDatabase"] = catalog,
            ["ConnectionStrings:DeveloperPortalDatabase"] = catalog,
            ["ConnectionStrings:ChangeIntelligenceDatabase"] = changeGov,
            ["ConnectionStrings:WorkflowDatabase"] = changeGov,
            ["ConnectionStrings:RulesetGovernanceDatabase"] = changeGov,
            ["ConnectionStrings:PromotionDatabase"] = changeGov,
            ["ConnectionStrings:IncidentDatabase"] = incidents,
            ["ConnectionStrings:CostIntelligenceDatabase"] = cost,
            ["ConnectionStrings:RuntimeIntelligenceDatabase"] = runtime,
            ["ConnectionStrings:AuditDatabase"] = audit,
            ["ConnectionStrings:AiGovernanceDatabase"] = aiKnowledge,
            ["ConnectionStrings:ExternalAiDatabase"] = externalAi,
            ["ConnectionStrings:AiOrchestrationDatabase"] = aiOrchestration,
            ["ConnectionStrings:GovernanceDatabase"] = governance,
        };
    }

    private void ApplyEnvironmentOverrides()
    {
        foreach (var (key, value) in BuildConnectionStringOverrides())
        {
            if (string.IsNullOrWhiteSpace(value))
                continue;

            Environment.SetEnvironmentVariable(key.Replace(":", "__", StringComparison.Ordinal), value);
        }

        Environment.SetEnvironmentVariable("Jwt__Secret", "e2e-testing-jwt-secret-min-256-bits-nextraceone-platform");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "NexTraceOne");
        Environment.SetEnvironmentVariable("Jwt__Audience", "nextraceone-api");
        Environment.SetEnvironmentVariable("Jwt__AccessTokenExpirationMinutes", "60");
        Environment.SetEnvironmentVariable("NexTraceOne__IntegrityCheck", "false");
        Environment.SetEnvironmentVariable("NEXTRACE_IGNORE_PENDING_MODEL_CHANGES", "true");
    }

    /// <summary>
    /// Seeds minimal test data required for E2E auth flows.
    /// Uses the same password hash as development seed: Admin@123
    /// (PBKDF2-SHA256, 100k iterations, format v1.{salt}.{hash})
    /// </summary>
    private async Task SeedMinimalTestDataAsync()
    {
        if (!_connectionStrings.TryGetValue("identity", out var identityConnectionString))
            return;

        await using var connection = new NpgsqlConnection(identityConnectionString);
        await connection.OpenAsync();

        var sql = """
                  INSERT INTO identity_tenants (\"Id\", \"Name\", \"Slug\", \"IsActive\", \"CreatedAt\")
                  VALUES ('a0000000-0000-0000-0000-000000000099', 'E2E Test Org', 'e2e-test-org', true, NOW())
                  ON CONFLICT DO NOTHING;

                  INSERT INTO identity_users (
                      \"Id\", \"Email\", \"first_name\", \"last_name\", \"PasswordHash\",
                      \"IsActive\", \"LastLoginAt\", \"FailedLoginAttempts\")
                  VALUES (
                      'b0000000-0000-0000-0000-000000000099',
                      'e2e.admin@nextraceone.test',
                      'E2E', 'Admin',
                      'v1.JmfnVELOwwfJunLeGbMN/g==.nkH8yRKF34TIfYNWcx84oAGJ+f2Q725ByMUk1JC7TNw=',
                      true, NOW(), 0),
                  (
                      'b0000000-0000-0000-0000-000000000098',
                      'e2e.viewer@nextraceone.test',
                      'E2E', 'Viewer',
                      'v1.JmfnVELOwwfJunLeGbMN/g==.nkH8yRKF34TIfYNWcx84oAGJ+f2Q725ByMUk1JC7TNw=',
                      true, NOW(), 0)
                  ON CONFLICT DO NOTHING;

                  INSERT INTO identity_tenant_memberships (
                      \"Id\", \"UserId\", \"TenantId\", \"RoleId\", \"JoinedAt\", \"IsActive\")
                  VALUES (
                      'e0000000-0000-0000-0000-000000000099',
                      'b0000000-0000-0000-0000-000000000099',
                      'a0000000-0000-0000-0000-000000000099',
                      '1e91a557-fade-46df-b248-0f5f5899c001',
                      NOW(), true),
                  (
                      'e0000000-0000-0000-0000-000000000098',
                      'b0000000-0000-0000-0000-000000000098',
                      'a0000000-0000-0000-0000-000000000099',
                      '1e91a557-fade-46df-b248-0f5f5899c004',
                      NOW(), true)
                  ON CONFLICT DO NOTHING;
                  """;

        await using var command = new NpgsqlCommand(sql, connection);
        command.CommandTimeout = 30;
        await command.ExecuteNonQueryAsync();
    }

    private async Task<string> CreateDatabaseAsync(string databaseName)
    {
        var adminConnectionString = _adminConnectionString
            ?? throw new InvalidOperationException("PostgreSQL admin connection string was not initialized.");

        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();

        await using var existsCommand = connection.CreateCommand();
        existsCommand.CommandText = "SELECT 1 FROM pg_database WHERE datname = @name";
        existsCommand.Parameters.AddWithValue("name", databaseName);
        var exists = await existsCommand.ExecuteScalarAsync();

        if (exists is null)
        {
            await using var createCommand = connection.CreateCommand();
            createCommand.CommandText = $"CREATE DATABASE \"{databaseName.Replace("\"", "\"\"")}\"";
            await createCommand.ExecuteNonQueryAsync();
        }

        return new NpgsqlConnectionStringBuilder(adminConnectionString)
        {
            Database = databaseName
        }.ToString();
    }

    private async Task<string> StartPostgreSqlAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("postgres")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            await _container.StartAsync();

            return new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
            {
                Database = "postgres"
            }.ToString();
        }
        catch
        {
            foreach (var candidate in LocalAdminConnectionCandidates.Where(value => !string.IsNullOrWhiteSpace(value)))
            {
                try
                {
                    await using var connection = new NpgsqlConnection(candidate);
                    await connection.OpenAsync();
                    await using var command = connection.CreateCommand();
                    command.CommandText = "SELECT 1";
                    await command.ExecuteScalarAsync();
                    return candidate;
                }
                catch
                {
                    // Try next local candidate.
                }
            }

            throw new InvalidOperationException(
                "Neither Docker/Testcontainers nor a local PostgreSQL admin connection is available for RH-6 E2E tests.");
        }
    }

    private async Task WaitForApplicationReadyAsync()
    {
        // Make up to 10 attempts to hit the health endpoint,
        // giving the app time to apply migrations and start listening
        for (var attempt = 0; attempt < 10; attempt++)
        {
            try
            {
                var response = await Client.GetAsync("/health");
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch
            {
                // App not ready yet
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }
    }

    private async Task InitializeRespawnersAsync()
    {
        foreach (var (key, _) in DatabaseTargets)
        {
            if (!_connectionStrings.TryGetValue(key, out var connectionString))
                continue;

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            _respawners[key] = await Respawner.CreateAsync(
                connection,
                new RespawnerOptions
                {
                    DbAdapter = DbAdapter.Postgres,
                    SchemasToInclude = ["public"],
                    TablesToIgnore = [new Table("__EFMigrationsHistory")]
                });
        }
    }
}
