using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetCodeQualityReport;

/// <summary>
/// Feature: GetCodeQualityReport — relatório de qualidade de código por tenant.
///
/// Agrega os últimos resultados de análise (SonarQube ou compatível) por serviço
/// e calcula distribuição de quality gates, cobertura média e top offenders por
/// dívida técnica (bugs + vulnerabilidades + code smells).
///
/// Wave AQ.2 — Code Quality &amp; Static Analysis Intelligence.
/// </summary>
public static class GetCodeQualityReport
{
    // ── Query ──────────────────────────────────────────────────────────────
    public sealed record Query(string TenantId) : IQuery<Report>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(200);
        }
    }

    // ── Value objects ──────────────────────────────────────────────────────
    public sealed record ServiceQualityRow(
        string ServiceId,
        string ServiceName,
        string ProjectKey,
        string QualityGateStatus,
        bool QualityGatePassed,
        double Coverage,
        int Bugs,
        int Vulnerabilities,
        int CodeSmells,
        double DuplicatedLinesDensity,
        int TechDebtScore,
        string? Branch,
        DateTimeOffset AnalyzedAt);

    public sealed record Report(
        string TenantId,
        int TotalServices,
        int QualityGatePassedCount,
        int QualityGateFailedCount,
        double QualityGatePassRate,
        double AverageCoverage,
        int TotalBugs,
        int TotalVulnerabilities,
        int TotalCodeSmells,
        IReadOnlyList<ServiceQualityRow> ByService,
        IReadOnlyList<ServiceQualityRow> TopOffenders,
        DateTimeOffset GeneratedAt);

    // ── Handler ────────────────────────────────────────────────────────────
    internal sealed class Handler(
        ICodeQualityReader reader,
        IDateTimeProvider clock) : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(request.TenantId);

            var entries = await reader.ListLatestByTenantAsync(request.TenantId, cancellationToken);

            var rows = entries.Select(e => new ServiceQualityRow(
                e.ServiceId, e.ServiceName, e.ProjectKey, e.QualityGateStatus,
                e.QualityGatePassed, e.Coverage, e.Bugs, e.Vulnerabilities,
                e.CodeSmells, e.DuplicatedLinesDensity,
                TechDebtScore: e.Bugs * 3 + e.Vulnerabilities * 5 + e.CodeSmells,
                e.Branch, e.AnalyzedAt))
                .OrderByDescending(r => r.TechDebtScore)
                .ToList();

            var passed = rows.Count(r => r.QualityGatePassed);
            var failed = rows.Count - passed;
            var passRate = rows.Count == 0 ? 100.0 : Math.Round((double)passed / rows.Count * 100, 2);
            var avgCoverage = rows.Count == 0 ? 0.0 : Math.Round(rows.Average(r => r.Coverage), 2);

            return Result<Report>.Success(new Report(
                TenantId: request.TenantId,
                TotalServices: rows.Count,
                QualityGatePassedCount: passed,
                QualityGateFailedCount: failed,
                QualityGatePassRate: passRate,
                AverageCoverage: avgCoverage,
                TotalBugs: rows.Sum(r => r.Bugs),
                TotalVulnerabilities: rows.Sum(r => r.Vulnerabilities),
                TotalCodeSmells: rows.Sum(r => r.CodeSmells),
                ByService: rows,
                TopOffenders: rows.Take(10).ToList(),
                GeneratedAt: clock.UtcNow));
        }
    }
}
