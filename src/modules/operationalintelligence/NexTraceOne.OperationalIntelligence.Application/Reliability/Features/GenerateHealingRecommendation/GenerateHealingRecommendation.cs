using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.OperationalIntelligence.Application.Reliability.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Entities;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

namespace NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GenerateHealingRecommendation;

/// <summary>
/// Feature: GenerateHealingRecommendation — gera uma recomendação de self-healing
/// para um serviço afectado, com base na causa raiz identificada e padrões históricos.
/// A recomendação é criada no estado Proposed e requer aprovação antes da execução.
///
/// Pilar: Operational Reliability + Self-Healing Recommendations.
/// Ideia 7 — Self-Healing Recommendations.
/// </summary>
public static class GenerateHealingRecommendation
{
    /// <summary>Comando para gerar uma recomendação de self-healing.</summary>
    public sealed record Command(
        string ServiceName,
        string Environment,
        Guid? IncidentId,
        string RootCauseDescription,
        string ActionType,
        string ActionDetails,
        int ConfidenceScore,
        string? EstimatedImpact = null,
        string? RelatedRunbookIds = null,
        decimal? HistoricalSuccessRate = null) : ICommand<Response>;

    /// <summary>Valida os parâmetros do comando de geração de recomendação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        private static readonly string[] ValidActionTypes = Enum.GetNames<HealingActionType>();

        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.RootCauseDescription).NotEmpty().MaximumLength(2000);
            RuleFor(x => x.ActionType).NotEmpty()
                .Must(t => ValidActionTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
                .WithMessage($"Valid action types: {string.Join(", ", ValidActionTypes)}");
            RuleFor(x => x.ActionDetails).NotEmpty();
            RuleFor(x => x.ConfidenceScore).InclusiveBetween(0, 100);
            RuleFor(x => x.HistoricalSuccessRate)
                .InclusiveBetween(0m, 100m)
                .When(x => x.HistoricalSuccessRate.HasValue);
        }
    }

    /// <summary>
    /// Handler que cria e persiste a recomendação de self-healing.
    /// Resolve o ActionType a partir da string, aplica tenant e timestamp,
    /// e persiste via IHealingRecommendationRepository + IUnitOfWork.
    /// </summary>
    public sealed class Handler(
        IHealingRecommendationRepository repository,
        ICurrentTenant currentTenant,
        IDateTimeProvider dateTimeProvider,
        IReliabilityUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!Enum.TryParse<HealingActionType>(request.ActionType, true, out var actionType))
                return Error.Validation("INVALID_ACTION_TYPE", $"Invalid action type: {request.ActionType}");

            var now = dateTimeProvider.UtcNow;

            var recommendation = HealingRecommendation.Generate(
                request.ServiceName,
                request.Environment,
                request.IncidentId,
                request.RootCauseDescription,
                actionType,
                request.ActionDetails,
                request.ConfidenceScore,
                request.EstimatedImpact,
                request.RelatedRunbookIds,
                request.HistoricalSuccessRate,
                currentTenant.Id,
                now);

            repository.Add(recommendation);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                recommendation.Id.Value,
                recommendation.ServiceName,
                recommendation.Environment,
                recommendation.IncidentId,
                recommendation.RootCauseDescription,
                recommendation.ActionType.ToString(),
                recommendation.ConfidenceScore,
                recommendation.HistoricalSuccessRate,
                recommendation.Status.ToString(),
                recommendation.GeneratedAt));
        }
    }

    /// <summary>Resposta com os dados da recomendação gerada.</summary>
    public sealed record Response(
        Guid RecommendationId,
        string ServiceName,
        string Environment,
        Guid? IncidentId,
        string RootCauseDescription,
        string ActionType,
        int ConfidenceScore,
        decimal? HistoricalSuccessRate,
        string Status,
        DateTimeOffset GeneratedAt);
}
