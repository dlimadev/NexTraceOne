using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Application.Graph.ConfigurationKeys;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Domain.Graph.Errors;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Catalog.Application.Graph.Features.DetectOwnershipDrift;

/// <summary>
/// Feature: DetectOwnershipDrift — detecta sinais de drift de ownership num serviço individual.
/// Drift é definido como a degradação da qualidade/freshness dos dados de ownership:
/// ownership desactualizado (acima do threshold de dias), campos em falta, ou on-call ausente
/// em serviços críticos.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class DetectOwnershipDrift
{
    /// <summary>Query para detectar drift de ownership num serviço.</summary>
    public sealed record Query(Guid ServiceId) : IQuery<Response>;

    /// <summary>Valida a query DetectOwnershipDrift.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty();
        }
    }

    /// <summary>Handler que analisa os sinais de drift de ownership do serviço.</summary>
    public sealed class Handler(
        IServiceAssetRepository serviceAssetRepository,
        IConfigurationResolutionService config,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var service = await serviceAssetRepository.GetByIdAsync(
                ServiceAssetId.From(request.ServiceId), cancellationToken);
            if (service is null)
                return CatalogGraphErrors.ServiceAssetNotFoundById(request.ServiceId);

            var thresholdDto = await config.ResolveEffectiveValueAsync(
                ServiceCatalogConfigKeys.OwnershipDriftThresholdDays,
                ConfigurationScope.System, null, cancellationToken);
            var thresholdDays = int.TryParse(thresholdDto?.EffectiveValue, out var td) ? td : 90;

            var now = clock.UtcNow;
            var signals = ComputeSignals(service, now, thresholdDays);
            var driftScore = signals.Count > 0
                ? Math.Round(signals.Average(s => s.Severity == "critical" ? 1m : 0.5m), 2)
                : 0m;

            return new Response(
                ServiceId: service.Id.Value,
                ServiceName: service.Name,
                Tier: service.Tier.ToString(),
                HasDrift: signals.Count > 0,
                DriftScore: driftScore,
                DaysSinceOwnershipReview: service.LastOwnershipReviewAt.HasValue
                    ? (int)(now - service.LastOwnershipReviewAt.Value).TotalDays
                    : null,
                ThresholdDays: thresholdDays,
                Signals: signals,
                EvaluatedAt: now);
        }

        private static List<DriftSignalDto> ComputeSignals(
            ServiceAsset service,
            DateTimeOffset now,
            int thresholdDays)
        {
            var signals = new List<DriftSignalDto>();

            // Sinal 1: ownership não revisto dentro do threshold
            if (service.LastOwnershipReviewAt.HasValue)
            {
                var days = (int)(now - service.LastOwnershipReviewAt.Value).TotalDays;
                if (days > thresholdDays)
                    signals.Add(new("OwnershipReviewStale",
                        $"Ownership not reviewed for {days} days (threshold: {thresholdDays})",
                        service.Tier == ServiceTierType.Critical ? "critical" : "high"));
            }
            else
            {
                signals.Add(new("OwnershipNeverReviewed",
                    "Ownership has never been explicitly reviewed",
                    "high"));
            }

            // Sinal 2: owner técnico em falta
            if (string.IsNullOrWhiteSpace(service.TechnicalOwner))
                signals.Add(new("NoTechnicalOwner", "Technical owner not set", "critical"));

            // Sinal 3: equipa em falta
            if (string.IsNullOrWhiteSpace(service.TeamName))
                signals.Add(new("NoTeam", "Team not assigned", "critical"));

            // Sinal 4: on-call obrigatório para Critical/Standard
            if (service.Tier is not ServiceTierType.Experimental
                && string.IsNullOrWhiteSpace(service.OnCallRotationId))
            {
                var sev = service.Tier == ServiceTierType.Critical ? "critical" : "high";
                signals.Add(new("NoOnCallRotation",
                    $"On-call rotation not configured (required for {service.Tier} tier)", sev));
            }

            // Sinal 5: canal de contacto em falta
            if (string.IsNullOrWhiteSpace(service.ContactChannel))
                signals.Add(new("NoContactChannel", "Contact channel not set", "medium"));

            // Sinal 6: business owner em falta em Critical
            if (service.Tier == ServiceTierType.Critical
                && string.IsNullOrWhiteSpace(service.BusinessOwner))
            {
                signals.Add(new("NoBusinessOwner",
                    "Business owner not set (required for Critical tier)", "high"));
            }

            return signals;
        }
    }

    /// <summary>Resposta da detecção de drift de ownership.</summary>
    public sealed record Response(
        Guid ServiceId,
        string ServiceName,
        string Tier,
        bool HasDrift,
        decimal DriftScore,
        int? DaysSinceOwnershipReview,
        int ThresholdDays,
        IReadOnlyList<DriftSignalDto> Signals,
        DateTimeOffset EvaluatedAt);

    /// <summary>Sinal individual de drift detectado.</summary>
    public sealed record DriftSignalDto(string Code, string Description, string Severity);
}
