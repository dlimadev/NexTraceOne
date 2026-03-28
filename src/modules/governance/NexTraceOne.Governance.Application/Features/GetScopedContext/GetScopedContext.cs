using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.GetScopedContext;

/// <summary>
/// Feature: GetScopedContext — contexto de governança do utilizador autenticado.
/// Retorna equipas, domínios, scopes e persona para segmentação da experiência no frontend.
/// </summary>
public static class GetScopedContext
{
    /// <summary>Query para obter o contexto de governança do utilizador atual.</summary>
    public sealed record Query() : IQuery<Response>;

    /// <summary>Handler que retorna o contexto de governança do utilizador com equipas e domínios permitidos.</summary>
    public sealed class Handler(
        ICurrentUser currentUser,
        IDelegatedAdministrationRepository delegationRepository,
        ITeamRepository teamRepository,
        ITeamDomainLinkRepository teamDomainLinkRepository,
        IGovernanceDomainRepository domainRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("UNAUTHENTICATED", "Current user is not authenticated.");

            var delegations = await delegationRepository.ListByGranteeAsync(currentUser.Id, cancellationToken);
            var activeDelegations = delegations.Where(d => d.IsActive && !d.IsExpired()).ToList();

            var adminScopes = new List<string>(activeDelegations.Count);
            var teamScopes = new List<(Team Team, DelegatedAdministration Delegation)>(activeDelegations.Count);
            var allowedDomainsMap = new Dictionary<string, AllowedScopeDto>(StringComparer.OrdinalIgnoreCase);

            foreach (var delegation in activeDelegations)
            {
                var role = ResolveRole(delegation.Scope);

                if (!string.IsNullOrWhiteSpace(delegation.TeamId)
                    && Guid.TryParse(delegation.TeamId, out var teamGuid))
                {
                    var team = await teamRepository.GetByIdAsync(new TeamId(teamGuid), cancellationToken);
                    if (team is not null)
                    {
                        teamScopes.Add((team, delegation));
                        if (role is "Admin")
                            adminScopes.Add(team.Id.Value.ToString());
                    }
                }

                if (!string.IsNullOrWhiteSpace(delegation.DomainId)
                    && Guid.TryParse(delegation.DomainId, out var domainGuid))
                {
                    var domain = await domainRepository.GetByIdAsync(new GovernanceDomainId(domainGuid), cancellationToken);
                    if (domain is not null)
                    {
                        allowedDomainsMap[domain.Id.Value.ToString()] = new AllowedScopeDto(
                            Id: domain.Id.Value.ToString(),
                            Name: domain.Name,
                            DisplayName: domain.DisplayName,
                            Role: role);

                        if (role is "Admin")
                            adminScopes.Add(domain.Id.Value.ToString());
                    }
                }
            }

            var allowedTeams = teamScopes
                .Select(ts =>
                {
                    var role = ResolveRole(ts.Delegation.Scope);

                    return new AllowedScopeDto(
                        Id: ts.Team.Id.Value.ToString(),
                        Name: ts.Team.Name,
                        DisplayName: ts.Team.DisplayName,
                        Role: role);
                })
                .DistinctBy(t => t.Id)
                .ToList();

            foreach (var team in teamScopes.Select(ts => ts.Team).DistinctBy(t => t.Id))
            {
                var links = await teamDomainLinkRepository.ListByTeamIdAsync(team.Id, cancellationToken);
                foreach (var link in links)
                {
                    var domain = await domainRepository.GetByIdAsync(link.DomainId, cancellationToken);
                    if (domain is null)
                        continue;

                    if (!allowedDomainsMap.ContainsKey(domain.Id.Value.ToString()))
                    {
                        allowedDomainsMap[domain.Id.Value.ToString()] = new AllowedScopeDto(
                            Id: domain.Id.Value.ToString(),
                            Name: domain.Name,
                            DisplayName: domain.DisplayName,
                            Role: "Member");
                    }
                }
            }

            var allowedDomains = allowedDomainsMap.Values.ToList();

            var defaultTeam = allowedTeams.FirstOrDefault();
            var defaultDomain = allowedDomains.FirstOrDefault();

            var personaHint = activeDelegations.Any(d =>
                    d.Scope is Governance.Domain.Enums.DelegationScope.FullAdmin
                    or Governance.Domain.Enums.DelegationScope.TeamAdmin
                    or Governance.Domain.Enums.DelegationScope.DomainAdmin)
                ? "PlatformAdmin"
                : "Engineer";

            var response = new Response(
                UserId: currentUser.Id,
                DefaultTeamId: defaultTeam?.Id ?? string.Empty,
                DefaultTeamName: defaultTeam?.DisplayName ?? string.Empty,
                DefaultDomainId: defaultDomain?.Id,
                DefaultDomainName: defaultDomain?.DisplayName,
                AllowedTeams: allowedTeams,
                AllowedDomains: allowedDomains,
                AdminScopes: adminScopes.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                IsCentralAdmin: activeDelegations.Any(d => d.Scope == Governance.Domain.Enums.DelegationScope.FullAdmin),
                PersonaHint: personaHint);

            return Result<Response>.Success(response);
        }

        private static string ResolveRole(Governance.Domain.Enums.DelegationScope scope) =>
            scope switch
            {
                Governance.Domain.Enums.DelegationScope.FullAdmin
                or Governance.Domain.Enums.DelegationScope.TeamAdmin
                or Governance.Domain.Enums.DelegationScope.DomainAdmin => "Admin",
                Governance.Domain.Enums.DelegationScope.ReadOnly => "Viewer",
                _ => "Member"
            };
    }

    /// <summary>Resposta com o contexto de governança do utilizador.</summary>
    public sealed record Response(
        string UserId,
        string DefaultTeamId,
        string DefaultTeamName,
        string? DefaultDomainId,
        string? DefaultDomainName,
        IReadOnlyList<AllowedScopeDto> AllowedTeams,
        IReadOnlyList<AllowedScopeDto> AllowedDomains,
        IReadOnlyList<string> AdminScopes,
        bool IsCentralAdmin,
        string PersonaHint);

    /// <summary>DTO de scope permitido para o utilizador.</summary>
    public sealed record AllowedScopeDto(
        string Id,
        string Name,
        string DisplayName,
        string Role);
}
