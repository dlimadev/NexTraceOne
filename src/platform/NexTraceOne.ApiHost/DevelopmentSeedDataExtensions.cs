using Npgsql;

namespace NexTraceOne.ApiHost;

/// <summary>
/// Extension method para semear dados de desenvolvimento no banco de dados PostgreSQL.
/// Executado automaticamente em ambiente Development após as migrações.
///
/// O script SQL é idempotente (usa ON CONFLICT DO NOTHING) e pode ser re-executado
/// sem efeitos colaterais. Insere dados mockados em todas as tabelas dos módulos
/// para permitir navegação completa no frontend com dados realistas.
///
/// Usuários criados:
/// - admin@nextraceone.dev (PlatformAdmin) — senha: Admin@123
/// - techlead@nextraceone.dev (TechLead) — senha: Admin@123
/// - dev@nextraceone.dev (Developer) — senha: Admin@123
/// - auditor@nextraceone.dev (Auditor) — senha: Admin@123
/// </summary>
public static class DevelopmentSeedDataExtensions
{
    /// <summary>
    /// Aplica o script SQL de seed data para desenvolvimento.
    /// Requer que as migrações já tenham sido executadas com sucesso.
    /// Ignora execução silenciosamente em ambientes que não sejam Development.
    /// </summary>
    public static async Task SeedDevelopmentDataAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return;

        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        var configuration = app.Services.GetRequiredService<IConfiguration>();
        var connectionString = configuration.GetConnectionString("NexTraceOne");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogWarning("Connection string 'NexTraceOne' not configured. Skipping seed data.");
            return;
        }

        var sqlPath = Path.Combine(AppContext.BaseDirectory, "SeedData", "seed-development.sql");

        if (!File.Exists(sqlPath))
        {
            logger.LogWarning("Seed data file not found at {Path}. Skipping.", sqlPath);
            return;
        }

        try
        {
            logger.LogInformation("Applying development seed data...");

            var sql = await File.ReadAllTextAsync(sqlPath);

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);
            command.CommandTimeout = 60;
            await command.ExecuteNonQueryAsync();

            logger.LogInformation("Development seed data applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error applying development seed data.");
        }
    }
}
