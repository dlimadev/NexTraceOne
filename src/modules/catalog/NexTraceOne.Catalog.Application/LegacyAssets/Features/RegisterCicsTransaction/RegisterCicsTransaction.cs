using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;
using NexTraceOne.Catalog.Domain.LegacyAssets.Errors;
using NexTraceOne.Catalog.Domain.LegacyAssets.ValueObjects;

namespace NexTraceOne.Catalog.Application.LegacyAssets.Features.RegisterCicsTransaction;

/// <summary>
/// Feature: RegisterCicsTransaction — regista uma nova transação CICS no catálogo legacy.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class RegisterCicsTransaction
{
    /// <summary>Comando de registo de uma transação CICS.</summary>
    public sealed record Command(
        string TransactionId,
        Guid SystemId,
        string ProgramName,
        string RegionName,
        string? CicsVersion,
        int? Port) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de registo de transação CICS.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TransactionId).NotEmpty().Length(4);
            RuleFor(x => x.SystemId).NotEmpty();
            RuleFor(x => x.ProgramName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.RegionName).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que regista uma nova transação CICS no catálogo legacy.</summary>
    public sealed class Handler(
        ICicsTransactionRepository cicsTransactionRepository,
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

            var existing = await cicsTransactionRepository.GetByTransactionIdAndSystemAsync(
                request.TransactionId, systemId, cancellationToken);
            if (existing is not null)
            {
                return LegacyAssetsErrors.CicsTransactionAlreadyExists(request.TransactionId, request.SystemId);
            }

            var region = CicsRegion.Create(request.RegionName, request.CicsVersion, request.Port);
            var transaction = CicsTransaction.Create(request.TransactionId, systemId, request.ProgramName, region);

            cicsTransactionRepository.Add(transaction);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                transaction.Id.Value,
                transaction.TransactionId,
                transaction.SystemId.Value,
                transaction.ProgramName,
                transaction.Region.RegionName);
        }
    }

    /// <summary>Resposta do registo da transação CICS.</summary>
    public sealed record Response(
        Guid Id,
        string TransactionId,
        Guid SystemId,
        string ProgramName,
        string RegionName);
}
