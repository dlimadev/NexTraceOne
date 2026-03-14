using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Promotion.Application.Abstractions;
using NexTraceOne.Promotion.Domain.Entities;
using NexTraceOne.Promotion.Domain.Enums;
using NexTraceOne.Promotion.Domain.Errors;

namespace NexTraceOne.Promotion.Application.Features.EvaluatePromotionGates;

/// <summary>
/// Feature: EvaluatePromotionGates — avalia os gates de promoção e decide sobre aprovação ou rejeição automática.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class EvaluatePromotionGates
{
    /// <summary>Entrada de avaliação de um gate individual.</summary>
    public sealed record GateEvaluationInput(Guid GateId, bool Passed, string? Details);

    /// <summary>Resultado da avaliação de um gate individual.</summary>
    public sealed record GateResultItem(Guid GateId, string GateName, bool Passed, bool IsRequired);

    /// <summary>Comando para avaliação dos gates de uma solicitação de promoção.</summary>
    public sealed record Command(
        Guid PromotionRequestId,
        string EvaluatedBy,
        IReadOnlyList<GateEvaluationInput> Evaluations) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de avaliação de gates.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.PromotionRequestId).NotEmpty();
            RuleFor(x => x.EvaluatedBy).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Evaluations).NotEmpty();
        }
    }

    /// <summary>Handler que avalia os gates de promoção e atualiza o status da solicitação.</summary>
    public sealed class Handler(
        IPromotionRequestRepository requestRepository,
        IPromotionGateRepository gateRepository,
        IGateEvaluationRepository evaluationRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var promotionRequest = await requestRepository.GetByIdAsync(
                PromotionRequestId.From(request.PromotionRequestId), cancellationToken);
            if (promotionRequest is null)
                return PromotionErrors.RequestNotFound(request.PromotionRequestId.ToString());

            if (promotionRequest.Status != PromotionStatus.Pending)
                return PromotionErrors.InvalidStatusTransition(
                    promotionRequest.Status.ToString(), PromotionStatus.InEvaluation.ToString());

            var now = dateTimeProvider.UtcNow;

            var requiredGates = await gateRepository.ListRequiredByEnvironmentIdAsync(
                promotionRequest.TargetEnvironmentId, cancellationToken);

            var allGates = await gateRepository.ListByEnvironmentIdAsync(
                promotionRequest.TargetEnvironmentId, cancellationToken);

            var gateMap = allGates.ToDictionary(g => g.Id.Value);

            var evaluations = new List<GateEvaluation>();
            foreach (var input in request.Evaluations)
            {
                if (!gateMap.TryGetValue(input.GateId, out var gate))
                    return PromotionErrors.GateNotFound(input.GateId.ToString());

                var evaluation = GateEvaluation.Create(
                    promotionRequest.Id,
                    gate.Id,
                    input.Passed,
                    request.EvaluatedBy,
                    input.Details,
                    now);

                evaluationRepository.Add(evaluation);
                evaluations.Add(evaluation);
            }

            var evalMap = request.Evaluations.ToDictionary(e => e.GateId);
            var requiredGateFailed = requiredGates.FirstOrDefault(g =>
                evalMap.TryGetValue(g.Id.Value, out var eval) && !eval.Passed);

            var startResult = promotionRequest.StartEvaluation();
            if (startResult.IsFailure)
                return startResult.Error;

            bool allRequiredPassed;
            if (requiredGateFailed is not null)
            {
                var rejectResult = promotionRequest.Reject(now);
                if (rejectResult.IsFailure)
                    return rejectResult.Error;
                allRequiredPassed = false;
            }
            else
            {
                var approveResult = promotionRequest.Approve(now);
                if (approveResult.IsFailure)
                    return approveResult.Error;
                allRequiredPassed = true;
            }

            requestRepository.Update(promotionRequest);
            await unitOfWork.CommitAsync(cancellationToken);

            int passedCount = evaluations.Count(e => e.Passed);
            var results = evaluations.Select(e =>
            {
                gateMap.TryGetValue(e.PromotionGateId.Value, out var g);
                return new GateResultItem(
                    e.PromotionGateId.Value,
                    g?.GateName ?? string.Empty,
                    e.Passed,
                    g?.IsRequired ?? false);
            }).ToList();

            return new Response(
                promotionRequest.Id.Value,
                promotionRequest.Status.ToString(),
                evaluations.Count,
                passedCount,
                allRequiredPassed,
                results);
        }
    }

    /// <summary>Resposta da avaliação de gates de promoção.</summary>
    public sealed record Response(
        Guid PromotionRequestId,
        string Status,
        int TotalGates,
        int PassedGates,
        bool AllRequiredPassed,
        IReadOnlyList<GateResultItem> Results);
}
