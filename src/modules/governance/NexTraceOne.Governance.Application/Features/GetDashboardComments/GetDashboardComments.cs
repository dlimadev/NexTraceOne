using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;

namespace NexTraceOne.Governance.Application.Features.GetDashboardComments;

/// <summary>
/// Feature: GetDashboardComments — lista comentários de um dashboard, opcionalmente filtrados por widget.
/// V3.7 — Real-time Collaboration &amp; War Room.
/// </summary>
public static class GetDashboardComments
{
    public sealed record Query(
        Guid DashboardId,
        string TenantId,
        string? WidgetId = null,
        bool IncludeResolved = false) : IQuery<Response>;

    public sealed record CommentDto(
        Guid Id,
        Guid DashboardId,
        string? WidgetId,
        string AuthorUserId,
        string Content,
        Guid? ParentCommentId,
        bool IsResolved,
        string? ResolvedByUserId,
        DateTimeOffset CreatedAt,
        DateTimeOffset? EditedAt);

    public sealed record Response(IReadOnlyList<CommentDto> Comments);

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.DashboardId).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty();
        }
    }

    public sealed class Handler(IDashboardCommentRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var comments = await repository.ListAsync(
                request.DashboardId,
                request.TenantId,
                request.WidgetId,
                request.IncludeResolved,
                cancellationToken);

            var dtos = comments.Select(c => new CommentDto(
                c.Id.Value,
                c.DashboardId,
                c.WidgetId,
                c.AuthorUserId,
                c.Content,
                c.ParentCommentId,
                c.IsResolved,
                c.ResolvedByUserId,
                c.CreatedAt,
                c.EditedAt)).ToList();

            return Result<Response>.Success(new Response(dtos));
        }
    }
}
