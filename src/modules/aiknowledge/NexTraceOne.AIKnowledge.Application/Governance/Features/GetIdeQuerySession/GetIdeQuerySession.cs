using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.GetIdeQuerySession;

/// <summary>
/// Feature: GetIdeQuerySession — obtém detalhes completos de uma sessão de consulta IDE pelo identificador.
/// Estrutura VSA: Query + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class GetIdeQuerySession
{
    /// <summary>Query de consulta de uma sessão IDE pelo identificador.</summary>
    public sealed record Query(Guid SessionId) : IQuery<Response>;

    /// <summary>Validador da query GetIdeQuerySession.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.SessionId).NotEmpty();
        }
    }

    /// <summary>Handler que obtém os detalhes completos de uma sessão de consulta IDE.</summary>
    public sealed class Handler(
        IIdeQuerySessionRepository sessionRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var session = await sessionRepository.GetByIdAsync(
                IdeQuerySessionId.From(request.SessionId), cancellationToken);

            if (session is null)
                return AiGovernanceErrors.IdeQuerySessionNotFound(request.SessionId.ToString());

            return new Response(
                session.Id.Value,
                session.UserId,
                session.IdeClient,
                session.IdeClientVersion,
                session.QueryType,
                session.QueryText,
                session.QueryContext,
                session.ResponseText,
                session.ModelUsed,
                session.TokensUsed,
                session.PromptTokens,
                session.CompletionTokens,
                session.Status,
                session.GovernanceCheckResult,
                session.ResponseTimeMs,
                session.SubmittedAt,
                session.RespondedAt,
                session.ErrorMessage);
        }
    }

    /// <summary>Resposta com detalhes completos de uma sessão de consulta IDE.</summary>
    public sealed record Response(
        Guid SessionId,
        string UserId,
        string IdeClient,
        string IdeClientVersion,
        IdeQueryType QueryType,
        string QueryText,
        string? QueryContext,
        string? ResponseText,
        string ModelUsed,
        int TokensUsed,
        int PromptTokens,
        int CompletionTokens,
        IdeQuerySessionStatus Status,
        string? GovernanceCheckResult,
        long? ResponseTimeMs,
        DateTimeOffset SubmittedAt,
        DateTimeOffset? RespondedAt,
        string? ErrorMessage);
}
