using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.AddDashboardComment;

/// <summary>
/// Feature: AddDashboardComment — adiciona um comentário ancorado num widget de dashboard.
/// V3.7 — Real-time Collaboration &amp; War Room.
/// </summary>
public static class AddDashboardComment
{
    public sealed record Command(
        Guid DashboardId,
        string TenantId,
        string AuthorUserId,
        string Content,
        string? WidgetId = null,
        Guid? ParentCommentId = null,
        IReadOnlyList<string>? Mentions = null) : ICommand<Response>;

    public sealed record Response(
        Guid CommentId,
        Guid DashboardId,
        string? WidgetId,
        string AuthorUserId,
        string Content,
        Guid? ParentCommentId,
        DateTimeOffset CreatedAt);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.DashboardId).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.AuthorUserId).NotEmpty();
            RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
        }
    }

    public sealed class Handler(
        IDashboardCommentRepository repository,
        IGovernanceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var comment = DashboardComment.Create(
                dashboardId: request.DashboardId,
                tenantId: request.TenantId,
                authorUserId: request.AuthorUserId,
                content: request.Content,
                now: clock.UtcNow,
                widgetId: request.WidgetId,
                parentCommentId: request.ParentCommentId,
                mentions: request.Mentions);

            await repository.AddAsync(comment, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                comment.Id.Value,
                comment.DashboardId,
                comment.WidgetId,
                comment.AuthorUserId,
                comment.Content,
                comment.ParentCommentId,
                comment.CreatedAt));
        }
    }
}
