using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.ListOnboardingSessions;

/// <summary>
/// Feature: ListOnboardingSessions — lista sessões de onboarding com filtros opcionais.
/// Estrutura VSA: Query + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class ListOnboardingSessions
{
    /// <summary>Query de listagem de sessões de onboarding com filtros opcionais.</summary>
    public sealed record Query(Guid? TeamId = null, string? StatusValue = null) : IQuery<Response>;

    /// <summary>Validador da query ListOnboardingSessions.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.StatusValue)
                .Must(v => v is null or "Active" or "Completed" or "Abandoned")
                .WithMessage("StatusValue must be 'Active', 'Completed', or 'Abandoned'.");
        }
    }

    /// <summary>Handler que lista sessões de onboarding com filtros opcionais.</summary>
    public sealed class Handler(
        IOnboardingSessionRepository sessionRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            OnboardingSessionStatus? status = null;
            if (request.StatusValue is not null)
            {
                if (!Enum.TryParse<OnboardingSessionStatus>(request.StatusValue, ignoreCase: true, out var parsedStatus))
                    return Error.Validation("OnboardingSession.InvalidStatus", $"'{request.StatusValue}' is not a valid onboarding session status.");
                status = parsedStatus;
            }

            var sessions = await sessionRepository.ListAsync(
                request.TeamId, status, cancellationToken);

            var items = sessions
                .Select(s => new OnboardingSessionItem(
                    s.Id.Value,
                    s.UserId,
                    s.UserDisplayName,
                    s.TeamId,
                    s.TeamName,
                    s.ExperienceLevel,
                    s.Status,
                    s.CompletedItems,
                    s.TotalItems,
                    s.ProgressPercent,
                    s.AiInteractionCount,
                    s.StartedAt,
                    s.CompletedAt))
                .ToList();

            return new Response(items);
        }
    }

    /// <summary>Resposta da listagem de sessões de onboarding.</summary>
    public sealed record Response(IReadOnlyList<OnboardingSessionItem> Items);

    /// <summary>Item resumido de uma sessão de onboarding.</summary>
    public sealed record OnboardingSessionItem(
        Guid SessionId,
        string UserId,
        string UserDisplayName,
        Guid TeamId,
        string TeamName,
        OnboardingExperienceLevel ExperienceLevel,
        OnboardingSessionStatus Status,
        int CompletedItems,
        int TotalItems,
        int ProgressPercent,
        int AiInteractionCount,
        DateTimeOffset StartedAt,
        DateTimeOffset? CompletedAt);
}
