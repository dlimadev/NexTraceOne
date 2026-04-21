using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Application.Graph.Features.GetServiceOwnershipHealthReport;

/// <summary>
/// Feature: GetServiceOwnershipHealthReport — relatório de saúde de ownership dos serviços no catálogo.
///
/// Avalia a cobertura de ownership por serviço e identifica problemas de governança como:
/// - serviços sem equipa atribuída (TeamName ausente ou "unassigned")
/// - serviços sem technical owner
/// - serviços sem business owner
/// - serviços com revisão de ownership em atraso (LastOwnershipReviewAt há mais de N dias)
/// - serviços sem documentação URL
/// - score de saúde de ownership (0–100) por serviço e score global do catálogo
///
/// Orientado para Tech Lead, Architect e Platform Admin personas.
/// Permite identificar gaps de ownership que comprometem o Source of Truth do NexTraceOne.
///
/// Wave L.1 — Service Ownership Health Report (Catalog Graph).
/// </summary>
public static class GetServiceOwnershipHealthReport
{
    /// <summary>
    /// <para><c>MaxServices</c>: limita o número de serviços no relatório (1–500, default 100).</para>
    /// <para><c>OwnershipReviewStalenessThresholdDays</c>: dias sem revisão para considerar ownership stale (7–365, default 180).</para>
    /// <para><c>TierFilter</c>: filtra por tier de serviço (opcional).</para>
    /// <para><c>HealthScoreThreshold</c>: inclui apenas serviços com score abaixo deste threshold (0–100, null = todos).</para>
    /// </summary>
    public sealed record Query(
        Guid TenantId,
        int MaxServices = 100,
        int OwnershipReviewStalenessThresholdDays = 180,
        ServiceTierType? TierFilter = null,
        int? HealthScoreThreshold = null) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.MaxServices).InclusiveBetween(1, 500);
            RuleFor(x => x.OwnershipReviewStalenessThresholdDays).InclusiveBetween(7, 365);
            RuleFor(x => x.HealthScoreThreshold).InclusiveBetween(0, 100)
                .When(x => x.HealthScoreThreshold.HasValue);
        }
    }

    public sealed class Handler(
        IServiceAssetRepository serviceRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var allServices = await serviceRepository.ListAllAsync(cancellationToken);

            var services = allServices.ToList();
            if (request.TierFilter.HasValue)
                services = services.Where(s => s.Tier == request.TierFilter.Value).ToList();

            var now = DateTimeOffset.UtcNow;
            var stalenessThreshold = now.AddDays(-request.OwnershipReviewStalenessThresholdDays);

            var serviceHealthItems = services.Select(svc =>
            {
                var issues = new List<OwnershipIssue>();

                var hasMissingTeam = IsBlankOrPlaceholder(svc.TeamName);
                var hasMissingTechOwner = IsBlankOrPlaceholder(svc.TechnicalOwner);
                var hasMissingBizOwner = IsBlankOrPlaceholder(svc.BusinessOwner);
                var hasStaleReview = svc.LastOwnershipReviewAt is null
                    || svc.LastOwnershipReviewAt < stalenessThreshold;
                var hasMissingDoc = string.IsNullOrWhiteSpace(svc.DocumentationUrl);

                if (hasMissingTeam)
                    issues.Add(OwnershipIssue.MissingTeam);
                if (hasMissingTechOwner)
                    issues.Add(OwnershipIssue.MissingTechnicalOwner);
                if (hasMissingBizOwner)
                    issues.Add(OwnershipIssue.MissingBusinessOwner);
                if (hasStaleReview)
                    issues.Add(OwnershipIssue.StaleReview);
                if (hasMissingDoc)
                    issues.Add(OwnershipIssue.MissingDocumentation);

                var score = ComputeHealthScore(hasMissingTeam, hasMissingTechOwner, hasMissingBizOwner, hasStaleReview, hasMissingDoc);

                return new ServiceOwnershipHealth(
                    ServiceId: svc.Id.Value,
                    ServiceName: svc.Name,
                    DisplayName: svc.DisplayName,
                    TeamName: svc.TeamName,
                    TechnicalOwner: svc.TechnicalOwner,
                    BusinessOwner: svc.BusinessOwner,
                    Tier: svc.Tier,
                    Domain: svc.Domain,
                    LastOwnershipReviewAt: svc.LastOwnershipReviewAt,
                    HealthScore: score,
                    Issues: issues);
            })
            .OrderBy(s => s.HealthScore)
            .ToList();

            if (request.HealthScoreThreshold.HasValue)
                serviceHealthItems = serviceHealthItems
                    .Where(s => s.HealthScore <= request.HealthScoreThreshold.Value)
                    .ToList();

            serviceHealthItems = serviceHealthItems.Take(request.MaxServices).ToList();

            var totalAnalyzed = serviceHealthItems.Count;
            var catalogHealthScore = totalAnalyzed > 0
                ? Math.Round(serviceHealthItems.Average(s => (decimal)s.HealthScore), 1)
                : 100m;

            var issueBreakdown = new OwnershipIssueBreakdown(
                MissingTeam: serviceHealthItems.Count(s => s.Issues.Contains(OwnershipIssue.MissingTeam)),
                MissingTechnicalOwner: serviceHealthItems.Count(s => s.Issues.Contains(OwnershipIssue.MissingTechnicalOwner)),
                MissingBusinessOwner: serviceHealthItems.Count(s => s.Issues.Contains(OwnershipIssue.MissingBusinessOwner)),
                StaleReview: serviceHealthItems.Count(s => s.Issues.Contains(OwnershipIssue.StaleReview)),
                MissingDocumentation: serviceHealthItems.Count(s => s.Issues.Contains(OwnershipIssue.MissingDocumentation)));

            var healthBand = ClassifyHealthBand(catalogHealthScore);

            return Result<Response>.Success(new Response(
                GeneratedAt: now,
                TenantId: request.TenantId,
                TierFilter: request.TierFilter,
                OwnershipReviewStalenessThresholdDays: request.OwnershipReviewStalenessThresholdDays,
                TotalServicesAnalyzed: totalAnalyzed,
                CatalogHealthScore: catalogHealthScore,
                HealthBand: healthBand,
                IssueBreakdown: issueBreakdown,
                Services: serviceHealthItems));
        }

        private static int ComputeHealthScore(
            bool missingTeam,
            bool missingTechOwner,
            bool missingBizOwner,
            bool staleReview,
            bool missingDoc)
        {
            var score = 100;
            if (missingTeam) score -= 35;
            if (missingTechOwner) score -= 25;
            if (missingBizOwner) score -= 15;
            if (staleReview) score -= 15;
            if (missingDoc) score -= 10;
            return Math.Max(0, score);
        }

        private static bool IsBlankOrPlaceholder(string? value) =>
            string.IsNullOrWhiteSpace(value)
            || value.Equals("unassigned", StringComparison.OrdinalIgnoreCase)
            || value.Equals("unknown", StringComparison.OrdinalIgnoreCase)
            || value.Equals("n/a", StringComparison.OrdinalIgnoreCase);

        private static OwnershipHealthBand ClassifyHealthBand(decimal score) => score switch
        {
            >= 90 => OwnershipHealthBand.Healthy,
            >= 70 => OwnershipHealthBand.Fair,
            >= 50 => OwnershipHealthBand.AtRisk,
            _     => OwnershipHealthBand.Critical,
        };
    }

    // ── Enums ────────────────────────────────────────────────────────────

    /// <summary>Tipo de problema de ownership detetado num serviço.</summary>
    public enum OwnershipIssue
    {
        MissingTeam = 0,
        MissingTechnicalOwner = 1,
        MissingBusinessOwner = 2,
        StaleReview = 3,
        MissingDocumentation = 4,
    }

    /// <summary>Classificação de saúde global do catálogo de ownership.</summary>
    public enum OwnershipHealthBand
    {
        Healthy  = 0,
        Fair     = 1,
        AtRisk   = 2,
        Critical = 3,
    }

    // ── Response DTOs ────────────────────────────────────────────────────

    public sealed record ServiceOwnershipHealth(
        Guid ServiceId,
        string ServiceName,
        string DisplayName,
        string TeamName,
        string TechnicalOwner,
        string BusinessOwner,
        ServiceTierType Tier,
        string Domain,
        DateTimeOffset? LastOwnershipReviewAt,
        int HealthScore,
        IReadOnlyList<OwnershipIssue> Issues);

    public sealed record OwnershipIssueBreakdown(
        int MissingTeam,
        int MissingTechnicalOwner,
        int MissingBusinessOwner,
        int StaleReview,
        int MissingDocumentation);

    public sealed record Response(
        DateTimeOffset GeneratedAt,
        Guid TenantId,
        ServiceTierType? TierFilter,
        int OwnershipReviewStalenessThresholdDays,
        int TotalServicesAnalyzed,
        decimal CatalogHealthScore,
        OwnershipHealthBand HealthBand,
        OwnershipIssueBreakdown IssueBreakdown,
        IReadOnlyList<ServiceOwnershipHealth> Services);
}
