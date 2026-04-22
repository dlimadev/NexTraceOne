using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Compliance.Enums;

namespace NexTraceOne.ChangeGovernance.Application.Compliance.Features.GetZeroTrustPostureReport;

/// <summary>
/// Feature: GetZeroTrustPostureReport — avaliação de postura Zero Trust por serviço do tenant.
///
/// Calcula ZeroTrustScore (0–100) por 4 dimensões com pesos (soma = 100):
/// - Authentication (30 pts) — esquema de autenticação definido e activo
/// - Mutual TLS (25 pts) — mTLS habilitado para comunicação inter-serviço
/// - Token Rotation (20 pts) — política de rotação de tokens definida
/// - Policy Coverage (25 pts) — pelo menos 1 PolicyDefinition de acesso aplicada ao serviço
///
/// Classifica cada serviço num <see cref="ZeroTrustTier"/>:
/// - Enforced: score &gt;= 85
/// - Controlled: score &gt;= 65
/// - Partial: score &gt;= 40
/// - Exposed: score &lt; 40
///
/// Sinaliza CriticalExposure para serviços de tier Critical com ZeroTrustTier = Exposed.
/// Produz TenantZeroTrustScore ponderado por tier de serviço (Critical=3, Standard=2, Experimental=1).
///
/// Wave AD.1 — Zero Trust &amp; Security Posture Analytics (ChangeGovernance).
/// </summary>
public static class GetZeroTrustPostureReport
{
    // ── Pesos das dimensões ───────────────────────────────────────────────
    private const int AuthWeight = 30;
    private const int MtlsWeight = 25;
    private const int TokenRotationWeight = 20;
    private const int PolicyCoverageWeight = 25;

    // ── Pesos de tier de serviço para score ponderado do tenant ──────────
    private const int CriticalTierWeight = 3;
    private const int StandardTierWeight = 2;
    private const int ExperimentalTierWeight = 1;

