using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Errors;
using NexTraceOne.Catalog.Domain.Contracts.Policies;
using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Features.CreateDraft;

/// <summary>
/// Feature: CreateDraft — cria um novo rascunho de contrato no Contract Studio.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class CreateDraft
{
    /// <summary>Comando de criação de draft de contrato.</summary>
    public sealed record Command(
        string Title,
        string Author,
        ContractType ContractType,
        ContractProtocol Protocol,
        Guid ServiceId,
        string? Description = null) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de criação de draft.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Author).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ContractType).IsInEnum();
            RuleFor(x => x.Protocol).IsInEnum();
            RuleFor(x => x.ServiceId).NotEqual(Guid.Empty).WithMessage("ServiceId is required.");
        }
    }

    /// <summary>
    /// Handler que cria um novo draft de contrato.
    /// Valida que o serviço existe e que o tipo de contrato é permitido pelo ServiceContractPolicy.
    /// </summary>
    public sealed class Handler(
        IContractDraftRepository repository,
        IServiceAssetRepository serviceAssetRepository,
        IContractsUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            // Valida existência do serviço
            var service = await serviceAssetRepository.GetByIdAsync(
                ServiceAssetId.From(request.ServiceId),
                cancellationToken);

            if (service is null)
                return ContractsErrors.CatalogLinkNotFound(request.ServiceId.ToString());

            // Valida que o tipo de serviço suporta contratos
            if (!ServiceContractPolicy.SupportsContracts(service.ServiceType))
                return ContractsErrors.ServiceTypeDoesNotSupportContracts(service.ServiceType.ToString());

            // Valida que o tipo de contrato é permitido para o tipo de serviço
            if (!ServiceContractPolicy.IsContractTypeAllowed(service.ServiceType, request.ContractType))
                return ContractsErrors.ContractTypeNotAllowedForServiceType(
                    request.ContractType.ToString(),
                    service.ServiceType.ToString());

            var result = ContractDraft.Create(
                request.Title,
                request.Author,
                request.ContractType,
                request.Protocol,
                request.ServiceId,
                request.Description);

            if (result.IsFailure)
                return result.Error;

            var draft = result.Value;
            repository.Add(draft);
            await unitOfWork.CommitAsync(cancellationToken);

            var persistedDraft = (await repository.ListAsync(
                    DraftStatus.Editing,
                    request.ServiceId,
                    request.Author,
                    1,
                    20,
                    cancellationToken))
                .OrderByDescending(item => item.CreatedAt)
                .FirstOrDefault(item => item.Title == request.Title && item.Protocol == request.Protocol)
                ?? draft;

            return new Response(
                persistedDraft.Id.Value,
                persistedDraft.Title,
                persistedDraft.Status.ToString(),
                persistedDraft.CreatedAt == default ? dateTimeProvider.UtcNow : persistedDraft.CreatedAt);
        }
    }

    /// <summary>Resposta da criação de draft de contrato.</summary>
    public sealed record Response(
        Guid DraftId,
        string Title,
        string Status,
        DateTimeOffset CreatedAt);
}
