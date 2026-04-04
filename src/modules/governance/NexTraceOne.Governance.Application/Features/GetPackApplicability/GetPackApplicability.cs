using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;
using FluentValidation;

namespace NexTraceOne.Governance.Application.Features.GetPackApplicability;

/// <summary>
/// Feature: GetPackApplicability — escopos de aplicabilidade de um governance pack.
/// Retorna os escopos onde o pack está aplicado com modo de enforcement e metadados.
    /// Retorna dados reais de rollout persistidos no módulo Governance.
/// </summary>
public static class GetPackApplicability
{
    /// <summary>Query para obter os escopos de aplicabilidade de um governance pack.</summary>
    public sealed record Query(string PackId) : IQuery<Response>;

    /// <summary>Handler que retorna os escopos de aplicabilidade do governance pack.</summary>
    /// <summary>Valida os parâmetros da query de aplicabilidade de pack.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.PackId).NotEmpty().MaximumLength(200);
        }
    }

    public sealed class Handler(
        IGovernanceRolloutRecordRepository rolloutRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(request.PackId, out var packGuid))
                return Error.Validation("INVALID_PACK_ID", "Pack ID '{0}' is not a valid GUID.", request.PackId);

            var rollouts = await rolloutRepository.ListByPackIdAsync(new GovernancePackId(packGuid), cancellationToken);
            var scopes = rollouts
                .OrderByDescending(r => r.InitiatedAt)
                .Select(r => new ApplicabilityScopeDto(
                    ScopeType: r.ScopeType,
                    ScopeValue: r.Scope,
                    EnforcementMode: r.EnforcementMode,
                    AppliedAt: r.InitiatedAt,
                    AppliedBy: r.InitiatedBy))
                .ToList();

            var response = new Response(
                PackId: request.PackId,
                Scopes: scopes);

            return Result<Response>.Success(response);
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
