using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetComplianceSummary;

/// <summary>
/// Feature: GetComplianceSummary — resumo de compliance técnico-operacional.
/// Avalia gaps de governança: owner, contrato, documentação, runbook, dependências.
/// </summary>
public static class GetComplianceSummary
{
    /// <summary>Query de resumo de compliance. Permite filtragem por equipa ou domínio.</summary>
    public sealed record Query(
        string? TeamId = null,
        string? DomainId = null) : IQuery<Response>;

    /// <summary>
    /// Handler que computa indicadores de compliance enterprise a partir de dados persistidos no módulo Governance.
    /// Nesta etapa, compliance é derivado de adoção (rollouts) e exceções (waivers) por Governance Pack.
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

            var rows = packs
                .Select(p => BuildPackRow(p, waiversByPack, rolloutsByPack))
                .OrderBy(r => r.PackName)
                .ToList();

            var total = rows.Count;
            var compliant = rows.Count(r => r.Status == ComplianceStatus.Compliant);
            var partial = rows.Count(r => r.Status == ComplianceStatus.PartiallyCompliant);
            var non = rows.Count(r => r.Status == ComplianceStatus.NonCompliant);

            // Score real: % de packs sem waivers pendentes.
            // Não tenta inferir "compliance de serviços" sem dados de avaliação.
            var score = total == 0 ? 0m : Math.Round(((decimal)(total - non) / total) * 100m, 1);

            var response = new Response(
                OverallScore: score,
                TotalPacksAssessed: total,
                CompliantCount: compliant,
                PartiallyCompliantCount: partial,
                NonCompliantCount: non,
                TotalRollouts: rollouts.Count,
                PendingRollouts: rollouts.Count(r => r.Status == RolloutStatus.Pending),
                CompletedRollouts: rollouts.Count(r => r.Status == RolloutStatus.Completed),
                FailedRollouts: rollouts.Count(r => r.Status == RolloutStatus.Failed),
                TotalWaivers: waivers.Count,
                PendingWaivers: waivers.Count(w => w.Status == WaiverStatus.Pending),
                ApprovedWaivers: waivers.Count(w => w.Status == WaiverStatus.Approved),
                Packs: rows,
                GeneratedAt: DateTimeOffset.UtcNow);

            return Result<Response>.Success(response);
        }

        private static CompliancePackRowDto BuildPackRow(
            GovernancePack pack,
            IReadOnlyDictionary<GovernancePackId, List<GovernanceWaiver>> waiversByPack,
            IReadOnlyDictionary<GovernancePackId, List<GovernanceRolloutRecord>> rolloutsByPack)
        {
            waiversByPack.TryGetValue(pack.Id, out var packWaivers);
            rolloutsByPack.TryGetValue(pack.Id, out var packRollouts);

            var pendingWaivers = (packWaivers ?? []).Count(w => w.Status == WaiverStatus.Pending);
            var approvedWaivers = (packWaivers ?? []).Count(w => w.Status == WaiverStatus.Approved);

            var status = pendingWaivers > 0
                ? ComplianceStatus.NonCompliant
                : approvedWaivers > 0
                    ? ComplianceStatus.PartiallyCompliant
                    : ComplianceStatus.Compliant;

            return new CompliancePackRowDto(
                PackId: pack.Id.Value.ToString(),
                PackName: pack.DisplayName,
                Category: pack.Category,
                PackStatus: pack.Status,
                Status: status,
                PendingWaivers: pendingWaivers,
                ApprovedWaivers: approvedWaivers,
                RolloutCount: (packRollouts ?? []).Count,
                CompletedRollouts: (packRollouts ?? []).Count(r => r.Status == RolloutStatus.Completed),
                FailedRollouts: (packRollouts ?? []).Count(r => r.Status == RolloutStatus.Failed));
        }
    }

    /// <summary>Resposta do resumo de compliance enterprise.</summary>
    public sealed record Response(
        decimal OverallScore,
        int TotalPacksAssessed,
        int CompliantCount,
        int PartiallyCompliantCount,
        int NonCompliantCount,
        int TotalRollouts,
        int PendingRollouts,
        int CompletedRollouts,
        int FailedRollouts,
        int TotalWaivers,
        int PendingWaivers,
        int ApprovedWaivers,
        IReadOnlyList<CompliancePackRowDto> Packs,
        DateTimeOffset GeneratedAt);

    public sealed record CompliancePackRowDto(
        string PackId,
        string PackName,
        GovernanceRuleCategory Category,
        GovernancePackStatus PackStatus,
        ComplianceStatus Status,
        int PendingWaivers,
        int ApprovedWaivers,
        int RolloutCount,
        int CompletedRollouts,
        int FailedRollouts);
}
