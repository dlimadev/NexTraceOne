using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;
using FluentValidation;

namespace NexTraceOne.Governance.Application.Features.ListGovernanceWaivers;

/// <summary>
/// Feature: ListGovernanceWaivers — catálogo de waivers de governança.
/// Retorna pedidos de exceção com estado de aprovação, justificação e metadados.
/// </summary>
public static class ListGovernanceWaivers
{
    /// <summary>Query para listar waivers de governança. Permite filtragem por pack e status.</summary>
    public sealed record Query(
        string? PackId = null,
        string? Status = null) : IQuery<Response>;

    /// <summary>Handler que retorna o catálogo de waivers de governança.</summary>
    /// <summary>Valida os filtros opcionais da query de waivers de governança.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.PackId).MaximumLength(200)
                .When(x => x.PackId is not null);
            RuleFor(x => x.Status).MaximumLength(200)
                .When(x => x.Status is not null);
        }
    }

    public sealed class Handler(
        IGovernanceWaiverRepository waiverRepository,
        IGovernancePackRepository packRepository,
        IGovernancePackVersionRepository versionRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            // Parse de filtros opcionais
            GovernancePackId? packIdFilter = null;
            if (!string.IsNullOrEmpty(request.PackId) && Guid.TryParse(request.PackId, out var packGuid))
                packIdFilter = new GovernancePackId(packGuid);

            WaiverStatus? statusFilter = null;
            if (!string.IsNullOrEmpty(request.Status) &&
                Enum.TryParse<WaiverStatus>(request.Status, ignoreCase: true, out var st))
                statusFilter = st;

            var waivers = await waiverRepository.ListAsync(packIdFilter, statusFilter, cancellationToken);

            // Obter nomes dos packs para enriquecer DTOs
            var packIds = waivers.Select(w => w.PackId).Distinct().ToList();
            var packs = new Dictionary<GovernancePackId, GovernancePack>();
            var packVersions = new Dictionary<GovernancePackId, IReadOnlyList<GovernanceRuleBinding>>();
            
            foreach (var pid in packIds)
            {
                var pack = await packRepository.GetByIdAsync(pid, cancellationToken);
                if (pack is not null)
                    packs[pid] = pack;

                var latestVersion = await versionRepository.GetLatestByPackIdAsync(pid, cancellationToken);
                if (latestVersion is not null)
                    packVersions[pid] = latestVersion.Rules;
            }

            var dtos = waivers.Select(w => new WaiverDto(
                WaiverId: w.Id.Value.ToString(),
                PackId: w.PackId.Value.ToString(),
                PackName: packs.TryGetValue(w.PackId, out var p) ? p.DisplayName : "Unknown",
                RuleId: w.RuleId ?? string.Empty,
                RuleName: ResolveRuleName(w.RuleId, w.PackId, packVersions),
                Scope: w.Scope,
                ScopeType: w.ScopeType.ToString(),
                Justification: w.Justification,
                Status: w.Status.ToString(),
                RequestedBy: w.RequestedBy,
                RequestedAt: w.RequestedAt,
                ReviewedBy: w.ReviewedBy,
                ReviewedAt: w.ReviewedAt,
                ExpiresAt: w.ExpiresAt
            )).ToList();

            var response = new Response(
                TotalWaivers: dtos.Count,
                PendingCount: dtos.Count(w => w.Status == WaiverStatus.Pending.ToString()),
                ApprovedCount: dtos.Count(w => w.Status == WaiverStatus.Approved.ToString()),
                Waivers: dtos);

            return Result<Response>.Success(response);
        }

        private static string ResolveRuleName(string? ruleId, GovernancePackId packId, Dictionary<GovernancePackId, IReadOnlyList<GovernanceRuleBinding>> packVersions)
        {
            if (string.IsNullOrEmpty(ruleId)) return "(Entire Pack)";
            if (packVersions.TryGetValue(packId, out var rules))
            {
                var rule = rules.FirstOrDefault(r => r.RuleId.Equals(ruleId, StringComparison.OrdinalIgnoreCase));
                if (rule is not null) return rule.RuleName;
            }
            return ruleId; // fallback: show rule ID if name not found
        }
    }

    /// <summary>Resposta com lista de waivers de governança.</summary>
    public sealed record Response(
        int TotalWaivers,
        int PendingCount,
        int ApprovedCount,
        IReadOnlyList<WaiverDto> Waivers);

    /// <summary>DTO de um waiver de governança.</summary>
    public sealed record WaiverDto(
        string WaiverId,
        string PackId,
        string PackName,
        string RuleId,
        string RuleName,
        string Scope,
        string ScopeType,
        string Justification,
        string Status,
        string RequestedBy,
        DateTimeOffset RequestedAt,
        string? ReviewedBy,
        DateTimeOffset? ReviewedAt,
        DateTimeOffset? ExpiresAt);
}
