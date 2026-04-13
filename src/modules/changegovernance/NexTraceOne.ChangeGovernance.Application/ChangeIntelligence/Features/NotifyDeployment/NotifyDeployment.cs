using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Enums;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.NotifyDeployment;

/// <summary>
/// Feature: NotifyDeployment — recebe eventos de deployment do CI/CD, correlaciona com uma Release
/// existente ou cria uma nova, e regista rastreabilidade via ChangeEvent e ExternalMarker.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class NotifyDeployment
{
    /// <summary>
    /// Comando de notificação de deployment do CI/CD.
    /// <para>
    /// <see cref="ApiAssetId"/> é opcional — pode ser <c>null</c> quando o evento chega via
    /// ingestão externa e ainda não foi correlacionado ao catálogo de serviços.
    /// </para>
    /// </summary>
    public sealed record Command(
        Guid? ApiAssetId,
        string ServiceName,
        string Version,
        string Environment,
        string PipelineSource,
        string CommitSha,
        string? ExternalDeploymentId = null) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de notificação de deployment.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Version).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Environment).NotEmpty().MaximumLength(100);
            RuleFor(x => x.PipelineSource).NotEmpty().MaximumLength(500);
            RuleFor(x => x.CommitSha).NotEmpty().MaximumLength(100);
            RuleFor(x => x.ExternalDeploymentId).MaximumLength(500).When(x => x.ExternalDeploymentId is not null);
        }
    }

    /// <summary>
    /// Handler que correlaciona um evento de deployment a uma Release existente ou cria uma nova.
    /// Regista rastreabilidade via <see cref="ChangeEvent"/> e <see cref="ExternalMarker"/>.
    /// Calcula automaticamente o ChangeIntelligenceScore a partir dos metadados da release.
    /// </summary>
    public sealed class Handler(
        IReleaseRepository repository,
        IChangeEventRepository changeEventRepository,
        IExternalMarkerRepository markerRepository,
        IChangeScoreRepository scoreRepository,
        IChangeScoreCalculator scoreCalculator,
        ICurrentTenant currentTenant,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var now = dateTimeProvider.UtcNow;

            // ── Correlação: procura Release existente por ServiceName + Version + Environment ──
            var existing = await repository.GetByServiceNameVersionEnvironmentAsync(
                request.ServiceName,
                request.Version,
                request.Environment,
                cancellationToken);

            bool isNewRelease;
            Release release;

            if (existing is not null)
            {
                // Enriquece a release existente com o novo evento de deploy
                release = existing;
                isNewRelease = false;

                var enrichEvent = ChangeEvent.Create(
                    release.Id,
                    eventType: "deploy_notified",
                    description: $"Deploy event received from '{request.PipelineSource}' for {request.ServiceName}@{request.Version} in {request.Environment}. CommitSha: {request.CommitSha}",
                    source: request.PipelineSource,
                    occurredAt: now);

                changeEventRepository.Add(enrichEvent);
            }
            else
            {
                // Cria nova Release e regista o evento inicial
                release = Release.Create(
                    currentTenant.Id,
                    request.ApiAssetId ?? Guid.Empty,
                    request.ServiceName,
                    request.Version,
                    request.Environment,
                    request.PipelineSource,
                    request.CommitSha,
                    now);

                repository.Add(release);
                isNewRelease = true;

                var createEvent = ChangeEvent.Create(
                    release.Id,
                    eventType: "deploy_created",
                    description: $"Release created via deploy event from '{request.PipelineSource}' for {request.ServiceName}@{request.Version} in {request.Environment}. CommitSha: {request.CommitSha}",
                    source: request.PipelineSource,
                    occurredAt: now);

                changeEventRepository.Add(createEvent);
            }

            // ── ExternalMarker: regista o evento de pipeline para rastreabilidade ────────────
            var externalId = request.ExternalDeploymentId
                ?? $"{request.ServiceName}-{request.Version}-{request.Environment}-{now:yyyyMMddHHmmss}";

            var marker = ExternalMarker.Create(
                release.Id,
                MarkerType.DeploymentStarted,
                sourceSystem: request.PipelineSource,
                externalId: externalId,
                payload: null,
                occurredAt: now,
                receivedAt: now);

            markerRepository.Add(marker);

            // ── Auto-compute ChangeIntelligenceScore ────────────────────────────────────
            // Calcula automaticamente o score com blast radius = null (ainda não disponível).
            // O score será recalculado quando CalculateBlastRadius for chamado.
            var factors = scoreCalculator.Compute(release.ChangeLevel, release.Environment, blastRadius: null);
            var autoScore = ChangeIntelligenceScore.Compute(
                release.Id,
                factors.BreakingChangeWeight,
                factors.BlastRadiusWeight,
                factors.EnvironmentWeight,
                now,
                factors.ScoreSource);
            _ = release.SetChangeScore(autoScore.Score);
            scoreRepository.Add(autoScore);

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                release.Id.Value,
                release.ServiceName,
                release.Version,
                release.Environment,
                release.Status.ToString(),
                release.CreatedAt,
                isNewRelease,
                marker.Id.Value,
                autoScore.Score,
                factors.ScoreSource);
        }
    }

    /// <summary>Resposta da correlação de evento de deployment com uma Release.</summary>
    public sealed record Response(
        Guid ReleaseId,
        string ServiceName,
        string Version,
        string Environment,
        string Status,
        DateTimeOffset CreatedAt,
        bool IsNewRelease,
        Guid ExternalMarkerId,
        decimal AutoScore,
        string ScoreSource);
}
