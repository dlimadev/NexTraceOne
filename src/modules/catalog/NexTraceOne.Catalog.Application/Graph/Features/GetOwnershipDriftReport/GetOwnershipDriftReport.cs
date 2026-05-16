using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Application.Graph.ConfigurationKeys;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Catalog.Application.Graph.Features.GetOwnershipDriftReport;

/// <summary>
/// Feature: GetOwnershipDriftReport — relatório de drift de ownership por tenant/equipa/domínio.
/// Lista todos os serviços activos com sinais de drift, ordenados por gravidade.
/// Serve como input para alertas automáticos e revisões periódicas de ownership.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetOwnershipDriftReport
{
    /// <summary>Query para o relatório de drift. Filtros opcionais por equipa, domínio e tier.</summary>
    public sealed record Query(
        string? TeamName = null,
        string? Domain = null,
        string? Tier = null) : IQuery<Response>;

    /// <summary>Valida a query GetOwnershipDriftReport.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TeamName).MaximumLength(200).When(x => x.TeamName is not null);
            RuleFor(x => x.Domain).MaximumLength(200).When(x => x.Domain is not null);
            RuleFor(x => x.Tier).MaximumLength(30).When(x => x.Tier is not null);
        }
    }

    /// <summary>Handler que agrega sinais de drift por serviço para o tenant.</summary>
    public sealed class Handler(
        IServiceAssetRepository serviceAssetRepository,
        IConfigurationResolutionService config,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var thresholdDto = await config.ResolveEffectiveValueAsync(
                ServiceCatalogConfigKeys.OwnershipDriftThresholdDays,
                ConfigurationScope.System, null, cancellationToken);
            var thresholdDays = int.TryParse(thresholdDto?.EffectiveValue, out var td) ? td : 90;

            var tierFilter = request.Tier is not null
                && Enum.TryParse<ServiceTierType>(request.Tier, ignoreCase: true, out var t)
                ? t : (ServiceTierType?)null;

            var (services, _) = await serviceAssetRepository.ListFilteredAsync(
                request.TeamName,
                request.Domain,
                serviceType: null,
                criticality: null,
                lifecycleStatus: null,
                exposureType: null,
                searchTerm: null,
                page: 1,
                pageSize: 10_000,
                cancellationToken);

            var now = clock.UtcNow;
            var findings = new List<ServiceDriftDto>();

            foreach (var service in services)
            {
                if (service.LifecycleStatus == LifecycleStatus.Retired) continue;
                if (tierFilter.HasValue && service.Tier != tierFilter.Value) continue;

                var driftSignals = new List<string>();
                var maxSeverity = "info";

                // Missing technical owner
                if (string.IsNullOrWhiteSpace(service.TechnicalOwner))
                {
                    driftSignals.Add("NoTechnicalOwner");
                    maxSeverity = "critical";
                }

                // Missing team
                if (string.IsNullOrWhiteSpace(service.TeamName))
                {
                    driftSignals.Add("NoTeam");
                    maxSeverity = "critical";
                }

                // Missing on-call for non-experimental
                if (service.Tier is not ServiceTierType.Experimental
                    && string.IsNullOrWhiteSpace(service.OnCallRotationId))
                {
                    driftSignals.Add("NoOnCallRotation");
                    if (maxSeverity != "critical")
                        maxSeverity = service.Tier == ServiceTierType.Critical ? "critical" : "high";
                }

                // Stale ownership review
                int? daysSinceReview = service.LastOwnershipReviewAt.HasValue
                    ? (int)(now - service.LastOwnershipReviewAt.Value).TotalDays
                    : null;
                if (daysSinceReview is null || daysSinceReview > thresholdDays)
                {
                    driftSignals.Add(daysSinceReview is null
                        ? "OwnershipNeverReviewed"
                        : "OwnershipReviewStale");
                    if (maxSeverity is "info")
                        maxSeverity = "high";
                }

                // Missing contact channel
                if (string.IsNullOrWhiteSpace(service.ContactChannel))
                {
                    driftSignals.Add("NoContactChannel");
                    if (maxSeverity is "info") maxSeverity = "medium";
                }

                if (driftSignals.Count > 0)
                {
                    findings.Add(new(
                        ServiceId: service.Id.Value,
                        ServiceName: service.Name,
                        DisplayName: service.DisplayName,
                        TeamName: service.TeamName,
                        Domain: service.Domain,
                        Tier: service.Tier.ToString(),
                        Signals: driftSignals,
                        MaxSeverity: maxSeverity,
                        DaysSinceOwnershipReview: daysSinceReview));
                }
            }

            var ordered = findings
                .OrderByDescending(f => SeverityRank(f.MaxSeverity))
                .ThenBy(f => f.DaysSinceOwnershipReview ?? int.MaxValue)
                .ToList();

            return new Response(
                Summary: new DriftSummaryDto(
                    TotalServicesEvaluated: services.Count(s => s.LifecycleStatus != LifecycleStatus.Retired),
                    ServicesWithDrift: ordered.Count,
                    CriticalDrift: ordered.Count(f => f.MaxSeverity == "critical"),
                    HighDrift: ordered.Count(f => f.MaxSeverity == "high"),
                    MediumDrift: ordered.Count(f => f.MaxSeverity == "medium"),
                    ThresholdDays: thresholdDays),
                Findings: ordered,
                GeneratedAt: now);
        }

        private static int SeverityRank(string s) => s switch
        {
            "critical" => 3, "high" => 2, "medium" => 1, _ => 0
        };
    }

    /// <summary>Resposta do relatório de drift de ownership.</summary>
    public sealed record Response(
        DriftSummaryDto Summary,
        IReadOnlyList<ServiceDriftDto> Findings,
        DateTimeOffset GeneratedAt);

    /// <summary>Sumário agregado do relatório de drift.</summary>
    public sealed record DriftSummaryDto(
        int TotalServicesEvaluated,
        int ServicesWithDrift,
        int CriticalDrift,
        int HighDrift,
        int MediumDrift,
        int ThresholdDays);

    /// <summary>Entrada de drift para um serviço individual.</summary>
    public sealed record ServiceDriftDto(
        Guid ServiceId,
        string ServiceName,
        string DisplayName,
        string TeamName,
        string Domain,
        string Tier,
        IReadOnlyList<string> Signals,
        string MaxSeverity,
        int? DaysSinceOwnershipReview);
}
