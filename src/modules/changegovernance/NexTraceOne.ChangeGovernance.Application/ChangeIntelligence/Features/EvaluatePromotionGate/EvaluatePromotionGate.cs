using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.EvaluatePromotionGate;

/// <summary>
/// Feature: EvaluatePromotionGate — avalia um gate de promoção contra uma mudança específica.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class EvaluatePromotionGate
{
    /// <summary>Comando para avaliar um gate de promoção contra uma mudança.</summary>
    public sealed record Command(
        Guid GateId,
        string ChangeId,
        GateEvaluationResult Result,
        string? RuleResults) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de avaliação de gate de promoção.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.GateId).NotEmpty();
            RuleFor(x => x.ChangeId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Result).IsInEnum();
        }
    }

    /// <summary>
    /// Handler que avalia um gate de promoção contra uma mudança e persiste o resultado.
    /// </summary>
    public sealed class Handler(
        IPromotionGateRepository gateRepository,
        IPromotionGateEvaluationRepository evaluationRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        ICurrentUser currentUser) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var gateId = PromotionGateId.From(request.GateId);
            var gate = await gateRepository.GetByIdAsync(gateId, cancellationToken);
            if (gate is null)
                return PromotionGateErrors.GateNotFound(request.GateId.ToString());

            var evaluation = PromotionGateEvaluation.Evaluate(
                gateId,
                request.ChangeId,
                request.Result,
                request.RuleResults,
                dateTimeProvider.UtcNow,
                currentUser.Id,
                null);

            await evaluationRepository.AddAsync(evaluation, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                evaluation.Id.Value,
                evaluation.GateId.Value,
                evaluation.ChangeId,
                evaluation.Result,
                evaluation.EvaluatedAt);
        }
    }

    /// <summary>Resposta da avaliação de gate de promoção.</summary>
    public sealed record Response(
        Guid EvaluationId,
        Guid GateId,
        string ChangeId,
        GateEvaluationResult Result,
        DateTimeOffset EvaluatedAt);
}
