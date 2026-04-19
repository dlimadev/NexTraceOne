using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ListGuardianAlerts;

/// <summary>
/// Feature: ListGuardianAlerts — lista alertas do Guardian com filtros opcionais.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class ListGuardianAlerts
{
    public sealed record Query(Guid TenantId, string? ServiceName, string? Status) : IQuery<Response>;

    public sealed class Handler(IGuardianAlertRepository alertRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken ct)
        {
            IReadOnlyList<GuardianAlert> alerts;

            if (!string.IsNullOrEmpty(request.ServiceName))
                alerts = await alertRepository.ListByServiceAsync(request.ServiceName, request.TenantId, ct);
            else if (string.Equals(request.Status, "open", StringComparison.OrdinalIgnoreCase))
                alerts = await alertRepository.ListOpenAsync(request.TenantId, ct);
            else
                alerts = await alertRepository.ListByTenantAsync(request.TenantId, ct);

            var items = alerts.Select(a => new GuardianAlertSummary(
                a.Id.Value, a.ServiceName, a.Category, a.PatternDetected, a.Confidence, a.Severity, a.Status, a.DetectedAt
            )).ToList().AsReadOnly();

            return new Response(items);
        }
    }

    public sealed record GuardianAlertSummary(
        Guid AlertId,
        string ServiceName,
        string Category,
        string PatternDetected,
        double Confidence,
        string Severity,
        string Status,
        DateTimeOffset DetectedAt);

    public sealed record Response(IReadOnlyList<GuardianAlertSummary> Items);
}
