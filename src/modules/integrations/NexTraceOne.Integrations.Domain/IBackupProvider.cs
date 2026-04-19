namespace NexTraceOne.Integrations.Domain;

/// <summary>
/// Contrato para integração com sistemas de backup (pg_dump, pgBackRest, Barman, etc.).
/// A implementação padrão é <c>NullBackupProvider</c> que retorna lista vazia até que um sistema
/// de backup real seja configurado.
/// </summary>
public interface IBackupProvider
{
    /// <summary>
    /// Indica se o provider está configurado e ligado a um sistema de backup real.
    /// Usado para mostrar nota informativa na UI quando não há integração.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Lista os pontos de restauro disponíveis.
    /// Retorna lista vazia se nenhum sistema de backup estiver configurado.
    /// </summary>
    Task<IReadOnlyList<BackupRestorePoint>> ListRestorePointsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se um ponto de restauro específico existe e está disponível.
    /// </summary>
    Task<BackupRestorePoint?> GetRestorePointAsync(string restorePointId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Ponto de restauro disponível fornecido pelo sistema de backup externo.
/// </summary>
public sealed record BackupRestorePoint(
    string Id,
    DateTimeOffset CreatedAt,
    long SizeMb,
    string Status,
    string StorageProvider);
