using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Contracts.Application.Abstractions;
using NexTraceOne.Contracts.Domain.Entities;
using NexTraceOne.Contracts.Domain.Errors;
using NexTraceOne.Contracts.Domain.Services;

namespace NexTraceOne.Contracts.Application.Features.VerifySignature;

/// <summary>
/// Feature: VerifySignature — verifica a integridade da assinatura digital de uma versão de contrato.
/// Recalcula o hash SHA-256 da representação canônica e compara com o fingerprint armazenado.
/// Útil para auditoria regulatória e validação pós-promoção para produção.
/// Estrutura VSA: Query + Validator + Handler + Response.
/// </summary>
public static class VerifySignature
{
    /// <summary>Query para verificação de integridade da assinatura de um contrato.</summary>
    public sealed record Query(Guid ContractVersionId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de verificação.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ContractVersionId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que recalcula o hash canônico e compara com o fingerprint armazenado.
    /// Retorna resultado de verificação com detalhes de proveniência.
    /// </summary>
    public sealed class Handler(
        IContractVersionRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var version = await repository.GetByIdAsync(
                ContractVersionId.From(request.ContractVersionId), cancellationToken);

            if (version is null)
                return ContractsErrors.ContractVersionNotFound(request.ContractVersionId.ToString());

            if (version.Signature is null)
                return new Response(
                    version.Id.Value,
                    false,
                    false,
                    null,
                    null,
                    "No signature found for this contract version.");

            var canonical = ContractCanonicalizer.Canonicalize(version.SpecContent, version.Format);
            var isValid = version.Signature.Verify(canonical);

            return new Response(
                version.Id.Value,
                true,
                isValid,
                version.Signature.Fingerprint,
                version.Signature.Algorithm,
                isValid ? "Signature verification passed." : "Signature verification FAILED — content may have been tampered with.");
        }
    }

    /// <summary>Resposta da verificação de assinatura.</summary>
    public sealed record Response(
        Guid ContractVersionId,
        bool HasSignature,
        bool IsValid,
        string? Fingerprint,
        string? Algorithm,
        string VerificationMessage);
}
