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

namespace NexTraceOne.Catalog.Application.Contracts.Features.CreateBackgroundServiceDraft;

/// <summary>
/// Feature: CreateBackgroundServiceDraft — cria um draft de Background Service Contract no Contract Studio.
/// Distingue-se de outros drafts: para além de criar o ContractDraft com ContractType=BackgroundService,
/// cria também um BackgroundServiceDraftMetadata com os campos específicos do processo.
/// Estrutura VSA: Command + Validator + Handler + Response em ficheiro único.
/// </summary>
public static class CreateBackgroundServiceDraft
{
    /// <summary>
    /// Comando de criação de draft de Background Service Contract com metadados específicos.
    /// </summary>
    public sealed record Command(
        string Title,
        string Author,
        string ServiceName,
        string Category = "Job",
        string TriggerType = "OnDemand",
        Guid ServiceId = default,
        string? Description = null,
        string? ScheduleExpression = null,
        string? InputsJson = null,
        string? OutputsJson = null,
        string? SideEffectsJson = null) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de criação de draft de background service.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
            RuleFor(x => x.Author).NotEmpty().MaximumLength(200);
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(300);
            RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
            RuleFor(x => x.TriggerType).NotEmpty().MaximumLength(50);
            RuleFor(x => x.ServiceId).NotEqual(Guid.Empty).WithMessage("ServiceId is required.");
        }
    }

    /// <summary>
    /// Handler que cria um draft de Background Service Contract:
    /// 1. Valida existência do serviço e compatibilidade do tipo de contrato
    /// 2. Cria ContractDraft com ContractType=BackgroundService
    /// 3. Cria BackgroundServiceDraftMetadata com os campos do processo
    /// </summary>
    public sealed class Handler(
        IContractDraftRepository draftRepository,
        IServiceAssetRepository serviceAssetRepository,
        IBackgroundServiceDraftMetadataRepository metadataRepository,
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

            // Valida que o tipo de contrato BackgroundService é permitido para o tipo de serviço
            if (!ServiceContractPolicy.IsContractTypeAllowed(service.ServiceType, ContractType.BackgroundService))
                return ContractsErrors.ContractTypeNotAllowedForServiceType(
                    ContractType.BackgroundService.ToString(),
                    service.ServiceType.ToString());

            // 1. Cria ContractDraft com tipo BackgroundService
            var draftResult = ContractDraft.Create(
                request.Title,
                request.Author,
                ContractType.BackgroundService,
                ContractProtocol.OpenApi,  // Protocolo genérico — background services não têm protocolo de wire
                request.ServiceId,
                request.Description);

            if (draftResult.IsFailure)
                return draftResult.Error;

            var draft = draftResult.Value;
            draftRepository.Add(draft);

            // 2. Cria BackgroundServiceDraftMetadata com metadados do processo
            var metadata = BackgroundServiceDraftMetadata.Create(
                draft.Id,
                request.ServiceName,
                request.Category,
                request.TriggerType,
                request.ScheduleExpression,
                request.InputsJson ?? "{}",
                request.OutputsJson ?? "{}",
                request.SideEffectsJson ?? "[]");

            metadataRepository.Add(metadata);

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                DraftId: draft.Id.Value,
                Title: draft.Title,
                Status: draft.Status.ToString(),
                ServiceName: request.ServiceName,
                Category: request.Category,
                TriggerType: request.TriggerType,
                ScheduleExpression: request.ScheduleExpression,
                CreatedAt: dateTimeProvider.UtcNow);
        }
    }

    /// <summary>Resposta da criação de draft de background service.</summary>
    public sealed record Response(
        Guid DraftId,
        string Title,
        string Status,
        string ServiceName,
        string Category,
        string TriggerType,
        string? ScheduleExpression,
        DateTimeOffset CreatedAt);
}
