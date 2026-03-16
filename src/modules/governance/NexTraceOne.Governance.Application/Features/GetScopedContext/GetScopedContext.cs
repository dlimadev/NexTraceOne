using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

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
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var allowedTeams = new List<AllowedScopeDto>
            {
                new("team-commerce", "commerce-squad", "Commerce", "Admin"),
                new("team-platform", "platform-squad", "Platform", "Member")
            };

            var allowedDomains = new List<AllowedScopeDto>
            {
                new("domain-commerce", "commerce", "Commerce", "Owner"),
                new("domain-platform", "platform", "Platform", "Viewer")
            };

            var response = new Response(
                UserId: "user-001",
                DefaultTeamId: "team-commerce",
                DefaultTeamName: "Commerce",
                DefaultDomainId: "domain-commerce",
                DefaultDomainName: "Commerce",
                AllowedTeams: allowedTeams,
                AllowedDomains: allowedDomains,
                AdminScopes: new List<string> { "team-commerce", "domain-commerce" },
                IsCentralAdmin: false,
                PersonaHint: "TechLead");

            return Task.FromResult(Result<Response>.Success(response));
        }
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
