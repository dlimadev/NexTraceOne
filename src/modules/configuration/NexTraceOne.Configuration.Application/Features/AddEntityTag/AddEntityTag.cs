using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.Tags;
using NexTraceOne.Configuration.Application.Abstractions;

namespace NexTraceOne.Configuration.Application.Features.AddEntityTag;

/// <summary>Feature: AddEntityTag — associa uma tag a uma entidade da plataforma.</summary>
public static class AddEntityTag
{
    public sealed record Command(
        string TenantId,
        string EntityType,
        string EntityId,
        string Key,
        string Value,
        string CreatedBy) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.EntityType).NotEmpty();
            RuleFor(x => x.EntityId).NotEmpty();
            RuleFor(x => x.Key).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Value).MaximumLength(100);
            RuleFor(x => x.CreatedBy).NotEmpty();
        }
    }

    public sealed class Handler(
        IEntityTagRepository repository,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var existing = (await repository.ListByEntityAsync(request.TenantId, request.EntityType, request.EntityId, cancellationToken))
                .FirstOrDefault(t => t.Key == request.Key.Trim().ToLower());

            if (existing is not null)
            {
                existing.UpdateValue(request.Value, clock.UtcNow);
                await repository.UpdateAsync(existing, cancellationToken);
                return Result<Response>.Success(new Response(existing.Id.Value, existing.Key, existing.Value));
            }

            var tag = EntityTag.Create(request.TenantId, request.EntityType, request.EntityId, request.Key, request.Value, request.CreatedBy, clock.UtcNow);
            await repository.AddAsync(tag, cancellationToken);
            return Result<Response>.Success(new Response(tag.Id.Value, tag.Key, tag.Value));
        }
    }

    public sealed record Response(Guid TagId, string Key, string Value);
}
