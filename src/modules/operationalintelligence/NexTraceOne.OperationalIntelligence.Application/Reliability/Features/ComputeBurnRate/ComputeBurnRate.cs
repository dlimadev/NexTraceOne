using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Services;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Features.ComputeBurnRate;

/// <summary>
/// Feature: ComputeBurnRate — calcula o burn rate real de um SLO para todas as
/// janelas de tempo definidas em BurnRateWindow e persiste os resultados.
///
/// Pipeline de cálculo:
/// 1. Carrega a definição do SLO
/// 2. Obtém o sinal de runtime actual do serviço (ErrorRate do RuntimeSnapshot mais recente)
/// 3. Calcula toleratedErrorRate = 1 − (targetPercent/100)
/// 4. Calcula burnRate = observedErrorRate / toleratedErrorRate
/// 5. Persiste um BurnRateSnapshot por janela de tempo solicitada
///
/// Nota: nesta fase o cálculo é baseado no snapshot mais recente — em P6.3+
/// o cálculo por janela pode usar médias ou agregados históricos do ClickHouse.
/// </summary>
public static class ComputeBurnRate
{
    public sealed record Command(
        Guid SloDefinitionId,
        BurnRateWindow? Window = null) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.SloDefinitionId).NotEmpty();
            RuleFor(x => x.Window).IsInEnum().When(x => x.Window.HasValue);
        }
    }

    public sealed class Handler(
        ISloDefinitionRepository sloRepository,
        IBurnRateSnapshotRepository burnRateRepository,
        IReliabilityRuntimeSurface runtimeSurface,
        IErrorBudgetCalculator calculator,
        ICurrentTenant tenant,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var sloId = SloDefinitionId.From(request.SloDefinitionId);
            var slo = await sloRepository.GetByIdAsync(sloId, tenant.Id, cancellationToken);

            if (slo is null)
            {
                Result<Response> notFound = Error.NotFound("Reliability.SloNotFound",
                    "SLO definition '{0}' not found", request.SloDefinitionId);
                return notFound;
            }

            var signal = await runtimeSurface.GetLatestSignalAsync(slo.ServiceId, slo.Environment, cancellationToken);
            var observedErrorRate   = signal?.ErrorRate ?? 0m;
            var toleratedErrorRate  = calculator.ComputeToleratedErrorRate(slo);
            var burnRateValue       = calculator.ComputeBurnRate(slo, observedErrorRate);
            var now                 = clock.UtcNow;

            // Calcula e persiste para todas as janelas ou apenas a janela solicitada
            var windows = request.Window.HasValue
                ? new[] { request.Window.Value }
                : Enum.GetValues<BurnRateWindow>();

            var snapshots = new List<BurnRateSnapshotResult>(windows.Length);

            foreach (var window in windows)
            {
                var snapshot = BurnRateSnapshot.Create(
                    tenant.Id,
                    sloId,
                    slo.ServiceId,
                    slo.Environment,
                    window,
                    observedErrorRate,
                    toleratedErrorRate,
                    now);

                await burnRateRepository.AddAsync(snapshot, cancellationToken);

                snapshots.Add(new BurnRateSnapshotResult(
                    window,
                    snapshot.BurnRate,
                    snapshot.Status));
            }

            return Result<Response>.Success(new Response(
                SloDefinitionId:  slo.Id.Value,
                SloName:          slo.Name,
                ServiceId:        slo.ServiceId,
                Environment:      slo.Environment,
                ObservedErrorRate:  observedErrorRate,
                ToleratedErrorRate: toleratedErrorRate,
                Snapshots:          snapshots,
                ComputedAt:       now));
        }
    }

    public sealed record BurnRateSnapshotResult(
        BurnRateWindow Window,
        decimal BurnRate,
        SloStatus Status);

    public sealed record Response(
        Guid SloDefinitionId,
        string SloName,
        string ServiceId,
        string Environment,
        decimal ObservedErrorRate,
        decimal ToleratedErrorRate,
        IReadOnlyList<BurnRateSnapshotResult> Snapshots,
        DateTimeOffset ComputedAt);
}
