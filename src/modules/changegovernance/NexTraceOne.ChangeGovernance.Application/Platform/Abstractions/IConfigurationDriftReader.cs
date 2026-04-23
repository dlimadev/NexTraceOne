using NexTraceOne.ChangeGovernance.Application.Platform.Features.GetConfigurationDriftReport;

namespace NexTraceOne.ChangeGovernance.Application.Platform.Abstractions;

/// <summary>
/// Abstracção para leitura de dados de deriva de configuração entre ambientes.
/// Por omissão satisfeita por <c>NullConfigurationDriftReader</c> (honest-null).
/// Wave AU.1 — GetConfigurationDriftReport.
/// </summary>
public interface IConfigurationDriftReader
{
    /// <summary>
    /// Retorna linhas de deriva de chaves de configuração para o tenant desde <paramref name="since"/>.
    /// </summary>
    Task<IReadOnlyList<ConfigKeyDriftRow>> GetConfigKeyDriftAsync(
        string tenantId,
        DateTimeOffset since,
        CancellationToken ct);

    /// <summary>Linha de deriva de uma chave de configuração entre ambientes.</summary>
    public sealed record ConfigKeyDriftRow(
        string Key,
        string Module,
        IReadOnlyDictionary<string, string?> ValueByEnvironment,
        bool IsDivergent,
        GetConfigurationDriftReport.DivergenceType DivergenceType,
        bool IsHighImpact,
        DateTimeOffset? LastUpdatedAt);
}
