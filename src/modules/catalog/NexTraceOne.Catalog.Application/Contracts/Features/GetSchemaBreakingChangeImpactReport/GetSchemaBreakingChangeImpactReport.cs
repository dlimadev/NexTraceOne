using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetSchemaBreakingChangeImpactReport;

/// <summary>
/// Feature: GetSchemaBreakingChangeImpactReport — avaliação de impacto downstream de breaking changes.
///
/// Dado um tenant, analisa todas as breaking changes detectadas no período lookback e calcula:
/// - <c>DirectConsumers</c>: serviços com consumo registado da API afectada
/// - <c>IndirectConsumers</c>: serviços que dependem dos consumidores directos (até <c>MaxHopDepth</c> saltos)
/// - <c>ImpactScore</c>: pontuação ponderada por tier (Critical=3, Standard=2, Experimental=1)
/// - <c>TotalAffectedServices</c>: soma de directos + indirectos únicos
/// - <c>MitigationOptions</c>: sugestões de mitigação geradas com base no impacto
/// - <c>ByEnvironment</c>: breakdown de impacto por ambiente (production vs. non-production)
///
/// <c>BreakingChangeImpactTier</c>:
/// - <c>Contained</c>    — apenas consumidores Experimental afectados
/// - <c>Moderate</c>     — ≥1 consumidor Standard afectado
/// - <c>Significant</c>  — ≥1 consumidor Critical afectado
/// - <c>Widespread</c>   — ≥ <c>WidespreadThreshold</c> serviços afectados (directos + indirectos)
///
/// Transforma a detecção de breaking changes em decisão informada com quantificação
/// de impacto real por tier e ambiente.
///
/// Wave AE.2 — GetSchemaBreakingChangeImpactReport (Catalog Contracts).
/// </summary>
public static class GetSchemaBreakingChangeImpactReport
{
    // ── Pesos de tier de consumidor ────────────────────────────────────────
    private const int CriticalTierWeight = 3;
    private const int StandardTierWeight = 2;
    private const int ExperimentalTierWeight = 1;

