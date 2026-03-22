using Ardalis.GuardClauses;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Runtime.Features.GetTokenUsage;

/// <summary>
/// Feature: GetTokenUsage — consulta o consumo recente de tokens de IA.
/// Suporta filtragem por utilizador ou tenant para governança e FinOps.
/// </summary>
public static class GetTokenUsage
{
    public sealed record Query(
        string? UserId,
        Guid? TenantId) : IQuery<Response>;

    public sealed class Handler(
        IAiTokenUsageLedgerRepository usageLedgerRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            IReadOnlyList<Domain.Governance.Entities.AiTokenUsageLedger> entries;

            if (!string.IsNullOrWhiteSpace(request.UserId))
            {
                entries = await usageLedgerRepository.GetByUserAsync(request.UserId, cancellationToken);
            }
            else if (request.TenantId.HasValue && request.TenantId.Value != Guid.Empty)
            {
                entries = await usageLedgerRepository.GetByTenantAsync(request.TenantId.Value, cancellationToken);
            }
            else
            {
                return Error.Validation(
                    "AI.TokenUsageFilterRequired",
                    "At least one filter (UserId or TenantId) must be provided.");
            }

            var items = entries.Select(e => new UsageItem(
                e.Id.Value,
                e.UserId,
                e.TenantId,
                e.ProviderId,
                e.ModelId,
                e.ModelName,
                e.PromptTokens,
                e.CompletionTokens,
                e.TotalTokens,
                e.PolicyName,
                e.IsBlocked,
                e.BlockReason,
                e.Timestamp,
                e.Status,
                e.DurationMs)).ToList();

            return new Response(items, items.Count);
        }
    }

    public sealed record Response(
        IReadOnlyList<UsageItem> Items,
        int TotalCount);

    public sealed record UsageItem(
        Guid Id,
        string UserId,
        Guid TenantId,
        string ProviderId,
        string ModelId,
        string ModelName,
        int PromptTokens,
        int CompletionTokens,
        int TotalTokens,
        string? PolicyName,
        bool IsBlocked,
        string? BlockReason,
        DateTimeOffset Timestamp,
        string Status,
        double DurationMs);
}
