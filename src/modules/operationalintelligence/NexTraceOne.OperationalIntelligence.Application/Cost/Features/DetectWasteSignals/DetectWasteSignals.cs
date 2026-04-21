using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Cost.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Cost.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Cost.Features.DetectWasteSignals;

/// <summary>
/// Feature: DetectWasteSignals — analisa perfis de custo e registos de utilização para
/// detetar sinais de desperdício operacional e persistir os findings para ação.
/// Owner: OI Cost module. Pilar: FinOps contextual.
/// </summary>
public static class DetectWasteSignals
{
    public sealed record Command(
        string ServiceName,
        string Environment,
        string? TeamName = null,
        string Currency = "USD") : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
        }
    }

    public sealed class Handler(
        IServiceCostProfileRepository profileRepo,
        ICostRecordRepository costRecordRepo,
        IWasteSignalRepository wasteSignalRepo,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var profile = await profileRepo.GetByServiceAndEnvironmentAsync(request.ServiceName, request.Environment, cancellationToken);
            var records = await costRecordRepo.ListByServiceAsync(request.ServiceName, cancellationToken: cancellationToken);

            var signals = new List<WasteSignal>();
            var now = clock.UtcNow;

            // ── Heurística 1: Over-budget service with no trend reduction ──────
            if (profile is not null && profile.IsOverBudget && profile.MonthlyBudget.HasValue)
            {
                var overagePct = (profile.CurrentMonthCost - profile.MonthlyBudget.Value) / profile.MonthlyBudget.Value * 100m;
                if (overagePct > 20m)
                {
                    var signal = WasteSignal.Create(
                        request.ServiceName,
                        request.Environment,
                        WasteSignalType.Overprovisioned,
                        Math.Round(profile.CurrentMonthCost - profile.MonthlyBudget.Value, 2),
                        $"Service '{request.ServiceName}' is {overagePct:N1}% over budget in {request.Environment}.",
                        now,
                        request.TeamName,
                        request.Currency);
                    signals.Add(signal);
                }
            }

            // ── Heurística 2: Zero-cost records in recent period (idle) ────────
            var recentRecords = records
                .Where(r => r.Environment == request.Environment)
                .OrderByDescending(r => r.Period)
                .Take(3)
                .ToList();

            if (recentRecords.Count >= 2 && recentRecords.All(r => r.TotalCost == 0m))
            {
                var signal = WasteSignal.Create(
                    request.ServiceName,
                    request.Environment,
                    WasteSignalType.IdleResources,
                    0m,
                    $"Service '{request.ServiceName}' has zero cost in recent periods — may be idle.",
                    now,
                    request.TeamName,
                    request.Currency);
                signals.Add(signal);
            }

            // ── Persist signals ───────────────────────────────────────────────
            foreach (var signal in signals)
                await wasteSignalRepo.AddAsync(signal, cancellationToken);

            return Result<Response>.Success(new Response(
                ServiceName: request.ServiceName,
                Environment: request.Environment,
                DetectedCount: signals.Count,
                Signals: signals.Select(s => new WasteSignalDto(
                    s.Id.Value.ToString(),
                    s.SignalType.ToString(),
                    s.EstimatedMonthlySavings,
                    s.Currency,
                    s.Description,
                    s.DetectedAt)).ToList()));
        }
    }

    public sealed record Response(
        string ServiceName,
        string Environment,
        int DetectedCount,
        IReadOnlyList<WasteSignalDto> Signals);

    public sealed record WasteSignalDto(
        string Id,
        string Type,
        decimal EstimatedMonthlySavings,
        string Currency,
        string Description,
        DateTimeOffset DetectedAt);
}
