namespace NexTraceOne.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Abstração para gestão de schemas PostgreSQL por tenant.
/// Permite criar, verificar e manter schemas isolados por tenant
/// como alternativa ou complemento ao Row-Level Security (RLS).
/// A abordagem schema-per-tenant oferece isolamento forte: cada tenant
/// tem as suas próprias tabelas, sem partilha de dados físicos.
///
/// Declarada na camada Application para que handlers e queries possam depender
/// desta abstração sem criar uma dependência de compilação sobre Infrastructure.
/// A implementação concreta (TenantSchemaManager) reside em BuildingBlocks.Infrastructure.
/// </summary>
public interface ITenantSchemaManager
{
    /// <summary>
    /// Cria o schema PostgreSQL para o tenant se não existir.
    /// Retorna true se o schema foi criado, false se já existia.
    /// </summary>
    Task<bool> EnsureSchemaCreatedAsync(
        string tenantSlug,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se o schema do tenant existe na base de dados.
    /// </summary>
    Task<bool> SchemaExistsAsync(
        string tenantSlug,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove o schema do tenant e todas as suas tabelas.
    /// Operação destrutiva — apenas para desprovisionamento ou testes.
    /// </summary>
    Task DropSchemaAsync(
        string tenantSlug,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista todos os schemas de tenants presentes na base de dados.
    /// Retorna os slugs dos tenants com schema criado.
    /// </summary>
    Task<IReadOnlyList<string>> ListTenantSchemasAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna o search_path PostgreSQL para o schema do tenant.
    /// Formato: "{schemaName}, public"
    /// </summary>
    string GetSearchPath(string tenantSlug);
}
