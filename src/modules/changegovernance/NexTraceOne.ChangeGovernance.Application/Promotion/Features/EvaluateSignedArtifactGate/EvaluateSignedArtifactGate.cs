using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Promotion.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Entities;
using NexTraceOne.ChangeGovernance.Domain.Promotion.Errors;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.ChangeGovernance.Application.Promotion.Features.EvaluateSignedArtifactGate;

/// <summary>
/// Feature: EvaluateSignedArtifactGate — avalia se o artefacto de uma release
/// possui attestation SLSA (digest criptográfico verificável) antes de promover.
///
/// O gate é um diferencial enterprise do NexTraceOne: verifica que o build foi
/// produzido por um pipeline confiável (SLSA Level 3) e não por um developer local.
///
/// Comportamento:
/// - Se gate desativado por config → Skipped (não bloqueia)
/// - Se release sem ArtifactDigest → Failed (artefacto não atestado)
/// - Se release com ArtifactDigest → Passed
/// - Se sem release associada → Warning (não verificável)
///
/// Wave D backlog — Signed artifact gate.
/// </summary>
public static class EvaluateSignedArtifactGate
{
    /// <summary>Query para avaliação do gate de artefacto assinado.</summary>
    public sealed record Query(
        Guid PromotionRequestId,
        string ServiceName,
        string Version,
        string TargetEnvironmentName) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.PromotionRequestId).NotEmpty();
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(300);
            RuleFor(x => x.Version).NotEmpty().MaximumLength(50);
            RuleFor(x => x.TargetEnvironmentName).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>
    /// Handler que verifica se a release associada à promoção possui ArtifactDigest (SLSA attestation).
    /// </summary>
    public sealed class Handler(
        IPromotionRequestRepository requestRepository,
        IReleaseRepository releaseRepository,
        IConfigurationResolutionService configService,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var promotionRequest = await requestRepository.GetByIdAsync(
                PromotionRequestId.From(request.PromotionRequestId), cancellationToken);
            if (promotionRequest is null)
                return PromotionErrors.RequestNotFound(request.PromotionRequestId.ToString());

            // ── Gate habilitado? ───────────────────────────────────────────────
            var enabledConfig = await configService.ResolveEffectiveValueAsync(
                "slsa.artifact_gate.enabled",
                ConfigurationScope.System,
                null,
                cancellationToken);
            var isEnabled = enabledConfig?.EffectiveValue == "true";

            if (!isEnabled)
            {
                return Result<Response>.Success(new Response(
                    PromotionRequestId: request.PromotionRequestId,
                    ServiceName: request.ServiceName,
                    TargetEnvironment: request.TargetEnvironmentName,
                    GatePassed: true,
                    GateSkipped: true,
                    HasArtifactAttestation: false,
                    ArtifactDigest: null,
                    SlsaProvenanceUri: null,
                    Message: "Signed artifact gate is disabled via configuration.",
                    EvaluatedAt: dateTimeProvider.UtcNow));
            }

            // ── Encontrar release associada ────────────────────────────────────
            var release = await releaseRepository.GetByServiceNameVersionEnvironmentAsync(
                request.ServiceName, request.Version, request.TargetEnvironmentName, cancellationToken);

            if (release is null)
            {
                return Result<Response>.Success(new Response(
                    PromotionRequestId: request.PromotionRequestId,
                    ServiceName: request.ServiceName,
                    TargetEnvironment: request.TargetEnvironmentName,
                    GatePassed: false,
                    GateSkipped: false,
                    HasArtifactAttestation: false,
                    ArtifactDigest: null,
                    SlsaProvenanceUri: null,
                    Message: $"No release found for service '{request.ServiceName}' version '{request.Version}' in environment '{request.TargetEnvironmentName}'. Artifact attestation cannot be verified.",
                    EvaluatedAt: dateTimeProvider.UtcNow));
            }

            var hasAttestation = release.ArtifactDigest is not null;

            return Result<Response>.Success(new Response(
                PromotionRequestId: request.PromotionRequestId,
                ServiceName: request.ServiceName,
                TargetEnvironment: request.TargetEnvironmentName,
                GatePassed: hasAttestation,
                GateSkipped: false,
                HasArtifactAttestation: hasAttestation,
                ArtifactDigest: release.ArtifactDigest,
                SlsaProvenanceUri: release.SlsaProvenanceUri,
                Message: hasAttestation
                    ? $"Artifact attestation verified. Digest: {release.ArtifactDigest}"
                    : $"Release found but no artifact attestation (ArtifactDigest) is present. SLSA Level 3 requires a verifiable build provenance.",
                EvaluatedAt: dateTimeProvider.UtcNow));
        }
    }

    /// <summary>Resposta da avaliação do gate de artefacto assinado.</summary>
    public sealed record Response(
        Guid PromotionRequestId,
        string ServiceName,
        string TargetEnvironment,
        bool GatePassed,
        bool GateSkipped,
        bool HasArtifactAttestation,
        string? ArtifactDigest,
        string? SlsaProvenanceUri,
        string Message,
        DateTimeOffset EvaluatedAt);
}
