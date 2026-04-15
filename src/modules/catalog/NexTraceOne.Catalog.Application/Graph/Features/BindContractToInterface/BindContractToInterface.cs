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

namespace NexTraceOne.Catalog.Application.Graph.Features.BindContractToInterface;

/// <summary>
/// Feature: BindContractToInterface — vincula uma versão de contrato a uma interface de serviço.
/// Estrutura VSA: Command + Validator + Handler + Response em ficheiro único.
/// </summary>
public static class BindContractToInterface
{
    /// <summary>Comando de vinculação de versão de contrato a uma interface.</summary>
    public sealed record Command(
        Guid ServiceInterfaceId,
        Guid ContractVersionId,
        string BindingEnvironment,
        bool IsDefaultVersion = false,
        string BoundBy = "") : ICommand<Response>;

    /// <summary>Valida a entrada do comando de vinculação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceInterfaceId).NotEqual(Guid.Empty).WithMessage("ServiceInterfaceId is required.");
            RuleFor(x => x.ContractVersionId).NotEqual(Guid.Empty).WithMessage("ContractVersionId is required.");
            RuleFor(x => x.BindingEnvironment).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que vincula uma versão de contrato a uma interface de serviço.</summary>
    public sealed class Handler(
        IServiceInterfaceRepository serviceInterfaceRepository,
        IContractBindingRepository contractBindingRepository,
        ICatalogGraphUnitOfWork unitOfWork,
        IEventBus eventBus,
        IAuditModule auditModule) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var iface = await serviceInterfaceRepository.GetByIdAsync(
                ServiceInterfaceId.From(request.ServiceInterfaceId),
                cancellationToken);

            if (iface is null)
                return CatalogGraphErrors.InterfaceNotFound(request.ServiceInterfaceId);

            if (iface.Status == Domain.Graph.Enums.InterfaceStatus.Retired)
                return CatalogGraphErrors.InterfaceAlreadyRetired(request.ServiceInterfaceId);

            var binding = ContractBinding.Create(
                request.ServiceInterfaceId,
                request.ContractVersionId,
                request.BindingEnvironment);

            if (request.IsDefaultVersion)
                binding.SetAsDefault(true);

            contractBindingRepository.Add(binding);
            await unitOfWork.CommitAsync(cancellationToken);

            await eventBus.PublishAsync(new ContractBoundToInterfaceIntegrationEvent(
                binding.Id.Value,
                binding.ServiceInterfaceId,
                binding.ContractVersionId,
                binding.BindingEnvironment,
                binding.IsDefaultVersion,
                request.BoundBy,
                TenantId: null), cancellationToken);

            if (!string.IsNullOrWhiteSpace(request.BoundBy))
                await auditModule.RecordEventAsync(
                    "Catalog", "ContractBinding.Created",
                    binding.Id.Value.ToString(), "ContractBinding",
                    request.BoundBy, Guid.Empty, null, cancellationToken);

            return new Response(
                binding.Id.Value,
                binding.ServiceInterfaceId,
                binding.ContractVersionId,
                binding.Status.ToString());
        }
    }

    /// <summary>Resposta da vinculação de contrato a interface.</summary>
    public sealed record Response(
        Guid BindingId,
        Guid ServiceInterfaceId,
        Guid ContractVersionId,
        string Status);
}
