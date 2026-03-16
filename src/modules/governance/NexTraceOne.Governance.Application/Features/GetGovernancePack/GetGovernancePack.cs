using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetGovernancePack;

/// <summary>
/// Feature: GetGovernancePack — detalhe completo de um governance pack.
/// Retorna regras vinculadas, escopos de aplicação e histórico de versões recentes.
/// MVP com dados estáticos para validação de fluxo.
/// </summary>
public static class GetGovernancePack
{
    /// <summary>Query para obter o detalhe de um governance pack pelo seu identificador.</summary>
    public sealed record Query(string PackId) : IQuery<Response>;

    /// <summary>Handler que retorna os detalhes completos de um governance pack.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var packs = BuildMockPacks();

            var pack = packs.FirstOrDefault(p => p.PackId == request.PackId);
            if (pack is null)
                return Task.FromResult<Result<Response>>(Error.NotFound("PACK_NOT_FOUND", "Governance pack '{0}' not found.", request.PackId));

            var response = new Response(pack);
            return Task.FromResult(Result<Response>.Success(response));
        }

        private static List<GovernancePackDetailDto> BuildMockPacks()
        {
            var rules = new List<RuleBindingDto>
            {
                new("rule-001", "CONTRACT-SCHEMA-VALID", "Contract schema must pass validation against the published standard",
                    GovernanceRuleCategory.Contracts, EnforcementMode.Blocking, true),
                new("rule-002", "CONTRACT-VERSION-SEMVER", "Contracts must follow semantic versioning",
                    GovernanceRuleCategory.Contracts, EnforcementMode.Required, true),
                new("rule-003", "CONTRACT-EXAMPLES-PRESENT", "Contract must include request/response examples",
                    GovernanceRuleCategory.Contracts, EnforcementMode.Advisory, false),
                new("rule-004", "CONTRACT-OWNER-ASSIGNED", "Contract must have a designated owner",
                    GovernanceRuleCategory.Contracts, EnforcementMode.Required, true),
                new("rule-005", "CONTRACT-CHANGELOG-UPDATED", "Changelog must be updated on every version change",
                    GovernanceRuleCategory.Contracts, EnforcementMode.Advisory, false)
            };

            var scopes = new List<ScopeDto>
            {
                new(GovernanceScopeType.Global, "*", EnforcementMode.Advisory),
                new(GovernanceScopeType.Environment, "Production", EnforcementMode.Blocking),
                new(GovernanceScopeType.Domain, "payments", EnforcementMode.Required),
                new(GovernanceScopeType.Team, "platform-core", EnforcementMode.Required)
            };

            var versions = new List<VersionSummaryDto>
            {
                new("ver-003", "2.1.0", DateTimeOffset.UtcNow.AddDays(-10), "architect@nextraceone.io", 18),
                new("ver-002", "2.0.0", DateTimeOffset.UtcNow.AddDays(-45), "techlead@nextraceone.io", 15),
                new("ver-001", "1.0.0", DateTimeOffset.UtcNow.AddDays(-120), "admin@nextraceone.io", 10)
            };

            return new List<GovernancePackDetailDto>
            {
                new("pack-001", "contracts-baseline", "Contracts Baseline",
                    "Baseline governance rules for API and event contract quality, versioning and documentation",
                    GovernanceRuleCategory.Contracts, GovernancePackStatus.Published,
                    "2.1.0", 4, 18, DateTimeOffset.UtcNow.AddDays(-120), DateTimeOffset.UtcNow.AddDays(-10),
                    rules, scopes, versions)
            };
        }
    }

    /// <summary>Resposta com detalhe completo do governance pack.</summary>
    public sealed record Response(GovernancePackDetailDto Pack);

    /// <summary>DTO com detalhe completo de um governance pack.</summary>
    public sealed record GovernancePackDetailDto(
        string PackId,
        string Name,
        string DisplayName,
        string Description,
        GovernanceRuleCategory Category,
        GovernancePackStatus Status,
        string CurrentVersion,
        int ScopeCount,
        int RuleCount,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        IReadOnlyList<RuleBindingDto> Rules,
        IReadOnlyList<ScopeDto> Scopes,
        IReadOnlyList<VersionSummaryDto> RecentVersions);

    /// <summary>DTO de uma regra vinculada ao governance pack.</summary>
    public sealed record RuleBindingDto(
        string RuleId,
        string RuleName,
        string Description,
        GovernanceRuleCategory Category,
        EnforcementMode DefaultEnforcementMode,
        bool IsRequired);

    /// <summary>DTO de um escopo de aplicação do governance pack.</summary>
    public sealed record ScopeDto(
        GovernanceScopeType ScopeType,
        string ScopeValue,
        EnforcementMode EnforcementMode);

    /// <summary>DTO resumido de uma versão do governance pack.</summary>
    public sealed record VersionSummaryDto(
        string VersionId,
        string Version,
        DateTimeOffset CreatedAt,
        string CreatedBy,
        int RuleCount);
}
