using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.AddWorkItemToRelease;

/// <summary>
/// Feature: AddWorkItemToRelease — adiciona um work item de sistema externo a uma release.
/// O PO/PM pode adicionar work items (histórias/bugs/features) a qualquer momento
/// enquanto a release estiver em estado mutável.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class AddWorkItemToRelease
{
    /// <summary>Comando para adicionar um work item a uma release.</summary>
    public sealed record Command(
        Guid ReleaseId,
        string ExternalWorkItemId,
        string ExternalSystem,
        string Title,
        string WorkItemType,
        string? ExternalStatus = null,
        string? ExternalUrl = null) : ICommand<Response>;

    /// <summary>Valida a entrada do comando.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
            RuleFor(x => x.ExternalWorkItemId).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ExternalSystem).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
            RuleFor(x => x.WorkItemType).NotEmpty().MaximumLength(100);
        }
    }

    /// <summary>Handler que adiciona o work item à release, prevenindo duplicação.</summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IWorkItemAssociationRepository workItemRepository,
        ICurrentTenant currentTenant,
        ICurrentUser currentUser,
        IChangeIntelligenceUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releaseId = ReleaseId.From(request.ReleaseId);
            var release = await releaseRepository.GetByIdAsync(releaseId, cancellationToken);
            if (release is null)
                return Error.NotFound("RELEASE_NOT_FOUND", $"Release '{request.ReleaseId}' not found.");

            var alreadyExists = await workItemRepository.ExistsActiveAsync(
                releaseId, request.ExternalWorkItemId, cancellationToken);
            if (alreadyExists)
                return Error.Conflict("WORK_ITEM_ALREADY_ASSOCIATED",
                    $"Work item '{request.ExternalWorkItemId}' is already associated with this release.");

            if (!Enum.TryParse<ExternalWorkItemSystem>(request.ExternalSystem, ignoreCase: true, out var system))
                system = ExternalWorkItemSystem.Custom;

            var tenantId = currentTenant.Id;
            var addedBy = currentUser.Email ?? currentUser.Id ?? "system";
            var now = dateTimeProvider.UtcNow;

            var workItem = WorkItemAssociation.Create(
                tenantId: tenantId,
                releaseId: releaseId,
                externalWorkItemId: request.ExternalWorkItemId,
                externalSystem: system,
                title: request.Title,
                workItemType: request.WorkItemType,
                externalStatus: request.ExternalStatus,
                externalUrl: request.ExternalUrl,
                addedBy: addedBy,
                addedAt: now);

            workItemRepository.Add(workItem);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(workItem.Id.Value, request.ReleaseId, request.ExternalWorkItemId);
        }
    }

    /// <summary>Resposta da adição de work item.</summary>
    public sealed record Response(
        Guid WorkItemAssociationId,
        Guid ReleaseId,
        string ExternalWorkItemId);
}
