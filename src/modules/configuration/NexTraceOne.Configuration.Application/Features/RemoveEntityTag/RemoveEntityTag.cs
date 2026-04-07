using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.BuildingBlocks.Core.Tags;
using NexTraceOne.Configuration.Application.Abstractions;

namespace NexTraceOne.Configuration.Application.Features.RemoveEntityTag;

/// <summary>Feature: RemoveEntityTag — remove uma tag de uma entidade.</summary>
public static class RemoveEntityTag
{
    public sealed record Command(Guid TagId, string TenantId) : ICommand<bool>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TagId).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty();
        }
    }

    public sealed class Handler(IEntityTagRepository repository) : ICommandHandler<Command, bool>
    {
        public async Task<Result<bool>> Handle(Command request, CancellationToken cancellationToken)
        {
            var id = new EntityTagId(request.TagId);
            var tag = await repository.GetByIdAsync(id, request.TenantId, cancellationToken);
            if (tag is null)
                return Error.NotFound("EntityTag.NotFound", "Tag not found.");

            await repository.DeleteAsync(id, cancellationToken);
            return Result<bool>.Success(true);
        }
    }
}
