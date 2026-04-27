using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;

namespace NexTraceOne.Governance.Application.Features.ResolveComment;

public static class ResolveComment
{
    public sealed record Command(Guid CommentId, string TenantId, string UserId) : ICommand<Response>;
    public sealed record Response(Guid CommentId, bool IsResolved, string ResolvedByUserId, DateTimeOffset ResolvedAt);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.CommentId).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
        }
    }

    public sealed class Handler(
        IDashboardCommentRepository repository,
        IGovernanceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var comment = await repository.GetByIdAsync(new DashboardCommentId(request.CommentId), request.TenantId, cancellationToken);
            if (comment is null)
                return Result.Failure<Response>(Error.NotFound("comment.notFound", "Comment not found."));

            comment.Resolve(request.UserId, clock.UtcNow);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(new Response(comment.Id.Value, comment.IsResolved, comment.ResolvedByUserId!, comment.ResolvedAt!.Value));
        }
    }
}
