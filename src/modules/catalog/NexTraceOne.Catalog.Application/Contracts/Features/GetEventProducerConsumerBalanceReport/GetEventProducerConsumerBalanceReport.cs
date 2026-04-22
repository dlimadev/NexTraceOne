using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetEventProducerConsumerBalanceReport;

/// <summary>
/// Feature: GetEventProducerConsumerBalanceReport — análise de equilíbrio produtor-consumidor de eventos.
///
/// Para cada contrato de evento registado no tenant, calcula:
/// - <c>ProducerCount</c>: serviços que produzem o evento
/// - <c>ConsumerCount</c>: serviços que consomem o evento
/// - <c>IsOrphaned</c>: <c>ConsumerCount = 0</c> e contrato activo (produzido sem utilidade registada)
/// - <c>IsBlind</c>: <c>ProducerCount = 0</c> e <c>ConsumerCount > 0</c> (dependência sem produtor)
/// - <c>FanOutRisk</c>: <c>ConsumerCount ≥ FanOutThreshold</c> (blast radius elevado)
///
/// Identifica problemas estruturais na arquitectura de eventos invisíveis sem um catálogo centralizado.
///
/// Wave AH.2 — GetEventProducerConsumerBalanceReport (Catalog Contracts).
/// </summary>
public static class GetEventProducerConsumerBalanceReport
{
    private const int DefaultFanOutThreshold = 10;

    /// <summary>
    /// <para><c>TenantId</c>: identificador do tenant (obrigatório).</para>
    /// <para><c>FanOutThreshold</c>: número mínimo de consumidores para activar FanOutRisk (2–100, default 10).</para>
    /// <para><c>MaxContracts</c>: máximo de contratos no relatório (10–500, default 200).</para>
    /// <para><c>TopFanOutCount</c>: número máximo de eventos de alto fan-out a listar (1–50, default 10).</para>
    /// </summary>
    public sealed record Query(
        string TenantId,
        int FanOutThreshold = DefaultFanOutThreshold,
        int MaxContracts = 200,
        int TopFanOutCount = 10) : IQuery<Report>;

    /// <summary>Entrada de equilíbrio produtor-consumidor de um evento.</summary>
    public sealed record EventBalanceReportEntry(
        string ContractId,
        string EventName,
        int ProducerCount,
        int ConsumerCount,
        bool IsOrphaned,
        bool IsBlind,
        bool FanOutRisk);

    /// <summary>Sumário global de equilíbrio de eventos do tenant.</summary>
    public sealed record BalanceSummary(
        int TotalEvents,
        int OrphanedCount,
        int BlindConsumerCount,
        int FanOutRiskCount,
        double OrphanedPct,
        double BlindConsumerPct,
        double FanOutRiskPct);

    /// <summary>Relatório de equilíbrio produtor-consumidor de eventos.</summary>
    public sealed record Report(
        string TenantId,
        IReadOnlyList<EventBalanceReportEntry> AllEvents,
        IReadOnlyList<EventBalanceReportEntry> OrphanedEvents,
        IReadOnlyList<EventBalanceReportEntry> BlindConsumers,
        IReadOnlyList<EventBalanceReportEntry> HighFanOutEvents,
        BalanceSummary Summary);

    internal sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(q => q.TenantId).NotEmpty().MaximumLength(200);
            RuleFor(q => q.FanOutThreshold).InclusiveBetween(2, 100);
            RuleFor(q => q.MaxContracts).InclusiveBetween(10, 500);
            RuleFor(q => q.TopFanOutCount).InclusiveBetween(1, 50);
        }
    }

    internal sealed class Handler : IQueryHandler<Query, Report>
    {
        private readonly IEventProducerConsumerReader _reader;

        public Handler(IEventProducerConsumerReader reader)
        {
            _reader = Guard.Against.Null(reader);
        }

        public async Task<Result<Report>> Handle(Query query, CancellationToken ct)
        {
            Guard.Against.NullOrWhiteSpace(query.TenantId);

            var rawEntries = await _reader.ListByTenantAsync(query.TenantId, ct);

            var entries = rawEntries
                .Take(query.MaxContracts)
                .Select(e => MapEntry(e, query.FanOutThreshold))
                .ToList();

            var orphaned = entries
                .Where(e => e.IsOrphaned)
                .OrderBy(e => e.EventName)
                .ToList();

            var blind = entries
                .Where(e => e.IsBlind)
                .OrderBy(e => e.EventName)
                .ToList();

            var highFanOut = entries
                .Where(e => e.FanOutRisk)
                .OrderByDescending(e => e.ConsumerCount)
                .Take(query.TopFanOutCount)
                .ToList();

            var summary = BuildSummary(entries);

            return Result<Report>.Success(new Report(
                query.TenantId,
                entries,
                orphaned,
                blind,
                highFanOut,
                summary));
        }

        private static EventBalanceReportEntry MapEntry(EventBalanceEntry e, int fanOutThreshold)
        {
            var isOrphaned = e.IsActive && e.ConsumerCount == 0;
            var isBlind    = e.ProducerCount == 0 && e.ConsumerCount > 0;
            var fanOutRisk = e.ConsumerCount >= fanOutThreshold;

            return new EventBalanceReportEntry(
                e.ContractId,
                e.EventName,
                e.ProducerCount,
                e.ConsumerCount,
                isOrphaned,
                isBlind,
                fanOutRisk);
        }

        private static BalanceSummary BuildSummary(List<EventBalanceReportEntry> entries)
        {
            int total = entries.Count;
            if (total == 0)
                return new BalanceSummary(0, 0, 0, 0, 0.0, 0.0, 0.0);

            int orphaned = entries.Count(e => e.IsOrphaned);
            int blind    = entries.Count(e => e.IsBlind);
            int fanOut   = entries.Count(e => e.FanOutRisk);

            return new BalanceSummary(
                total,
                orphaned,
                blind,
                fanOut,
                Math.Round((double)orphaned / total * 100.0, 2),
                Math.Round((double)blind    / total * 100.0, 2),
                Math.Round((double)fanOut   / total * 100.0, 2));
        }
    }
}
