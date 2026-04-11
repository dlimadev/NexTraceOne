using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Application.Features.AddBookmark;

public static class AddBookmark
{
    public sealed record Command(BookmarkEntityType EntityType, string EntityId, string DisplayName, string? Url) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.EntityType).IsInEnum();
            RuleFor(x => x.EntityId).NotEmpty().MaximumLength(256);
            RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(300);
        }
    }

    public sealed class Handler(
        IUserBookmarkRepository repository,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var tenantId = currentTenant.Id.ToString();

            var existing = await repository.FindAsync(
                currentUser.Id, tenantId, request.EntityType, request.EntityId, cancellationToken);

            if (existing is not null)
                return new Response(existing.Id.Value, existing.EntityType, existing.EntityId, existing.DisplayName, existing.CreatedAt);

            var bookmark = UserBookmark.Create(
                userId: currentUser.Id,
                tenantId: tenantId,
                entityType: request.EntityType,
                entityId: request.EntityId,
                displayName: request.DisplayName,
                url: request.Url);

            await repository.AddAsync(bookmark, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(bookmark.Id.Value, bookmark.EntityType, bookmark.EntityId, bookmark.DisplayName, bookmark.CreatedAt);
        }
    }

    public sealed record Response(Guid Id, BookmarkEntityType EntityType, string EntityId, string DisplayName, DateTimeOffset CreatedAt);
}
