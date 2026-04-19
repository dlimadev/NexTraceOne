using NexTraceOne.Integrations.Domain;

namespace NexTraceOne.Integrations.Infrastructure;

/// <summary>
/// Implementação nula de IBackupProvider.
/// Retorna lista vazia enquanto nenhum sistema de backup real (pg_dump, pgBackRest, Barman, etc.)
/// estiver configurado. Registado como default via DI.
/// Substitua por uma implementação concreta quando o sistema de backup estiver disponível.
/// </summary>
internal sealed class NullBackupProvider : IBackupProvider
{
    /// <inheritdoc />
    public bool IsConfigured => false;

    /// <inheritdoc />
    public Task<IReadOnlyList<BackupRestorePoint>> ListRestorePointsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<BackupRestorePoint>>([]);

    /// <inheritdoc />
    public Task<BackupRestorePoint?> GetRestorePointAsync(string restorePointId, CancellationToken cancellationToken = default)
        => Task.FromResult<BackupRestorePoint?>(null);
}
