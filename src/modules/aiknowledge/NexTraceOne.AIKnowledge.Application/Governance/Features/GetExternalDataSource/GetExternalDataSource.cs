using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.GetExternalDataSource;

/// <summary>Retorna detalhe completo de uma fonte de dados externa.</summary>
public static class GetExternalDataSource
{
    public sealed record Query(Guid Id) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }

    public sealed class Handler(
        IExternalDataSourceRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var source = await repository.GetByIdAsync(
                ExternalDataSourceId.From(request.Id), cancellationToken);

            if (source is null)
                return AiGovernanceErrors.ExternalDataSourceNotFound(request.Id.ToString());

            return new Response(
                Id: source.Id.Value,
                Name: source.Name,
                Description: source.Description,
                ConnectorType: source.ConnectorType.ToString(),
                ConnectorConfigJson: source.ConnectorConfigJson,
                IsActive: source.IsActive,
                Priority: source.Priority,
                SyncIntervalMinutes: source.SyncIntervalMinutes,
                LastSyncedAt: source.LastSyncedAt,
                LastSyncStatus: source.LastSyncStatus,
                LastSyncError: source.LastSyncError,
                LastSyncDocumentCount: source.LastSyncDocumentCount,
                RegisteredAt: source.RegisteredAt);
        }
    }

    public sealed record Response(
        Guid Id,
        string Name,
        string? Description,
        string ConnectorType,
        string ConnectorConfigJson,
        bool IsActive,
        int Priority,
        int SyncIntervalMinutes,
        DateTimeOffset? LastSyncedAt,
        string? LastSyncStatus,
        string? LastSyncError,
        int LastSyncDocumentCount,
        DateTimeOffset RegisteredAt);
}
