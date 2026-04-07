using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Features.DeleteContractTemplate;

/// <summary>Feature: DeleteContractTemplate — remove um template de contrato.</summary>
public static class DeleteContractTemplate
{
    public sealed record Command(Guid Id) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }

    public sealed class Handler(
        IContractTemplateRepository repository,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!currentUser.IsAuthenticated)
                return Error.Unauthorized("User.NotAuthenticated", "User must be authenticated.");

            var template = await repository.GetByIdAsync(
                new ContractTemplateId(request.Id),
                currentTenant.Id.ToString(),
                cancellationToken);

            if (template is null)
                return Error.NotFound("ContractTemplate.NotFound", $"Contract template '{request.Id}' not found.");

            await repository.DeleteAsync(new ContractTemplateId(request.Id), cancellationToken);
            return new Response(request.Id);
        }
    }

    public sealed record Response(Guid Id);
}
