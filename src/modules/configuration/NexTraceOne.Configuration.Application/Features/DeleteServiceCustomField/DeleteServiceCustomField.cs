using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Features.DeleteServiceCustomField;

/// <summary>Feature: DeleteServiceCustomField — remove um campo personalizado.</summary>
public static class DeleteServiceCustomField
{
    public sealed record Command(Guid FieldId, string TenantId) : ICommand<bool>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.FieldId).NotEmpty();
            RuleFor(x => x.TenantId).NotEmpty();
        }
    }

    public sealed class Handler(IServiceCustomFieldRepository repository) : ICommandHandler<Command, bool>
    {
        public async Task<Result<bool>> Handle(Command request, CancellationToken cancellationToken)
        {
            var id = new ServiceCustomFieldId(request.FieldId);
            var field = await repository.GetByIdAsync(id, request.TenantId, cancellationToken);
            if (field is null)
                return Error.NotFound("ServiceCustomField.NotFound", "Custom field not found.");
            await repository.DeleteAsync(id, cancellationToken);
            return Result<bool>.Success(true);
        }
    }
}
