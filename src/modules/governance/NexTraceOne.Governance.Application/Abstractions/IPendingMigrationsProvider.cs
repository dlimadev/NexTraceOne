namespace NexTraceOne.Governance.Application.Abstractions;

/// <summary>
/// Abstração para obter a lista de migrations pendentes de todos os DbContexts registados.
/// Implementada na camada ApiHost que tem acesso a todos os DbContexts dos módulos.
/// </summary>
public interface IPendingMigrationsProvider
{
    /// <summary>
    /// Retorna a lista de migrations pendentes para todos os DbContexts registados.
    /// Cada item contém o contexto, ID da migration e indicadores de risco.
    /// </summary>
    Task<IReadOnlyList<PendingMigrationInfo>> GetPendingMigrationsAsync(CancellationToken cancellationToken);
}

/// <summary>Informação sobre uma migration pendente de um DbContext específico.</summary>
/// <param name="MigrationId">Identificador único da migration (ex: "20260410_AddServiceHealthIndex").</param>
/// <param name="ContextName">Nome do DbContext sem o sufixo "DbContext" (ex: "CatalogGraph").</param>
public sealed record PendingMigrationInfo(
    string MigrationId,
    string ContextName);
