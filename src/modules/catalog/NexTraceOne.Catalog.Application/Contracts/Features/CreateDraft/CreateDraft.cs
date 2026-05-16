using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using System.Diagnostics;

using NexTraceOne.BuildingBlocks.Observability.Tracing;
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
            using var activity = NexTraceActivitySources.Commands.StartActivity("CreateDraft");
            Guard.Against.Null(request);

            activity?.SetTag("draft.serviceId", request.ServiceId.ToString());
            activity?.SetTag("draft.protocol", request.Protocol.ToString());

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

            activity?.SetTag("draft.id", draft.Id.Value.ToString());

            return new Response(
                draft.Id.Value,
                draft.Title,
                draft.Status.ToString(),
                dateTimeProvider.UtcNow);
        }
    }

    /// <summary>Resposta da criação de draft de contrato.</summary>
    public sealed record Response(
        Guid DraftId,
        string Title,
        string Status,
        DateTimeOffset CreatedAt);
}
