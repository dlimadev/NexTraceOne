using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Incidents.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Incidents.Features.GetPublicStatus;

/// <summary>
/// Feature: GetPublicStatus — status page pública do tenant.
/// Endpoint anônimo: agrega incidentes abertos em um status geral
/// (Operational/DegradedPerformance/PartialOutage/MajorOutage) sem expor
/// dados internos além de referência, título, severidade e serviço.
/// </summary>
public static class GetPublicStatus
{
    /// <summary>Query pública por tenant.</summary>
    public sealed record Query(Guid TenantId) : IQuery<Response>, IPublicRequest;

    /// <summary>Resposta com status geral e incidentes ativos.</summary>
    public sealed record Response(
        string OverallStatus,
        DateTimeOffset GeneratedAt,
        IReadOnlyList<PublicStatusIncident> ActiveIncidents);

    /// <summary>Handler que computa o status geral a partir dos incidentes abertos.</summary>
    internal sealed class Handler(
        IPublicStatusReader statusReader,
        NexTraceOne.BuildingBlocks.Application.Abstractions.IDateTimeProvider clock)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            if (request.TenantId == Guid.Empty)
                return Error.Validation("status.invalidTenant", "A tenant identifier is required.");

            var snapshot = await statusReader.GetSnapshotAsync(request.TenantId, cancellationToken);

            var overall = ComputeOverallStatus(snapshot.ActiveIncidents);

            return new Response(overall, clock.UtcNow, snapshot.ActiveIncidents);
        }

        private static string ComputeOverallStatus(IReadOnlyList<PublicStatusIncident> incidents)
        {
            if (incidents.Count == 0)
                return "Operational";
            if (incidents.Any(i => i.Severity == "Critical"))
                return "MajorOutage";
            if (incidents.Any(i => i.Severity == "Major"))
                return "PartialOutage";
            return "DegradedPerformance";
        }
    }
}
