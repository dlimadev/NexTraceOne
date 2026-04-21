using Ardalis.GuardClauses;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;

namespace NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetRiskCenterReport;

/// <summary>
/// Feature: GetRiskCenterReport — relatório de serviços ranqueados por risco para o Risk Center.
/// Optimizado para persona Platform Admin / CTO / Executive.
/// Lista serviços com maior risco primeiro, com breakdown dimensional.
/// Wave F.2 — Risk Center.
/// </summary>
public static class GetRiskCenterReport
{
    public sealed record Query(
        string TenantId,
        int MaxResults = 50,
        RiskLevel? MinimumRiskLevel = null) : IQuery<Response>;

    public sealed record ServiceRiskSummaryDto(
        Guid ServiceAssetId,
        string ServiceName,
        RiskLevel OverallRiskLevel,
        int OverallScore,
        int VulnerabilityScore,
        int ChangeFailureScore,
        int BlastRadiusScore,
        int PolicyViolationScore,
        int ActiveSignalCount,
        DateTimeOffset ComputedAt);

    public sealed record RiskDistributionDto(
        int CriticalCount,
        int HighCount,
        int MediumCount,
        int LowCount,
        int NegligibleCount,
        int TotalServices);

    public sealed record Response(
        string TenantId,
        IReadOnlyList<ServiceRiskSummaryDto> Services,
        RiskDistributionDto Distribution,
        int TotalReturned,
        int TotalWithProfiles);

    public sealed class Handler(
        IServiceRiskProfileRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);
            Guard.Against.OutOfRange(request.MaxResults, nameof(request.MaxResults), 1, 200);

            var profiles = await repository.ListByTenantRankedAsync(
                request.TenantId,
                request.MaxResults,
                cancellationToken);

            var filtered = request.MinimumRiskLevel.HasValue
                ? profiles.Where(p => p.OverallRiskLevel >= request.MinimumRiskLevel.Value).ToList()
                : profiles.ToList();

            var dtos = filtered.Select(p => new ServiceRiskSummaryDto(
                p.ServiceAssetId,
                p.ServiceName,
                p.OverallRiskLevel,
                p.OverallScore,
                p.VulnerabilityScore,
                p.ChangeFailureScore,
                p.BlastRadiusScore,
                p.PolicyViolationScore,
                p.ActiveSignalCount,
                p.ComputedAt)).ToList();

            var distribution = new RiskDistributionDto(
                CriticalCount: profiles.Count(p => p.OverallRiskLevel == RiskLevel.Critical),
                HighCount: profiles.Count(p => p.OverallRiskLevel == RiskLevel.High),
                MediumCount: profiles.Count(p => p.OverallRiskLevel == RiskLevel.Medium),
                LowCount: profiles.Count(p => p.OverallRiskLevel == RiskLevel.Low),
                NegligibleCount: profiles.Count(p => p.OverallRiskLevel == RiskLevel.Negligible),
                TotalServices: profiles.Count);

            return Result<Response>.Success(new Response(
                request.TenantId,
                dtos,
                distribution,
                dtos.Count,
                profiles.Count));
        }
    }
}
