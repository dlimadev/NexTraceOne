using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetPackApplicability;

/// <summary>
/// Feature: GetPackApplicability — escopos de aplicabilidade de um governance pack.
/// Retorna os escopos onde o pack está aplicado com modo de enforcement e metadados.
/// MVP com dados estáticos para validação de fluxo.
/// </summary>
public static class GetPackApplicability
{
    /// <summary>Query para obter os escopos de aplicabilidade de um governance pack.</summary>
    public sealed record Query(string PackId) : IQuery<Response>;

    /// <summary>Handler que retorna os escopos de aplicabilidade do governance pack.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var scopes = new List<ApplicabilityScopeDto>
            {
                new(GovernanceScopeType.Global, "*", EnforcementMode.Advisory,
                    DateTimeOffset.UtcNow.AddDays(-90), "admin@nextraceone.io"),
                new(GovernanceScopeType.Environment, "Production", EnforcementMode.Blocking,
                    DateTimeOffset.UtcNow.AddDays(-60), "architect@nextraceone.io"),
                new(GovernanceScopeType.Domain, "payments", EnforcementMode.Required,
                    DateTimeOffset.UtcNow.AddDays(-45), "techlead@nextraceone.io"),
                new(GovernanceScopeType.Team, "platform-core", EnforcementMode.Advisory,
                    DateTimeOffset.UtcNow.AddDays(-30), "engineer@nextraceone.io")
            };

            var response = new Response(
                PackId: request.PackId,
                Scopes: scopes);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com escopos de aplicabilidade do governance pack.</summary>
    public sealed record Response(
        string PackId,
        IReadOnlyList<ApplicabilityScopeDto> Scopes);

    /// <summary>DTO de um escopo de aplicabilidade do governance pack.</summary>
    public sealed record ApplicabilityScopeDto(
        GovernanceScopeType ScopeType,
        string ScopeValue,
        EnforcementMode EnforcementMode,
        DateTimeOffset AppliedAt,
        string AppliedBy);
}
