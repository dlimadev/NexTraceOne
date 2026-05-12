using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Enums;

namespace NexTraceOne.IdentityAccess.Application.Features.GetOnboardingStatus;

/// <summary>Feature: GetOnboardingStatus — retorna estado actual do wizard de onboarding.</summary>
public static class GetOnboardingStatus
{
    /// <summary>Query para obter o estado actual do wizard de onboarding do tenant.</summary>
    public sealed record Query : IQuery<Response>;

    /// <summary>Resposta com o estado actual do wizard de onboarding.</summary>
    public sealed record Response(
        Guid? ProgressId,
        OnboardingStep CurrentStep,
        IReadOnlyList<OnboardingStep> CompletedSteps,
        bool IsCompleted,
        bool IsSkipped,
        DateTimeOffset? CompletedAt);

    internal sealed class Handler(
        IOnboardingProgressRepository repository,
        ICurrentTenant currentTenant) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);
            var progress = await repository.GetByTenantAsync(currentTenant.Id, cancellationToken);

            // Se não existir registo, devolve estado inicial sem criar
            if (progress is null)
                return new Response(null, OnboardingStep.Install, [], false, false, null);

            return new Response(
                progress.Id.Value,
                progress.CurrentStep,
                progress.CompletedSteps,
                progress.IsCompleted,
                progress.SkippedAt.HasValue,
                progress.CompletedAt);
        }
    }
}
