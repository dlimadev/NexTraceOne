using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RemoveWorkItemFromRelease;

/// <summary>
/// Feature: RemoveWorkItemFromRelease — remove um work item de uma release (soft-delete).
/// O histórico da associação é preservado com IsActive=false e timestamp de remoção.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class RemoveWorkItemFromRelease
{
    /// <summary>Comando para remover um work item de uma release.</summary>
    public sealed record Command(Guid WorkItemAssociationId) : ICommand<Response>;

    /// <summary>Valida a entrada do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.WorkItemAssociationId).NotEmpty();
        }
    }

    /// <summary>Handler que marca o work item como inactivo (soft-remove).</summary>
    public sealed class Handler(
        IWorkItemAssociationRepository repository,
        ICurrentUser currentUser,
        IChangeIntelligenceUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var id = Domain.ChangeIntelligence.Entities.WorkItemAssociationId.From(request.WorkItemAssociationId);
            var workItem = await repository.GetByIdAsync(id, cancellationToken);

            if (workItem is null)
                return Error.NotFound("WORK_ITEM_ASSOCIATION_NOT_FOUND",
                    $"Work item association '{request.WorkItemAssociationId}' not found.");

            if (!workItem.IsActive)
                return new Response(request.WorkItemAssociationId, false, "Already removed.");

            var removedBy = currentUser.Email ?? currentUser.Id ?? "system";
            var now = dateTimeProvider.UtcNow;

            workItem.Remove(removedBy, now);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(request.WorkItemAssociationId, true, null);
        }
    }

    /// <summary>Resposta da remoção de work item.</summary>
    public sealed record Response(
        Guid WorkItemAssociationId,
        bool Removed,
        string? Message);
}
