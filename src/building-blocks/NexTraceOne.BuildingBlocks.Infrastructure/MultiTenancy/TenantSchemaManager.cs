using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Npgsql;

using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.BuildingBlocks.Infrastructure.MultiTenancy;

/// <summary>
/// Implementação de TenantSchemaManager para PostgreSQL.
/// Cria e mantém schemas isolados por tenant usando DDL nativo do PostgreSQL.
///
/// ESTRATÉGIA DE ISOLAMENTO:
/// - Cada tenant tem um schema com nome derivado do slug: "tenant_{slug_normalizado}"
/// - As tabelas são criadas dentro do schema do tenant via SET search_path
/// - O schema "public" é reservado para dados partilhados e tabelas de sistema
/// - Row-Level Security (RLS) é mantido como camada adicional mesmo em mode schema-per-tenant
///
/// SEGURANÇA:
/// - Schema names são validados para conter apenas caracteres alfanuméricos e underscore
/// - Nunca interpolados diretamente em SQL — usados em CREATE SCHEMA com QuoteIdentifier
/// - A conexão principal (admin) cria os schemas; tenants usam roles com permissão limitada
///
/// NOTAS:
/// - Em produção, este manager deve ser invocado durante o provisionamento de tenant,
///   não em cada request HTTP
/// - As migrations são aplicadas via EF Core MigrationsExtensions por schema
/// - Implementa ITenantSchemaManager definida na camada Application para respeitar
///   a Dependency Rule da Clean Architecture.
/// </summary>
public sealed class TenantSchemaManager(
    string connectionString,
    ILogger<TenantSchemaManager> logger) : ITenantSchemaManager
{
    private const string TenantSchemaPrefix = "tenant_";

    /// <summary>
    /// Cria o schema PostgreSQL para o tenant se não existir.
    /// Usa CREATE SCHEMA IF NOT EXISTS — idempotente e seguro para chamadas repetidas.
    /// </summary>
    public async Task<bool> EnsureSchemaCreatedAsync(
        string tenantSlug,
        CancellationToken cancellationToken = default)
    {
        var schemaName = BuildSchemaName(tenantSlug);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var exists = await SchemaExistsInternalAsync(connection, schemaName, cancellationToken);
        if (exists)
        {
            logger.LogDebug("Schema {SchemaName} already exists for tenant {Slug}", schemaName, tenantSlug);
            return false;
        }

        // CREATE SCHEMA usa NpgsqlConnection.QuoteIdentifier para prevenir SQL injection
        var sql = $"CREATE SCHEMA IF NOT EXISTS {NpgsqlUtils.QuoteIdentifier(schemaName)}";
        await using var cmd = new NpgsqlCommand(sql, connection);
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        logger.LogInformation(
            "Created PostgreSQL schema {SchemaName} for tenant {Slug}",
            schemaName, tenantSlug);

        return true;
    }

    /// <summary>Verifica se o schema do tenant existe em information_schema.schemata.</summary>
    public async Task<bool> SchemaExistsAsync(
        string tenantSlug,
        CancellationToken cancellationToken = default)
    {
        var schemaName = BuildSchemaName(tenantSlug);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        return await SchemaExistsInternalAsync(connection, schemaName, cancellationToken);
    }

    /// <summary>
    /// Aplica migrations EF Core ao schema do tenant.
    /// Define search_path antes de executar as migrations para que as tabelas
    /// sejam criadas no schema correcto.
    /// </summary>
    public async Task MigrateSchemaAsync(
        string tenantSlug,
        DbContext context,
        CancellationToken cancellationToken = default)
    {
        var schemaName = BuildSchemaName(tenantSlug);

        await EnsureSchemaCreatedAsync(tenantSlug, cancellationToken);

        logger.LogInformation(
            "Applying migrations to schema {SchemaName} for tenant {Slug}",
            schemaName, tenantSlug);

        // Definir search_path para que as migrations sejam aplicadas no schema do tenant
        // EF1002: schemaName is sanitized via NpgsqlUtils.QuoteIdentifier which escapes double-quotes.
#pragma warning disable EF1002
        await context.Database.ExecuteSqlRawAsync(
            $"SET search_path TO {NpgsqlUtils.QuoteIdentifier(schemaName)}, public",
            cancellationToken);
#pragma warning restore EF1002

        await context.Database.MigrateAsync(cancellationToken);

        logger.LogInformation(
            "Migrations applied to schema {SchemaName} for tenant {Slug}",
            schemaName, tenantSlug);
    }

    /// <summary>
    /// Remove o schema do tenant com CASCADE.
    /// ATENÇÃO: operação destrutiva e irreversível.
    /// </summary>
    public async Task DropSchemaAsync(
        string tenantSlug,
        CancellationToken cancellationToken = default)
    {
        var schemaName = BuildSchemaName(tenantSlug);

        logger.LogWarning(
            "Dropping schema {SchemaName} for tenant {Slug} — all data will be lost",
            schemaName, tenantSlug);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var sql = $"DROP SCHEMA IF EXISTS {NpgsqlUtils.QuoteIdentifier(schemaName)} CASCADE";
        await using var cmd = new NpgsqlCommand(sql, connection);
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        logger.LogWarning(
            "Schema {SchemaName} dropped for tenant {Slug}",
            schemaName, tenantSlug);
    }

    /// <summary>
    /// Lista todos os schemas de tenants na base de dados consultando information_schema.
    /// Retorna os slugs derivados a partir dos nomes dos schemas.
    /// </summary>
    public async Task<IReadOnlyList<string>> ListTenantSchemasAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT schema_name
            FROM information_schema.schemata
            WHERE schema_name LIKE 'tenant_%'
              AND catalog_name = current_database()
            ORDER BY schema_name
            """;

        await using var cmd = new NpgsqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var schemas = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var schemaName = reader.GetString(0);
            var slug = schemaName.StartsWith(TenantSchemaPrefix, StringComparison.Ordinal)
                ? schemaName[TenantSchemaPrefix.Length..]
                : schemaName;
            schemas.Add(slug);
        }

        return schemas;
    }

    /// <summary>
    /// Retorna o search_path para o schema do tenant.
    /// O schema do tenant tem precedência sobre o schema public.
    /// </summary>
    public string GetSearchPath(string tenantSlug)
        => $"{BuildSchemaName(tenantSlug)}, public";

    // ── Internal ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Constrói o nome do schema PostgreSQL a partir do slug do tenant.
    /// Normaliza para lowercase e substitui caracteres inválidos por underscore.
    /// Formato: tenant_{slug_normalizado}
    /// </summary>
    private static string BuildSchemaName(string tenantSlug)
    {
        if (string.IsNullOrWhiteSpace(tenantSlug))
            throw new ArgumentException("Tenant slug cannot be empty.", nameof(tenantSlug));

        // Normaliza o slug: lowercase, apenas alfanumérico e underscore
        var normalized = new System.Text.StringBuilder();
        foreach (var c in tenantSlug.ToLowerInvariant())
        {
            normalized.Append(char.IsLetterOrDigit(c) ? c : '_');
        }

        var slug = normalized.ToString().Trim('_');
        if (string.IsNullOrEmpty(slug))
            throw new ArgumentException(
                $"Tenant slug '{tenantSlug}' normalizes to empty string.", nameof(tenantSlug));

        return $"{TenantSchemaPrefix}{slug}";
    }

    private static async Task<bool> SchemaExistsInternalAsync(
        NpgsqlConnection connection,
        string schemaName,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM information_schema.schemata
            WHERE schema_name = @schemaName
              AND catalog_name = current_database()
            """;

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@schemaName", schemaName);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result, System.Globalization.CultureInfo.InvariantCulture) > 0;
    }
}

/// <summary>
/// Utilitário para quoting seguro de identificadores PostgreSQL.
/// Previne SQL injection em nomes de schemas e tabelas.
/// </summary>
internal static class NpgsqlUtils
{
    /// <summary>Envolve o identificador em aspas duplas, escapando aspas internas.</summary>
    public static string QuoteIdentifier(string identifier)
        => $"\"{identifier.Replace("\"", "\"\"")}\"";
}
