using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;
using NexTraceOne.Governance.Domain.Enums;

namespace NexTraceOne.Governance.Application.Features.GetComplianceGaps;

/// <summary>
/// Feature: GetComplianceGaps — gaps de compliance agrupados por dimensão.
/// Permite filtragem por equipa, domínio ou serviço.
/// </summary>
public static class GetComplianceGaps
{
    /// <summary>Query para gaps de compliance.</summary>
    public sealed record Query(
        string? TeamId = null,
        string? DomainId = null,
        string? ServiceId = null) : IQuery<Response>;

    /// <summary>Handler que retorna gaps de compliance agrupados.</summary>
    public sealed class Handler(
        IGovernancePackRepository packRepository,
        IGovernanceWaiverRepository waiverRepository,
        IGovernanceRolloutRecordRepository rolloutRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            GovernanceScopeType? scopeType = null;
            string? scopeValue = null;

            if (!string.IsNullOrWhiteSpace(request.TeamId))
            {
                scopeType = GovernanceScopeType.Team;
                scopeValue = request.TeamId;
            }
            else if (!string.IsNullOrWhiteSpace(request.DomainId))
            {
                scopeType = GovernanceScopeType.Domain;
                scopeValue = request.DomainId;
            }
            else if (!string.IsNullOrWhiteSpace(request.ServiceId))
            {
                scopeType = null;
                scopeValue = request.ServiceId;
            }

            var packs = await packRepository.ListAsync(category: null, status: GovernancePackStatus.Published, cancellationToken);
            var waivers = await waiverRepository.ListAsync(packId: null, status: WaiverStatus.Pending, cancellationToken);

            var rollouts = await rolloutRepository.ListAsync(
                packId: null,
                scopeType: scopeType,
                scopeValue: scopeValue,
                status: null,
                ct: cancellationToken);

            if (!string.IsNullOrWhiteSpace(request.ServiceId))
            {
                rollouts = rollouts
                    .Where(r => string.Equals(r.Scope, request.ServiceId, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var rolloutByPack = rollouts
                .GroupBy(r => r.PackId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.InitiatedAt).First());

            var gaps = packs
                .Select(pack => BuildComplianceGapFromPack(pack, rolloutByPack, waivers))
                .Where(g => g is not null)
                .Select(g => g!)
                .OrderByDescending(g => g.Severity)
                .ThenByDescending(g => g.DetectedAt)
                .ToList();

            var response = new Response(
                TotalGaps: gaps.Count,
                CriticalCount: gaps.Count(g => g.Severity == PolicySeverity.Critical),
                HighCount: gaps.Count(g => g.Severity == PolicySeverity.High),
                MediumCount: gaps.Count(g => g.Severity == PolicySeverity.Medium),
                LowCount: gaps.Count(g => g.Severity == PolicySeverity.Low),
                Gaps: gaps,
                GeneratedAt: DateTimeOffset.UtcNow);

            return Result<Response>.Success(response);
        }

        private static ComplianceGapDto? BuildComplianceGapFromPack(
            GovernancePack pack,
            IReadOnlyDictionary<GovernancePackId, GovernanceRolloutRecord> rolloutByPack,
            IReadOnlyList<GovernanceWaiver> waivers)
        {
            var hasPendingWaiver = waivers.Any(w => w.PackId == pack.Id && w.Status == WaiverStatus.Pending);
            var latestRollout = rolloutByPack.GetValueOrDefault(pack.Id);
            var missingRollout = latestRollout is null;
            var rolloutFailed = latestRollout?.Status == RolloutStatus.Failed;

            if (!hasPendingWaiver && !missingRollout && !rolloutFailed)
                return null;

            var violatedPolicies = new List<string>();
            if (hasPendingWaiver)
                violatedPolicies.Add("POL-WAIVER-PENDING");
            if (missingRollout)
                violatedPolicies.Add("POL-ROLLOUT-MISSING");
            if (rolloutFailed)
                violatedPolicies.Add("POL-ROLLOUT-FAILED");

            var severity = (rolloutFailed, missingRollout, hasPendingWaiver) switch
            {
                (true, _, _) => PolicySeverity.Critical,
                (_, true, true) => PolicySeverity.High,
                (_, true, false) => PolicySeverity.Medium,
                _ => PolicySeverity.Low
            };

            var scope = latestRollout?.Scope ?? "unassigned";
            var scopeType = latestRollout?.ScopeType.ToString() ?? "Global";
            var detectedAt = latestRollout?.InitiatedAt ?? pack.UpdatedAt;

            return new ComplianceGapDto(
                GapId: $"gap-{pack.Id.Value:N}",
                ServiceId: $"pack-{pack.Id.Value:N}",
                ServiceName: pack.DisplayName,
                Team: scope,
                Domain: scopeType,
                Description: BuildDescription(hasPendingWaiver, missingRollout, rolloutFailed),
                Severity: severity,
                ViolatedPolicyIds: violatedPolicies.ToArray(),
                ViolationCount: violatedPolicies.Count,
                DetectedAt: detectedAt);
        }

        private static string BuildDescription(bool hasPendingWaiver, bool missingRollout, bool rolloutFailed)
        {
            var parts = new List<string>(3);
            if (hasPendingWaiver) parts.Add("Pending waiver");
            if (missingRollout) parts.Add("No rollout evidence");
            if (rolloutFailed) parts.Add("Latest rollout failed");
            return string.Join(", ", parts);
        }
    }

    /// <summary>Resposta com gaps de compliance.</summary>
    public sealed record Response(
        int TotalGaps,
        int CriticalCount,
        int HighCount,
        int MediumCount,
        int LowCount,
        IReadOnlyList<ComplianceGapDto> Gaps,
        DateTimeOffset GeneratedAt);

    /// <summary>DTO de gap de compliance.</summary>
    public sealed record ComplianceGapDto(
        string GapId,
        string ServiceId,
        string ServiceName,
        string Team,
        string Domain,
        string Description,
        PolicySeverity Severity,
        string[] ViolatedPolicyIds,
        int ViolationCount,
        DateTimeOffset DetectedAt);
}
