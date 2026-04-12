using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Features.CreateContractTemplate;

/// <summary>Feature: CreateContractTemplate — cria um template de contrato para o tenant.</summary>
public static class CreateContractTemplate
{
    private static readonly string[] ValidContractTypes = ["REST", "SOAP", "Event", "AsyncAPI", "Background"];

    public sealed record Command(
        string Name,
        string ContractType,
        string TemplateJson,
        string Description) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ContractType).NotEmpty()
                .Must(t => ValidContractTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
                .WithMessage($"ContractType must be one of: {string.Join(", ", ValidContractTypes)}");
            RuleFor(x => x.TemplateJson).NotNull();
        }
    }

    public sealed class Handler(
        IContractTemplateRepository repository,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var template = ContractTemplate.Create(
                currentTenant.Id.ToString(),
                request.Name,
                request.ContractType,
                request.TemplateJson,
                request.Description,
                currentUser.Id,
                isBuiltIn: false,
                clock.UtcNow);

            await repository.AddAsync(template, cancellationToken);

            return new Response(
                template.Id.Value,
                template.Name,
                template.ContractType,
                template.Description,
                template.CreatedAt);
        }
    }

    public sealed record Response(
        Guid TemplateId,
        string Name,
        string ContractType,
        string Description,
        DateTimeOffset CreatedAt);
}
