using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.UpdateExternalDataSource;

/// <summary>
/// Actualiza configuração de uma fonte de dados externa existente.
/// O ConnectorType não pode ser alterado após o registo.
/// </summary>
public static class UpdateExternalDataSource
{
    public sealed record Command(
        Guid Id,
        string? Description,
        string ConnectorConfigJson,
        int Priority,
        int SyncIntervalMinutes) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
            RuleFor(x => x.ConnectorConfigJson).NotEmpty();
            RuleFor(x => x.Priority).GreaterThanOrEqualTo(0);
            RuleFor(x => x.SyncIntervalMinutes).GreaterThanOrEqualTo(0);
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

            source.Update(
                description: request.Description,
                connectorConfigJson: request.ConnectorConfigJson,
                priority: request.Priority,
                syncIntervalMinutes: request.SyncIntervalMinutes);

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(source.Id.Value, source.Name);
        }
    }

    public sealed record Response(Guid Id, string Name);
}
