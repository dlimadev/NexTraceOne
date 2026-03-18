using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ListAuditEntries;

/// <summary>
/// Feature: ListAuditEntries — lista entradas da trilha de auditoria de uso de IA.
/// Permite filtros compostos para rastreabilidade completa de interações com IA.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class ListAuditEntries
{
    /// <summary>Query de listagem filtrada de entradas de auditoria de uso de IA.</summary>
    public sealed record Query(
        string? UserId,
        Guid? ModelId,
        DateTimeOffset? StartDate,
        DateTimeOffset? EndDate,
        UsageResult? Result,
        AIClientType? ClientType,
        int PageSize = 50) : IQuery<Response>;

    /// <summary>Handler que lista entradas de auditoria com filtros compostos.</summary>
    public sealed class Handler(
        IAiUsageEntryRepository usageEntryRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var entries = await usageEntryRepository.ListAsync(
                request.UserId,
                request.ModelId,
                request.StartDate,
                request.EndDate,
                request.Result,
                request.ClientType,
                request.PageSize,
                cancellationToken);

            var items = entries
                .Select(e => new AuditItem(
                    e.Id.Value,
                    e.UserId,
                    e.UserDisplayName,
                    e.ModelId,
                    e.ModelName,
                    e.Provider,
                    e.IsInternal,
                    e.Timestamp,
                    e.PromptTokens,
                    e.CompletionTokens,
                    e.TotalTokens,
                     e.PolicyId,
                     e.PolicyName,
                    e.Result.ToString(),
                    e.ContextScope,
                    e.ClientType.ToString(),
                    e.CorrelationId,
                    e.ConversationId))
                .ToList();

            return new Response(items, items.Count);
        }
    }

    /// <summary>Resposta da listagem de entradas de auditoria de uso de IA.</summary>
    public sealed record Response(
        IReadOnlyList<AuditItem> Items,
        int TotalCount);

    /// <summary>Item resumido de uma entrada na trilha de auditoria.</summary>
    public sealed record AuditItem(
        Guid EntryId,
        string UserId,
        string UserDisplayName,
        Guid ModelId,
        string ModelName,
        string Provider,
        bool IsInternal,
        DateTimeOffset Timestamp,
        int PromptTokens,
        int CompletionTokens,
        int TotalTokens,
         Guid? PolicyId,
         string? PolicyName,
        string Result,
        string ContextScope,
        string ClientType,
        string CorrelationId,
        Guid? ConversationId);
}
