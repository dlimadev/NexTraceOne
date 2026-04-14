using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Portal.Abstractions;
using NexTraceOne.Catalog.Domain.Portal.Entities;
using NexTraceOne.Catalog.Domain.Portal.Enums;
using NexTraceOne.Catalog.Domain.Portal.Errors;

namespace NexTraceOne.Catalog.Application.Portal.Features.PublishContractToPortal;

/// <summary>
/// Feature: PublishContractToPortal — publica uma versão de contrato aprovada no Developer Portal.
/// Distingue-se de PublishDraft (que converte um rascunho em versão interna):
/// esta operação controla a visibilidade pública/interna do contrato no catálogo do portal.
///
/// Pré-condição: O contrato deve ter lifecycle state Approved ou Locked (verificado pelo caller via UI).
/// Não deve existir publicação ativa para a mesma versão (unicidade garantida por índice na tabela).
///
/// Fluxo: Cria ContractPublicationEntry em estado PendingPublication → Publish() → Published.
/// Estrutura VSA: Command + Validator + Handler + Response em ficheiro único.
/// </summary>
public static class PublishContractToPortal
{
    /// <summary>
    /// Comando de publicação de contrato no Developer Portal.
    /// O caller deve fornecer os dados do contrato já verificados (title, semVer, apiAssetId).
    /// </summary>
    public sealed record Command(
        Guid ContractVersionId,
        Guid ApiAssetId,
        string ContractTitle,
        string SemVer,
        string PublishedBy,
        string LifecycleState,
        PublicationVisibility Visibility = PublicationVisibility.Internal,
        string? ReleaseNotes = null) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de publicação no portal.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        private static readonly HashSet<string> PublishableStates = new(StringComparer.OrdinalIgnoreCase)
        {
            "Approved", "Locked"
        };

        public Validator()
        {
            RuleFor(x => x.ContractVersionId).NotEmpty();
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.ContractTitle).NotEmpty().MaximumLength(300);
            RuleFor(x => x.SemVer).NotEmpty().MaximumLength(50);
            RuleFor(x => x.PublishedBy).NotEmpty().MaximumLength(200);
            RuleFor(x => x.LifecycleState)
                .NotEmpty()
                .Must(s => PublishableStates.Contains(s))
                .WithMessage("Contract must be in Approved or Locked lifecycle state to be published to the portal.");
            RuleFor(x => x.ReleaseNotes).MaximumLength(2000).When(x => x.ReleaseNotes is not null);
        }
    }

    /// <summary>
    /// Handler que publica um contrato no Developer Portal:
    /// 1. Verifica que não existe publicação ativa para a versão
    /// 2. Cria ContractPublicationEntry em PendingPublication
    /// 3. Efetua transição para Published
    /// </summary>
    public sealed class Handler(
        IContractPublicationEntryRepository repository,
        IPortalUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // Verifica unicidade (cada versão só pode ter uma publicação ativa)
            var existing = await repository.GetByContractVersionIdAsync(request.ContractVersionId, cancellationToken);
            if (existing is not null)
                return DeveloperPortalErrors.PublicationAlreadyExists(request.ContractVersionId.ToString());

            // Cria entrada em PendingPublication
            var createResult = ContractPublicationEntry.Create(
                request.ContractVersionId,
                request.ApiAssetId,
                request.ContractTitle,
                request.SemVer,
                request.PublishedBy,
                request.Visibility,
                request.ReleaseNotes);

            if (createResult.IsFailure)
                return createResult.Error;

            var entry = createResult.Value;

            // Transição imediata para Published (fluxo mínimo P4.4 — sem approval workflow separado)
            var publishResult = entry.Publish(dateTimeProvider.UtcNow);
            if (publishResult.IsFailure)
                return publishResult.Error;

            repository.Add(entry);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                PublicationEntryId: entry.Id.Value,
                ContractVersionId: entry.ContractVersionId,
                ApiAssetId: entry.ApiAssetId,
                ContractTitle: entry.ContractTitle,
                SemVer: entry.SemVer,
                Status: entry.Status.ToString(),
                Visibility: entry.Visibility.ToString(),
                PublishedAt: entry.PublishedAt!.Value);
        }
    }

    /// <summary>Resposta da publicação de contrato no Developer Portal.</summary>
    public sealed record Response(
        Guid PublicationEntryId,
        Guid ContractVersionId,
        Guid ApiAssetId,
        string ContractTitle,
        string SemVer,
        string Status,
        string Visibility,
        DateTimeOffset PublishedAt);
}
