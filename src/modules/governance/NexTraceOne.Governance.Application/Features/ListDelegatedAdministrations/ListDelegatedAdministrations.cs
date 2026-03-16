using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

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
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var delegations = new List<DelegatedAdminDto>
            {
                new("deleg-001", "user-042", "Ana Silva",
                    "TeamAdmin", "team-commerce", "Commerce", null, null,
                    "Delegação temporária durante licença do team lead",
                    true, DateTimeOffset.UtcNow.AddDays(-15), DateTimeOffset.UtcNow.AddDays(15)),
                new("deleg-002", "user-078", "Carlos Mendes",
                    "DomainAdmin", null, null, "domain-platform", "Platform",
                    "Administração delegada para supervisão de domínio durante reestruturação",
                    true, DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow.AddDays(60))
            };

            var response = new Response(Delegations: delegations);

            return Task.FromResult(Result<Response>.Success(response));
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
