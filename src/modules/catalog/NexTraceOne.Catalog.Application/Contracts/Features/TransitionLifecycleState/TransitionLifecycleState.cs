using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Contracts.Application.Abstractions;
using NexTraceOne.Contracts.Domain.Entities;
using NexTraceOne.Contracts.Domain.Enums;
using NexTraceOne.Contracts.Domain.Errors;

namespace NexTraceOne.Contracts.Application.Features.TransitionLifecycleState;

/// <summary>
/// Feature: TransitionLifecycleState — transiciona o estado do ciclo de vida de uma versão de contrato.
/// Valida transições permitidas na máquina de estados do lifecycle:
/// Draft → InReview → Approved → Locked → Deprecated → Sunset → Retired.
/// Toda transição é auditável e retorna erro se for inválida.
/// Estrutura VSA: Command + Validator + Handler + Response.
/// </summary>
public static class TransitionLifecycleState
{
    /// <summary>Comando para transicionar o estado do lifecycle de uma versão de contrato.</summary>
    public sealed record Command(
        Guid ContractVersionId,
        ContractLifecycleState NewState) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de transição de lifecycle.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ContractVersionId).NotEmpty();
            RuleFor(x => x.NewState).IsInEnum();
        }
    }

    /// <summary>Handler que executa a transição de lifecycle e persiste o resultado.</summary>
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

            var previousState = version.LifecycleState;
            var result = version.TransitionTo(request.NewState, dateTimeProvider.UtcNow);

            if (result.IsFailure)
                return result.Error;

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                version.Id.Value,
                previousState,
                version.LifecycleState,
                dateTimeProvider.UtcNow);
        }
    }

    /// <summary>Resposta da transição de lifecycle.</summary>
    public sealed record Response(
        Guid ContractVersionId,
        ContractLifecycleState PreviousState,
        ContractLifecycleState CurrentState,
        DateTimeOffset TransitionedAt);
}
