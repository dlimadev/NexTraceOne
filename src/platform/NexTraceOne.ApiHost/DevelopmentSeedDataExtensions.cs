using Npgsql;

namespace NexTraceOne.ApiHost;

/// <summary>
/// Extension method para semear dados de desenvolvimento nos bancos de dados PostgreSQL.
/// Executado automaticamente em ambiente Development após as migrações.
///
/// Cada módulo possui seu próprio ficheiro SQL de seed, executado contra a base de dados
/// correspondente. Os scripts são idempotentes (ON CONFLICT DO NOTHING) e podem ser
/// re-executados sem efeitos colaterais.
///
/// Usuários criados:
/// - admin@nextraceone.dev (PlatformAdmin) — senha: Admin@123
/// - techlead@nextraceone.dev (TechLead) — senha: Admin@123
/// - dev@nextraceone.dev (Developer) — senha: Admin@123
/// - auditor@nextraceone.dev (Auditor) — senha: Admin@123
/// </summary>
public static class DevelopmentSeedDataExtensions
{
    private static readonly (string ConnectionStringName, string SqlFileName)[] SeedTargets =
    [
        ("IdentityDatabase", "seed-identity.sql"),
        ("CatalogDatabase", "seed-catalog.sql"),
        ("ChangeIntelligenceDatabase", "seed-changegovernance.sql"),
        ("AuditDatabase", "seed-audit.sql"),
        ("IncidentDatabase", "seed-incidents.sql"),
    ];

    /// <summary>
    /// Aplica os scripts SQL de seed data para desenvolvimento, um por base de dados.
    /// Requer que as migrações já tenham sido executadas com sucesso.
    /// Ignora execução silenciosamente em ambientes que não sejam Development.
    /// </summary>
    public static async Task SeedDevelopmentDataAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return;

        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        var configuration = app.Services.GetRequiredService<IConfiguration>();

        logger.LogInformation("Applying development seed data...");

        foreach (var (connectionStringName, sqlFileName) in SeedTargets)
        {
            var connectionString = configuration.GetConnectionString(connectionStringName);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                logger.LogWarning("Connection string '{Name}' not configured. Skipping seed for {File}.",
                    connectionStringName, sqlFileName);
                continue;
            }

            var sqlPath = Path.Combine(AppContext.BaseDirectory, "SeedData", sqlFileName);

            if (!File.Exists(sqlPath))
            {
                logger.LogWarning("Seed data file not found at {Path}. Skipping.", sqlPath);
                continue;
            }

            try
            {
                var sql = await File.ReadAllTextAsync(sqlPath);

                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                await using var command = new NpgsqlCommand(sql, connection);
                command.CommandTimeout = 60;
                await command.ExecuteNonQueryAsync();

                logger.LogInformation("Seed data applied for {Database}.", connectionStringName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error applying seed data for {Database}.", connectionStringName);
            }
        }

        logger.LogInformation("Development seed data process completed.");
    }
}
