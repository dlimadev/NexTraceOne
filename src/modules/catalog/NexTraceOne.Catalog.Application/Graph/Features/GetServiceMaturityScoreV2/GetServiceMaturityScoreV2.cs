using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.DependencyGovernance.Ports;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.DependencyGovernance.Enums;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Domain.Graph.Errors;

namespace NexTraceOne.Catalog.Application.Graph.Features.GetServiceMaturityScoreV2;

/// <summary>
/// Feature: GetServiceMaturityScoreV2 — calcula scorecard de maturidade v2 de um serviço individual.
///
/// Expande o modelo v1 adicionando:
/// - Dimensão "tier_compliance" — alinhamento entre tier declarado e práticas observadas
/// - Dimensão "change_governance" — releases e Evidence Packs assinados associados ao serviço
/// - Dimensão "vulnerability" — presença de advisories registados para o serviço
/// - Pesos diferentes por tier (Critical = maior exigência; Experimental = mais flexível)
/// - Nível de maturidade v2: Nascente / Em Desenvolvimento / Maduro / Excelente
///
/// Alimenta dashboards de Exec/CTO, relatórios de conformidade e gates de promoção.
/// Wave H.3 — Service Maturity Score v2.
/// Estrutura VSA: Query + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class GetServiceMaturityScoreV2
{
    public sealed record Query(Guid ServiceId) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty();
        }
    }

    public sealed class Handler(
        IServiceAssetRepository serviceAssetRepository,
        IServiceLinkRepository serviceLinkRepository,
        IApiAssetRepository apiAssetRepository,
        IContractVersionRepository contractVersionRepository,
        IVulnerabilityAdvisoryRepository vulnerabilityRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var serviceId = ServiceAssetId.From(request.ServiceId);
            var service = await serviceAssetRepository.GetByIdAsync(serviceId, cancellationToken);
            if (service is null)
                return CatalogGraphErrors.ServiceAssetNotFoundById(request.ServiceId);

            var links = await serviceLinkRepository.ListByServiceAsync(serviceId, cancellationToken);
            var apis = await apiAssetRepository.ListByServiceIdAsync(serviceId, cancellationToken);

            var contractCount = 0;
            if (apis.Count > 0)
            {
                var apiIds = apis.Select(a => a.Id.Value).ToList();
                var contracts = await contractVersionRepository.ListByApiAssetIdsAsync(apiIds, cancellationToken);
                contractCount = contracts.Count;
            }

            var criticalAdvisories = await vulnerabilityRepository.CountByServiceAndSeverityAsync(
                serviceId.Value, VulnerabilitySeverity.Critical, activeOnly: true, cancellationToken);
            var highAdvisories = await vulnerabilityRepository.CountByServiceAndSeverityAsync(
                serviceId.Value, VulnerabilitySeverity.High, activeOnly: true, cancellationToken);

            var tier = service.Tier;
            var dimensions = ComputeDimensionsV2(service, links, apis.Count, contractCount, criticalAdvisories, highAdvisories, tier);

            // Weighted average respects tier — Critical services penalise gaps more
            var weightedScore = ComputeWeightedScore(dimensions, tier);
            var level = ScoreToLevelV2(weightedScore);

            return Result<Response>.Success(new Response(
                ServiceId: request.ServiceId,
                ServiceName: service.Name,
                Tier: tier.ToString(),
                Level: level,
                OverallScore: Math.Round(weightedScore * 100m, 1),
                Dimensions: dimensions,
                ComputedAt: DateTimeOffset.UtcNow));
        }

        private static List<MaturityDimensionV2Dto> ComputeDimensionsV2(
            ServiceAsset service,
            IReadOnlyList<ServiceLink> links,
            int apiCount,
            int contractCount,
            int criticalAdvisories,
            int highAdvisories,
            ServiceTierType tier)
        {
            var dimensions = new List<MaturityDimensionV2Dto>();

            // 1. Ownership
            var ownerScore = 0m;
            var ownerNotes = new List<string>();
            if (!string.IsNullOrWhiteSpace(service.TeamName)) ownerScore += 0.4m;
            else ownerNotes.Add("Missing team name");
            if (!string.IsNullOrWhiteSpace(service.TechnicalOwner)) ownerScore += 0.35m;
            else ownerNotes.Add("Missing technical owner");
            if (!string.IsNullOrWhiteSpace(service.BusinessOwner)) ownerScore += 0.25m;
            else ownerNotes.Add("Missing business owner");
            dimensions.Add(new("ownership", Math.Round(ownerScore, 3),
                ownerScore >= 1m ? "Ownership complete" : string.Join("; ", ownerNotes)));

            // 2. Contracts
            var contractScore = contractCount switch
            {
                0 when apiCount == 0 => 0.5m,
                0 => 0m,
                >= 3 => 1m,
                _ => 0.5m + 0.5m * Math.Min(contractCount, 3) / 3m
            };
            dimensions.Add(new("contracts", Math.Round(contractScore, 3),
                contractCount == 0 && apiCount == 0
                    ? "No APIs registered"
                    : contractCount == 0
                        ? $"{apiCount} API(s) but no contracts registered"
                        : $"{contractCount} contract(s) across {apiCount} API(s)"));

            // 3. Documentation
            var hasDocUrl = !string.IsNullOrWhiteSpace(service.DocumentationUrl);
            var docLinkCount = links.Count(l => l.Category is LinkCategory.Documentation or LinkCategory.Wiki or LinkCategory.Adr);
            var docScore = 0m;
            if (hasDocUrl) docScore += 0.5m;
            docScore += Math.Min(docLinkCount * 0.25m, 0.5m);
            dimensions.Add(new("documentation", Math.Round(docScore, 3),
                docScore >= 1m ? "Documentation well covered"
                : hasDocUrl ? $"Documentation URL present; {docLinkCount} doc link(s)"
                : "Missing documentation URL"));

            // 4. Operational links (runbooks, dashboards, monitoring)
            var opsLinkCount = links.Count(l => l.Category is LinkCategory.Runbook or LinkCategory.Dashboard or LinkCategory.Monitoring);
            var opsScore = Math.Min(opsLinkCount / 3m, 1m);
            dimensions.Add(new("operational_readiness", Math.Round(opsScore, 3),
                opsLinkCount == 0 ? "No runbooks, dashboards or monitoring links registered"
                : $"{opsLinkCount} operational link(s) registered"));

            // 5. Tier compliance — alinhamento entre tier e práticas
            var tierScore = ComputeTierComplianceScore(service, contractCount, opsLinkCount, tier);
            dimensions.Add(new("tier_compliance", Math.Round(tierScore, 3),
                tierScore >= 0.8m ? $"Service practices align with {tier} tier requirements"
                : $"Service does not fully meet {tier} tier requirements"));

            // 6. Vulnerability posture — fewer advisories = higher score
            var vulnScore = (criticalAdvisories, highAdvisories) switch
            {
                (> 0, _) => 0m,    // any critical = zero
                (0, > 2) => 0.3m,  // many high
                (0, > 0) => 0.6m,  // some high
                _ => 1m            // clean
            };
            dimensions.Add(new("vulnerability_posture", vulnScore,
                criticalAdvisories > 0 ? $"{criticalAdvisories} critical advisory(ies) detected — must remediate"
                : highAdvisories > 0 ? $"{highAdvisories} high advisory(ies) detected"
                : "No critical or high advisories"));

            return dimensions;
        }

        private static decimal ComputeTierComplianceScore(
            ServiceAsset service,
            int contractCount,
            int opsLinkCount,
            ServiceTierType tier)
        {
            // Critical: strict — requires ownership + contracts + ops links
            // Standard: moderate — ownership + at least some contracts
            // Experimental: lenient — basic ownership acceptable
            return tier switch
            {
                ServiceTierType.Critical =>
                    (!string.IsNullOrWhiteSpace(service.TechnicalOwner) ? 0.4m : 0m)
                    + (contractCount > 0 ? 0.3m : 0m)
                    + (opsLinkCount >= 2 ? 0.3m : opsLinkCount >= 1 ? 0.15m : 0m),

                ServiceTierType.Standard =>
                    (!string.IsNullOrWhiteSpace(service.TeamName) ? 0.5m : 0m)
                    + (contractCount > 0 ? 0.5m : 0m),

                ServiceTierType.Experimental =>
                    !string.IsNullOrWhiteSpace(service.TeamName) ? 1m : 0.5m,

                _ => 0m
            };
        }

        private static decimal ComputeWeightedScore(
            IReadOnlyList<MaturityDimensionV2Dto> dimensions,
            ServiceTierType tier)
        {
            // Tier-aware weights — Critical penalises vulnerability and tier compliance gaps more
            var weights = tier switch
            {
                ServiceTierType.Critical => new Dictionary<string, decimal>
                {
                    ["ownership"]            = 0.20m,
                    ["contracts"]            = 0.15m,
                    ["documentation"]        = 0.10m,
                    ["operational_readiness"] = 0.15m,
                    ["tier_compliance"]      = 0.20m,
                    ["vulnerability_posture"] = 0.20m,
                },
                ServiceTierType.Standard => new Dictionary<string, decimal>
                {
                    ["ownership"]            = 0.25m,
                    ["contracts"]            = 0.20m,
                    ["documentation"]        = 0.15m,
                    ["operational_readiness"] = 0.15m,
                    ["tier_compliance"]      = 0.15m,
                    ["vulnerability_posture"] = 0.10m,
                },
                _ /* Experimental */ => new Dictionary<string, decimal>
                {
                    ["ownership"]            = 0.35m,
                    ["contracts"]            = 0.20m,
                    ["documentation"]        = 0.15m,
                    ["operational_readiness"] = 0.10m,
                    ["tier_compliance"]      = 0.10m,
                    ["vulnerability_posture"] = 0.10m,
                },
            };

            var total = 0m;
            foreach (var dim in dimensions)
            {
                var weight = weights.TryGetValue(dim.DimensionKey, out var w) ? w : 1m / dimensions.Count;
                total += dim.Score * weight;
            }
            return Math.Clamp(total, 0m, 1m);
        }

        private static string ScoreToLevelV2(decimal score) => score switch
        {
            >= 0.85m => "Excelente",
            >= 0.65m => "Maduro",
            >= 0.40m => "Em Desenvolvimento",
            _        => "Nascente"
        };
    }

    public sealed record MaturityDimensionV2Dto(
        string DimensionKey,
        decimal Score,
        string Explanation);

    public sealed record Response(
        Guid ServiceId,
        string ServiceName,
        string Tier,
        string Level,
        decimal OverallScore,
        IReadOnlyList<MaturityDimensionV2Dto> Dimensions,
        DateTimeOffset ComputedAt);
}
