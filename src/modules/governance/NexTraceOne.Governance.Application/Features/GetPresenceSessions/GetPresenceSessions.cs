using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Governance.Application.Abstractions;

namespace NexTraceOne.Governance.Application.Features.GetPresenceSessions;

public static class GetPresenceSessions
{
    public sealed record Query(string ResourceType, Guid ResourceId, string TenantId) : IQuery<Response>;

    public sealed record PresenceDto(string UserId, string DisplayName, string AvatarColor, DateTimeOffset LastSeenAt);
    public sealed record Response(IReadOnlyList<PresenceDto> ActiveUsers, int Count);

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ResourceType).NotEmpty();
            RuleFor(x => x.ResourceId).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty();
        }
    }

    public sealed class Handler(IPresenceSessionRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var sessions = await repository.ListActiveAsync(request.TenantId, request.ResourceType, request.ResourceId, cancellationToken);
            var users = sessions.Select(s => new PresenceDto(s.UserId, s.DisplayName, s.AvatarColor, s.LastSeenAt)).ToList();
            return Result<Response>.Success(new Response(users, users.Count));
        }
    }
}
