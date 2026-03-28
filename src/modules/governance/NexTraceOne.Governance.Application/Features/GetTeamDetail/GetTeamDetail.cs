using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Contracts.Graph.ServiceInterfaces;
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

            var serviceCount = await catalogGraph.CountServicesByTeamAsync(team.Name, cancellationToken);

            // Enrich with real services, contracts and cross-team dependencies from Catalog
            var catalogServices = await catalogGraph.ListServicesByTeamAsync(team.Name, cancellationToken);
            var services = catalogServices
                .Select(s => new TeamServiceDto(s.ServiceId, s.Name, s.Domain, s.Criticality, s.OwnershipType))
                .ToList();

            var catalogContracts = await catalogGraph.ListContractsByTeamAsync(team.Name, cancellationToken);
            var contracts = catalogContracts
                .Select(c => new TeamContractDto(c.ContractId, c.Name, c.Type, c.Version, c.Status))
                .ToList();

            var catalogDeps = await catalogGraph.ListCrossTeamDependenciesAsync(team.Name, cancellationToken);
            var crossTeamDeps = catalogDeps
                .Select(d => new CrossTeamDependencyDto(d.DependencyId, d.SourceServiceName, d.TargetServiceName, d.TargetTeamId, d.TargetTeamName, d.DependencyType))
                .ToList();

            var response = new Response(
                TeamId: team.Id.Value.ToString(),
                Name: team.Name,
                DisplayName: team.DisplayName,
                Description: team.Description,
                Status: team.Status.ToString(),
                ParentOrganizationUnit: team.ParentOrganizationUnit,
                ServiceCount: serviceCount,
                ContractCount: contracts.Count,
                ActiveIncidentCount: 0,
                RecentChangeCount: 0,
                MaturityLevel: "Developing",
                ReliabilityScore: 0m,
                Services: services,
                Contracts: contracts,
                CrossTeamDependencies: crossTeamDeps,
                CreatedAt: team.CreatedAt,
                IsSimulated: false);

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
        DateTimeOffset CreatedAt,
        bool IsSimulated = false)
    {
        /// <summary>Fields not yet backed by real data.</summary>
        public IReadOnlyList<string> DeferredFields { get; init; } =
            ["ActiveIncidentCount", "RecentChangeCount", "MaturityLevel", "ReliabilityScore"];
    }

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
