namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

/// <summary>
/// Leitor de dados de padrões de release para o relatório GetReleasePatternAnalysisReport.
/// Por omissão satisfeita por <c>NullReleasePatternReader</c> (honest-null).
/// Wave AW.1 — Release Pattern Analysis.
/// </summary>
public interface IReleasePatternReader
{
    /// <summary>Lista entries de padrão de release por tenant numa janela temporal.</summary>
    Task<IReadOnlyList<ReleasePatternEntry>> ListReleasesByTenantAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);
}

/// <summary>
/// Dados de uma release para análise de padrões.
/// </summary>
/// <param name="ReleaseId">Identificador da release.</param>
/// <param name="ServiceName">Nome canónico do serviço.</param>
/// <param name="TeamName">Equipa responsável.</param>
/// <param name="Environment">Ambiente de deploy.</param>
/// <param name="DeployedAt">Timestamp do deploy.</param>
/// <param name="HasIncident">Indica se houve incidente associado.</param>
/// <param name="IncidentAt">Timestamp do incidente (se HasIncident).</param>
/// <param name="ServiceChangesCount">Número de serviços alterados nesta release.</param>
/// <param name="IsEndOfSprint">Indica se a release ocorreu no fim de sprint.</param>
public sealed record ReleasePatternEntry(
    Guid ReleaseId,
    string ServiceName,
    string TeamName,
    string Environment,
    DateTimeOffset DeployedAt,
    bool HasIncident,
    DateTimeOffset? IncidentAt,
    int ServiceChangesCount,
    bool IsEndOfSprint);
