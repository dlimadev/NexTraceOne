using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.ListGovernancePacks;

/// <summary>
/// Feature: ListGovernancePacks — catálogo de governance packs disponíveis na plataforma.
/// Retorna packs configurados com categoria, status, versão e métricas de abrangência.
/// MVP com dados estáticos para validação de fluxo.
/// </summary>
public static class ListGovernancePacks
{
    /// <summary>Query para listar governance packs. Permite filtragem por categoria e status.</summary>
    public sealed record Query(
        string? Category = null,
        string? Status = null) : IQuery<Response>;

    /// <summary>Handler que retorna o catálogo de governance packs.</summary>
    public sealed class Handler : IQueryHandler<Query, Response>
    {
        public Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var packs = new List<GovernancePackDto>
            {
                new("pack-001", "contracts-baseline", "Contracts Baseline",
                    "Baseline governance rules for API and event contract quality, versioning and documentation",
                    GovernanceRuleCategory.Contracts, GovernancePackStatus.Published,
                    "2.1.0", 12, 18, DateTimeOffset.UtcNow.AddDays(-120)),
                new("pack-002", "source-of-truth-standards", "Source of Truth Standards",
                    "Ensures services, ownership and dependency data are accurate and up-to-date",
                    GovernanceRuleCategory.SourceOfTruth, GovernancePackStatus.Published,
                    "1.3.0", 8, 14, DateTimeOffset.UtcNow.AddDays(-90)),
                new("pack-003", "change-governance", "Change Governance",
                    "Rules for production change validation, blast radius assessment and rollback readiness",
                    GovernanceRuleCategory.Changes, GovernancePackStatus.Published,
                    "3.0.0", 15, 22, DateTimeOffset.UtcNow.AddDays(-60)),
                new("pack-004", "ai-usage-policy", "AI Usage Policy",
                    "Governance rules for AI model usage, token budgets, prompt auditing and data classification",
                    GovernanceRuleCategory.AIGovernance, GovernancePackStatus.Draft,
                    "0.9.0", 5, 10, DateTimeOffset.UtcNow.AddDays(-20)),
                new("pack-005", "operational-readiness", "Operational Readiness",
                    "Readiness checks for runbooks, incident response, monitoring coverage and SLO definitions",
                    GovernanceRuleCategory.Operations, GovernancePackStatus.Published,
                    "1.5.0", 10, 16, DateTimeOffset.UtcNow.AddDays(-75))
            };

            IEnumerable<GovernancePackDto> filtered = packs;

            if (!string.IsNullOrEmpty(request.Category) &&
                Enum.TryParse<GovernanceRuleCategory>(request.Category, out var cat))
                filtered = filtered.Where(p => p.Category == cat);

            if (!string.IsNullOrEmpty(request.Status) &&
                Enum.TryParse<GovernancePackStatus>(request.Status, out var st))
                filtered = filtered.Where(p => p.Status == st);

            var list = filtered.ToList();

            var response = new Response(
                TotalPacks: packs.Count,
                PublishedCount: packs.Count(p => p.Status == GovernancePackStatus.Published),
                DraftCount: packs.Count(p => p.Status == GovernancePackStatus.Draft),
                Packs: list);

            return Task.FromResult(Result<Response>.Success(response));
        }
    }

    /// <summary>Resposta com lista de governance packs.</summary>
    public sealed record Response(
        int TotalPacks,
        int PublishedCount,
        int DraftCount,
        IReadOnlyList<GovernancePackDto> Packs);

    /// <summary>DTO de um governance pack.</summary>
    public sealed record GovernancePackDto(
        string PackId,
        string Name,
        string DisplayName,
        string Description,
        GovernanceRuleCategory Category,
        GovernancePackStatus Status,
        string CurrentVersion,
        int ScopeCount,
        int RuleCount,
        DateTimeOffset CreatedAt);
}
