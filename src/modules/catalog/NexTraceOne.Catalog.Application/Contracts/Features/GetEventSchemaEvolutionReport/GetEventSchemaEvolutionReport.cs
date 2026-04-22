using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetEventSchemaEvolutionReport;

/// <summary>
/// Feature: GetEventSchemaEvolutionReport — rastreamento da evolução de schemas de eventos AsyncAPI/Kafka.
///
/// Para cada contrato AsyncAPI/evento no período, calcula:
/// - <c>TotalSchemaChanges</c>: total de changelogs registados
/// - <c>BreakingSchemaChanges</c>: changelogs com breaking change
/// - <c>BreakingChangeRate</c>: % de mudanças breaking
/// - <c>ActiveConsumersOnOldVersion</c>: consumidores em versão anterior à corrente
/// - <c>SchemaLagDays</c>: dias de atraso do consumidor mais atrasado
///
/// <c>EventSchemaStabilityTier</c>:
/// - <c>Stable</c>     — BreakingChangeRate &lt; 5%
/// - <c>Evolving</c>   — BreakingChangeRate &lt; 20%
/// - <c>Volatile</c>   — BreakingChangeRate &lt; 50%
/// - <c>Unstable</c>   — BreakingChangeRate ≥ 50%
///
/// <c>MigrationLag flag</c> — contratos com <c>ActiveConsumersOnOldVersion > 0</c>
/// e <c>SchemaLagDays > LagAlertDays</c>.
///
/// Wave AH.1 — GetEventSchemaEvolutionReport (Catalog Contracts).
/// </summary>
public static class GetEventSchemaEvolutionReport
{
    // ── Tier thresholds (BreakingChangeRate %) ────────────────────────────
    private const double StableMaxRate = 5.0;
    private const double EvolvingMaxRate = 20.0;
    private const double VolatileMaxRate = 50.0;

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>LookbackDays</c>: janela temporal de análise em dias (1–365, default 90).</para>
    /// <para><c>LagAlertDays</c>: dias de SchemaLag para activar MigrationLag flag (1–365, default 30).</para>
    /// <para><c>MaxContracts</c>: máximo de contratos no relatório (10–500, default 200).</para>
    /// <para><c>TopUnstableCount</c>: número máximo de contratos mais instáveis a listar (1–50, default 10).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int LookbackDays = 90,
        int LagAlertDays = 30,
        int MaxContracts = 200,
        int TopUnstableCount = 10) : IQuery<Report>;

    /// <summary>Tier de estabilidade do schema de um evento.</summary>
    public enum EventSchemaStabilityTier
    {
        /// <summary>BreakingChangeRate &lt; 5%.</summary>
        Stable,
        /// <summary>BreakingChangeRate 5–20%.</summary>
        Evolving,
        /// <summary>BreakingChangeRate 20–50%.</summary>
        Volatile,
        /// <summary>BreakingChangeRate ≥ 50%.</summary>
        Unstable
    }

    /// <summary>Entrada de evolução de schema de um contrato de evento.</summary>
    public sealed record EventSchemaEntry(
        string ContractId,
        string EventName,
        string ProducerServiceName,
        string CurrentSchemaVersion,
        int TotalSchemaChanges,
        int BreakingSchemaChanges,
        double BreakingChangeRate,
        int ActiveConsumersOnOldVersion,
        double SchemaLagDays,
        EventSchemaStabilityTier StabilityTier,
        bool MigrationLag);

    /// <summary>Sumário de saúde de schemas de eventos do tenant.</summary>
    public sealed record TenantEventSchemaHealthSummary(
        int TotalContracts,
        int StableCount,
        int EvolvingCount,
        int VolatileCount,
        int UnstableCount,
        int MigrationLagCount,
        double AvgBreakingChangeRate);

    /// <summary>Relatório de evolução de schemas de eventos.</summary>
    public sealed record Report(
        string TenantId,
        int LookbackDays,
        IReadOnlyList<EventSchemaEntry> AllContracts,
        IReadOnlyList<EventSchemaEntry> TopUnstableContracts,
        IReadOnlyList<EventSchemaEntry> MigrationLagContracts,
        TenantEventSchemaHealthSummary HealthSummary);

    internal sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(q => q.LookbackDays).InclusiveBetween(1, 365);
            RuleFor(q => q.LagAlertDays).InclusiveBetween(1, 365);
            RuleFor(q => q.MaxContracts).InclusiveBetween(10, 500);
            RuleFor(q => q.TopUnstableCount).InclusiveBetween(1, 50);
        }
    }

    internal sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly IEventSchemaEvolutionReader _reader;

        public Handler(IEventSchemaEvolutionReader reader)
        {
            _reader = Guard.Against.Null(reader);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken ct)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var rawEntries = await _reader.ListByTenantAsync(query.TenantId, query.LookbackDays, ct);

            var entries = rawEntries
                .Take(query.MaxContracts)
                .Select(e => MapEntry(e, query.LagAlertDays))
                .ToList();

            var topUnstable = entries
                .Where(e => e.BreakingChangeRate > 0)
                .OrderByDescending(e => e.BreakingChangeRate)
                .ThenByDescending(e => e.BreakingSchemaChanges)
                .Take(query.TopUnstableCount)
                .ToList();

            var migrationLag = entries
                .Where(e => e.MigrationLag)
                .OrderByDescending(e => e.SchemaLagDays)
                .ToList();

            var summary = BuildSummary(entries);

            return Result<Report>.Success(new Report(
                query.TenantId,
                query.LookbackDays,
                entries,
                topUnstable,
                migrationLag,
                summary));
        }

        private static EventSchemaEntry MapEntry(EventSchemaEvolutionEntry e, int lagAlertDays)
        {
            var breakingRate = e.TotalSchemaChanges > 0
                ? Math.Round((double)e.BreakingSchemaChanges / e.TotalSchemaChanges * 100.0, 2)
                : 0.0;

            var tier = ClassifyTier(breakingRate);
            var migrationLag = e.ActiveConsumersOnOldVersion > 0 && e.SchemaLagDays > lagAlertDays;

            return new EventSchemaEntry(
                e.ContractId,
                e.EventName,
                e.ProducerServiceName,
                e.CurrentSchemaVersion,
                e.TotalSchemaChanges,
                e.BreakingSchemaChanges,
                breakingRate,
                e.ActiveConsumersOnOldVersion,
                e.SchemaLagDays,
                tier,
                migrationLag);
        }

        private static EventSchemaStabilityTier ClassifyTier(double breakingRate) => breakingRate switch
        {
            < StableMaxRate   => EventSchemaStabilityTier.Stable,
            < EvolvingMaxRate => EventSchemaStabilityTier.Evolving,
            < VolatileMaxRate => EventSchemaStabilityTier.Volatile,
            _                 => EventSchemaStabilityTier.Unstable
        };

        private static TenantEventSchemaHealthSummary BuildSummary(List<EventSchemaEntry> entries)
        {
            if (entries.Count == 0)
                return new TenantEventSchemaHealthSummary(0, 0, 0, 0, 0, 0, 0.0);

            var stable   = entries.Count(e => e.StabilityTier == EventSchemaStabilityTier.Stable);
            var evolving = entries.Count(e => e.StabilityTier == EventSchemaStabilityTier.Evolving);
            var volatile_ = entries.Count(e => e.StabilityTier == EventSchemaStabilityTier.Volatile);
            var unstable = entries.Count(e => e.StabilityTier == EventSchemaStabilityTier.Unstable);
            var lagCount = entries.Count(e => e.MigrationLag);
            var avgRate  = Math.Round(entries.Average(e => e.BreakingChangeRate), 2);

            return new TenantEventSchemaHealthSummary(
                entries.Count,
                stable,
                evolving,
                volatile_,
                unstable,
                lagCount,
                avgRate);
        }
    }
}
