using System.Security.Cryptography;
using System.Text;
using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.Workflow.Abstractions;
using NexTraceOne.ChangeGovernance.Application.Workflow.Features.SignEvidencePack;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Entities;
using NexTraceOne.ChangeGovernance.Domain.Workflow.Errors;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.ChangeGovernance.Application.Workflow.Features.VerifyEvidencePackIntegrity;

/// <summary>
/// Feature: VerifyEvidencePackIntegrity — verifica a assinatura HMAC-SHA256 de um evidence pack.
/// Re-computa o hash sobre o manifesto armazenado e compara com o hash gravado.
/// Wave C.2 — Compliance &amp; Evidence integrity.
/// </summary>
public static class VerifyEvidencePackIntegrity
{
    public sealed record Query(Guid WorkflowInstanceId) : IQuery<Response>;

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator() => RuleFor(x => x.WorkflowInstanceId).NotEmpty();
    }

    public sealed class Handler(
        IWorkflowInstanceRepository instanceRepository,
        IEvidencePackRepository evidencePackRepository,
        IConfigurationResolutionService configService) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var instanceId = WorkflowInstanceId.From(request.WorkflowInstanceId);
            var instance = await instanceRepository.GetByIdAsync(instanceId, cancellationToken);
            if (instance is null)
                return WorkflowErrors.InstanceNotFound(request.WorkflowInstanceId.ToString());

            var evidencePack = await evidencePackRepository.GetByWorkflowInstanceIdAsync(instanceId, cancellationToken);
            if (evidencePack is null)
                return WorkflowErrors.EvidencePackNotFound(request.WorkflowInstanceId.ToString());

            if (!evidencePack.IsIntegritySigned)
                return Result<Response>.Success(new Response(
                    EvidencePackId: evidencePack.Id.Value,
                    IsSigned: false,
                    IsValid: false,
                    SignedBy: null,
                    SignedAt: null,
                    VerificationNote: "Evidence pack has not been signed."));

            var signingKeyRaw = await configService.ResolveEffectiveValueAsync(
                EvidencePackConfigKeys.SigningKeyHex, ConfigurationScope.System, null, cancellationToken);
            var signingKeyHex = signingKeyRaw?.EffectiveValue ?? string.Empty;

            var keyBytes = Encoding.UTF8.GetBytes(signingKeyHex);
            var dataBytes = Encoding.UTF8.GetBytes(evidencePack.IntegrityManifest!);
            var expectedHashBytes = HMACSHA256.HashData(keyBytes, dataBytes);
            var expectedHash = Convert.ToHexString(expectedHashBytes).ToLowerInvariant();

            var isValid = string.Equals(expectedHash, evidencePack.IntegrityHash,
                StringComparison.OrdinalIgnoreCase);

            return Result<Response>.Success(new Response(
                EvidencePackId: evidencePack.Id.Value,
                IsSigned: true,
                IsValid: isValid,
                SignedBy: evidencePack.IntegritySignedBy,
                SignedAt: evidencePack.IntegritySignedAt,
                VerificationNote: isValid
                    ? "Integrity verified successfully."
                    : "Integrity check FAILED — evidence pack may have been tampered with."));
        }
    }

    public sealed record Response(
        Guid EvidencePackId,
        bool IsSigned,
        bool IsValid,
        string? SignedBy,
        DateTimeOffset? SignedAt,
        string VerificationNote);
}
