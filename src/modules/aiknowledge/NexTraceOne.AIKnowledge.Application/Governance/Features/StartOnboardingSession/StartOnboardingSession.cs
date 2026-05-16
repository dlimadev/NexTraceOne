using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Domain.Governance.Entities;
using NexTraceOne.AIKnowledge.Domain.Governance.Enums;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.AIKnowledge.Application.Governance.Features.StartOnboardingSession;

/// <summary>
/// Feature: StartOnboardingSession — inicia uma nova sessão de onboarding assistido por IA.
/// Estrutura VSA: Command + Validator + Handler + Response num único ficheiro.
/// </summary>
public static class StartOnboardingSession
{
    /// <summary>Comando de início de sessão de onboarding.</summary>
    public sealed record Command(
        string UserDisplayName,
        Guid TeamId,
        string TeamName,
        string ExperienceLevelValue,
        string ChecklistItems,
        int TotalItems) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de início de onboarding.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.UserDisplayName).NotEmpty().MaximumLength(300);
            RuleFor(x => x.TeamId).NotEmpty();
            RuleFor(x => x.TeamName).NotEmpty().MaximumLength(300);
            RuleFor(x => x.ExperienceLevelValue).NotEmpty()
                .Must(v => v is "Junior" or "Mid" or "Senior" or "Expert")
                .WithMessage("ExperienceLevelValue must be 'Junior', 'Mid', 'Senior', or 'Expert'.");
            RuleFor(x => x.ChecklistItems).NotEmpty();
            RuleFor(x => x.TotalItems).GreaterThan(0);
        }
    }

    /// <summary>Handler que cria uma nova sessão de onboarding.</summary>
    public sealed class Handler(
        IOnboardingSessionRepository sessionRepository,
        IDateTimeProvider dateTimeProvider,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!Enum.TryParse<OnboardingExperienceLevel>(request.ExperienceLevelValue, ignoreCase: true, out var experienceLevel))
                return Error.Validation("OnboardingSession.InvalidExperienceLevel", $"'{request.ExperienceLevelValue}' is not a valid onboarding experience level.");

            var session = OnboardingSession.Create(
                userId: currentUser.Id,
                userDisplayName: request.UserDisplayName,
                teamId: request.TeamId,
                teamName: request.TeamName,
                experienceLevel: experienceLevel,
                checklistItems: request.ChecklistItems,
                totalItems: request.TotalItems,
                tenantId: currentTenant.Id,
                startedAt: dateTimeProvider.UtcNow);

            await sessionRepository.AddAsync(session, cancellationToken);

            return new Response(session.Id.Value);
        }
    }

    /// <summary>Resposta do início da sessão de onboarding.</summary>
    public sealed record Response(Guid SessionId);
}
