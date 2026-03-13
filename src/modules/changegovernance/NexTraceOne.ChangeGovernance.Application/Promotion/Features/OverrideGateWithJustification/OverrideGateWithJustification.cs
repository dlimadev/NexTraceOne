using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Promotion.Application.Abstractions;
using NexTraceOne.Promotion.Domain.Entities;
using NexTraceOne.Promotion.Domain.Errors;

namespace NexTraceOne.Promotion.Application.Features.OverrideGateWithJustification;

/// <summary>
/// Feature: OverrideGateWithJustification — realiza override justificado de uma avaliação de gate reprovada.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class OverrideGateWithJustification
{
    /// <summary>Comando para override justificado de avaliação de gate.</summary>
    public sealed record Command(
        Guid GateEvaluationId,
        string Justification,
        string OverriddenBy) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de override de avaliação de gate.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.GateEvaluationId).NotEmpty();
            RuleFor(x => x.Justification).NotEmpty().MaximumLength(4000);
            RuleFor(x => x.OverriddenBy).NotEmpty().MaximumLength(500);
        }
    }

    /// <summary>Handler que aplica override justificado em uma avaliação de gate existente.</summary>
    public sealed class Handler(
        IGateEvaluationRepository evaluationRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var evaluation = await evaluationRepository.GetByIdAsync(
                GateEvaluationId.From(request.GateEvaluationId), cancellationToken);
            if (evaluation is null)
                return PromotionErrors.GateEvaluationNotFound(request.GateEvaluationId.ToString());

            evaluation.Override(request.Justification, request.OverriddenBy, dateTimeProvider.UtcNow);

            evaluationRepository.Update(evaluation);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(evaluation.Id.Value, evaluation.Passed, evaluation.OverrideJustification);
        }
    }

    /// <summary>Resposta do override de avaliação de gate.</summary>
    public sealed record Response(Guid GateEvaluationId, bool Passed, string? OverrideJustification);
}
