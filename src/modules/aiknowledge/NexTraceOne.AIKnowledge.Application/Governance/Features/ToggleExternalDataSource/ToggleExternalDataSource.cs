using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ToggleExternalDataSource;

/// <summary>
/// Activa ou desactiva uma fonte de dados externa.
/// Operação idempotente — não falha se o estado já for o desejado.
/// </summary>
public static class ToggleExternalDataSource
{
    public sealed record Command(Guid Id, bool Activate) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }

    public sealed class Handler(
        IExternalDataSourceRepository repository,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var source = await repository.GetByIdAsync(
                ExternalDataSourceId.From(request.Id), cancellationToken);

            if (source is null)
                return AiGovernanceErrors.ExternalDataSourceNotFound(request.Id.ToString());

            if (request.Activate)
                source.Activate();
            else
                source.Deactivate();

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(source.Id.Value, source.IsActive);
        }
    }

    public sealed record Response(Guid Id, bool IsActive);
}
