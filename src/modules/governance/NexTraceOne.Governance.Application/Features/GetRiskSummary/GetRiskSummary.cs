using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;
using FluentValidation;

namespace NexTraceOne.Governance.Application.Features.GetRiskSummary;

/// <summary>
/// Feature: GetRiskSummary — resumo de risco operacional contextualizado.
/// Cada indicador de risco está vinculado a um serviço, equipa ou domínio.
/// </summary>
public static class GetRiskSummary
{
    /// <summary>Query de resumo de risco. Permite filtragem por equipa ou domínio.</summary>
    public sealed record Query(
        string? TeamId = null,
        string? DomainId = null) : IQuery<Response>;

    /// <summary>
    /// Handler que computa indicadores de risco enterprise a partir de dados persistidos de rollouts e waivers.
    /// Nesta etapa, risco é calculado por Governance Pack (não por serviço) para evitar métricas fake.
    /// </summary>
    /// <summary>Valida os filtros opcionais da query de resumo de risco.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TeamId).MaximumLength(200)
                .When(x => x.TeamId is not null);
            RuleFor(x => x.DomainId).MaximumLength(200)
                .When(x => x.DomainId is not null);
        }
    }

    public sealed class Handler(
        IGovernancePackRepository packRepository,
        IGovernanceWaiverRepository waiverRepository,
        IGovernanceRolloutRecordRepository rolloutRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var packs = await packRepository.ListAsync(category: null, status: null, ct: cancellationToken);

            var waivers = await waiverRepository.ListAsync(packId: null, status: null, ct: cancellationToken);
            if (!string.IsNullOrWhiteSpace(request.TeamId))
                waivers = waivers.Where(w => w.ScopeType == GovernanceScopeType.Team && w.Scope == request.TeamId).ToList();
            if (!string.IsNullOrWhiteSpace(request.DomainId))
                waivers = waivers.Where(w => w.ScopeType == GovernanceScopeType.Domain && w.Scope == request.DomainId).ToList();

            var rollouts = await rolloutRepository.ListAsync(
                packId: null,
                scopeType: null,
                scopeValue: null,
                status: null,
                ct: cancellationToken);
            if (!string.IsNullOrWhiteSpace(request.TeamId))
                rollouts = rollouts.Where(r => r.ScopeType == GovernanceScopeType.Team && r.Scope == request.TeamId).ToList();
            if (!string.IsNullOrWhiteSpace(request.DomainId))
                rollouts = rollouts.Where(r => r.ScopeType == GovernanceScopeType.Domain && r.Scope == request.DomainId).ToList();

            var waiversByPack = waivers
                .GroupBy(w => w.PackId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var rolloutsByPack = rollouts
                .GroupBy(r => r.PackId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var indicators = packs
                .Select(p => BuildRiskIndicator(p, waiversByPack, rolloutsByPack))
                .OrderByDescending(i => i.RiskLevel)
                .ThenBy(i => i.PackName)
                .ToList();

            var overall = indicators.Count == 0
                ? RiskLevel.Low
                : indicators.Max(i => i.RiskLevel);

            var response = new Response(
                OverallRiskLevel: overall,
                TotalPacksAssessed: indicators.Count,
                CriticalCount: indicators.Count(i => i.RiskLevel == RiskLevel.Critical),
                HighCount: indicators.Count(i => i.RiskLevel == RiskLevel.High),
                MediumCount: indicators.Count(i => i.RiskLevel == RiskLevel.Medium),
                LowCount: indicators.Count(i => i.RiskLevel == RiskLevel.Low),
                Indicators: indicators,
                GeneratedAt: DateTimeOffset.UtcNow);

            return Result<Response>.Success(response);
        }

        private static RiskIndicatorDto BuildRiskIndicator(
            GovernancePack pack,
            IReadOnlyDictionary<GovernancePackId, List<GovernanceWaiver>> waiversByPack,
            IReadOnlyDictionary<GovernancePackId, List<GovernanceRolloutRecord>> rolloutsByPack)
        {
            waiversByPack.TryGetValue(pack.Id, out var packWaivers);
            rolloutsByPack.TryGetValue(pack.Id, out var packRollouts);

            var pendingWaivers = (packWaivers ?? []).Count(w => w.Status == WaiverStatus.Pending);
            var approvedWaivers = (packWaivers ?? []).Count(w => w.Status == WaiverStatus.Approved);
            var failedRollouts = (packRollouts ?? []).Count(r => r.Status == RolloutStatus.Failed);
            var pendingRollouts = (packRollouts ?? []).Count(r => r.Status == RolloutStatus.Pending);

            var risk = failedRollouts > 0
                ? RiskLevel.Critical
                : pendingWaivers > 0
                    ? RiskLevel.High
                    : pack.Status == GovernancePackStatus.Draft || pendingRollouts > 0
                        ? RiskLevel.Medium
                        : RiskLevel.Low;

            var dims = new List<RiskDimensionDto>();

            if (failedRollouts > 0)
                dims.Add(new RiskDimensionDto(RiskDimension.Rollouts, RiskLevel.Critical, $"{failedRollouts} failed rollout(s)"));
            else if (pendingRollouts > 0)
                dims.Add(new RiskDimensionDto(RiskDimension.Rollouts, RiskLevel.Medium, $"{pendingRollouts} pending rollout(s)"));
            else
                dims.Add(new RiskDimensionDto(RiskDimension.Rollouts, RiskLevel.Low, "No failed rollouts"));

            if (pendingWaivers > 0)
                dims.Add(new RiskDimensionDto(RiskDimension.Waivers, RiskLevel.High, $"{pendingWaivers} pending waiver(s)"));
            else if (approvedWaivers > 0)
                dims.Add(new RiskDimensionDto(RiskDimension.Waivers, RiskLevel.Medium, $"{approvedWaivers} approved waiver(s)"));
            else
                dims.Add(new RiskDimensionDto(RiskDimension.Waivers, RiskLevel.Low, "No active waivers"));

            var lifecycleLevel = pack.Status switch
            {
                GovernancePackStatus.Draft => RiskLevel.Medium,
                GovernancePackStatus.Deprecated => RiskLevel.Medium,
                GovernancePackStatus.Archived => RiskLevel.High,
                GovernancePackStatus.Published => RiskLevel.Low,
                _ => RiskLevel.Low
            };
            dims.Add(new RiskDimensionDto(RiskDimension.Lifecycle, lifecycleLevel, $"Pack status: {pack.Status}"));

            return new RiskIndicatorDto(
                PackId: pack.Id.Value.ToString(),
                PackName: pack.DisplayName,
                Category: pack.Category,
                RiskLevel: risk,
                Dimensions: dims);
        }
    }

    /// <summary>Resposta do resumo de risco.</summary>
    public sealed record Response(
        RiskLevel OverallRiskLevel,
        int TotalPacksAssessed,
        int CriticalCount,
        int HighCount,
        int MediumCount,
        int LowCount,
        IReadOnlyList<RiskIndicatorDto> Indicators,
        DateTimeOffset GeneratedAt);

    /// <summary>Indicador de risco por serviço.</summary>
    public sealed record RiskIndicatorDto(
        string PackId,
        string PackName,
        GovernanceRuleCategory Category,
        RiskLevel RiskLevel,
        IReadOnlyList<RiskDimensionDto> Dimensions);

    /// <summary>Dimensão de risco individual com explicação.</summary>
    public sealed record RiskDimensionDto(
        RiskDimension Dimension,
        RiskLevel Level,
        string Explanation);
}
