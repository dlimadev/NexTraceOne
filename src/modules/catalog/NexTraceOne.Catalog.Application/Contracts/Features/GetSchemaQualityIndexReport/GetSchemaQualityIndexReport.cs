using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetSchemaQualityIndexReport;

/// <summary>
/// Feature: GetSchemaQualityIndexReport — índice de qualidade de schema por contrato.
/// Wave AQ.2 — Schema Quality Index.
/// </summary>
public static class GetSchemaQualityIndexReport
{
    // ── Query ──────────────────────────────────────────────────────────────
    public sealed record Query(
        string TenantId,
        int LookbackDays = 30,
        int MinDescWords = 5,
        int TopWorstCount = 10) : IQuery<Report>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.LookbackDays).InclusiveBetween(1, 365);
            RuleFor(x => x.TopWorstCount).InclusiveBetween(1, 100);
        }
    }

    // ── Enums ──────────────────────────────────────────────────────────────
    public enum SchemaQualityTier { Excellent, Good, Fair, Poor }

    // ── Value objects ──────────────────────────────────────────────────────
    public sealed record ContractQualityRow(
        string ContractId,
        string ContractName,
        string Protocol,
        string ServiceTier,
        double DescriptionCoverage,
        double ExampleCoverage,
        double ErrorCodeCoverage,
        double FieldConstraintCoverage,
        double EnumCoverage,
        double SchemaQualityScore,
        SchemaQualityTier Tier,
        IReadOnlyList<string> QualityImprovementHints);

    public sealed record ProtocolQualityRow(string Protocol, double AvgSchemaQualityScore, int ContractCount);

    public sealed record Report(
        DateTimeOffset GeneratedAt,
        string TenantId,
        int LookbackDays,
        double TenantSchemaHealthScore,
        IReadOnlyList<ContractQualityRow> WorstQualityContracts,
        IReadOnlyList<ProtocolQualityRow> QualityByProtocol,
        double? QualityTrendDelta,
        IReadOnlyList<ContractQualityRow> AllContracts);

    // ── Handler ────────────────────────────────────────────────────────────
    public sealed class Handler(
        ISchemaQualityReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var now = clock.UtcNow;
            var entries = await reader.ListByTenantAsync(request.TenantId, cancellationToken);
            var snapshots = await reader.GetMonthlySnapshotsAsync(request.TenantId, 2, cancellationToken);

            var rows = entries.Select(BuildRow).ToList();

            double tenantHealthScore = ComputeTenantHealthScore(rows);

            var worst = rows.OrderBy(r => r.SchemaQualityScore).Take(request.TopWorstCount).ToList();

            var byProtocol = rows.GroupBy(r => r.Protocol)
                .Select(g => new ProtocolQualityRow(g.Key,
                    Math.Round(g.Average(r => r.SchemaQualityScore), 2), g.Count()))
                .ToList();

            double? trendDelta = null;
            if (snapshots.Count > 0)
            {
                var lastSnapshot = snapshots.OrderByDescending(s => s.SnapshotDate).First();
                trendDelta = Math.Round(tenantHealthScore - lastSnapshot.TenantSchemaHealthScore, 2);
            }

            var report = new Report(now, request.TenantId, request.LookbackDays,
                tenantHealthScore, worst, byProtocol, trendDelta, rows);

            return Result<Report>.Success(report);
        }

        private static ContractQualityRow BuildRow(ISchemaQualityReader.ContractSchemaEntry e)
        {
            double descCov = Math.Round((double)e.FieldsWithDescription / Math.Max(e.TotalFields, 1) * 100.0, 2);
            double exCov = Math.Round((double)e.FieldsWithExamples / Math.Max(e.TotalOperations, 1) * 100.0, 2);
            double errCov = Math.Round((double)e.OperationsWithErrorCodes / Math.Max(e.TotalOperations, 1) * 100.0, 2);
            double constraintCov = Math.Round((double)e.FieldsWithConstraints / Math.Max(e.TotalFields, 1) * 100.0, 2);
            double enumCov = e.TotalEnumFields == 0 ? 100.0
                : Math.Round((double)e.EnumFieldsWith3PlusValues / e.TotalEnumFields * 100.0, 2);

            double score = Math.Round(descCov * 0.25 + exCov * 0.25 + errCov * 0.20 + constraintCov * 0.15 + enumCov * 0.15, 2);

            var tier = score >= 85 ? SchemaQualityTier.Excellent
                : score >= 65 ? SchemaQualityTier.Good
                : score >= 40 ? SchemaQualityTier.Fair
                : SchemaQualityTier.Poor;

            var hints = BuildHints(tier, descCov, exCov, errCov, constraintCov, enumCov);

            return new ContractQualityRow(e.ContractId, e.ContractName, e.Protocol, e.ServiceTier,
                descCov, exCov, errCov, constraintCov, enumCov, score, tier, hints);
        }

        private static IReadOnlyList<string> BuildHints(
            SchemaQualityTier tier, double descCov, double exCov, double errCov,
            double constraintCov, double enumCov)
        {
            if (tier != SchemaQualityTier.Poor && tier != SchemaQualityTier.Fair)
                return [];

            var hints = new List<string>();
            if (descCov < 50) hints.Add("Improve field descriptions coverage");
            if (exCov < 50) hints.Add("Add examples to operations");
            if (errCov < 50) hints.Add("Document error codes for operations");
            if (constraintCov < 50) hints.Add("Add field constraints (min/max/pattern)");
            if (enumCov < 50) hints.Add("Expand enum fields with 3+ values");
            return hints;
        }

        private static double ComputeTenantHealthScore(IReadOnlyList<ContractQualityRow> rows)
        {
            if (rows.Count == 0) return 0.0;

            double weightedSum = 0.0;
            double totalWeight = 0.0;

            foreach (var row in rows)
            {
                double weight = row.ServiceTier switch
                {
                    "Critical" => 3.0,
                    "Standard" => 2.0,
                    _ => 1.0  // Internal or others
                };
                weightedSum += row.SchemaQualityScore * weight;
                totalWeight += weight;
            }

            return totalWeight == 0 ? 0.0 : Math.Round(weightedSum / totalWeight, 2);
        }
    }
}
