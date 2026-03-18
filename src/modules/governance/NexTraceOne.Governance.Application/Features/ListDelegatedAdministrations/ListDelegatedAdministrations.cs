using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.ListDelegatedAdministrations;

/// <summary>
/// Feature: ListDelegatedAdministrations — lista de delegações de administração ativas e históricas.
/// Inclui informação de scope, equipa/domínio e validade da delegação para auditoria.
/// </summary>
public static class ListDelegatedAdministrations
{
    /// <summary>Query para listar todas as delegações de administração.</summary>
    public sealed record Query() : IQuery<Response>;

    /// <summary>Handler que retorna lista de delegações de administração com detalhes.</summary>
    public sealed class Handler(
        IDelegatedAdministrationRepository delegationRepository,
        ITeamRepository teamRepository,
        IGovernanceDomainRepository domainRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var delegations = await delegationRepository.ListAsync(scope: null, isActive: null, cancellationToken);

            // Enriquecer com nomes de equipas e domínios
            var dtos = new List<DelegatedAdminDto>();
            foreach (var d in delegations)
            {
                string? teamName = null;
                string? domainName = null;

                if (!string.IsNullOrEmpty(d.TeamId) && Guid.TryParse(d.TeamId, out var teamGuid))
                {
                    var team = await teamRepository.GetByIdAsync(new TeamId(teamGuid), cancellationToken);
                    teamName = team?.DisplayName;
                }

                if (!string.IsNullOrEmpty(d.DomainId) && Guid.TryParse(d.DomainId, out var domainGuid))
                {
                    var domain = await domainRepository.GetByIdAsync(new GovernanceDomainId(domainGuid), cancellationToken);
                    domainName = domain?.DisplayName;
                }

                dtos.Add(new DelegatedAdminDto(
                    DelegationId: d.Id.Value.ToString(),
                    GranteeUserId: d.GranteeUserId,
                    GranteeDisplayName: d.GranteeDisplayName,
                    Scope: d.Scope.ToString(),
                    TeamId: d.TeamId,
                    TeamName: teamName,
                    DomainId: d.DomainId,
                    DomainName: domainName,
                    Reason: d.Reason,
                    IsActive: d.IsActive && !d.IsExpired(),
                    GrantedAt: d.GrantedAt,
                    ExpiresAt: d.ExpiresAt
                ));
            }

            var response = new Response(Delegations: dtos);

            return Result<Response>.Success(response);
        }
    }

    /// <summary>Resposta com lista de delegações de administração.</summary>
    public sealed record Response(IReadOnlyList<DelegatedAdminDto> Delegations);

    /// <summary>DTO de delegação de administração com scope, validade e estado.</summary>
    public sealed record DelegatedAdminDto(
        string DelegationId,
        string GranteeUserId,
        string GranteeDisplayName,
        string Scope,
        string? TeamId,
        string? TeamName,
        string? DomainId,
        string? DomainName,
        string Reason,
        bool IsActive,
        DateTimeOffset GrantedAt,
        DateTimeOffset? ExpiresAt);
}
