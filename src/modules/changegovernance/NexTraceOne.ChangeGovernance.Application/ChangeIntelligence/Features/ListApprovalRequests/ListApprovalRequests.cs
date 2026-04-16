using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.ListApprovalRequests;

/// <summary>
/// Feature: ListApprovalRequests — lista os pedidos de aprovação de uma release.
/// Estrutura VSA: Query + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class ListApprovalRequests
{
    /// <summary>Query para listar pedidos de aprovação de uma release.</summary>
    public sealed record Query(Guid ReleaseId) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
        }
    }

    /// <summary>Handler que lista os pedidos de aprovação de uma release.</summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IApprovalRequestRepository approvalRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releaseId = ReleaseId.From(request.ReleaseId);
            var release = await releaseRepository.GetByIdAsync(releaseId, cancellationToken);
            if (release is null)
                return Error.NotFound("RELEASE_NOT_FOUND", $"Release '{request.ReleaseId}' not found.");

            var approvals = await approvalRepository.ListByReleaseIdAsync(releaseId, cancellationToken);

            var items = approvals.Select(a => new ApprovalItem(
                Id: a.Id.Value,
                ApprovalType: a.ApprovalType.ToString(),
                ExternalSystem: a.ExternalSystem,
                TargetEnvironment: a.TargetEnvironment,
                Status: a.Status.ToString(),
                RequestedAt: a.RequestedAt,
                RespondedAt: a.RespondedAt,
                RespondedBy: a.RespondedBy,
                Comments: a.Comments,
                ExternalRequestId: a.ExternalRequestId,
                CallbackTokenExpiresAt: a.CallbackTokenExpiresAt)).ToList();

            return new Response(request.ReleaseId, items);
        }
    }

    /// <summary>Item de pedido de aprovação na lista.</summary>
    public sealed record ApprovalItem(
        Guid Id,
        string ApprovalType,
        string? ExternalSystem,
        string TargetEnvironment,
        string Status,
        DateTimeOffset RequestedAt,
        DateTimeOffset? RespondedAt,
        string? RespondedBy,
        string? Comments,
        string? ExternalRequestId,
        DateTimeOffset CallbackTokenExpiresAt);

    /// <summary>Resposta com a lista de pedidos de aprovação de uma release.</summary>
    public sealed record Response(
        Guid ReleaseId,
        IReadOnlyList<ApprovalItem> ApprovalRequests);
}
