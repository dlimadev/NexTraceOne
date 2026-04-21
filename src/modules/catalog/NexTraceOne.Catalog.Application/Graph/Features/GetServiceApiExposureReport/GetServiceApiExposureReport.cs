using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Application.Graph.Features.GetServiceApiExposureReport;

/// <summary>
/// Feature: GetServiceApiExposureReport — mapa de exposição de APIs do tenant.
///
/// Agrega todos os serviços e os seus ativos de API para produzir:
/// - contagem total de serviços e APIs
/// - serviços sem APIs (órfãos sem contrato exposto)
/// - serviços com alta exposição (API count &gt; threshold configurável)
/// - distribuição de APIs por visibilidade (Public/Internal/Partner/Outros)
/// - distribuição de serviços por tipo de exposição (ExposureType: Internal/External/Partner)
/// - top serviços por contagem de APIs (governança de blast radius)
/// - média de APIs por serviço
///
/// Serve Architect, Tech Lead e Platform Admin como painel de governança de superfície de API.
///
/// Wave P.1 — Service API Exposure Report (Catalog Graph).
/// </summary>
public static class GetServiceApiExposureReport
{
    /// <summary>
    /// <para><c>MaxTopServices</c>: número máximo de serviços no ranking por contagem de APIs (1–100, default 10).</para>
    /// <para><c>HighExposureThreshold</c>: número mínimo de APIs para classificar como alta exposição (1–100, default 5).</para>
    /// </summary>
    public sealed record Query(
        int MaxTopServices = 10,
        int HighExposureThreshold = 5) : IQuery<Report>;

    // ── Value objects ─────────────────────────────────────────────────────

    /// <summary>Distribuição de APIs por visibilidade declarada.</summary>
    public sealed record ApiVisibilityDistribution(
        int PublicCount,
        int InternalCount,
        int PartnerCount,
        int OtherCount);

    /// <summary>Distribuição de serviços por tipo de exposição (ExposureType).</summary>
    public sealed record ServiceExposureDistribution(
        int InternalCount,
        int ExternalCount,
        int PartnerCount);

    /// <summary>Entrada de um serviço no ranking de exposição por contagem de APIs.</summary>
    public sealed record ServiceApiEntry(
        string ServiceName,
        string TeamName,
        ExposureType ExposureType,
        int ApiCount,
        bool IsHighExposure);

    /// <summary>Resultado do relatório de exposição de APIs por serviço.</summary>
    public sealed record Report(
        int TotalServices,
        int TotalApis,
        int OrphanedServiceCount,
        int HighExposureServiceCount,
        decimal ApiPerServiceAvg,
        ApiVisibilityDistribution ApisByVisibility,
        ServiceExposureDistribution ServicesByExposureType,
        IReadOnlyList<ServiceApiEntry> TopServicesByApiCount,
        DateTimeOffset GeneratedAt);

    // ── Validator ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.MaxTopServices).InclusiveBetween(1, 100);
            RuleFor(q => q.HighExposureThreshold).InclusiveBetween(1, 100);
        }
    }

    // ── Handler ───────────────────────────────────────────────────────────

    public sealed class Handler(
        IServiceAssetRepository serviceAssetRepository,
        IApiAssetRepository apiAssetRepository,
        IDateTimeProvider clock)
        : IQueryHandler<Query, Report>
    {
        public async Task<Result<Report>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.Null(query);

            var services = await serviceAssetRepository.ListAllAsync(cancellationToken);
            var apis = await apiAssetRepository.ListAllAsync(cancellationToken);

            var totalServices = services.Count;
            var totalApis = apis.Count;

            if (totalServices == 0)
            {
                return Result<Report>.Success(new Report(
                    TotalServices: 0,
                    TotalApis: 0,
                    OrphanedServiceCount: 0,
                    HighExposureServiceCount: 0,
                    ApiPerServiceAvg: 0m,
                    ApisByVisibility: new ApiVisibilityDistribution(0, 0, 0, 0),
                    ServicesByExposureType: new ServiceExposureDistribution(0, 0, 0),
                    TopServicesByApiCount: [],
                    GeneratedAt: clock.UtcNow));
            }

            // Group APIs by owner service ID
            var apisByService = apis
                .GroupBy(a => a.OwnerService.Id.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Per-service entry
            var serviceEntries = services
                .Select(s =>
                {
                    var serviceApis = apisByService.TryGetValue(s.Id.Value, out var sApis) ? sApis : [];
                    return new ServiceApiEntry(
                        ServiceName: s.Name,
                        TeamName: s.TeamName,
                        ExposureType: s.ExposureType,
                        ApiCount: serviceApis.Count,
                        IsHighExposure: serviceApis.Count >= query.HighExposureThreshold);
                })
                .ToList();

            var orphanedCount = serviceEntries.Count(e => e.ApiCount == 0);
            var highExposureCount = serviceEntries.Count(e => e.IsHighExposure);
            var apiPerServiceAvg = totalServices == 0 ? 0m : Math.Round((decimal)totalApis / totalServices, 2);

            // API visibility distribution
            var publicCount = apis.Count(a => string.Equals(a.Visibility, "Public", StringComparison.OrdinalIgnoreCase));
            var internalCount = apis.Count(a => string.Equals(a.Visibility, "Internal", StringComparison.OrdinalIgnoreCase));
            var partnerCount = apis.Count(a => string.Equals(a.Visibility, "Partner", StringComparison.OrdinalIgnoreCase));
            var otherCount = totalApis - publicCount - internalCount - partnerCount;

            var apisByVisibility = new ApiVisibilityDistribution(publicCount, internalCount, partnerCount, otherCount);

            // Service exposure type distribution
            var servicesByExposureType = new ServiceExposureDistribution(
                InternalCount: services.Count(s => s.ExposureType == ExposureType.Internal),
                ExternalCount: services.Count(s => s.ExposureType == ExposureType.External),
                PartnerCount: services.Count(s => s.ExposureType == ExposureType.Partner));

            // Top services by API count
            var topServices = serviceEntries
                .OrderByDescending(e => e.ApiCount)
                .ThenBy(e => e.ServiceName)
                .Take(query.MaxTopServices)
                .ToList();

            return Result<Report>.Success(new Report(
                TotalServices: totalServices,
                TotalApis: totalApis,
                OrphanedServiceCount: orphanedCount,
                HighExposureServiceCount: highExposureCount,
                ApiPerServiceAvg: apiPerServiceAvg,
                ApisByVisibility: apisByVisibility,
                ServicesByExposureType: servicesByExposureType,
                TopServicesByApiCount: topServices,
                GeneratedAt: clock.UtcNow));
        }
    }
}
