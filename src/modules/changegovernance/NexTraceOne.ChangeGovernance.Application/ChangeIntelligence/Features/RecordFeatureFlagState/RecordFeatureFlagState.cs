using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.RecordFeatureFlagState;

/// <summary>
/// Feature: RecordFeatureFlagState — regista o estado de feature flags activas no momento da release.
/// Deve ser invocado pelo pipeline CI/CD (ou pelo webhook de integração) imediatamente antes do deploy.
/// O estado fica disponível para o GetFeatureFlagAwareness e enriquece o Change Advisory.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class RecordFeatureFlagState
{
    /// <summary>Comando para registar o estado de feature flags de uma release.</summary>
    public sealed record Command(
        Guid ReleaseId,
        int ActiveFlagCount,
        int CriticalFlagCount,
        int NewFeatureFlagCount,
        string FlagProvider,
        string? FlagsJson) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de estado de feature flags.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
            RuleFor(x => x.ActiveFlagCount).GreaterThanOrEqualTo(0);
            RuleFor(x => x.CriticalFlagCount).GreaterThanOrEqualTo(0);
            RuleFor(x => x.NewFeatureFlagCount).GreaterThanOrEqualTo(0);
            RuleFor(x => x.FlagProvider).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>
    /// Handler que persiste o estado de feature flags activas no momento do deploy.
    /// Verifica que a release existe antes de registar.
    /// </summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IFeatureFlagStateRepository flagStateRepository,
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

            var state = ReleaseFeatureFlagState.Create(
                releaseId,
                request.ActiveFlagCount,
                request.CriticalFlagCount,
                request.NewFeatureFlagCount,
                request.FlagProvider,
                request.FlagsJson,
                dateTimeProvider.UtcNow);

            flagStateRepository.Add(state);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                state.Id.Value,
                request.ReleaseId,
                state.ActiveFlagCount,
                state.CriticalFlagCount,
                state.NewFeatureFlagCount,
                state.FlagProvider,
                state.RecordedAt);
        }
    }

    /// <summary>Resposta do registo de estado de feature flags.</summary>
    public sealed record Response(
        Guid StateId,
        Guid ReleaseId,
        int ActiveFlagCount,
        int CriticalFlagCount,
        int NewFeatureFlagCount,
        string FlagProvider,
        DateTimeOffset RecordedAt);
}
