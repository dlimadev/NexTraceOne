using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.GetTeamDetail;

/// <summary>
/// Feature: GetTeamDetail — detalhe completo de uma equipa incluindo serviços, contratos e dependências cross-team.
/// Centraliza a visão de governança, ownership e fiabilidade ao nível da equipa.
/// </summary>
public static class GetTeamDetail
{
    /// <summary>Query para obter detalhe de uma equipa pelo ID.</summary>
    public sealed record Query(string TeamId) : IQuery<Response>;

    /// <summary>Handler que retorna detalhe completo de uma equipa com serviços, contratos e dependências.</summary>
    public sealed class Handler(ITeamRepository teamRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(request.TeamId, out var teamGuid))
                return Error.Validation("INVALID_TEAM_ID", "Team ID '{0}' is not a valid GUID.", request.TeamId);

            var team = await teamRepository.GetByIdAsync(new TeamId(teamGuid), cancellationToken);
            if (team is null)
                return Error.NotFound("TEAM_NOT_FOUND", "Team '{0}' not found.", request.TeamId);

            // TODO: enriquecer com dados reais de serviços, contratos e dependências cross-team
            var services = new List<TeamServiceDto>();
            var contracts = new List<TeamContractDto>();
            var crossTeamDeps = new List<CrossTeamDependencyDto>();

            var response = new Response(
                TeamId: team.Id.Value.ToString(),
                Name: team.Name,
                DisplayName: team.DisplayName,
                Description: team.Description,
                Status: team.Status.ToString(),
                ParentOrganizationUnit: team.ParentOrganizationUnit,
                ServiceCount: 0,
                ContractCount: 0,
                ActiveIncidentCount: 0,
                RecentChangeCount: 0,
                MaturityLevel: "Developing",
                ReliabilityScore: 0m,
                Services: services,
                Contracts: contracts,
                CrossTeamDependencies: crossTeamDeps,
                CreatedAt: team.CreatedAt);

            return Result<Response>.Success(response);
        }
    }

    /// <summary>Resposta com detalhe completo de uma equipa.</summary>
    public sealed record Response(
        string TeamId,
        string Name,
        string DisplayName,
        string? Description,
        string Status,
        string? ParentOrganizationUnit,
        int ServiceCount,
        int ContractCount,
        int ActiveIncidentCount,
        int RecentChangeCount,
        string MaturityLevel,
        decimal ReliabilityScore,
        IReadOnlyList<TeamServiceDto> Services,
        IReadOnlyList<TeamContractDto> Contracts,
        IReadOnlyList<CrossTeamDependencyDto> CrossTeamDependencies,
        DateTimeOffset CreatedAt);

    /// <summary>DTO de serviço associado a uma equipa.</summary>
    public sealed record TeamServiceDto(
        string ServiceId,
        string Name,
        string Domain,
        string Criticality,
        string OwnershipType);

    /// <summary>DTO de contrato associado a uma equipa.</summary>
    public sealed record TeamContractDto(
        string ContractId,
        string Name,
        string Type,
        string Version,
        string Status);

    /// <summary>DTO de dependência cross-team.</summary>
    public sealed record CrossTeamDependencyDto(
        string DependencyId,
        string SourceServiceName,
        string TargetServiceName,
        string TargetTeamId,
        string TargetTeamName,
        string DependencyType);
}
