using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;
using NexTraceOne.Catalog.Domain.LegacyAssets.Errors;

namespace NexTraceOne.Catalog.Application.LegacyAssets.Features.RegisterImsTransaction;

/// <summary>
/// Feature: RegisterImsTransaction — regista uma nova transação IMS no catálogo legacy.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class RegisterImsTransaction
{
    /// <summary>Comando de registo de uma transação IMS.</summary>
    public sealed record Command(
        string TransactionCode,
        Guid SystemId,
        string PsbName,
        string? DbdName) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de registo de transação IMS.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TransactionCode).NotEmpty().MaximumLength(50);
            RuleFor(x => x.SystemId).NotEmpty();
            RuleFor(x => x.PsbName).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que regista uma nova transação IMS no catálogo legacy.</summary>
    public sealed class Handler(
        IImsTransactionRepository imsTransactionRepository,
        IMainframeSystemRepository mainframeSystemRepository,
        ILegacyAssetsUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var systemId = MainframeSystemId.From(request.SystemId);
            var system = await mainframeSystemRepository.GetByIdAsync(systemId, cancellationToken);
            if (system is null)
            {
                return LegacyAssetsErrors.MainframeSystemNotFound(request.SystemId);
            }

            var existing = await imsTransactionRepository.GetByCodeAndSystemAsync(
                request.TransactionCode, systemId, cancellationToken);
            if (existing is not null)
            {
                return LegacyAssetsErrors.ImsTransactionAlreadyExists(request.TransactionCode, request.SystemId);
            }

            var transaction = ImsTransaction.Create(request.TransactionCode, systemId, request.PsbName);

            if (request.DbdName is not null)
            {
                transaction.UpdateDetails(
                    transaction.DisplayName,
                    transaction.Description,
                    transaction.TransactionType,
                    request.DbdName,
                    transaction.Criticality,
                    transaction.LifecycleStatus);
            }

            imsTransactionRepository.Add(transaction);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                transaction.Id.Value,
                transaction.TransactionCode,
                transaction.SystemId.Value,
                transaction.PsbName);
        }
    }

    /// <summary>Resposta do registo da transação IMS.</summary>
    public sealed record Response(
        Guid Id,
        string TransactionCode,
        Guid SystemId,
        string PsbName);
}
