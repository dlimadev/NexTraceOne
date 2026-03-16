using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.Governance.Application.Features.SimulateGovernancePack;

/// <summary>
/// Feature: SimulateGovernancePack — simulação de impacto de um governance pack.
/// Projeta o efeito da aplicação do pack sobre entidades existentes sem alterar estado real.
/// MVP com dados estáticos para validação de fluxo.
/// </summary>
public static class SimulateGovernancePack
{
    /// <summary>Query para simular o impacto de aplicação de um governance pack.</summary>
    public sealed record Query(
        string PackId,
        string? ScopeType = null,
        string? ScopeValue = null) : IQuery<Response>;

    /// <summary>Handler que executa simulação e retorna projeção de impacto.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var entities = new List<SimulationImpactDto>
            {
                new("Service", "svc-001", "payment-api", "NonCompliant", "Compliant", false),
                new("Service", "svc-002", "order-service", "Compliant", "Compliant", false),
                new("Service", "svc-003", "notification-hub", "NonCompliant", "NonCompliant", true),
                new("Contract", "ctr-001", "payment-api-v2", "NonCompliant", "Compliant", false),
                new("Contract", "ctr-002", "order-events-v1", "Compliant", "Compliant", false),
                new("Contract", "ctr-003", "user-profile-api", "NonCompliant", "NonCompliant", true),
                new("Service", "svc-004", "analytics-pipeline", "NonCompliant", "Compliant", false),
                new("Contract", "ctr-004", "billing-soap-v3", "NonCompliant", "NonCompliant", true)
            };

            var byTeam = new Dictionary<string, int>
            {
                ["platform-core"] = 3,
                ["payments"] = 2,
                ["growth"] = 2,
                ["data-engineering"] = 1
            };

            var byDomain = new Dictionary<string, int>
            {
                ["payments"] = 3,
                ["operations"] = 2,
                ["analytics"] = 2,
                ["identity"] = 1
            };

            var summary = new SimulationSummaryDto(
                TotalAffected: entities.Count,
                CompliantCount: entities.Count(e => e.ProjectedStatus == "Compliant"),
                NonCompliantCount: entities.Count(e => e.ProjectedStatus == "NonCompliant"),
                BlockingCount: entities.Count(e => e.BlockingImpact),
                ByTeam: byTeam,
                ByDomain: byDomain);

            var response = new Response(
                SimulationId: Guid.NewGuid().ToString(),
                PackId: request.PackId,
                Summary: summary,
                ImpactedEntities: entities,
                SimulatedAt: DateTimeOffset.UtcNow);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com resultado da simulação de impacto do governance pack.</summary>
    public sealed record Response(
        string SimulationId,
        string PackId,
        SimulationSummaryDto Summary,
        IReadOnlyList<SimulationImpactDto> ImpactedEntities,
        DateTimeOffset SimulatedAt);

    /// <summary>DTO de impacto projetado sobre uma entidade.</summary>
    public sealed record SimulationImpactDto(
        string EntityType,
        string EntityId,
        string EntityName,
        string CurrentStatus,
        string ProjectedStatus,
        bool BlockingImpact);

    /// <summary>DTO de resumo agregado da simulação.</summary>
    public sealed record SimulationSummaryDto(
        int TotalAffected,
        int CompliantCount,
        int NonCompliantCount,
        int BlockingCount,
        Dictionary<string, int> ByTeam,
        Dictionary<string, int> ByDomain);
}
