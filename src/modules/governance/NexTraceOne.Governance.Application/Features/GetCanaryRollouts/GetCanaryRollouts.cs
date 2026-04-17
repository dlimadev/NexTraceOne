using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.GetCanaryRollouts;

/// <summary>
/// Feature: GetCanaryRollouts — dashboard de rollouts canary ativos e histórico.
/// Sem integração com sistema canary real. Retorna lista vazia com SimulatedNote.
/// </summary>
public static class GetCanaryRollouts
{
    /// <summary>Query com filtros opcionais de ambiente e estado.</summary>
    public sealed record Query(string? Environment, string? Status) : IQuery<CanaryDashboardResponse>;

    /// <summary>Handler que retorna rollouts canary (integração real pendente).</summary>
    public sealed class Handler : IQueryHandler<Query, CanaryDashboardResponse>
    {
        public Task<Result<CanaryDashboardResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var response = new CanaryDashboardResponse(
                Rollouts: [],
                CheckedAt: DateTimeOffset.UtcNow,
                SimulatedNote: "No canary rollout system integrated. This endpoint will return real data once a canary provider is configured.");

            return Task.FromResult(Result<CanaryDashboardResponse>.Success(response));
        }
    }

    /// <summary>Resposta do dashboard de canary rollouts.</summary>
    public sealed record CanaryDashboardResponse(
        IReadOnlyList<CanaryRolloutDto> Rollouts,
        DateTimeOffset CheckedAt,
        string SimulatedNote);

    /// <summary>Detalhes de um rollout canary.</summary>
    public sealed record CanaryRolloutDto(
        string Id,
        string ServiceName,
        string Environment,
        string CanaryVersion,
        string StableVersion,
        int CanaryTrafficPct,
        string Status,
        DateTimeOffset StartedAt,
        DateTimeOffset? CompletedAt);
}
