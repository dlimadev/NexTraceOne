using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Errors;

namespace NexTraceOne.Catalog.Application.Graph.Features.DeprecateServiceInterface;

/// <summary>
/// Feature: DeprecateServiceInterface — marca uma interface como depreciada com aviso e datas.
/// Estrutura VSA: Command + Validator + Handler em ficheiro único.
/// </summary>
public static class DeprecateServiceInterface
{
    /// <summary>Comando de deprecação de uma interface de serviço.</summary>
    public sealed record Command(
        Guid InterfaceId,
        DateTimeOffset? DeprecationDate = null,
        DateTimeOffset? SunsetDate = null,
        string? Notice = null) : ICommand<MediatR.Unit>;

    /// <summary>Valida a entrada do comando de deprecação.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.InterfaceId).NotEqual(Guid.Empty).WithMessage("InterfaceId is required.");
            RuleFor(x => x.Notice).MaximumLength(2000).When(x => x.Notice is not null);
        }
    }

    /// <summary>Handler que depreca uma interface de serviço.</summary>
    public sealed class Handler(
        IServiceInterfaceRepository serviceInterfaceRepository,
        ICatalogGraphUnitOfWork unitOfWork) : ICommandHandler<Command, MediatR.Unit>
    {
        public async Task<Result<MediatR.Unit>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var iface = await serviceInterfaceRepository.GetByIdAsync(
                ServiceInterfaceId.From(request.InterfaceId),
                cancellationToken);

            if (iface is null)
                return CatalogGraphErrors.InterfaceNotFound(request.InterfaceId);

            if (iface.Status == Domain.Graph.Enums.InterfaceStatus.Retired)
                return CatalogGraphErrors.InterfaceAlreadyRetired(request.InterfaceId);

            iface.Deprecate(request.DeprecationDate, request.SunsetDate, request.Notice);

            await unitOfWork.CommitAsync(cancellationToken);

            return MediatR.Unit.Value;
        }
    }
}