    // ── Query ─────────────────────────────────────────────────────────────

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>MaxServices</c>: número máximo de serviços a devolver no perfil (1–200, padrão 50).</para>
    /// <para><c>TeamFilter</c>: filtro opcional por equipa.</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int MaxServices = 50,
        string? TeamFilter = null) : IQuery<Response>;

    // ── Validator ─────────────────────────────────────────────────────────

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty();
            RuleFor(q => q.MaxServices).InclusiveBetween(1, 200);
        }
    }

    // ── Value objects ─────────────────────────────────────────────────────

    /// <summary>Perfil de postura Zero Trust de um serviço.</summary>
    public sealed record ServiceZeroTrustProfile(
        string ServiceId,
        string ServiceName,
        string? TeamName,
        string ServiceTier,
        int ZeroTrustScore,
        ZeroTrustTier Tier,
        bool HasAuthentication,
        bool MtlsEnabled,
        bool HasTokenRotation,
        bool HasPolicyCoverage,
        bool CriticalExposure);

    /// <summary>Distribuição de serviços por ZeroTrustTier.</summary>
    public sealed record TierDistribution(
        int EnforcedCount,
        int ControlledCount,
        int PartialCount,
        int ExposedCount);

    /// <summary>Relatório de postura Zero Trust do tenant.</summary>
    public sealed record Response(
        DateTimeOffset GeneratedAt,
        string TenantId,
        string? TeamFilter,
        int TotalServicesAnalyzed,
        double TenantZeroTrustScore,
        TierDistribution TierDistribution,
        IReadOnlyList<ServiceZeroTrustProfile> ServiceProfiles,
        IReadOnlyList<ServiceZeroTrustProfile> TopExposedServices,
        int CriticalExposureCount);

    // ── Handler ───────────────────────────────────────────────────────────

    public sealed class Handler : IQueryHandler<Query, Response>
    {
        private readonly IZeroTrustServiceReader _reader;
        private readonly IDateTimeProvider _clock;

        public Handler(IZeroTrustServiceReader reader, IDateTimeProvider clock)
        {
            _reader = Guard.Against.Null(reader);
            _clock = Guard.Against.Null(clock);
        }

        public async Task<Result<Response>> Handle(Query query, CancellationToken cancellationToken)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var now = _clock.UtcNow;
            var entries = await _reader.ListByTenantAsync(query.TenantId, cancellationToken);

            // Filtra por equipa se especificado
            var filtered = query.TeamFilter is not null
                ? entries.Where(e => string.Equals(e.TeamName, query.TeamFilter, StringComparison.OrdinalIgnoreCase)).ToList()
                : entries.ToList();

            if (filtered.Count == 0)
            {
                return Result<Response>.Success(new Response(
                    GeneratedAt: now,
                    TenantId: query.TenantId,
                    TeamFilter: query.TeamFilter,
                    TotalServicesAnalyzed: 0,
                    TenantZeroTrustScore: 0.0,
                    TierDistribution: new TierDistribution(0, 0, 0, 0),
                    ServiceProfiles: [],
                    TopExposedServices: [],
                    CriticalExposureCount: 0));
            }

            // Calcula perfil por serviço
            var profiles = filtered
                .Take(query.MaxServices)
                .Select(e => BuildProfile(e))
                .ToList();

            // Score ponderado do tenant
            double tenantScore = ComputeTenantScore(profiles);

            // Distribuição por tier
            var dist = new TierDistribution(
                EnforcedCount: profiles.Count(p => p.Tier == ZeroTrustTier.Enforced),
                ControlledCount: profiles.Count(p => p.Tier == ZeroTrustTier.Controlled),
                PartialCount: profiles.Count(p => p.Tier == ZeroTrustTier.Partial),
                ExposedCount: profiles.Count(p => p.Tier == ZeroTrustTier.Exposed));

            // Top serviços mais expostos (menor score primeiro)
            var topExposed = profiles
                .OrderBy(p => p.ZeroTrustScore)
                .Take(10)
                .ToList();

            return Result<Response>.Success(new Response(
                GeneratedAt: now,
                TenantId: query.TenantId,
                TeamFilter: query.TeamFilter,
                TotalServicesAnalyzed: profiles.Count,
                TenantZeroTrustScore: tenantScore,
                TierDistribution: dist,
                ServiceProfiles: profiles,
                TopExposedServices: topExposed,
                CriticalExposureCount: profiles.Count(p => p.CriticalExposure)));
        }

        // ── Cálculo do perfil de um serviço ───────────────────────────────

        private static ServiceZeroTrustProfile BuildProfile(ServiceSecurityEntry e)
        {
            int score = 0;
            if (e.HasAuthenticationScheme) score += AuthWeight;
            if (e.MtlsEnabled) score += MtlsWeight;
            if (e.HasTokenRotationPolicy) score += TokenRotationWeight;
            if (e.PolicyDefinitionCount >= 1) score += PolicyCoverageWeight;

            var tier = ClassifyTier(score);
            bool criticalExposure = string.Equals(e.ServiceTier, "Critical", StringComparison.OrdinalIgnoreCase)
                && tier == ZeroTrustTier.Exposed;

            return new ServiceZeroTrustProfile(
                ServiceId: e.ServiceId,
                ServiceName: e.ServiceName,
                TeamName: e.TeamName,
                ServiceTier: e.ServiceTier,
                ZeroTrustScore: score,
                Tier: tier,
                HasAuthentication: e.HasAuthenticationScheme,
                MtlsEnabled: e.MtlsEnabled,
                HasTokenRotation: e.HasTokenRotationPolicy,
                HasPolicyCoverage: e.PolicyDefinitionCount >= 1,
                CriticalExposure: criticalExposure);
        }

        private static ZeroTrustTier ClassifyTier(int score) => score switch
        {
            >= 85 => ZeroTrustTier.Enforced,
            >= 65 => ZeroTrustTier.Controlled,
            >= 40 => ZeroTrustTier.Partial,
            _ => ZeroTrustTier.Exposed
        };

        // ── Score ponderado do tenant por tier de serviço ─────────────────

        private static double ComputeTenantScore(IReadOnlyList<ServiceZeroTrustProfile> profiles)
        {
            if (profiles.Count == 0) return 0.0;

            double weightedSum = 0.0;
            int totalWeight = 0;

            foreach (var p in profiles)
            {
                int w = p.ServiceTier switch
                {
                    "Critical" => CriticalTierWeight,
                    "Standard" => StandardTierWeight,
                    _ => ExperimentalTierWeight
                };
                weightedSum += p.ZeroTrustScore * w;
                totalWeight += w;
            }

            return totalWeight > 0
                ? Math.Round(weightedSum / totalWeight, 2)
                : 0.0;
        }
    }
}
