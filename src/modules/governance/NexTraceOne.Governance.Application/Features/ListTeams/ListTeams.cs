using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Contracts.Graph.ServiceInterfaces;
using NexTraceOne.Governance.Application.Abstractions;

namespace NexTraceOne.Governance.Application.Features.ListTeams;

/// <summary>
/// Feature: ListTeams — lista sumária de todas as equipas registadas na plataforma.
/// Inclui contadores de serviços, contratos e membros para visão geral de governança.
/// </summary>
public static class ListTeams
{
    /// <summary>Query para listar todas as equipas.</summary>
    public sealed record Query() : IQuery<Response>;

    /// <summary>Handler que retorna lista de equipas com indicadores sumários.</summary>
    public sealed class Handler(
        ITeamRepository teamRepository,
        ITeamDomainLinkRepository teamDomainLinkRepository,
        ICatalogGraphModule catalogGraph) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var teams = await teamRepository.ListAsync(status: null, cancellationToken);

            var dtos = new List<TeamSummaryDto>();
            foreach (var t in teams)
            {
                var links = await teamDomainLinkRepository.ListByTeamIdAsync(t.Id, cancellationToken);
                var serviceCount = await catalogGraph.CountServicesByTeamAsync(t.Name, cancellationToken);
                var contracts = await catalogGraph.ListContractsByTeamAsync(t.Name, cancellationToken);
                dtos.Add(new TeamSummaryDto(
                    TeamId: t.Id.Value.ToString(),
                    Name: t.Name,
                    DisplayName: t.DisplayName,
                    Description: t.Description,
                    Status: t.Status.ToString(),
                    ServiceCount: serviceCount,
                    ContractCount: contracts.Count,
                    MemberCount: 0,     // Deferred: requires IdentityAccess integration
                    MaturityLevel: "Developing",
                    ParentOrganizationUnit: t.ParentOrganizationUnit
                ));
            }

            var response = new Response(Teams: dtos, IsSimulated: false);
            return Result<Response>.Success(response);
        }
    }

    /// <summary>Resposta com lista sumária de equipas.</summary>
    public sealed record Response(IReadOnlyList<TeamSummaryDto> Teams, bool IsSimulated = false);

    /// <summary>DTO sumário de equipa com indicadores de governança.</summary>
    public sealed record TeamSummaryDto(
        string TeamId,
        string Name,
        string DisplayName,
        string? Description,
        string Status,
        int ServiceCount,
        int ContractCount,
        int MemberCount,
        string MaturityLevel,
        string? ParentOrganizationUnit)
    {
        /// <summary>Fields not yet backed by real data.</summary>
        public IReadOnlyList<string> DeferredFields { get; init; } =
            ["MemberCount", "MaturityLevel"];
    }
}
