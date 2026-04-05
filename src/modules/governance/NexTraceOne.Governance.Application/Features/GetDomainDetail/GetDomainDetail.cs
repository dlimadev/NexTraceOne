using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Contracts.Graph.ServiceInterfaces;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using FluentValidation;

namespace NexTraceOne.Governance.Application.Features.GetDomainDetail;

/// <summary>
/// Feature: GetDomainDetail — detalhe completo de um domínio de negócio incluindo equipas, serviços e dependências cross-domain.
/// Centraliza a visão de governança, criticidade e fiabilidade ao nível do domínio.
/// </summary>
public static class GetDomainDetail
{
    /// <summary>Query para obter detalhe de um domínio pelo ID.</summary>
    public sealed record Query(string DomainId) : IQuery<Response>;

    /// <summary>Handler que retorna detalhe completo de um domínio com equipas, serviços e dependências.</summary>
    /// <summary>Valida os parâmetros da query de detalhe de domínio.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.DomainId).NotEmpty().MaximumLength(200);
        }
    }

    public sealed class Handler(
        IGovernanceDomainRepository domainRepository,
        ITeamDomainLinkRepository teamDomainLinkRepository,
        ITeamRepository teamRepository,
        ICatalogGraphModule catalogGraph) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(request.DomainId, out var domainGuid))
                return Error.Validation("INVALID_DOMAIN_ID", "Domain ID '{0}' is not a valid GUID.", request.DomainId);

            var domain = await domainRepository.GetByIdAsync(new GovernanceDomainId(domainGuid), cancellationToken);
            if (domain is null)
                return Error.NotFound("DOMAIN_NOT_FOUND", "Domain '{0}' not found.", request.DomainId);

            var links = await teamDomainLinkRepository.ListByDomainIdAsync(domain.Id, cancellationToken);

            var teams = new List<DomainTeamDto>();
            foreach (var link in links)
            {
                var team = await teamRepository.GetByIdAsync(link.TeamId, cancellationToken);
                if (team is not null)
                {
                    var teamServiceCount = await catalogGraph.CountServicesByTeamAsync(team.Name, cancellationToken);
                    teams.Add(new DomainTeamDto(
                        TeamId: team.Id.Value.ToString(),
                        Name: team.Name,
                        DisplayName: team.DisplayName,
                        ServiceCount: teamServiceCount,
                        OwnershipType: link.OwnershipType.ToString()));
                }
            }

            var domainServiceCount = await catalogGraph.CountServicesByDomainAsync(domain.Name, cancellationToken);
            var services = new List<DomainServiceDto>();
            var crossDomainDeps = new List<CrossDomainDependencyDto>();

            var response = new Response(
                DomainId: domain.Id.Value.ToString(),
                Name: domain.Name,
                DisplayName: domain.DisplayName,
                Description: domain.Description,
                Criticality: domain.Criticality.ToString(),
                CapabilityClassification: domain.CapabilityClassification,
                TeamCount: teams.Count,
                ServiceCount: domainServiceCount,
                ActiveIncidentCount: 0,
                RecentChangeCount: 0,
                MaturityLevel: "Developing",
                ReliabilityScore: 0m,
                Teams: teams,
                Services: services,
                CrossDomainDependencies: crossDomainDeps,
                CreatedAt: domain.CreatedAt,
                IsSimulated: false);

            return Result<Response>.Success(response);
        }
    }

    /// <summary>Resposta com detalhe completo de um domínio.</summary>
    public sealed record Response(
        string DomainId,
        string Name,
        string DisplayName,
        string? Description,
        string Criticality,
        string? CapabilityClassification,
        int TeamCount,
        int ServiceCount,
        int ActiveIncidentCount,
        int RecentChangeCount,
        string MaturityLevel,
        decimal ReliabilityScore,
        IReadOnlyList<DomainTeamDto> Teams,
        IReadOnlyList<DomainServiceDto> Services,
        IReadOnlyList<CrossDomainDependencyDto> CrossDomainDependencies,
        DateTimeOffset CreatedAt,
        bool IsSimulated = false)
    {
        /// <summary>Fields not yet backed by real data.</summary>
        public IReadOnlyList<string> DeferredFields { get; init; } =
            ["ActiveIncidentCount", "RecentChangeCount", "MaturityLevel", "ReliabilityScore"];
    }

    /// <summary>DTO de equipa associada a um domínio.</summary>
    public sealed record DomainTeamDto(
        string TeamId,
        string Name,
        string DisplayName,
        int ServiceCount,
        string OwnershipType);

    /// <summary>DTO de serviço pertencente a um domínio.</summary>
    public sealed record DomainServiceDto(
        string ServiceId,
        string Name,
        string TeamName,
        string Criticality,
        string Status);

    /// <summary>DTO de dependência cross-domain.</summary>
    public sealed record CrossDomainDependencyDto(
        string DependencyId,
        string SourceServiceName,
        string SourceDomainName,
        string TargetServiceName,
        string TargetDomainId,
        string TargetDomainName,
        string DependencyType);
}
