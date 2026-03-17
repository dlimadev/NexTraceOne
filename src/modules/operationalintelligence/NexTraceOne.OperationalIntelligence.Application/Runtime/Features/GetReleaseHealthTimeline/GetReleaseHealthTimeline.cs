using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Features.GetReleaseHealthTimeline;

/// <summary>
/// Feature: GetReleaseHealthTimeline — obtém snapshots de saúde em torno de uma janela de release.
/// Busca todos os snapshots capturados entre o início e fim do período da release,
/// permitindo visualizar a evolução de saúde do serviço antes, durante e após o deploy.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class GetReleaseHealthTimeline
{
    /// <summary>Query para obter a timeline de saúde de um serviço durante uma janela de release.</summary>
    public sealed record Query(
        string ServiceName,
        string Environment,
        DateTimeOffset WindowStart,
        DateTimeOffset WindowEnd) : IQuery<Response>;

    /// <summary>Valida os parâmetros de consulta de timeline de saúde por release.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.WindowStart).NotEmpty();
            RuleFor(x => x.WindowEnd).NotEmpty()
                .GreaterThan(x => x.WindowStart)
                .WithMessage("Window end must be after window start.");
        }
    }

    /// <summary>
    /// Handler que busca snapshots no período da release e ordena cronologicamente.
    /// Carrega uma página grande de snapshots e filtra pelo período informado para montar a timeline.
    /// </summary>
    public sealed class Handler(
        IRuntimeSnapshotRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var snapshots = await repository.ListByServiceAsync(
                request.ServiceName,
                request.Environment,
                1,
                1000,
                cancellationToken);

            // Filtra snapshots dentro da janela de release e ordena cronologicamente
            var timelineSnapshots = snapshots
                .Where(s => s.CapturedAt >= request.WindowStart && s.CapturedAt <= request.WindowEnd)
                .OrderBy(s => s.CapturedAt)
                .ToList();

            var items = timelineSnapshots.Select(s => new HealthTimelineItem(
                s.Id.Value,
                s.HealthStatus.ToString(),
                s.AvgLatencyMs,
                s.P99LatencyMs,
                s.ErrorRate,
                s.RequestsPerSecond,
                s.CpuUsagePercent,
                s.MemoryUsageMb,
                s.ActiveInstances,
                s.CapturedAt)).ToList();

            return new Response(
                request.ServiceName,
                request.Environment,
                request.WindowStart,
                request.WindowEnd,
                items,
                items.Count);
        }
    }

    /// <summary>Resposta com a timeline de snapshots de saúde durante a janela de release.</summary>
    public sealed record Response(
        string ServiceName,
        string Environment,
        DateTimeOffset WindowStart,
        DateTimeOffset WindowEnd,
        IReadOnlyList<HealthTimelineItem> Timeline,
        int DataPointCount);

    /// <summary>Item individual de snapshot na timeline de saúde.</summary>
    public sealed record HealthTimelineItem(
        Guid SnapshotId,
        string HealthStatus,
        decimal AvgLatencyMs,
        decimal P99LatencyMs,
        decimal ErrorRate,
        decimal RequestsPerSecond,
        decimal CpuUsagePercent,
        decimal MemoryUsageMb,
        int ActiveInstances,
        DateTimeOffset CapturedAt);
}
