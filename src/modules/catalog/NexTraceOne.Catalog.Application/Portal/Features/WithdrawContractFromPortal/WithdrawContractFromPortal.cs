using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Portal.Abstractions;
using NexTraceOne.Catalog.Domain.Portal.Entities;
using NexTraceOne.Catalog.Domain.Portal.Errors;

namespace NexTraceOne.Catalog.Application.Portal.Features.WithdrawContractFromPortal;

/// <summary>
/// Feature: WithdrawContractFromPortal — retira a publicação de um contrato do Developer Portal.
/// A versão de contrato permanece no catálogo interno (ContractVersion) mas deixa de estar
/// visível no Developer Portal. Permite a governança de visibilidade sem afetar o lifecycle interno.
/// Estrutura VSA: Command + Validator + Handler + Response em ficheiro único.
/// </summary>
public static class WithdrawContractFromPortal
{
    /// <summary>Comando de retirada de publicação do portal.</summary>
    public sealed record Command(
        Guid PublicationEntryId,
        string WithdrawnBy,
        string? Reason = null) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de retirada de publicação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.PublicationEntryId).NotEmpty();
            RuleFor(x => x.WithdrawnBy).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Reason).MaximumLength(500).When(x => x.Reason is not null);
        }
    }

    /// <summary>
    /// Handler que retira a publicação de um contrato do Developer Portal.
    /// A entrada de publicação transiciona para Withdrawn — não é eliminada.
    /// </summary>
    public sealed class Handler(
        IContractPublicationEntryRepository repository,
        IPortalUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var entry = await repository.GetByIdAsync(
                ContractPublicationEntryId.From(request.PublicationEntryId), cancellationToken);

            if (entry is null)
                return DeveloperPortalErrors.PublicationEntryNotFound(request.PublicationEntryId.ToString());

            var withdrawResult = entry.Withdraw(request.WithdrawnBy, request.Reason, dateTimeProvider.UtcNow);
            if (withdrawResult.IsFailure)
                return withdrawResult.Error;

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                PublicationEntryId: entry.Id.Value,
                ContractVersionId: entry.ContractVersionId,
                Status: entry.Status.ToString(),
                WithdrawnBy: entry.WithdrawnBy!,
                WithdrawnAt: entry.WithdrawnAt!.Value,
                WithdrawalReason: entry.WithdrawalReason);
        }
    }

    /// <summary>Resposta da retirada de publicação do portal.</summary>
    public sealed record Response(
        Guid PublicationEntryId,
        Guid ContractVersionId,
        string Status,
        string WithdrawnBy,
        DateTimeOffset WithdrawnAt,
        string? WithdrawalReason);
}
