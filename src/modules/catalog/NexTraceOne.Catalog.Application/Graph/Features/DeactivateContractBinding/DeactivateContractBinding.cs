using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.AuditCompliance.Contracts.ServiceInterfaces;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Contracts.IntegrationEvents;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Errors;

namespace NexTraceOne.Catalog.Application.Graph.Features.DeactivateContractBinding;

/// <summary>
/// Feature: DeactivateContractBinding — desactiva um vínculo entre interface e versão de contrato.
/// Estrutura VSA: Command + Validator + Handler em ficheiro único.
/// </summary>
public static class DeactivateContractBinding
{
    /// <summary>Comando de desactivação de um vínculo de contrato.</summary>
    public sealed record Command(
        Guid BindingId,
        string DeactivatedBy) : ICommand<MediatR.Unit>;

    /// <summary>Valida a entrada do comando de desactivação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.BindingId).NotEqual(Guid.Empty).WithMessage("BindingId is required.");
            RuleFor(x => x.DeactivatedBy).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que desactiva um vínculo de contrato.</summary>
    public sealed class Handler(
        IContractBindingRepository contractBindingRepository,
        ICatalogGraphUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IEventBus eventBus,
        IAuditModule auditModule) : ICommandHandler<Command, MediatR.Unit>
    {
        public async Task<Result<MediatR.Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var binding = await contractBindingRepository.GetByIdAsync(
                ContractBindingId.From(request.BindingId),
                cancellationToken);

            if (binding is null)
                return CatalogGraphErrors.ContractBindingNotFound(request.BindingId);

            var interfaceId = binding.ServiceInterfaceId.Value;
            var contractVersionId = binding.ContractVersionId;
            var bindingEnvironment = binding.BindingEnvironment;

            binding.Deactivate(request.DeactivatedBy, dateTimeProvider.UtcNow);

            await unitOfWork.CommitAsync(cancellationToken);

            await eventBus.PublishAsync(new ContractBindingDeactivatedIntegrationEvent(
                binding.Id.Value,
                interfaceId,
                contractVersionId,
                bindingEnvironment,
                request.DeactivatedBy,
                TenantId: null), cancellationToken);

            await auditModule.RecordEventAsync(
                "Catalog", "ContractBinding.Deactivated",
                binding.Id.Value.ToString(), "ContractBinding",
                request.DeactivatedBy, Guid.Empty, null, cancellationToken);

            return MediatR.Unit.Value;
        }
    }
}
