using Ardalis.GuardClauses;
using FluentValidation;

using MediatR;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Runtime.Features.ActivateModel;

/// <summary>
/// Feature: ActivateModel — ativa um modelo de IA no Model Registry.
/// Permite que modelos inativos, depreciados ou bloqueados voltem ao estado activo.
/// </summary>
public static class ActivateModel
{
    public sealed record Command(Guid ModelId) : ICommand;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ModelId).NotEmpty();
        }
    }

    public sealed class Handler(
        IAiModelRepository modelRepository) : ICommandHandler<Command>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var model = await modelRepository.GetByIdAsync(
                AIModelId.From(request.ModelId), cancellationToken);

            if (model is null)
            {
                return Error.NotFound(
                    "AI.ModelNotFound",
                    "AI model with id '{0}' was not found.",
                    request.ModelId);
            }

            var activationResult = model.Activate();
            if (activationResult.IsFailure)
            {
                return activationResult.Error;
            }

            await modelRepository.UpdateAsync(model, cancellationToken);

            return Unit.Value;
        }
    }
}
