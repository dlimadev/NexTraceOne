using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Contracts.Graph.ServiceInterfaces;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.GetCrossDomainDependencies;

/// <summary>
/// Feature: GetCrossDomainDependencies — dependências de entrada e saída entre domínios de negócio.
/// Permite visualizar o grafo de dependências ao nível do domínio para análise de blast radius e acoplamento inter-domínio.
/// </summary>
public static class GetCrossDomainDependencies
{
    /// <summary>Query para obter dependências cross-domain de um domínio pelo ID.</summary>
    public sealed record Query(string DomainId) : IQuery<Response>;

    /// <summary>Handler que retorna dependências outbound e inbound do domínio.</summary>
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
            var outbound = new List<OutboundDomainDependencyDto>();

            foreach (var link in links)
            {
                var sourceTeam = await teamRepository.GetByIdAsync(link.TeamId, cancellationToken);
                if (sourceTeam is null)
                    continue;

                var dependencies = await catalogGraph.ListCrossTeamDependenciesAsync(sourceTeam.Name, cancellationToken) ?? [];
                foreach (var dependency in dependencies)
                {
                    var targetDomainId = string.Empty;
                    var targetDomainName = string.Empty;

                    if (Guid.TryParse(dependency.TargetTeamId, out var targetTeamGuid))
                    {
                        var targetLinks = await teamDomainLinkRepository.ListByTeamIdAsync(new TeamId(targetTeamGuid), cancellationToken);
                        var targetLink = targetLinks.FirstOrDefault();
                        if (targetLink is not null)
                        {
                            var targetDomain = await domainRepository.GetByIdAsync(targetLink.DomainId, cancellationToken);
                            if (targetDomain is not null)
                            {
                                targetDomainId = targetDomain.Id.Value.ToString();
                                targetDomainName = targetDomain.DisplayName;
                            }
                        }
                    }

                    outbound.Add(new OutboundDomainDependencyDto(
                        ServiceName: dependency.SourceServiceName,
                        SourceDomainName: domain.DisplayName,
                        TargetServiceName: dependency.TargetServiceName,
                        TargetDomainId: targetDomainId,
                        TargetDomainName: targetDomainName,
                        DependencyType: dependency.DependencyType));
                }
            }

            var inbound = new List<InboundDomainDependencyDto>(0);

            var response = new Response(
                DomainId: request.DomainId,
                DomainName: domain.DisplayName,
                Outbound: outbound,
                Inbound: inbound,
                IsSimulated: false);

            return Result<Response>.Success(response);
        }
    }

    /// <summary>Resposta com dependências outbound e inbound do domínio.</summary>
    public sealed record Response(
        string DomainId,
        string DomainName,
        IReadOnlyList<OutboundDomainDependencyDto> Outbound,
        IReadOnlyList<InboundDomainDependencyDto> Inbound,
        bool IsSimulated = false);

    /// <summary>DTO de dependência outbound — serviço do domínio que depende de outro domínio.</summary>
    public sealed record OutboundDomainDependencyDto(
        string ServiceName,
        string SourceDomainName,
        string TargetServiceName,
        string TargetDomainId,
        string TargetDomainName,
        string DependencyType);

    /// <summary>DTO de dependência inbound — serviço externo que depende de um serviço do domínio.</summary>
    public sealed record InboundDomainDependencyDto(
        string ServiceName,
        string TargetDomainName,
        string SourceServiceName,
        string SourceDomainId,
        string SourceDomainName,
        string DependencyType);
}
