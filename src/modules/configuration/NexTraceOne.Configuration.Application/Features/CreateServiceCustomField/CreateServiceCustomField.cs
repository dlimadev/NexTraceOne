using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Features.CreateServiceCustomField;

/// <summary>Feature: CreateServiceCustomField — cria um campo personalizado para serviços.</summary>
public static class CreateServiceCustomField
{
    private static readonly string[] ValidFieldTypes = ["Text", "Number", "Date", "Select", "MultiSelect", "Url", "Email"];

    public sealed record Command(
        string TenantId,
        string FieldName,
        string FieldType,
        bool IsRequired,
        string DefaultValue,
        int SortOrder) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.FieldName).NotEmpty().MaximumLength(60);
            RuleFor(x => x.FieldType).NotEmpty()
                .Must(t => ValidFieldTypes.Contains(t, StringComparer.OrdinalIgnoreCase));
            RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
        }
    }

    public sealed class Handler(
        IServiceCustomFieldRepository repository,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            var field = ServiceCustomField.Create(request.TenantId, request.FieldName, request.FieldType, request.IsRequired, request.DefaultValue, request.SortOrder, clock.UtcNow);
            await repository.AddAsync(field, cancellationToken);
            return Result<Response>.Success(new Response(field.Id.Value, field.FieldName, field.FieldType, field.IsRequired, field.SortOrder));
        }
    }

    public sealed record Response(Guid FieldId, string FieldName, string FieldType, bool IsRequired, int SortOrder);
}