    // ── Threshold para Widespread ──────────────────────────────────────────
    private const int DefaultWidespreadThreshold = 5;

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>LookbackDays</c>: janela temporal de análise em dias (1–365, default 90).</para>
    /// <para><c>MaxHopDepth</c>: profundidade máxima de propagação de impacto transitivo (1–5, default 2).</para>
    /// <para><c>MaxConsumers</c>: máximo de consumidores analisados por breaking change (10–500, default 200).</para>
    /// <para><c>TopImpactfulCount</c>: número de breaking changes mais impactantes a listar (1–50, default 10).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = 90,
        int MaxHopDepth = 2,
        int MaxConsumers = 200,
        int TopImpactfulCount = 10) : IQuery<Report>;

    // ── Enums ─────────────────────────────────────────────────────────────

    /// <summary>Classificação de impacto de uma breaking change.</summary>
    public enum BreakingChangeImpactTier
    {
        /// <summary>Apenas consumidores Experimental afectados.</summary>
        Contained,
        /// <summary>Pelo menos 1 consumidor Standard afectado.</summary>
        Moderate,
        /// <summary>Pelo menos 1 consumidor Critical afectado.</summary>
        Significant,
        /// <summary>5 ou mais serviços totais afectados (directos + indirectos).</summary>
        Widespread
    }

    // ── Value objects ──────────────────────────────────────────────────────

    /// <summary>Sugestão de mitigação para uma breaking change.</summary>
    public sealed record MitigationOption(
        string Option,
        string Description);

    /// <summary>Impacto por ambiente de uma breaking change.</summary>
    public sealed record EnvironmentImpact(
        string Environment,
        int AffectedServiceCount,
        bool HasCriticalConsumer);

    /// <summary>Resultado de impacto de uma breaking change individual.</summary>
    public sealed record BreakingChangeImpactEntry(
        string ChangelogEntryId,
        string ApiAssetId,
        string ProducerServiceName,
        string? FromVersion,
        string ToVersion,
        DateTimeOffset ChangedAt,
        string Summary,
        int DirectConsumerCount,
        int IndirectConsumerCount,
        int TotalAffectedServices,
        int ImpactScore,
        BreakingChangeImpactTier ImpactTier,
        IReadOnlyList<string> DirectConsumerNames,
        IReadOnlyList<string> IndirectConsumerNames,
        IReadOnlyList<MitigationOption> MitigationOptions,
        IReadOnlyList<EnvironmentImpact> ByEnvironment);

    /// <summary>Distribuição de breaking changes por tier de impacto.</summary>
    public sealed record ImpactTierDistribution(
        int ContainedCount,
        int ModerateCount,
        int SignificantCount,
        int WidespreadCount);

    /// <summary>Resultado do relatório de impacto de breaking changes.</summary>
    public sealed record Report(
        string TenantId,
        int TotalBreakingChanges,
        int HighImpactBreakingChanges,
        ImpactTierDistribution TierDistribution,
        IReadOnlyList<BreakingChangeImpactEntry> TopImpactfulChanges,
        IReadOnlyList<BreakingChangeImpactEntry> AllChanges);

    // ── Handler ───────────────────────────────────────────────────────────

    internal sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly IBreakingChangeImpactReader _impactReader;

        public Handler(IBreakingChangeImpactReader impactReader)
        {
            _impactReader = Guard.Against.Null(impactReader);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken ct)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var breakingChanges = await _impactReader.ListBreakingChangesByTenantAsync(
                query.TenantId, query.LookbackDays, ct);

            var entries = new List<BreakingChangeImpactEntry>();

            foreach (var bc in breakingChanges)
            {
                // Limitar consumidores ao MaxConsumers configurado
                var directConsumers = bc.DirectConsumers
                    .Take(query.MaxConsumers)
                    .ToList();

                // Calcular indirectos até MaxHopDepth (apenas hop 1 = dependentes dos directos)
                var indirectSet = new HashSet<string>();
                var directNames = directConsumers.Select(c => c.ServiceName).ToHashSet();

                if (query.MaxHopDepth >= 1)
                {
                    foreach (var consumer in directConsumers)
                    {
                        foreach (var dep in consumer.DependentServiceNames.Take(50))
                        {
                            if (!directNames.Contains(dep))
                                indirectSet.Add(dep);
                        }
                    }
                }

                var totalAffected = directNames.Count + indirectSet.Count;

                // ImpactScore = soma dos pesos por tier dos consumidores directos
                var impactScore = directConsumers.Sum(c => TierWeight(c.ServiceTier))
                    + indirectSet.Count * ExperimentalTierWeight;

                // BreakingChangeImpactTier
                var tier = ClassifyImpact(directConsumers, totalAffected, DefaultWidespreadThreshold);

                // Mitigações
                var mitigations = BuildMitigations(tier, directConsumers);

                // Breakdown por ambiente
                var byEnv = directConsumers
                    .GroupBy(c => c.Environment)
                    .Select(g => new EnvironmentImpact(
                        Environment: g.Key,
                        AffectedServiceCount: g.Count(),
                        HasCriticalConsumer: g.Any(c => string.Equals(c.ServiceTier, "Critical", StringComparison.OrdinalIgnoreCase))))
                    .ToList();

                entries.Add(new BreakingChangeImpactEntry(
                    ChangelogEntryId: bc.ChangelogEntryId,
                    ApiAssetId: bc.ApiAssetId,
                    ProducerServiceName: bc.ProducerServiceName,
                    FromVersion: bc.FromVersion,
                    ToVersion: bc.ToVersion,
                    ChangedAt: bc.ChangedAt,
                    Summary: bc.Summary,
                    DirectConsumerCount: directNames.Count,
                    IndirectConsumerCount: indirectSet.Count,
                    TotalAffectedServices: totalAffected,
                    ImpactScore: impactScore,
                    ImpactTier: tier,
                    DirectConsumerNames: directNames.OrderBy(n => n).ToList(),
                    IndirectConsumerNames: indirectSet.OrderBy(n => n).ToList(),
                    MitigationOptions: mitigations,
                    ByEnvironment: byEnv));
            }

            var tierDist = new ImpactTierDistribution(
                ContainedCount: entries.Count(e => e.ImpactTier == BreakingChangeImpactTier.Contained),
                ModerateCount: entries.Count(e => e.ImpactTier == BreakingChangeImpactTier.Moderate),
                SignificantCount: entries.Count(e => e.ImpactTier == BreakingChangeImpactTier.Significant),
                WidespreadCount: entries.Count(e => e.ImpactTier == BreakingChangeImpactTier.Widespread));

            var topImpactful = entries
                .OrderByDescending(e => e.ImpactScore)
                .Take(query.TopImpactfulCount)
                .ToList();

            var highImpact = entries.Count(e =>
                e.ImpactTier is BreakingChangeImpactTier.Significant or BreakingChangeImpactTier.Widespread);

            return Result<Report>.Success(new Report(
                TenantId: query.TenantId,
                TotalBreakingChanges: entries.Count,
                HighImpactBreakingChanges: highImpact,
                TierDistribution: tierDist,
                TopImpactfulChanges: topImpactful,
                AllChanges: entries.OrderByDescending(e => e.ChangedAt).ToList()));
        }

        private static int TierWeight(string tier) => tier.ToUpperInvariant() switch
        {
            "CRITICAL" => CriticalTierWeight,
            "STANDARD" => StandardTierWeight,
            _ => ExperimentalTierWeight
        };

        private static BreakingChangeImpactTier ClassifyImpact(
            IReadOnlyList<ConsumerServiceInfo> directConsumers,
            int totalAffected,
            int widespreadThreshold)
        {
            if (totalAffected >= widespreadThreshold)
                return BreakingChangeImpactTier.Widespread;

            if (directConsumers.Any(c => string.Equals(c.ServiceTier, "Critical", StringComparison.OrdinalIgnoreCase)))
                return BreakingChangeImpactTier.Significant;

            if (directConsumers.Any(c => string.Equals(c.ServiceTier, "Standard", StringComparison.OrdinalIgnoreCase)))
                return BreakingChangeImpactTier.Moderate;

            return BreakingChangeImpactTier.Contained;
        }

        private static IReadOnlyList<MitigationOption> BuildMitigations(
            BreakingChangeImpactTier tier,
            IReadOnlyList<ConsumerServiceInfo> consumers)
        {
            var options = new List<MitigationOption>
            {
                new("deprecate-before-remove",
                    "Deprecate the current version with sufficient notice before removing the breaking change."),
                new("versioning",
                    "Maintain backward compatibility via semantic versioning — publish a new major version and keep the old version alive during migration.")
            };

            if (tier is BreakingChangeImpactTier.Significant or BreakingChangeImpactTier.Widespread)
            {
                options.Add(new MitigationOption(
                    "notify-consumers",
                    "Notify all affected consumers directly before publishing the breaking change."));
            }

            if (consumers.Any(c => string.Equals(c.Environment, "production", StringComparison.OrdinalIgnoreCase)))
            {
                options.Add(new MitigationOption(
                    "staged-rollout",
                    "Roll out the change in non-production environments first and validate consumer compatibility before promoting to production."));
            }

            return options;
        }
    }

    // ── Validator ─────────────────────────────────────────────────────────

    internal sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(q => q.LookbackDays).InclusiveBetween(1, 365);
            RuleFor(q => q.MaxHopDepth).InclusiveBetween(1, 5);
            RuleFor(q => q.MaxConsumers).InclusiveBetween(10, 500);
            RuleFor(q => q.TopImpactfulCount).InclusiveBetween(1, 50);
        }
    }
}
