using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListCommitsByRelease;

/// <summary>
/// Feature: ListCommitsByRelease — lista os commits associados a uma release.
/// Estrutura VSA: Query + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class ListCommitsByRelease
{
    /// <summary>Query para listar commits de uma release.</summary>
    public sealed record Query(Guid ReleaseId) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
        }
    }

    /// <summary>Handler que lista os commits de uma release.</summary>
    public sealed class Handler(
        ICommitAssociationRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releaseId = ReleaseId.From(request.ReleaseId);
            var commits = await repository.ListByReleaseIdAsync(releaseId, cancellationToken);

            var items = commits.Select(c => new CommitItem(
                Id: c.Id.Value,
                CommitSha: c.CommitSha,
                CommitMessage: c.CommitMessage,
                CommitAuthor: c.CommitAuthor,
                CommittedAt: c.CommittedAt,
                BranchName: c.BranchName,
                ServiceName: c.ServiceName,
                AssignmentStatus: c.AssignmentStatus.ToString(),
                AssignedBy: c.AssignedBy,
                AssignedAt: c.AssignedAt,
                ExtractedWorkItemRefs: c.ExtractedWorkItemRefs)).ToList();

            return new Response(request.ReleaseId, items);
        }
    }

    /// <summary>Item de commit na lista de commits de uma release.</summary>
    public sealed record CommitItem(
        Guid Id,
        string CommitSha,
        string CommitMessage,
        string CommitAuthor,
        DateTimeOffset CommittedAt,
        string BranchName,
        string ServiceName,
        string AssignmentStatus,
        string? AssignedBy,
        DateTimeOffset? AssignedAt,
        string? ExtractedWorkItemRefs);

    /// <summary>Resposta com a lista de commits de uma release.</summary>
    public sealed record Response(
        Guid ReleaseId,
        IReadOnlyList<CommitItem> Commits);
}
