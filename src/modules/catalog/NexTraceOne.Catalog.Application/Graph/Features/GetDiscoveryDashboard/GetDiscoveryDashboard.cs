using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Enums;

namespace NexTraceOne.Catalog.Application.Graph.Features.GetDiscoveryDashboard;

/// <summary>
/// Feature: GetDiscoveryDashboard — obtém estatísticas agregadas do discovery.
/// Stats: total discovered, pending, matched, registered, ignored, new this week.
/// Alimenta o dashboard card na ServiceDiscoveryPage e insight card no catálogo.
/// </summary>
public static class GetDiscoveryDashboard
{
    /// <summary>Query sem parâmetros obrigatórios.</summary>
    public sealed record Query() : IQuery<Response>;

    /// <summary>Validação mínima.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator() { }
    }

    /// <summary>Handler que agrega estatísticas.</summary>
    public sealed class Handler(
        IDiscoveredServiceRepository discoveredServiceRepository,
        IDiscoveryRunRepository discoveryRunRepository,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var pending = await discoveredServiceRepository.CountByStatusAsync(DiscoveryStatus.Pending, cancellationToken);
            var matched = await discoveredServiceRepository.CountByStatusAsync(DiscoveryStatus.Matched, cancellationToken);
            var registered = await discoveredServiceRepository.CountByStatusAsync(DiscoveryStatus.Registered, cancellationToken);
            var ignored = await discoveredServiceRepository.CountByStatusAsync(DiscoveryStatus.Ignored, cancellationToken);
            var total = pending + matched + registered + ignored;

            var oneWeekAgo = dateTimeProvider.UtcNow.AddDays(-7);
            var newThisWeek = await discoveredServiceRepository.CountNewSinceAsync(oneWeekAgo, cancellationToken);

            var recentRuns = await discoveryRunRepository.ListRecentAsync(5, cancellationToken);
            var runItems = recentRuns.Select(r => new RunSummaryItem(
                r.Id.Value,
                r.StartedAt,
                r.CompletedAt,
                r.Source,
                r.Environment,
                r.ServicesFound,
                r.NewServicesFound,
                r.Status)).ToList();

            return new Response(total, pending, matched, registered, ignored, newThisWeek, runItems);
        }
    }

    /// <summary>Resposta do dashboard de discovery.</summary>
    public sealed record Response(
        int TotalDiscovered,
        int Pending,
        int Matched,
        int Registered,
        int Ignored,
        int NewThisWeek,
        IReadOnlyList<RunSummaryItem> RecentRuns);

    /// <summary>Resumo de uma execução de discovery.</summary>
    public sealed record RunSummaryItem(
        Guid RunId,
        DateTimeOffset StartedAt,
        DateTimeOffset? CompletedAt,
        string Source,
        string Environment,
        int ServicesFound,
        int NewServicesFound,
        string Status);
}
