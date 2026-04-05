using Ardalis.GuardClauses;
using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ListConversations;

/// <summary>
/// Feature: ListConversations — lista resumos de conversas do assistente de IA.
/// Retorna conversas com metadados de persona, modelo e escopo de contexto.
/// Estrutura VSA: Query + Handler + Response num único ficheiro.
/// </summary>
public static class ListConversations
{
    private static string ResolveConversationOwner(ICurrentUser currentUser)
        => !string.IsNullOrWhiteSpace(currentUser.Email)
            ? currentUser.Email
            : currentUser.Id;

    private static bool IsCurrentUserScope(string requestedUserId, ICurrentUser currentUser)
        => string.Equals(requestedUserId, currentUser.Id, StringComparison.OrdinalIgnoreCase)
            || string.Equals(requestedUserId, currentUser.Email, StringComparison.OrdinalIgnoreCase);

    /// <summary>Query de listagem de conversas do assistente de IA.</summary>
    public sealed record Query(
        string? UserId,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Validador da query ListConversations.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).MaximumLength(200).When(x => x.UserId is not null);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>Handler que lista resumos de conversas maduras do assistente.</summary>
    public sealed class Handler(
        IAiAssistantConversationRepository conversationRepository,
        ICurrentUser currentUser) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!string.IsNullOrWhiteSpace(request.UserId) && !IsCurrentUserScope(request.UserId, currentUser))
                return AiGovernanceErrors.ConversationAccessDenied(request.UserId);

            var effectiveUserId = ResolveConversationOwner(currentUser);

            var conversations = await conversationRepository.ListAsync(
                effectiveUserId,
                isActive: null,
                request.PageSize,
                cancellationToken);

            var totalCount = await conversationRepository.CountAsync(
                effectiveUserId,
                isActive: null,
                cancellationToken);

            var items = conversations.Select(c => new ConversationSummary(
                c.Id.Value,
                c.Title,
                c.Persona,
                c.ClientType.ToString(),
                c.DefaultContextScope,
                c.LastModelUsed,
                c.CreatedBy,
                c.MessageCount,
                c.Tags,
                c.IsActive,
                c.LastMessageAt)).ToList();

            return new Response(items, totalCount);
        }
    }

    /// <summary>Resposta da listagem de conversas do assistente de IA.</summary>
    public sealed record Response(
        IReadOnlyList<ConversationSummary> Items,
        int TotalCount);

    /// <summary>Resumo maduro de uma conversa com o assistente de IA.</summary>
    public sealed record ConversationSummary(
        Guid Id,
        string Title,
        string Persona,
        string ClientType,
        string DefaultContextScope,
        string? LastModelUsed,
        string CreatedBy,
        int MessageCount,
        string Tags,
        bool IsActive,
        DateTimeOffset? LastMessageAt);
}
