using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

using IDateTimeProvider = NexTraceOne.BuildingBlocks.Application.Abstractions.IDateTimeProvider;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.RegisterExternalDataSource;

/// <summary>
/// Regista uma nova fonte de dados externa no sistema.
/// A fonte fica activa e com status Pending após o registo.
/// </summary>
public static class RegisterExternalDataSource
{
    public sealed record Command(
        string Name,
        string? Description,
        ExternalDataSourceConnectorType ConnectorType,
        string ConnectorConfigJson,
        int Priority = 10,
        int SyncIntervalMinutes = 0) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
            RuleFor(x => x.ConnectorConfigJson).NotEmpty();
            RuleFor(x => x.Priority).GreaterThanOrEqualTo(0);
            RuleFor(x => x.SyncIntervalMinutes).GreaterThanOrEqualTo(0);
        }
    }

    public sealed class Handler(
        IExternalDataSourceRepository repository,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var nameExists = await repository.ExistsByNameAsync(request.Name, cancellationToken);
            if (nameExists)
                return AiGovernanceErrors.ExternalDataSourceDuplicateName(request.Name);

            var source = ExternalDataSource.Register(
                name: request.Name,
                description: request.Description,
                connectorType: request.ConnectorType,
                connectorConfigJson: request.ConnectorConfigJson,
                priority: request.Priority,
                syncIntervalMinutes: request.SyncIntervalMinutes,
                registeredAt: dateTimeProvider.UtcNow);

            await repository.AddAsync(source, cancellationToken);

            return new Response(source.Id.Value, source.Name, source.ConnectorType.ToString());
        }
    }

    public sealed record Response(Guid Id, string Name, string ConnectorType);
}
