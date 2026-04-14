using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Errors;
using NexTraceOne.Catalog.Domain.Contracts.Services;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

namespace NexTraceOne.Catalog.Application.Contracts.Features.SignContractVersion;

/// <summary>
/// Feature: SignContractVersion — assina digitalmente uma versão de contrato após canonicalização.
/// Calcula o hash SHA-256 da representação canônica do artefato, garantindo integridade
/// verificável para evidência regulatória e audit trail.
/// Requer que o contrato esteja no estado Approved ou Locked.
/// Estrutura VSA: Command + Validator + Handler + Response.
/// </summary>
public static class SignContractVersion
{
    /// <summary>Comando para assinar digitalmente uma versão de contrato.</summary>
    public sealed record Command(Guid ContractVersionId) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de assinatura.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ContractVersionId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que canonicaliza o conteúdo do contrato, calcula o hash SHA-256
    /// e registra a assinatura digital na versão do contrato.
    /// </summary>
    public sealed class Handler(
        IContractVersionRepository repository,
        IContractsUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        ICurrentUser currentUser) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var version = await repository.GetByIdAsync(
                ContractVersionId.From(request.ContractVersionId), cancellationToken);

            if (version is null)
                return ContractsErrors.ContractVersionNotFound(request.ContractVersionId.ToString());

            var canonical = ContractCanonicalizer.Canonicalize(version.SpecContent, version.Format);
            var signature = ContractSignature.Create(canonical, currentUser.Id, dateTimeProvider.UtcNow);

            var signResult = version.Sign(signature);
            if (signResult.IsFailure)
                return signResult.Error;

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                version.Id.Value,
                signature.Fingerprint,
                signature.Algorithm,
                signature.SignedBy,
                signature.SignedAt);
        }
    }

    /// <summary>Resposta da assinatura de versão de contrato.</summary>
    public sealed record Response(
        Guid ContractVersionId,
        string Fingerprint,
        string Algorithm,
        string SignedBy,
        DateTimeOffset SignedAt);
}
