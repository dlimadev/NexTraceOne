using NexTraceOne.Knowledge.Application.Abstractions;

namespace NexTraceOne.Knowledge.Application;

/// <summary>
/// Implementação null (honest-null) de ITeamKnowledgeSharingReader.
/// Retorna listas vazias — sem dados de partilha de conhecimento disponíveis.
/// Wave AY.3 — GetTeamKnowledgeSharingReport.
/// </summary>
public sealed class NullTeamKnowledgeSharingReader : ITeamKnowledgeSharingReader
{
    public Task<IReadOnlyList<ITeamKnowledgeSharingReader.TeamKnowledgeEntry>>
        ListByTenantAsync(string tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<ITeamKnowledgeSharingReader.TeamKnowledgeEntry>>([]);

    public Task<IReadOnlyList<ITeamKnowledgeSharingReader.WeeklyKnowledgeSharingSnapshot>>
        GetTenantTrendAsync(string tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<ITeamKnowledgeSharingReader.WeeklyKnowledgeSharingSnapshot>>([]);

    public Task<IReadOnlyList<ITeamKnowledgeSharingReader.ServiceKnowledgeEntry>>
        ListServiceContributionsAsync(string tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<ITeamKnowledgeSharingReader.ServiceKnowledgeEntry>>([]);
}
