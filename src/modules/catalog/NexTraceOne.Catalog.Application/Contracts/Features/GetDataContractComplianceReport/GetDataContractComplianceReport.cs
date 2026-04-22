using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetDataContractComplianceReport;

/// <summary>
/// Feature: GetDataContractComplianceReport — relatório de compliance de data contracts.
/// Wave AQ.1 — DataContractRecord.
/// </summary>
public static class GetDataContractComplianceReport
{
    // ── Query ──────────────────────────────────────────────────────────────
    public sealed record Query(
        string TenantId,
        int LookbackDays = 30,
        int StaleDays = 90,
        double MinFieldCompletenessPct = 80.0) : IQuery<Report>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.LookbackDays).InclusiveBetween(1, 365);
            RuleFor(x => x.StaleDays).InclusiveBetween(1, 730);
            RuleFor(x => x.MinFieldCompletenessPct).InclusiveBetween(0.0, 100.0);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────
    public enum DataContractTier { Governed, Partial, Unmanaged }

    // ── Value objects ──────────────────────────────────────────────────────
    public sealed record ContractDetail(
        Guid Id,
        string ServiceId,
        string DatasetName,
        string ContractVersion,
        string? OwnerTeamId,
        bool HasFreshnessRequirement,
        double FieldCompletenessScore,
        double ContractAgeDays,
        DataContractTier Tier);

    public sealed record TeamGovernanceScore(
        string TeamId,
        int TotalContracts,
        int GovernedContracts,
        double GovernancePct);

    public sealed record Report(
        DateTimeOffset GeneratedAt,
        string TenantId,
        int LookbackDays,
        int DataContractCount,
        double DataContractCoveragePct,
        double GovernedPct,
        double PartialPct,
        double UnmanagedPct,
        int StaleContractCount,
        int FieldlessContractCount,
        IReadOnlyList<ContractDetail> ContractDetails,
        IReadOnlyList<TeamGovernanceScore> TeamGovernanceScores);

    // ── Handler ────────────────────────────────────────────────────────────
    public sealed class Handler(
        IDataContractRepository repository,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;
            var records = await repository.ListByTenantAsync(request.TenantId, cancellationToken);

            var details = records.Select(r => BuildDetail(r, now, request.StaleDays, request.MinFieldCompletenessPct)).ToList();

            int total = details.Count;
            int governed = details.Count(d => d.Tier == DataContractTier.Governed);
            int partial = details.Count(d => d.Tier == DataContractTier.Partial);
            int unmanaged = details.Count(d => d.Tier == DataContractTier.Unmanaged);

            double governedPct = total == 0 ? 0 : Math.Round((double)governed / total * 100.0, 2);
            double partialPct = total == 0 ? 0 : Math.Round((double)partial / total * 100.0, 2);
            double unmanagedPct = total == 0 ? 0 : Math.Round((double)unmanaged / total * 100.0, 2);

            int staleCount = details.Count(d => d.ContractAgeDays > request.StaleDays);
            int fieldlessCount = records.Count(r => string.IsNullOrWhiteSpace(r.FieldDefinitionsJson));

            var distinctServices = records.Select(r => r.ServiceId).Distinct().Count();
            double coveragePct = distinctServices == 0 ? 100.0 : Math.Round((double)distinctServices / distinctServices * 100.0, 2);

            var teamScores = details
                .Where(d => d.OwnerTeamId != null)
                .GroupBy(d => d.OwnerTeamId!)
                .Select(g =>
                {
                    int teamTotal = g.Count();
                    int teamGoverned = g.Count(d => d.Tier == DataContractTier.Governed);
                    return new TeamGovernanceScore(g.Key, teamTotal, teamGoverned,
                        teamTotal == 0 ? 0 : Math.Round((double)teamGoverned / teamTotal * 100.0, 2));
                })
                .ToList();

            var report = new Report(
                now, request.TenantId, request.LookbackDays,
                total, coveragePct,
                governedPct, partialPct, unmanagedPct,
                staleCount, fieldlessCount,
                details, teamScores);

            return Result<Report>.Success(report);
        }

        private static ContractDetail BuildDetail(
            DataContractRecord record, DateTimeOffset now, int staleDays, double minFieldCompletenessPct)
        {
            double fieldCompletenessScore = ComputeFieldCompleteness(record.FieldDefinitionsJson);
            double contractAgeDays = (now - record.UpdatedAt).TotalDays;

            bool hasOwner = record.OwnerTeamId != null;
            bool hasSla = record.FreshnessRequirementHours.HasValue;
            bool isFieldComplete = fieldCompletenessScore >= minFieldCompletenessPct;
            bool isNotStale = contractAgeDays <= staleDays;

            int criteriaMetForGoverned = (hasOwner ? 1 : 0) + (hasSla ? 1 : 0)
                                       + (isFieldComplete ? 1 : 0) + (isNotStale ? 1 : 0);

            DataContractTier tier = (!hasOwner || !hasSla) ? DataContractTier.Unmanaged
                : criteriaMetForGoverned >= 3 ? DataContractTier.Governed
                : DataContractTier.Partial;

            return new ContractDetail(
                record.Id, record.ServiceId, record.DatasetName, record.ContractVersion,
                record.OwnerTeamId, hasSla, fieldCompletenessScore, contractAgeDays, tier);
        }

        private static double ComputeFieldCompleteness(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return 0.0;
            // Heuristic: non-empty JSON array with items → 100%
            var trimmed = json.Trim();
            if (trimmed.StartsWith('[') && trimmed.Length > 2) return 100.0;
            return 0.0;
        }
    }
}
