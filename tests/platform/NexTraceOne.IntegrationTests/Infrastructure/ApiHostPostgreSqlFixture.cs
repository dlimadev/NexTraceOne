using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace NexTraceOne.IntegrationTests.Infrastructure;

/// <summary>
/// Fixture de integração RH-6 com stack real do ApiHost.
/// Sobe PostgreSQL via Testcontainers, aplica migrations reais no startup e
/// expõe HttpClient autenticado para validar handlers/endpoints contra banco real.
/// </summary>
public sealed class ApiHostPostgreSqlFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private string? _adminConnectionString;
    private WebApplicationFactory<Program>? _factory;
    private readonly Dictionary<string, string> _connectionStrings = new(StringComparer.OrdinalIgnoreCase);

    private static readonly string[] LocalAdminConnectionCandidates =
    [
        Environment.GetEnvironmentVariable("NEXTRACE_TEST_ADMIN_CONNECTION_STRING") ?? string.Empty,
        "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres;Include Error Detail=true",
    ];

    private static readonly (string Key, string DbName)[] DatabaseTargets =
    [
        ("catalog", "rh6_it_catalog"),
        ("change-governance", "rh6_it_change_governance"),
        ("identity", "rh6_it_identity"),
        ("incidents", "rh6_it_incidents"),
        ("runtime", "rh6_it_runtime"),
        ("cost", "rh6_it_cost"),
        ("aiknowledge", "rh6_it_aiknowledge"),
        ("external-ai", "rh6_it_external_ai"),
        ("ai-orchestration", "rh6_it_ai_orchestration"),
        ("governance", "rh6_it_governance"),
        ("audit", "rh6_it_audit"),
    ];

    public const string SeedAdminEmail = "admin@nextraceone.dev";
    public const string SeedAdminPassword = "Admin@123";
    public const string SeedDeveloperEmail = "dev@nextraceone.dev";
    public const string SeedDeveloperPassword = "Admin@123";
    public static readonly Guid NexTraceCorpTenantId = Guid.Parse("a0000000-0000-0000-0000-000000000001");
    public static readonly Guid AcmeFintechTenantId = Guid.Parse("a0000000-0000-0000-0000-000000000002");
    public HttpClient Client { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("NEXTRACE_SKIP_INTEGRITY", "true");
        Environment.SetEnvironmentVariable("NEXTRACE_IGNORE_PENDING_MODEL_CHANGES", "true");

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
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(BuildConnectionStringOverrides());
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Jwt:Secret"] = "rh6-integration-testing-jwt-secret-min-256-bits-nextraceone-platform",
                        ["Jwt:Issuer"] = "NexTraceOne",
                        ["Jwt:Audience"] = "nextraceone-api",
                        ["Jwt:AccessTokenExpirationMinutes"] = "60",
                        ["NexTraceOne:IntegrityCheck"] = "false",
                    });
                });
            });

        Client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
        });

        await WaitForApplicationReadyAsync();
        await EnsureCrossTenantMembershipAsync();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    public HttpClient CreateClient() => _factory!.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = false,
    });

    public HttpClient CreateCookieClient() => _factory!.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true,
    });

    public async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password)
    {
        var login = await LoginAsync(email, password);
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
    }

    public async Task<LoginTokens> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        using var content = JsonContent.Create(new { Email = email, Password = password });
        var response = await Client.PostAsync(
            "/api/v1/identity/auth/login",
            content,
            cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var payload = document.RootElement.TryGetProperty("data", out var nested) ? nested : document.RootElement;

        return new LoginTokens(
            payload.GetProperty("accessToken").GetString()!,
            payload.GetProperty("refreshToken").GetString()!,
            payload.GetProperty("user").GetProperty("tenantId").GetGuid());
    }

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
            if (!string.IsNullOrWhiteSpace(value))
            {
                Environment.SetEnvironmentVariable(key.Replace(":", "__", StringComparison.Ordinal), value);
            }
        }

        Environment.SetEnvironmentVariable("Jwt__Secret", "rh6-integration-testing-jwt-secret-min-256-bits-nextraceone-platform");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "NexTraceOne");
        Environment.SetEnvironmentVariable("Jwt__Audience", "nextraceone-api");
        Environment.SetEnvironmentVariable("Jwt__AccessTokenExpirationMinutes", "60");
        Environment.SetEnvironmentVariable("NexTraceOne__IntegrityCheck", "false");
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
            _container = new PostgreSqlBuilder("postgres:16-alpine")
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
                "Neither Docker/Testcontainers nor a local PostgreSQL admin connection is available for RH-6 integration tests.");
        }
    }

    private async Task WaitForApplicationReadyAsync()
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                var response = await Client.GetAsync("/health");
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
                // App still starting.
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new InvalidOperationException("ApiHost did not become ready for RH-6 integration tests.");
    }

    private async Task EnsureCrossTenantMembershipAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionStrings["identity"]);
        await connection.OpenAsync();

        const string sql = """
                           INSERT INTO identity_tenant_memberships ("Id", "UserId", "TenantId", "RoleId", "JoinedAt", "IsActive")
                           VALUES (
                               'e0000000-0000-0000-0000-000000000099',
                               'b0000000-0000-0000-0000-000000000001',
                               'a0000000-0000-0000-0000-000000000002',
                               '1e91a557-fade-46df-b248-0f5f5899c001',
                               NOW(),
                               true)
                           ON CONFLICT DO NOTHING;
                           """;

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    public sealed record LoginTokens(string AccessToken, string RefreshToken, Guid TenantId);
}
