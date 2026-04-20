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

namespace NexTraceOne.Catalog.Application.Graph.Features.GetServiceTierPolicy;

/// <summary>
/// Feature: GetServiceTierPolicy — retorna a política de tier aplicável a um serviço.
/// A política inclui thresholds mínimos de SLO, maturidade e indicadores de conformidade.
/// Permite que Change Gates e Promotion Gates validem se o serviço cumpre os requisitos do seu tier.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetServiceTierPolicy
{
    /// <summary>Query para obter a política de tier de um serviço.</summary>
    public sealed record Query(Guid ServiceId) : IQuery<Response>;

    /// <summary>Valida a query GetServiceTierPolicy.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty();
        }
    }

    /// <summary>Handler que retorna a política de tier do serviço com conformidade actual.</summary>
    public sealed class Handler(
        IServiceAssetRepository serviceAssetRepository,
        IConfigurationResolutionService config) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var service = await serviceAssetRepository.GetByIdAsync(
                ServiceAssetId.From(request.ServiceId), cancellationToken);
            if (service is null)
                return CatalogGraphErrors.ServiceAssetNotFoundById(request.ServiceId);

            var (sloKey, maturityKey) = service.Tier switch
            {
                ServiceTierType.Critical => (
                    ServiceCatalogConfigKeys.TierCriticalSloMinPercent,
                    ServiceCatalogConfigKeys.TierCriticalMaturityMinScore),
                ServiceTierType.Experimental => (
                    ServiceCatalogConfigKeys.TierExperimentalSloMinPercent,
                    (string?)null),
                _ => (
                    ServiceCatalogConfigKeys.TierStandardSloMinPercent,
                    ServiceCatalogConfigKeys.TierStandardMaturityMinScore)
            };

            var sloDto = await config.ResolveEffectiveValueAsync(sloKey, ConfigurationScope.System, null, cancellationToken);
            var sloMin = ParseDecimal(sloDto?.EffectiveValue, DefaultSloMin(service.Tier));

            var maturityMin = 0m;
            if (maturityKey is not null)
            {
                var matDto = await config.ResolveEffectiveValueAsync(maturityKey, ConfigurationScope.System, null, cancellationToken);
                maturityMin = ParseDecimal(matDto?.EffectiveValue, DefaultMaturityMin(service.Tier));
            }

            var sloConformant = !string.IsNullOrWhiteSpace(service.SloTarget)
                && decimal.TryParse(service.SloTarget.TrimEnd('%'), out var actualSlo)
                && actualSlo >= sloMin;

            return new Response(
                ServiceId: service.Id.Value,
                ServiceName: service.Name,
                Tier: service.Tier.ToString(),
                Policy: new TierPolicyDto(
                    MinSloPercent: sloMin,
                    MinMaturityScore: maturityMin,
                    OnCallRequired: service.Tier is ServiceTierType.Critical,
                    RunbookRequired: service.Tier is not ServiceTierType.Experimental,
                    MonitoringRequired: service.Tier is not ServiceTierType.Experimental),
                Conformance: new TierConformanceDto(
                    SloConformant: sloConformant,
                    HasOnCall: !string.IsNullOrWhiteSpace(service.OnCallRotationId),
                    HasContactChannel: !string.IsNullOrWhiteSpace(service.ContactChannel)));
        }

        private static decimal ParseDecimal(string? raw, decimal fallback) =>
            decimal.TryParse(raw, out var v) ? v : fallback;

        private static decimal DefaultSloMin(ServiceTierType tier) => tier switch
        {
            ServiceTierType.Critical => 99.9m,
            ServiceTierType.Standard => 99.5m,
            _ => 99.0m
        };

        private static decimal DefaultMaturityMin(ServiceTierType tier) => tier switch
        {
            ServiceTierType.Critical => 0.8m,
            ServiceTierType.Standard => 0.6m,
            _ => 0m
        };
    }

    /// <summary>Resposta com a política de tier e estado de conformidade do serviço.</summary>
    public sealed record Response(
        Guid ServiceId,
        string ServiceName,
        string Tier,
        TierPolicyDto Policy,
        TierConformanceDto Conformance);

    /// <summary>Thresholds mínimos definidos pelo tier.</summary>
    public sealed record TierPolicyDto(
        decimal MinSloPercent,
        decimal MinMaturityScore,
        bool OnCallRequired,
        bool RunbookRequired,
        bool MonitoringRequired);

    /// <summary>Estado de conformidade actual do serviço em relação à política do tier.</summary>
    public sealed record TierConformanceDto(
        bool SloConformant,
        bool HasOnCall,
        bool HasContactChannel);
}
