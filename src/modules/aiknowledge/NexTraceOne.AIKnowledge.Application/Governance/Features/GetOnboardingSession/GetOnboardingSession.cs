using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.AIKnowledge.Domain.Governance.Errors;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.GetOnboardingSession;

/// <summary>
/// Feature: GetOnboardingSession — obtém detalhes completos de uma sessão de onboarding pelo identificador.
/// Estrutura VSA: Query + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class GetOnboardingSession
{
    /// <summary>Query de consulta de uma sessão de onboarding pelo identificador.</summary>
    public sealed record Query(Guid SessionId) : IQuery<Response>;

    /// <summary>Validador da query GetOnboardingSession.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.SessionId).NotEmpty();
        }
    }

    /// <summary>Handler que obtém os detalhes completos de uma sessão de onboarding.</summary>
    public sealed class Handler(
        IOnboardingSessionRepository sessionRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var session = await sessionRepository.GetByIdAsync(
                OnboardingSessionId.From(request.SessionId), cancellationToken);

            if (session is null)
                return AiGovernanceErrors.OnboardingSessionNotFound(request.SessionId.ToString());

            return new Response(
                session.Id.Value,
                session.UserId,
                session.UserDisplayName,
                session.TeamId,
                session.TeamName,
                session.ExperienceLevel,
                session.Status,
                session.ChecklistItems,
                session.CompletedItems,
                session.TotalItems,
                session.ProgressPercent,
                session.ServicesExplored,
                session.ContractsReviewed,
                session.RunbooksRead,
                session.AiInteractionCount,
                session.StartedAt,
                session.CompletedAt);
        }
    }

    /// <summary>Resposta com detalhes completos de uma sessão de onboarding.</summary>
    public sealed record Response(
        Guid SessionId,
        string UserId,
        string UserDisplayName,
        Guid TeamId,
        string TeamName,
        OnboardingExperienceLevel ExperienceLevel,
        OnboardingSessionStatus Status,
        string ChecklistItems,
        int CompletedItems,
        int TotalItems,
        int ProgressPercent,
        string? ServicesExplored,
        string? ContractsReviewed,
        string? RunbooksRead,
        int AiInteractionCount,
        DateTimeOffset StartedAt,
        DateTimeOffset? CompletedAt);
}
