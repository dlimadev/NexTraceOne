using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Integrations.Domain;

namespace NexTraceOne.Governance.Application.Features.GetCanaryRollouts;

/// <summary>
/// Feature: GetCanaryRollouts — dashboard de rollouts canary ativos e histórico.
/// Delega para ICanaryProvider. Quando nenhum sistema canary está configurado,
/// ICanaryProvider.IsConfigured é false e a lista retornada é vazia com SimulatedNote.
/// </summary>
public static class GetCanaryRollouts
{
    /// <summary>Query com filtros opcionais de ambiente e estado.</summary>
    public sealed record Query(string? Environment, string? Status) : IQuery<CanaryDashboardResponse>;

    /// <summary>Handler que retorna rollouts canary via ICanaryProvider.</summary>
    public sealed class Handler(ICanaryProvider canaryProvider) : IQueryHandler<Query, CanaryDashboardResponse>
    {
        public async Task<Result<CanaryDashboardResponse>> Handle(Query request, CancellationToken cancellationToken)
        {
            var rollouts = await canaryProvider.GetActiveRolloutsAsync(request.Environment, cancellationToken);

            // Apply status filter client-side (data set is small)
            IEnumerable<CanaryRolloutInfo> filtered = rollouts;
            if (!string.IsNullOrWhiteSpace(request.Status))
                filtered = filtered.Where(r => string.Equals(r.Status, request.Status, StringComparison.OrdinalIgnoreCase));

            var items = filtered.Select(r => new CanaryRolloutDto(
                Id: r.Id,
                ServiceName: r.ServiceName,
                Environment: r.Environment,
                CanaryVersion: r.CanaryVersion,
                StableVersion: r.StableVersion,
                CanaryTrafficPct: r.CanaryTrafficPct,
                Status: r.Status,
                StartedAt: r.StartedAt,
                CompletedAt: r.CompletedAt)).ToList();

            var simulatedNote = canaryProvider.IsConfigured
                ? null
                : "No canary rollout system integrated. Configure ICanaryProvider to see real data.";

            var response = new CanaryDashboardResponse(
                Rollouts: items,
                CheckedAt: DateTimeOffset.UtcNow,
                SimulatedNote: simulatedNote);

            return Result<CanaryDashboardResponse>.Success(response);
        }
    }

    /// <summary>Resposta do dashboard de canary rollouts.</summary>
    public sealed record CanaryDashboardResponse(
        IReadOnlyList<CanaryRolloutDto> Rollouts,
        DateTimeOffset CheckedAt,
        string? SimulatedNote);

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
