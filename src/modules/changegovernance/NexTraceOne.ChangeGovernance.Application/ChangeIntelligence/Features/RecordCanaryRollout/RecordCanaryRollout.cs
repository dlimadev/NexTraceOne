using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RecordCanaryRollout;

/// <summary>
/// Feature: RecordCanaryRollout — regista a percentagem de rollout de um canary deployment.
/// Deve ser invocado pelo sistema de rollout (Argo Rollouts, Flagger, Split.io, etc.) à medida
/// que o tráfego é migrado para a nova versão. Múltiplos registos por release são suportados,
/// permitindo rastrear a evolução do rollout ao longo do tempo.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class RecordCanaryRollout
{
    /// <summary>Comando para registar a percentagem de rollout de um canary deployment.</summary>
    public sealed record Command(
        Guid ReleaseId,
        decimal RolloutPercentage,
        int ActiveInstances,
        int TotalInstances,
        string SourceSystem,
        bool IsPromoted,
        bool IsAborted) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de registo de canary rollout.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
            RuleFor(x => x.RolloutPercentage).InclusiveBetween(0m, 100m);
            RuleFor(x => x.ActiveInstances).GreaterThanOrEqualTo(0);
            RuleFor(x => x.TotalInstances).GreaterThanOrEqualTo(0);
            RuleFor(x => x.SourceSystem).NotEmpty().MaximumLength(200);
            RuleFor(x => x)
                .Must(x => !(x.IsPromoted && x.IsAborted))
                .WithMessage("A canary rollout cannot be both promoted and aborted simultaneously.");
        }
    }

    /// <summary>
    /// Handler que persiste um registo de canary rollout para a release especificada.
    /// Verifica a existência da release antes de persistir.
    /// </summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        ICanaryRolloutRepository canaryRolloutRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var releaseId = ReleaseId.From(request.ReleaseId);
            var release = await releaseRepository.GetByIdAsync(releaseId, cancellationToken);
            if (release is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(request.ReleaseId.ToString());

            var rollout = CanaryRollout.Create(
                releaseId,
                request.RolloutPercentage,
                request.ActiveInstances,
                request.TotalInstances,
                request.SourceSystem,
                request.IsPromoted,
                request.IsAborted,
                dateTimeProvider.UtcNow);

            canaryRolloutRepository.Add(rollout);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                rollout.Id.Value,
                request.ReleaseId,
                rollout.RolloutPercentage,
                rollout.ActiveInstances,
                rollout.TotalInstances,
                rollout.SourceSystem,
                rollout.IsPromoted,
                rollout.IsAborted,
                rollout.RecordedAt);
        }
    }

    /// <summary>Resposta do registo de canary rollout.</summary>
    public sealed record Response(
        Guid RolloutId,
        Guid ReleaseId,
        decimal RolloutPercentage,
        int ActiveInstances,
        int TotalInstances,
        string SourceSystem,
        bool IsPromoted,
        bool IsAborted,
        DateTimeOffset RecordedAt);
}
