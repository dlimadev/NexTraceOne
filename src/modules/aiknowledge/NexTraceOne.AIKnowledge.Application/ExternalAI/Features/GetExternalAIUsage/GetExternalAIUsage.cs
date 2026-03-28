using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.ExternalAI.Features.GetExternalAIUsage;

/// <summary>
/// Feature: GetExternalAIUsage — consolida métricas de token usage por conversa.
/// </summary>
public static class GetExternalAIUsage
{
    public sealed record Query(
        Guid? ConversationId,
        string? UserId,
        DateTimeOffset? From,
        DateTimeOffset? To,
        string? Provider,
        string? Model,
        Guid? TenantId,
        Guid? EnvironmentId) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.From).LessThan(x => x.To)
                .When(x => x.From.HasValue && x.To.HasValue)
                .WithMessage("'From' must be earlier than 'To'.");

            RuleFor(x => x.UserId)
                .MaximumLength(500)
                .When(x => !string.IsNullOrWhiteSpace(x.UserId));

            RuleFor(x => x.Provider)
                .MaximumLength(200)
                .When(x => !string.IsNullOrWhiteSpace(x.Provider));

            RuleFor(x => x.Model)
                .MaximumLength(200)
                .When(x => !string.IsNullOrWhiteSpace(x.Model));
        }
    }

    public sealed class Handler(
        IAiUsageEntryRepository usageEntryRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var entries = await usageEntryRepository.ListAsync(
                request.UserId,
                modelId: null,
                request.From,
                request.To,
                result: null,
                clientType: null,
                pageSize: 1_000,
                cancellationToken);

            var filtered = entries
                .Where(entry => !request.ConversationId.HasValue || entry.ConversationId == request.ConversationId)
                .Where(entry => string.IsNullOrWhiteSpace(request.Provider) ||
                                string.Equals(entry.Provider, request.Provider, StringComparison.OrdinalIgnoreCase))
                .Where(entry => string.IsNullOrWhiteSpace(request.Model) ||
                                string.Equals(entry.ModelName, request.Model, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var totalTokens = filtered.Sum(entry => (long)entry.TotalTokens);
            var inputTokens = filtered.Sum(entry => (long)entry.PromptTokens);
            var outputTokens = filtered.Sum(entry => (long)entry.CompletionTokens);
            var conversationCount = filtered
                .Where(entry => entry.ConversationId.HasValue)
                .Select(entry => entry.ConversationId!.Value)
                .Distinct()
                .Count();

            var averageTokensPerConversation = conversationCount > 0
                ? Math.Round(totalTokens / (double)conversationCount, 2)
                : 0d;

            var byProvider = filtered
                .GroupBy(entry => entry.Provider)
                .Select(group => new ProviderUsage(
                    group.Key,
                    group.Sum(entry => (long)entry.TotalTokens),
                    group.Sum(entry => (long)entry.PromptTokens),
                    group.Sum(entry => (long)entry.CompletionTokens),
                    group.Count()))
                .OrderByDescending(item => item.TotalTokens)
                .ToList();

            return new Response(
                totalTokens,
                inputTokens,
                outputTokens,
                conversationCount,
                averageTokensPerConversation,
                byProvider,
                request.ConversationId,
                request.UserId,
                request.From,
                request.To,
                request.Provider,
                request.Model,
                request.TenantId,
                request.EnvironmentId);
        }
    }

    public sealed record Response(
        long TotalTokens,
        long InputTokens,
        long OutputTokens,
        int ConversationCount,
        double AverageTokensPerConversation,
        IReadOnlyList<ProviderUsage> ByProvider,
        Guid? ConversationId,
        string? UserId,
        DateTimeOffset? PeriodFrom,
        DateTimeOffset? PeriodTo,
        string? Provider,
        string? Model,
        Guid? TenantId,
        Guid? EnvironmentId);

    public sealed record ProviderUsage(
        string ProviderId,
        long TotalTokens,
        long InputTokens,
        long OutputTokens,
        int EntryCount);
}
