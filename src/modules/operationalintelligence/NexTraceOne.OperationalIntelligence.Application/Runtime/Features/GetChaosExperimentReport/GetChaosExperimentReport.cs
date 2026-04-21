using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetChaosExperimentReport;

/// <summary>
/// Feature: GetChaosExperimentReport — relatório analítico de experimentos de chaos engineering.
///
/// Agrega todos os experimentos do tenant num período, calculando:
/// - taxa de sucesso (Completed vs Completed+Failed)
/// - distribuição por tipo de experimento
/// - distribuição por nível de risco
/// - distribuição por estado
/// - top serviços mais testados
///
/// Complementa CreateChaosExperiment + ListChaosExperiments com visibilidade analítica.
/// Wave K.1 — Chaos Engineering Analytics (OperationalIntelligence).
/// </summary>
public static class GetChaosExperimentReport
{
    public sealed record Query(
        string TenantId,
        int Days = 30,
        string? ServiceName = null,
        string? Environment = null) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Days).InclusiveBetween(1, 90);
            RuleFor(x => x.ServiceName).MaximumLength(200).When(x => x.ServiceName is not null);
            RuleFor(x => x.Environment).MaximumLength(100).When(x => x.Environment is not null);
        }
    }

    public sealed class Handler(
        IChaosExperimentRepository repository,
        IDateTimeProvider clock) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = clock.UtcNow;
            var since = now.AddDays(-request.Days);

            var all = await repository.ListAsync(
                request.TenantId,
                request.ServiceName,
                request.Environment,
                null,
                cancellationToken);

            // Filtrar por janela temporal (com base em CreatedAt)
            var experiments = all
                .Where(e => e.CreatedAt >= since)
                .ToList();

            var total = experiments.Count;
            var completed = experiments.Count(e => e.Status == ExperimentStatus.Completed);
            var failed = experiments.Count(e => e.Status == ExperimentStatus.Failed);
            var cancelled = experiments.Count(e => e.Status == ExperimentStatus.Cancelled);
            var running = experiments.Count(e => e.Status == ExperimentStatus.Running);
            var planned = experiments.Count(e => e.Status == ExperimentStatus.Planned);

            var successRate = (completed + failed) > 0
                ? Math.Round((decimal)completed / (completed + failed) * 100, 1)
                : 0m;

            var byType = experiments
                .GroupBy(e => e.ExperimentType)
                .Select(g => new ExperimentTypeCount(g.Key, g.Count()))
                .OrderByDescending(x => x.Count)
                .ToList();

            var byRisk = experiments
                .GroupBy(e => e.RiskLevel)
                .Select(g => new RiskLevelCount(g.Key, g.Count()))
                .OrderByDescending(x => x.Count)
                .ToList();

            var byStatus = new StatusDistribution(
                Planned: planned,
                Running: running,
                Completed: completed,
                Failed: failed,
                Cancelled: cancelled);

            var topServices = experiments
                .GroupBy(e => e.ServiceName)
                .Select(g => new ServiceExperimentCount(g.Key, g.Count()))
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            var avgDuration = total > 0
                ? Math.Round(experiments.Average(e => (decimal)e.DurationSeconds), 1)
                : 0m;

            var mostRecentAt = experiments
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => (DateTimeOffset?)e.CreatedAt)
                .FirstOrDefault();

            return Result<Response>.Success(new Response(
                GeneratedAt: now,
                PeriodDays: request.Days,
                TenantId: request.TenantId,
                ServiceFilter: request.ServiceName,
                EnvironmentFilter: request.Environment,
                TotalExperiments: total,
                SuccessRatePercent: successRate,
                AverageDurationSeconds: avgDuration,
                ByType: byType,
                ByRiskLevel: byRisk,
                ByStatus: byStatus,
                TopServices: topServices,
                MostRecentExperimentAt: mostRecentAt));
        }
    }

    public sealed record ExperimentTypeCount(string ExperimentType, int Count);
    public sealed record RiskLevelCount(string RiskLevel, int Count);
    public sealed record ServiceExperimentCount(string ServiceName, int Count);

    public sealed record StatusDistribution(
        int Planned,
        int Running,
        int Completed,
        int Failed,
        int Cancelled);

    public sealed record Response(
        DateTimeOffset GeneratedAt,
        int PeriodDays,
        string TenantId,
        string? ServiceFilter,
        string? EnvironmentFilter,
        int TotalExperiments,
        decimal SuccessRatePercent,
        decimal AverageDurationSeconds,
        IReadOnlyList<ExperimentTypeCount> ByType,
        IReadOnlyList<RiskLevelCount> ByRiskLevel,
        StatusDistribution ByStatus,
        IReadOnlyList<ServiceExperimentCount> TopServices,
        DateTimeOffset? MostRecentExperimentAt);
}
