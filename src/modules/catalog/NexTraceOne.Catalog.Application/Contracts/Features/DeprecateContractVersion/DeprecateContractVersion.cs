using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Contracts.Application.Abstractions;
using NexTraceOne.Contracts.Domain.Entities;
using NexTraceOne.Contracts.Domain.Errors;

namespace NexTraceOne.Contracts.Application.Features.DeprecateContractVersion;

/// <summary>
/// Feature: DeprecateContractVersion — deprecia uma versão de contrato com aviso para consumers.
/// Registra a mensagem de depreciação, data de deprecação e data de sunset opcional.
/// Transiciona automaticamente o lifecycle state para Deprecated.
/// Estrutura VSA: Command + Validator + Handler + Response.
/// </summary>
public static class DeprecateContractVersion
{
    /// <summary>Comando para depreciar uma versão de contrato.</summary>
    public sealed record Command(
        Guid ContractVersionId,
        string DeprecationNotice,
        DateTimeOffset? SunsetDate) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de deprecação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ContractVersionId).NotEmpty();
            RuleFor(x => x.DeprecationNotice).NotEmpty().MaximumLength(2000);
        }
    }

    /// <summary>Handler que executa a deprecação e persiste o resultado.</summary>
    public sealed class Handler(
        IContractVersionRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var version = await repository.GetByIdAsync(
                ContractVersionId.From(request.ContractVersionId), cancellationToken);

            if (version is null)
                return ContractsErrors.ContractVersionNotFound(request.ContractVersionId.ToString());

            var result = version.Deprecate(
                request.DeprecationNotice,
                dateTimeProvider.UtcNow,
                request.SunsetDate);

            if (result.IsFailure)
                return result.Error;

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                version.Id.Value,
                version.LifecycleState.ToString(),
                request.DeprecationNotice,
                dateTimeProvider.UtcNow,
                request.SunsetDate);
        }
    }

    /// <summary>Resposta da deprecação de versão de contrato.</summary>
    public sealed record Response(
        Guid ContractVersionId,
        string LifecycleState,
        string DeprecationNotice,
        DateTimeOffset DeprecatedAt,
        DateTimeOffset? SunsetDate);
}
