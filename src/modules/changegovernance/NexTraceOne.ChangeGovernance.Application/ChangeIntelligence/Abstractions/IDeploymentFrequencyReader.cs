namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

/// <summary>
/// Leitor de dados de frequência de deployments para o relatório GetDeploymentFrequencyHealthReport.
/// Por omissão satisfeita por <c>NullDeploymentFrequencyReader</c> (honest-null).
/// Wave AW.3 — Deployment Frequency Health Report.
/// </summary>
public interface IDeploymentFrequencyReader
{
    /// <summary>Lista entries de deployment por tenant numa janela temporal.</summary>
    Task<IReadOnlyList<DeploymentFrequencyEntry>> ListDeploymentsByTenantAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);
}

/// <summary>
/// Dados de um deployment individual para análise de frequência.
/// </summary>
/// <param name="ReleaseId">Identificador da release.</param>
/// <param name="ServiceName">Nome canónico do serviço.</param>
/// <param name="TeamName">Equipa responsável.</param>
/// <param name="ServiceTier">Tier do serviço (ex.: Critical, Standard, Internal).</param>
/// <param name="Environment">Ambiente de deploy.</param>
/// <param name="DeployedAt">Timestamp do deploy.</param>
/// <param name="Succeeded">Indica se o deploy teve sucesso.</param>
public sealed record DeploymentFrequencyEntry(
    Guid ReleaseId,
    string ServiceName,
    string TeamName,
    string ServiceTier,
    string Environment,
    DateTimeOffset DeployedAt,
    bool Succeeded);
