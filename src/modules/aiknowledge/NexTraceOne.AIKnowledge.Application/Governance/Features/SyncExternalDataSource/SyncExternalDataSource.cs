using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.SyncExternalDataSource;

/// <summary>
/// Dispara sincronização manual de uma fonte de dados externa.
/// Executa o ciclo fetch → embed → persist no pipeline RAG.
/// </summary>
public static class SyncExternalDataSource
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
        IExternalDataSourceRepository repository,
        IDataSourceSyncService syncService,
        IUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var source = await repository.GetByIdAsync(
                ExternalDataSourceId.From(request.Id), cancellationToken);

            if (source is null)
                return AiGovernanceErrors.ExternalDataSourceNotFound(request.Id.ToString());

            var result = await syncService.SyncAsync(source, cancellationToken);

            await unitOfWork.CommitAsync(cancellationToken);

            if (!result.Success)
                return AiGovernanceErrors.ExternalDataSourceSyncFailed(source.Name, result.ErrorMessage ?? "unknown error");

            return new Response(source.Id.Value, result.DocumentsIndexed, source.LastSyncedAt);
        }
    }

    public sealed record Response(Guid Id, int DocumentsIndexed, DateTimeOffset? SyncedAt);
}
