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
            return Task.FromResult<Result<Response>>(
                Error.Business(
                    "Governance.Packs.Simulation.PreviewOnly",
                    "Governance pack simulation remains a preview-only workflow and is not backed by production data in this release."));
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
