using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Application.Cost.Features.GetWasteReport;

/// <summary>
/// Feature: GetWasteReport — relatório consolidado de sinais de desperdício operacional.
/// Agrega sinais por tipo e equipa, ordenados por impacto financeiro potencial.
/// </summary>
public static class GetWasteReport
{
    public sealed record Query(
        string? TeamName = null,
        bool IncludeAcknowledged = false) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.TeamName).MaximumLength(200).When(x => x.TeamName is not null);
        }
    }

    public sealed class Handler(IWasteSignalRepository repo) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var signals = await repo.ListAllAsync(request.TeamName, request.IncludeAcknowledged, cancellationToken);

            var grouped = signals
                .GroupBy(s => s.SignalType)
                .Select(g => new WasteGroupDto(
                    Type: g.Key.ToString(),
                    Count: g.Count(),
                    TotalEstimatedSavings: Math.Round(g.Sum(s => s.EstimatedMonthlySavings), 2),
                    Services: g.Select(s => s.ServiceName).Distinct().ToList()))
                .OrderByDescending(g => g.TotalEstimatedSavings)
                .ToList();

            var totalSavings = Math.Round(signals.Sum(s => s.EstimatedMonthlySavings), 2);
            var acknowledgedCount = signals.Count(s => s.IsAcknowledged);

            return Result<Response>.Success(new Response(
                TotalSignals: signals.Count,
                AcknowledgedSignals: acknowledgedCount,
                PendingSignals: signals.Count - acknowledgedCount,
                TotalEstimatedMonthlySavings: totalSavings,
                ByType: grouped,
                Signals: signals.Select(s => new WasteSignalDto(
                    s.Id.Value.ToString(),
                    s.ServiceName,
                    s.Environment,
                    s.TeamName,
                    s.SignalType.ToString(),
                    s.EstimatedMonthlySavings,
                    s.Currency,
                    s.Description,
                    s.IsAcknowledged,
                    s.DetectedAt)).ToList()));
        }
    }

    public sealed record Response(
        int TotalSignals,
        int AcknowledgedSignals,
        int PendingSignals,
        decimal TotalEstimatedMonthlySavings,
        IReadOnlyList<WasteGroupDto> ByType,
        IReadOnlyList<WasteSignalDto> Signals);

    public sealed record WasteGroupDto(string Type, int Count, decimal TotalEstimatedSavings, IReadOnlyList<string> Services);

    public sealed record WasteSignalDto(
        string Id, string ServiceName, string Environment, string? TeamName,
        string Type, decimal EstimatedMonthlySavings, string Currency,
        string Description, bool IsAcknowledged, DateTimeOffset DetectedAt);
}
