using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Contracts.Graph.ServiceInterfaces;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.GetCrossTeamDependencies;

/// <summary>
/// Feature: GetCrossTeamDependencies — dependências de entrada e saída entre equipas.
/// Permite visualizar o grafo de dependências ao nível da equipa para análise de blast radius e acoplamento.
/// </summary>
public static class GetCrossTeamDependencies
{
    /// <summary>Query para obter dependências cross-team de uma equipa pelo ID.</summary>
    public sealed record Query(string TeamId) : IQuery<Response>;

    /// <summary>Handler que retorna dependências outbound e inbound da equipa.</summary>
    public sealed class Handler(
        ITeamRepository teamRepository,
        ICatalogGraphModule catalogGraph) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(request.TeamId, out var teamGuid))
                return Error.Validation("INVALID_TEAM_ID", "Team ID '{0}' is not a valid GUID.", request.TeamId);

            var team = await teamRepository.GetByIdAsync(new TeamId(teamGuid), cancellationToken);
            if (team is null)
                return Error.NotFound("TEAM_NOT_FOUND", "Team '{0}' not found.", request.TeamId);

            var dependencies = await catalogGraph.ListCrossTeamDependenciesAsync(team.Name, cancellationToken) ?? [];
            var outbound = dependencies
                .Select(d => new OutboundDependencyDto(
                    ServiceName: d.SourceServiceName,
                    TargetServiceName: d.TargetServiceName,
                    TargetTeamId: d.TargetTeamId,
                    TargetTeamName: d.TargetTeamName,
                    DependencyType: d.DependencyType))
                .ToList();

            var inbound = new List<InboundDependencyDto>();

            var response = new Response(
                TeamId: request.TeamId,
                TeamName: team.DisplayName,
                Outbound: outbound,
                Inbound: inbound,
                IsSimulated: false);

            return Result<Response>.Success(response);
        }
    }

    /// <summary>Resposta com dependências outbound e inbound da equipa.</summary>
    public sealed record Response(
        string TeamId,
        string TeamName,
        IReadOnlyList<OutboundDependencyDto> Outbound,
        IReadOnlyList<InboundDependencyDto> Inbound,
        bool IsSimulated = false);

    /// <summary>DTO de dependência outbound — serviço da equipa que depende de outra equipa.</summary>
    public sealed record OutboundDependencyDto(
        string ServiceName,
        string TargetServiceName,
        string TargetTeamId,
        string TargetTeamName,
        string DependencyType);

    /// <summary>DTO de dependência inbound — serviço externo que depende de um serviço da equipa.</summary>
    public sealed record InboundDependencyDto(
        string ServiceName,
        string SourceServiceName,
        string SourceTeamId,
        string SourceTeamName,
        string DependencyType);
}
