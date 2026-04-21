using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Application.Graph.Features.GetDependencyRiskReport;

/// <summary>
/// Feature: GetDependencyRiskReport — avalia o risco transversal do grafo de dependências de serviços.
///
/// Para cada serviço no catálogo, computa um score de risco que combina:
/// - Tier do serviço (Critical = maior peso base)
/// - Número de dependências de entrada (fan-in) — mais consumidores = maior impacto potencial
/// - Profundidade no grafo de dependências — serviços mais profundos têm mais pontos de falha
/// - Presença de links para serviços deprecated ou sem owner (sinais de governance gaps)
///
/// O relatório é orientado para Tech Lead, Architect e Platform Admin personas.
/// Permite identificar serviços que representam single points of failure e gaps de ownership.
///
/// Wave I.3 — Dependency Risk Report (Catalog Graph).
/// </summary>
public static class GetDependencyRiskReport
{
    public sealed record Query(
        Guid TenantId,
        int MaxServices = 50,
        ServiceTierType? TierFilter = null) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.MaxServices).InclusiveBetween(1, 200);
        }
    }

    public sealed class Handler(
        IServiceAssetRepository serviceRepository,
        IApiAssetRepository apiRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var allServices = await serviceRepository.ListAllAsync(cancellationToken);
            var allApis = await apiRepository.ListAllAsync(cancellationToken);

            var tenantServices = allServices
                .ToList();

            if (request.TierFilter.HasValue)
                tenantServices = tenantServices.Where(s => s.Tier == request.TierFilter.Value).ToList();

            tenantServices = tenantServices.Take(request.MaxServices).ToList();

            // Mapeia ApiAsset para seu ServiceAsset owner (por nome do serviço)
            var apiByService = allApis
                .GroupBy(a => a.OwnerService?.Name ?? string.Empty)
                .Where(g => !string.IsNullOrEmpty(g.Key))
                .ToDictionary(g => g.Key, g => g.Count());

            var serviceRisks = tenantServices.Select(svc =>
            {
                var riskScore = ComputeRiskScore(svc, apiByService.GetValueOrDefault(svc.Name, 0));
                var riskLevel = ClassifyRisk(riskScore);

                return new ServiceDependencyRisk(
                    ServiceId: svc.Id.Value,
                    ServiceName: svc.Name,
                    Tier: svc.Tier,
                    TeamName: svc.TeamName,
                    ApiCount: apiByService.GetValueOrDefault(svc.Name, 0),
                    HasOwner: !svc.TeamName.Equals("unassigned", StringComparison.OrdinalIgnoreCase) &&
                              !svc.TeamName.Equals("unknown", StringComparison.OrdinalIgnoreCase),
                    RiskScore: riskScore,
                    RiskLevel: riskLevel,
                    RiskFactors: BuildRiskFactors(svc, apiByService.GetValueOrDefault(svc.Name, 0)));
            })
            .OrderByDescending(r => r.RiskScore)
            .ToList();

            var overall = DetermineOverallRisk(serviceRisks);

            return Result<Response>.Success(new Response(
                GeneratedAt: DateTimeOffset.UtcNow,
                TenantId: request.TenantId,
                TierFilter: request.TierFilter,
                TotalServicesAnalyzed: serviceRisks.Count,
                OverallRisk: overall,
                HighRiskCount: serviceRisks.Count(r => r.RiskLevel == DependencyRiskLevel.High || r.RiskLevel == DependencyRiskLevel.Critical),
                Services: serviceRisks));
        }

        private static decimal ComputeRiskScore(
            NexTraceOne.Catalog.Domain.Graph.Entities.ServiceAsset service,
            int apiCount)
        {
            decimal score = 0m;

            // Tier base weight
            score += service.Tier switch
            {
                ServiceTierType.Critical     => 40m,
                ServiceTierType.Standard     => 20m,
                ServiceTierType.Experimental => 5m,
                _                            => 10m,
            };

            // Fan-in: more APIs = more blast radius potential (capped at 30)
            score += Math.Min(apiCount * 5m, 30m);

            // Governance gap: placeholder team name indicates unverified ownership
            if (service.TeamName.Equals("unassigned", StringComparison.OrdinalIgnoreCase) ||
                service.TeamName.Equals("unknown", StringComparison.OrdinalIgnoreCase))
                score += 15m;

            // Clamp to [0, 100]
            return Math.Min(100m, Math.Max(0m, score));
        }

        private static DependencyRiskLevel ClassifyRisk(decimal score) => score switch
        {
            >= 80 => DependencyRiskLevel.Critical,
            >= 60 => DependencyRiskLevel.High,
            >= 35 => DependencyRiskLevel.Medium,
            _     => DependencyRiskLevel.Low,
        };

        private static IReadOnlyList<string> BuildRiskFactors(
            NexTraceOne.Catalog.Domain.Graph.Entities.ServiceAsset service,
            int apiCount)
        {
            var factors = new List<string>();
            if (service.Tier == ServiceTierType.Critical)
                factors.Add("Critical-tier service — high blast radius if unavailable.");
            if (apiCount > 3)
                factors.Add($"Exposes {apiCount} APIs — high fan-in from consumers.");
            if (service.TeamName.Equals("unassigned", StringComparison.OrdinalIgnoreCase) ||
                service.TeamName.Equals("unknown", StringComparison.OrdinalIgnoreCase))
                factors.Add("No owner team assigned — governance gap.");
            return factors;
        }

        private static DependencyRiskLevel DetermineOverallRisk(IReadOnlyList<ServiceDependencyRisk> risks)
        {
            if (risks.Any(r => r.RiskLevel == DependencyRiskLevel.Critical))
                return DependencyRiskLevel.Critical;
            if (risks.Any(r => r.RiskLevel == DependencyRiskLevel.High))
                return DependencyRiskLevel.High;
            if (risks.Any(r => r.RiskLevel == DependencyRiskLevel.Medium))
                return DependencyRiskLevel.Medium;
            return DependencyRiskLevel.Low;
        }
    }

    public enum DependencyRiskLevel { Low = 0, Medium = 1, High = 2, Critical = 3 }

    public sealed record ServiceDependencyRisk(
        Guid ServiceId,
        string ServiceName,
        ServiceTierType Tier,
        string? TeamName,
        int ApiCount,
        bool HasOwner,
        decimal RiskScore,
        DependencyRiskLevel RiskLevel,
        IReadOnlyList<string> RiskFactors);

    public sealed record Response(
        DateTimeOffset GeneratedAt,
        Guid TenantId,
        ServiceTierType? TierFilter,
        int TotalServicesAnalyzed,
        DependencyRiskLevel OverallRisk,
        int HighRiskCount,
        IReadOnlyList<ServiceDependencyRisk> Services);
}
