using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.LockContractVersion;

/// <summary>
/// Feature: LockContractVersion — bloqueia uma versão de contrato impedindo novas alterações.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class LockContractVersion
{
    /// <summary>Comando de bloqueio de versão de contrato.</summary>
    public sealed record Command(Guid ContractVersionId, string LockedBy) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de bloqueio de versão de contrato.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ContractVersionId).NotEmpty();
            RuleFor(x => x.LockedBy).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que bloqueia uma versão de contrato.</summary>
    public sealed class Handler(
        IContractVersionRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var version = await repository.GetByIdAsync(ContractVersionId.From(request.ContractVersionId), cancellationToken);
            if (version is null)
                return ContractsErrors.ContractVersionNotFound(request.ContractVersionId.ToString());

            var lockResult = version.Lock(request.LockedBy, dateTimeProvider.UtcNow);
            if (lockResult.IsFailure)
                return lockResult.Error;

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                version.Id.Value,
                version.SemVer,
                version.LockedBy ?? request.LockedBy,
                version.LockedAt ?? dateTimeProvider.UtcNow);
        }
    }

    /// <summary>Resposta do bloqueio de versão de contrato.</summary>
    public sealed record Response(
        Guid ContractVersionId,
        string SemVer,
        string LockedBy,
        DateTimeOffset LockedAt);
}

