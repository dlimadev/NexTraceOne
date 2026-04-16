using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListWorkItemsByRelease;

/// <summary>
/// Feature: ListWorkItemsByRelease — lista os work items activos associados a uma release.
/// Estrutura VSA: Query + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class ListWorkItemsByRelease
{
    /// <summary>Query para listar work items de uma release.</summary>
    public sealed record Query(Guid ReleaseId, bool IncludeRemoved = false) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
        }
    }

    /// <summary>Handler que lista os work items de uma release.</summary>
    public sealed class Handler(
        IWorkItemAssociationRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releaseId = ReleaseId.From(request.ReleaseId);
            var workItems = request.IncludeRemoved
                ? await repository.ListAllByReleaseIdAsync(releaseId, cancellationToken)
                : await repository.ListActiveByReleaseIdAsync(releaseId, cancellationToken);

            var items = workItems.Select(wi => new WorkItemItem(
                Id: wi.Id.Value,
                ExternalWorkItemId: wi.ExternalWorkItemId,
                ExternalSystem: wi.ExternalSystem.ToString(),
                Title: wi.Title,
                WorkItemType: wi.WorkItemType,
                ExternalStatus: wi.ExternalStatus,
                ExternalUrl: wi.ExternalUrl,
                AddedBy: wi.AddedBy,
                AddedAt: wi.AddedAt,
                IsActive: wi.IsActive,
                RemovedBy: wi.RemovedBy,
                RemovedAt: wi.RemovedAt)).ToList();

            return new Response(request.ReleaseId, items);
        }
    }

    /// <summary>Item de work item na lista.</summary>
    public sealed record WorkItemItem(
        Guid Id,
        string ExternalWorkItemId,
        string ExternalSystem,
        string Title,
        string WorkItemType,
        string? ExternalStatus,
        string? ExternalUrl,
        string AddedBy,
        DateTimeOffset AddedAt,
        bool IsActive,
        string? RemovedBy,
        DateTimeOffset? RemovedAt);

    /// <summary>Resposta com a lista de work items de uma release.</summary>
    public sealed record Response(
        Guid ReleaseId,
        IReadOnlyList<WorkItemItem> WorkItems);
}
