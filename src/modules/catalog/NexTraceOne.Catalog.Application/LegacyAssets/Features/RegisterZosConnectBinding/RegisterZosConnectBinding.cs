using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.LegacyAssets.Abstractions;
using NexTraceOne.Catalog.Domain.LegacyAssets.Entities;
using NexTraceOne.Catalog.Domain.LegacyAssets.Errors;

namespace NexTraceOne.Catalog.Application.LegacyAssets.Features.RegisterZosConnectBinding;

/// <summary>
/// Feature: RegisterZosConnectBinding — regista um novo binding z/OS Connect no catálogo legacy.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class RegisterZosConnectBinding
{
    /// <summary>Comando de registo de um binding z/OS Connect.</summary>
    public sealed record Command(
        string Name,
        Guid SystemId,
        string ServiceName,
        string OperationName,
        string? HttpMethod,
        string? BasePath) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de registo de binding z/OS Connect.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.SystemId).NotEmpty();
            RuleFor(x => x.ServiceName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.OperationName).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>Handler que regista um novo binding z/OS Connect no catálogo legacy.</summary>
    public sealed class Handler(
        IZosConnectBindingRepository zosConnectBindingRepository,
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

            var existing = await zosConnectBindingRepository.GetByNameAndSystemAsync(
                request.Name, systemId, cancellationToken);
            if (existing is not null)
            {
                return LegacyAssetsErrors.ZosConnectBindingAlreadyExists(request.Name, request.SystemId);
            }

            var binding = ZosConnectBinding.Create(request.Name, systemId, request.ServiceName, request.OperationName);

            if (request.HttpMethod is not null || request.BasePath is not null)
            {
                binding.UpdateDetails(
                    binding.DisplayName,
                    binding.Description,
                    request.HttpMethod ?? string.Empty,
                    request.BasePath ?? string.Empty,
                    binding.TargetTransaction,
                    binding.RequestSchema,
                    binding.ResponseSchema,
                    binding.Criticality,
                    binding.LifecycleStatus);
            }

            zosConnectBindingRepository.Add(binding);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                binding.Id.Value,
                binding.Name,
                binding.SystemId.Value,
                binding.ServiceName,
                binding.OperationName);
        }
    }

    /// <summary>Resposta do registo do binding z/OS Connect.</summary>
    public sealed record Response(
        Guid Id,
        string Name,
        Guid SystemId,
        string ServiceName,
        string OperationName);
}
