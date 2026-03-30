using Ardalis.GuardClauses;

using FluentValidation;

using MediatR;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.DeactivateFreezeWindow;

/// <summary>
/// Feature: DeactivateFreezeWindow — desativa uma janela de freeze antes do término previsto.
/// Estrutura VSA: Command + Validator + Handler em um único arquivo.
/// </summary>
public static class DeactivateFreezeWindow
{
    /// <summary>Comando para desativar uma janela de freeze.</summary>
    public sealed record Command(Guid FreezeWindowId) : ICommand<Unit>;

    /// <summary>Valida a entrada do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.FreezeWindowId).NotEmpty();
        }
    }

    /// <summary>Handler que desativa uma janela de freeze existente.</summary>
    public sealed class Handler(
        IFreezeWindowRepository repository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Unit>
    {
        public async Task<Result<Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var window = await repository.GetByIdAsync(
                FreezeWindowId.From(request.FreezeWindowId), cancellationToken);

            if (window is null)
                return Error.NotFound(
                    "change_intelligence.freeze.not_found",
                    "Freeze window not found.");

            var result = window.Deactivate();
            if (result.IsFailure)
                return result.Error;

            await unitOfWork.CommitAsync(cancellationToken);
            return Result<Unit>.Success(Unit.Value);
        }
    }
}
