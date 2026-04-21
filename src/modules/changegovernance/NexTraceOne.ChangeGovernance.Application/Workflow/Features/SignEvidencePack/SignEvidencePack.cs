using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Errors;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.ChangeGovernance.Application.Workflow.Features.SignEvidencePack;

/// <summary>
/// Feature: SignEvidencePack — aplica uma assinatura HMAC-SHA256 ao evidence pack.
/// O manifesto canónico inclui os campos imutáveis do evidence pack: Id, ReleaseId,
/// ContractHash, BlastRadiusScore, SpectralScore, ChangeIntelligenceScore, GeneratedAt.
/// A assinatura permite que auditores externos verifiquem integridade sem acesso ao sistema.
/// Wave C.2 — Compliance &amp; Evidence integrity.
/// </summary>
public static class SignEvidencePack
{
    public sealed record Command(Guid WorkflowInstanceId, string SignedBy) : ICommand<Response>;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.WorkflowInstanceId).NotEmpty();
            RuleFor(x => x.SignedBy).NotEmpty().MaximumLength(500);
        }
    }

    public sealed class Handler(
        IWorkflowInstanceRepository instanceRepository,
        IEvidencePackRepository evidencePackRepository,
        IWorkflowUnitOfWork unitOfWork,
        IDateTimeProvider clock,
        IConfigurationResolutionService configService) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var instanceId = WorkflowInstanceId.From(request.WorkflowInstanceId);
            var instance = await instanceRepository.GetByIdAsync(instanceId, cancellationToken);
            if (instance is null)
                return WorkflowErrors.InstanceNotFound(request.WorkflowInstanceId.ToString());

            var evidencePack = await evidencePackRepository.GetByWorkflowInstanceIdAsync(instanceId, cancellationToken);
            if (evidencePack is null)
                return WorkflowErrors.EvidencePackNotFound(request.WorkflowInstanceId.ToString());

            if (evidencePack.IsIntegritySigned)
                return Error.Business("evidence_pack.already_signed",
                    "Evidence pack has already been signed. Re-signing is not permitted.");

            // ── Ler chave de assinatura da configuração ─────────────────────
            var signingKeyRaw = await configService.ResolveEffectiveValueAsync(
                EvidencePackConfigKeys.SigningKeyHex, ConfigurationScope.System, null, cancellationToken);
            var signingKeyHex = signingKeyRaw?.EffectiveValue;

            if (string.IsNullOrWhiteSpace(signingKeyHex) ||
                string.Equals(signingKeyHex, "change-me-in-production", StringComparison.Ordinal))
                return Error.Business("evidence_pack.signing_key_not_configured",
                    "Evidence pack signing key is not configured or is still set to the default value. Update 'security.evidence_pack.signing_key' before signing.");

            // ── Construir manifesto canónico ────────────────────────────────
            var manifest = new
            {
                evidencePackId = evidencePack.Id.Value,
                workflowInstanceId = instance.Id.Value,
                releaseId = evidencePack.ReleaseId,
                contractHash = evidencePack.ContractHash,
                blastRadiusScore = evidencePack.BlastRadiusScore,
                spectralScore = evidencePack.SpectralScore,
                changeIntelligenceScore = evidencePack.ChangeIntelligenceScore,
                generatedAt = evidencePack.GeneratedAt.ToString("O"),
                signedBy = request.SignedBy,
            };

            var manifestJson = JsonSerializer.Serialize(manifest,
                new JsonSerializerOptions { WriteIndented = false });

            // ── Computar HMAC-SHA256 ────────────────────────────────────────
            var keyBytes = Encoding.UTF8.GetBytes(signingKeyHex);
            var dataBytes = Encoding.UTF8.GetBytes(manifestJson);
            var hashBytes = HMACSHA256.HashData(keyBytes, dataBytes);
            var hashHex = Convert.ToHexString(hashBytes).ToLowerInvariant();

            var now = clock.UtcNow;
            evidencePack.ApplyIntegritySignature(manifestJson, hashHex, request.SignedBy, now);
            evidencePackRepository.Update(evidencePack);

            await unitOfWork.CommitAsync(cancellationToken);

            return Result<Response>.Success(new Response(
                EvidencePackId: evidencePack.Id.Value,
                IntegrityHash: hashHex,
                SignedBy: request.SignedBy,
                SignedAt: now));
        }
    }

    public sealed record Response(Guid EvidencePackId, string IntegrityHash, string SignedBy, DateTimeOffset SignedAt);
}
