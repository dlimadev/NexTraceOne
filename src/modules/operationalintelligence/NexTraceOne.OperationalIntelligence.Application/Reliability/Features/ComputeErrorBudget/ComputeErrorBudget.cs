using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Services;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Features.ComputeErrorBudget;

/// <summary>
/// Feature: ComputeErrorBudget — calcula o error budget real de um SLO a partir de
/// dados de runtime observados e persiste o resultado como ErrorBudgetSnapshot.
///
/// Pipeline de cálculo:
/// 1. Carrega a definição do SLO
/// 2. Obtém o sinal de runtime actual do serviço (ErrorRate do RuntimeSnapshot mais recente)
/// 3. Calcula total_budget_minutes e consumed_budget_minutes
/// 4. Persiste um ErrorBudgetSnapshot com os valores calculados
///
/// O observed_error_rate é obtido de IReliabilityRuntimeSurface, que acede ao
/// RuntimeSnapshot mais recente — fonte de dados real do módulo Operational Intelligence.
/// </summary>
public static class ComputeErrorBudget
{
    public sealed record Command(Guid SloDefinitionId) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.SloDefinitionId).NotEmpty();
        }
    }

    public sealed class Handler(
        ISloDefinitionRepository sloRepository,
        IErrorBudgetSnapshotRepository budgetRepository,
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

            // Obtém sinal de runtime mais recente — fonte do observed error rate
            var signal = await runtimeSurface.GetLatestSignalAsync(slo.ServiceId, slo.Environment, cancellationToken);

            var observedErrorRate = signal?.ErrorRate ?? 0m;
            var totalBudget    = calculator.ComputeTotalBudgetMinutes(slo);
            var consumedBudget = calculator.ComputeConsumedBudgetMinutes(slo, observedErrorRate);

            var snapshot = ErrorBudgetSnapshot.Create(
                tenant.Id,
                sloId,
                slo.ServiceId,
                slo.Environment,
                totalBudget,
                consumedBudget,
                clock.UtcNow);

            await budgetRepository.AddAsync(snapshot, cancellationToken);

            return Result<Response>.Success(new Response(
                SloDefinitionId:       slo.Id.Value,
                SloName:               slo.Name,
                ServiceId:             slo.ServiceId,
                Environment:           slo.Environment,
                TargetPercent:         slo.TargetPercent,
                WindowDays:            slo.WindowDays,
                ObservedErrorRate:     observedErrorRate,
                TotalBudgetMinutes:    snapshot.TotalBudgetMinutes,
                ConsumedBudgetMinutes: snapshot.ConsumedBudgetMinutes,
                RemainingBudgetMinutes:snapshot.RemainingBudgetMinutes,
                ConsumedPercent:       snapshot.ConsumedPercent,
                Status:                snapshot.Status,
                ComputedAt:            snapshot.ComputedAt));
        }
    }

    public sealed record Response(
        Guid SloDefinitionId,
        string SloName,
        string ServiceId,
        string Environment,
        decimal TargetPercent,
        int WindowDays,
        decimal ObservedErrorRate,
        decimal TotalBudgetMinutes,
        decimal ConsumedBudgetMinutes,
        decimal RemainingBudgetMinutes,
        decimal ConsumedPercent,
        SloStatus Status,
        DateTimeOffset ComputedAt);
}
