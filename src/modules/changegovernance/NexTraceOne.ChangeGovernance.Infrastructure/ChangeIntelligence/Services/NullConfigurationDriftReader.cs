using NexTraceOne.ChangeGovernance.Application.Platform.Abstractions;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Services;

/// <summary>
/// Implementação honest-null de <see cref="IConfigurationDriftReader"/>.
/// Retorna lista vazia quando o bridge com dados de configuração não está configurado.
///
/// Wave AU.1 — GetConfigurationDriftReport (ChangeGovernance Platform).
/// </summary>
internal sealed class NullConfigurationDriftReader : IConfigurationDriftReader
{
    /// <inheritdoc/>
    public Task<IReadOnlyList<IConfigurationDriftReader.ConfigKeyDriftRow>> GetConfigKeyDriftAsync(
        string tenantId,
        DateTimeOffset since,
        CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IConfigurationDriftReader.ConfigKeyDriftRow>>([]);
}
