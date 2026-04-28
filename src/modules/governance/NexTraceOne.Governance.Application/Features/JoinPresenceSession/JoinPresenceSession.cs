using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Features.JoinPresenceSession;

public static class JoinPresenceSession
{
    public sealed record Command(
        string ResourceType,
        Guid ResourceId,
        string TenantId,
        string UserId,
        string DisplayName,
        string AvatarColor) : ICommand<Response>;

    public sealed record Response(Guid SessionId, string UserId, string DisplayName, string AvatarColor, DateTimeOffset JoinedAt);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ResourceType).NotEmpty().Must(t => t is "dashboard" or "notebook");
            RuleFor(x => x.ResourceId).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(100);
            RuleFor(x => x.AvatarColor).NotEmpty().MaximumLength(20);
        }
    }

    public sealed class Handler(
        IPresenceSessionRepository repository,
        IGovernanceUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var existing = await repository.GetActiveAsync(
                request.TenantId, request.ResourceType, request.ResourceId, request.UserId, cancellationToken);

            if (existing is not null)
            {
                existing.Heartbeat(clock.UtcNow);
                await repository.SaveAsync(existing, cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken);
                return Result<Response>.Success(new Response(existing.Id.Value, existing.UserId, existing.DisplayName, existing.AvatarColor, existing.JoinedAt));
            }

            var session = PresenceSession.Join(
                request.ResourceType, request.ResourceId, request.TenantId,
                request.UserId, request.DisplayName, request.AvatarColor, clock.UtcNow);

            await repository.AddAsync(session, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            return Result<Response>.Success(new Response(session.Id.Value, session.UserId, session.DisplayName, session.AvatarColor, session.JoinedAt));
        }
    }
}
