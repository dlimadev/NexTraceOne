using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;
using FluentValidation;

namespace NexTraceOne.Governance.Application.Features.GetPackCoverage;

/// <summary>
/// Feature: GetPackCoverage — cobertura de conformidade de um governance pack.
/// Retorna métricas de cobertura por escopo com percentagem de conformidade.
/// P03.5: Dados reais baseados em rollout records e compliance gaps.
/// </summary>
public static class GetPackCoverage
{
    /// <summary>Query para obter a cobertura de conformidade de um governance pack.</summary>
    public sealed record Query(string PackId) : IQuery<Response>;

    /// <summary>
    /// Handler que calcula as métricas de cobertura do governance pack
    /// com base nos rollout records e compliance gaps persistidos.
    /// </summary>
    /// <summary>Valida os parâmetros da query de cobertura de pack.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.PackId).NotEmpty().MaximumLength(200);
        }
    }

    public sealed class Handler(
        IGovernanceRolloutRecordRepository rolloutRepository,
        IGovernancePackVersionRepository versionRepository,
        IComplianceGapRepository gapRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(request.PackId, out var packGuid))
                return Error.Validation("INVALID_PACK_ID", "Pack ID '{0}' is not a valid GUID.", request.PackId);

            var packId = new GovernancePackId(packGuid);

            // Fetch completed rollouts for this pack
            var rollouts = await rolloutRepository.ListAsync(
                packId, scopeType: null, scopeValue: null, status: RolloutStatus.Completed, cancellationToken);

            if (rollouts.Count == 0)
            {
                return Result<Response>.Success(new Response(
                    PackId: request.PackId,
                    OverallCoveragePercent: 0m,
                    TotalScopes: 0,
                    Items: []));
            }

            // Find the latest version to determine total rule count
            var latestVersion = await versionRepository.GetLatestByPackIdAsync(packId, cancellationToken);
            var totalRules = latestVersion?.Rules.Count ?? 0;

            // Fetch all compliance gaps to cross-reference violations per scope
            var allGaps = await gapRepository.ListAsync(teamId: null, domainId: null, serviceId: null, cancellationToken);

            // Build coverage items grouped by (ScopeType, ScopeValue)
            var distinctScopes = rollouts
                .GroupBy(r => (r.ScopeType, Scope: r.Scope))
                .Select(g => g.OrderByDescending(r => r.InitiatedAt).First())
                .ToList();

            var items = new List<CoverageItemDto>();

            foreach (var rollout in distinctScopes)
            {
                // Count violations matching this scope.
                // If violations exceed totalRules (e.g. stale gaps), clamp compliant to 0.
                var violations = CountViolationsForScope(allGaps, rollout.ScopeType, rollout.Scope);
                var clampedViolations = Math.Min(violations, totalRules);
                var compliant = totalRules - clampedViolations;
                var coveragePercent = totalRules > 0
                    ? Math.Round((decimal)compliant / totalRules * 100, 1)
                    : 0m;

                items.Add(new CoverageItemDto(
                    ScopeType: rollout.ScopeType.ToString(),
                    ScopeValue: rollout.Scope,
                    TotalRules: totalRules,
                    CompliantCount: compliant,
                    NonCompliantCount: clampedViolations,
                    CoveragePercent: coveragePercent));
            }

            var totalRulesAll = items.Sum(i => i.TotalRules);
            var totalCompliant = items.Sum(i => i.CompliantCount);
            var overallPercent = totalRulesAll > 0
                ? Math.Round((decimal)totalCompliant / totalRulesAll * 100, 1)
                : 0m;

            return Result<Response>.Success(new Response(
                PackId: request.PackId,
                OverallCoveragePercent: overallPercent,
                TotalScopes: items.Count,
                Items: items));
        }

        /// <summary>
        /// Counts the number of distinct violated policy IDs that match a scope.
        /// Matches by Domain or Team field on the ComplianceGap entity.
        /// </summary>
        private static int CountViolationsForScope(
            IReadOnlyList<ComplianceGap> gaps,
            GovernanceScopeType scopeType,
            string scopeValue)
        {
            var matching = scopeType switch
            {
                GovernanceScopeType.Domain => gaps.Where(g =>
                    string.Equals(g.Domain, scopeValue, StringComparison.OrdinalIgnoreCase)),
                GovernanceScopeType.Team => gaps.Where(g =>
                    string.Equals(g.Team, scopeValue, StringComparison.OrdinalIgnoreCase)),
                // NOTE: ComplianceGap entity doesn't carry an environment field.
                // Environment-scoped rollouts report 0 violations until the entity is extended.
                GovernanceScopeType.Environment => Enumerable.Empty<ComplianceGap>(),
                _ => Enumerable.Empty<ComplianceGap>()
            };

            return matching.SelectMany(g => g.ViolatedPolicyIds).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        }
    }

    /// <summary>Resposta com métricas de cobertura do governance pack.</summary>
    public sealed record Response(
        string PackId,
        decimal OverallCoveragePercent,
        int TotalScopes,
        IReadOnlyList<CoverageItemDto> Items);

    /// <summary>DTO de cobertura de conformidade por escopo.</summary>
    public sealed record CoverageItemDto(
        string ScopeType,
        string ScopeValue,
        int TotalRules,
        int CompliantCount,
        int NonCompliantCount,
        decimal CoveragePercent);
}
