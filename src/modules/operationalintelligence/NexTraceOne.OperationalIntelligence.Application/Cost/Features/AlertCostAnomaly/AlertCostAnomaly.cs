using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.CostIntelligence.Application.Abstractions;
using NexTraceOne.CostIntelligence.Domain.Entities;
using NexTraceOne.CostIntelligence.Domain.Errors;
using NexTraceOne.CostIntelligence.Domain.Events;

namespace NexTraceOne.CostIntelligence.Application.Features.AlertCostAnomaly;

/// <summary>
/// Feature: AlertCostAnomaly — verifica se o custo de um serviço excedeu o limiar de alerta.
/// Atualiza o custo corrente no perfil do serviço, verifica o orçamento e,
/// se o limiar for atingido, publica um evento de anomalia de custo via EventBus.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class AlertCostAnomaly
{
    /// <summary>Comando para verificar e alertar anomalia de custo de um serviço.</summary>
    public sealed record Command(
        string ServiceName,
        string Environment,
        decimal CurrentCost) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de alerta de anomalia de custo.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.CurrentCost).GreaterThanOrEqualTo(0);
        }
    }

    /// <summary>
    /// Handler que verifica se o custo corrente excede o limiar configurado no perfil do serviço.
    /// Atualiza o custo no perfil e, se o orçamento for excedido, publica CostAnomalyDetectedEvent.
    /// </summary>
    public sealed class Handler(
        IServiceCostProfileRepository profileRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IEventBus eventBus) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var profile = await profileRepository.GetByServiceAndEnvironmentAsync(
                request.ServiceName,
                request.Environment,
                cancellationToken);

            if (profile is null)
                return CostIntelligenceErrors.ProfileNotFound($"{request.ServiceName}/{request.Environment}");

            var now = dateTimeProvider.UtcNow;

            var updateResult = profile.UpdateCurrentCost(request.CurrentCost, now);
            if (updateResult.IsFailure)
                return updateResult.Error;

            var budgetCheck = profile.CheckBudgetAlert();
            var isAnomalyDetected = budgetCheck.IsFailure;

            if (isAnomalyDetected)
            {
                var anomalyEvent = new CostAnomalyDetectedEvent(
                    Guid.NewGuid(),
                    request.ServiceName,
                    profile.MonthlyBudget ?? 0m,
                    request.CurrentCost,
                    now);

                await eventBus.PublishAsync(anomalyEvent, cancellationToken);
            }

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                profile.Id.Value,
                request.ServiceName,
                request.Environment,
                request.CurrentCost,
                profile.MonthlyBudget,
                profile.AlertThresholdPercent,
                isAnomalyDetected);
        }
    }

    /// <summary>Resposta da verificação de anomalia de custo com indicação de alerta.</summary>
    public sealed record Response(
        Guid ProfileId,
        string ServiceName,
        string Environment,
        decimal CurrentCost,
        decimal? MonthlyBudget,
        decimal AlertThresholdPercent,
        bool IsAnomalyDetected);
}
