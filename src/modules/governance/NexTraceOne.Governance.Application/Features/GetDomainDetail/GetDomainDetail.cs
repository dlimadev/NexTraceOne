using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

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
    public sealed class Handler(IGovernanceDomainRepository domainRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(request.DomainId, out var domainGuid))
                return Error.Validation("INVALID_DOMAIN_ID", "Domain ID '{0}' is not a valid GUID.", request.DomainId);

            var domain = await domainRepository.GetByIdAsync(new GovernanceDomainId(domainGuid), cancellationToken);
            if (domain is null)
                return Error.NotFound("DOMAIN_NOT_FOUND", "Domain '{0}' not found.", request.DomainId);

            // TODO: enriquecer com dados reais de equipas, serviços e dependências cross-domain
            var teams = new List<DomainTeamDto>();
            var services = new List<DomainServiceDto>();
            var crossDomainDeps = new List<CrossDomainDependencyDto>();

            var response = new Response(
                DomainId: domain.Id.Value.ToString(),
                Name: domain.Name,
                DisplayName: domain.DisplayName,
                Description: domain.Description,
                Criticality: domain.Criticality.ToString(),
                CapabilityClassification: domain.CapabilityClassification,
                TeamCount: 0,
                ServiceCount: 0,
                ActiveIncidentCount: 0,
                RecentChangeCount: 0,
                MaturityLevel: "Developing",
                ReliabilityScore: 0m,
                Teams: teams,
                Services: services,
                CrossDomainDependencies: crossDomainDeps,
                CreatedAt: domain.CreatedAt);

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
        DateTimeOffset CreatedAt);

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
