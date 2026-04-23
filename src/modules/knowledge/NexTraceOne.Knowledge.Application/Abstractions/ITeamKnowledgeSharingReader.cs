namespace NexTraceOne.Knowledge.Application.Abstractions;

/// <summary>
/// Abstracção para leitura de dados de partilha de conhecimento entre equipas.
/// Por omissão satisfeita por <c>NullTeamKnowledgeSharingReader</c> (honest-null).
/// Wave AY.3 — GetTeamKnowledgeSharingReport.
/// </summary>
public interface ITeamKnowledgeSharingReader
{
    Task<IReadOnlyList<TeamKnowledgeEntry>> ListByTenantAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);

    Task<IReadOnlyList<WeeklyKnowledgeSharingSnapshot>> GetTenantTrendAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);

    Task<IReadOnlyList<ServiceKnowledgeEntry>> ListServiceContributionsAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);

    /// <summary>Entrada de partilha de conhecimento por equipa.</summary>
    public sealed record TeamKnowledgeEntry(
        string TeamId,
        string TeamName,
        int DocContributionCount,
        int CrossTeamContributions,
        int DocConsumptionCount,
        int RunbookContributionCount,
        IReadOnlyList<string> TargetTeamIds);

    /// <summary>Snapshot semanal de KnowledgeSharingRatio do tenant.</summary>
    public sealed record WeeklyKnowledgeSharingSnapshot(
        int WeekOffset,
        decimal KnowledgeSharingRatio);

    /// <summary>Entrada de contribuições por serviço.</summary>
    public sealed record ServiceKnowledgeEntry(
        string ServiceId,
        string ServiceName,
        IReadOnlyList<string> ContributorIds);
}
