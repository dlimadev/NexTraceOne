using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetReportsSummary;

/// <summary>
/// Feature: GetReportsSummary — gera resumo executivo agregado para relatórios por persona.
/// Agrega indicadores de governança com base nos dados reais de Packs, Rollouts e Waivers.
/// Métricas cross-module (serviços, incidentes, mudanças) retornam 0 até integração futura.
/// </summary>
public static class GetReportsSummary
{
    /// <summary>Query para resumo de relatórios. Pode ser filtrada por equipa, domínio ou persona.</summary>
    public sealed record Query(
        string? TeamId = null,
        string? DomainId = null,
        string? Persona = null) : IQuery<Response>;

    /// <summary>
    /// Handler que computa o resumo de relatórios a partir de dados reais de Governance Packs,
    /// Rollouts e Waivers. Compliance score e risk level são derivados de dados persistidos.
    /// Métricas cross-module (serviços, incidentes, mudanças) retornam 0 — não disponíveis neste módulo.
    /// </summary>
    public sealed class Handler(
        IGovernancePackRepository packRepository,
        IGovernanceWaiverRepository waiverRepository,
        IGovernanceRolloutRecordRepository rolloutRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var packs = await packRepository.ListAsync(category: null, status: null, ct: cancellationToken);
            var waivers = await waiverRepository.ListAsync(packId: null, status: null, ct: cancellationToken);
            var rollouts = await rolloutRepository.ListAsync(
                packId: null, scopeType: null, scopeValue: null, status: null, ct: cancellationToken);

            // Filtros opcionais por scope
            if (!string.IsNullOrWhiteSpace(request.TeamId))
            {
                waivers = waivers.Where(w => w.ScopeType == GovernanceScopeType.Team && w.Scope == request.TeamId).ToList();
                rollouts = rollouts.Where(r => r.ScopeType == GovernanceScopeType.Team && r.Scope == request.TeamId).ToList();
            }
            if (!string.IsNullOrWhiteSpace(request.DomainId))
            {
                waivers = waivers.Where(w => w.ScopeType == GovernanceScopeType.Domain && w.Scope == request.DomainId).ToList();
                rollouts = rollouts.Where(r => r.ScopeType == GovernanceScopeType.Domain && r.Scope == request.DomainId).ToList();
            }

            var totalPacks = packs.Count;
            var pendingWaivers = waivers.Count(w => w.Status == WaiverStatus.Pending);
            var approvedWaivers = waivers.Count(w => w.Status == WaiverStatus.Approved);
            var failedRollouts = rollouts.Count(r => r.Status == RolloutStatus.Failed);
            var completedRollouts = rollouts.Count(r => r.Status == RolloutStatus.Completed);
            var pendingRollouts = rollouts.Count(r => r.Status == RolloutStatus.Pending);

            // Compliance score real: % de packs sem waivers pendentes
            var nonCompliantPacks = packs.Count(p =>
            {
                var pw = waivers.Where(w => w.PackId == p.Id && w.Status == WaiverStatus.Pending).ToList();
                return pw.Count > 0;
            });
            var complianceScore = totalPacks == 0
                ? 0m
                : Math.Round(((decimal)(totalPacks - nonCompliantPacks) / totalPacks) * 100m, 1);

            // Risk level real: derivado do estado dos rollouts e waivers
            var overallRisk = failedRollouts > 0
                ? RiskLevel.Critical
                : pendingWaivers > 2
                    ? RiskLevel.High
                    : pendingWaivers > 0 || pendingRollouts > 0
                        ? RiskLevel.Medium
                        : RiskLevel.Low;

            // Maturidade geral: derivada da taxa de rollouts completados
            var rolloutCompletionRate = rollouts.Count == 0 ? 0m : (decimal)completedRollouts / rollouts.Count;
            var overallMaturity = rolloutCompletionRate >= 0.9m
                ? MaturityLevel.Optimizing
                : rolloutCompletionRate >= 0.7m
                    ? MaturityLevel.Managed
                    : rolloutCompletionRate >= 0.5m
                        ? MaturityLevel.Defined
                        : rolloutCompletionRate >= 0.2m
                            ? MaturityLevel.Developing
                            : MaturityLevel.Initial;

            // Tendência de confiança: baseada no estado dos rollouts recentes
            var changeTrend = failedRollouts == 0 && completedRollouts > 0
                ? TrendDirection.Improving
                : failedRollouts > 0
                    ? TrendDirection.Declining
                    : TrendDirection.Stable;

            // TotalPacks (como proxy de "governança entities") — métricas cross-module retornam 0
            var response = new Response(
                TotalPacks: totalPacks,
                PublishedPacks: packs.Count(p => p.Status == GovernancePackStatus.Published),
                PacksWithRollout: rollouts.Select(r => r.PackId).Distinct().Count(),
                PacksWithCompletedRollout: rollouts.Where(r => r.Status == RolloutStatus.Completed).Select(r => r.PackId).Distinct().Count(),
                TotalWaivers: waivers.Count,
                PendingWaivers: pendingWaivers,
                ApprovedWaivers: approvedWaivers,
                TotalRollouts: rollouts.Count,
                CompletedRollouts: completedRollouts,
                FailedRollouts: failedRollouts,
                PendingRollouts: pendingRollouts,
                OverallRiskLevel: overallRisk,
                OverallMaturity: overallMaturity,
                ChangeConfidenceTrend: changeTrend,
                ReliabilityTrend: TrendDirection.Stable,
                ComplianceScore: complianceScore,
                GeneratedAt: DateTimeOffset.UtcNow);

            return Result<Response>.Success(response);
        }
    }

    /// <summary>
    /// Resposta do resumo de relatórios baseado em dados reais de Governance Packs, Rollouts e Waivers.
    /// Métricas de serviços, incidentes e mudanças são cross-module e não estão disponíveis neste módulo (retornam 0).
    /// </summary>
    public sealed record Response(
        int TotalPacks,
        int PublishedPacks,
        int PacksWithRollout,
        int PacksWithCompletedRollout,
        int TotalWaivers,
        int PendingWaivers,
        int ApprovedWaivers,
        int TotalRollouts,
        int CompletedRollouts,
        int FailedRollouts,
        int PendingRollouts,
        RiskLevel OverallRiskLevel,
        MaturityLevel OverallMaturity,
        TrendDirection ChangeConfidenceTrend,
        TrendDirection ReliabilityTrend,
        decimal ComplianceScore,
        DateTimeOffset GeneratedAt);
}
