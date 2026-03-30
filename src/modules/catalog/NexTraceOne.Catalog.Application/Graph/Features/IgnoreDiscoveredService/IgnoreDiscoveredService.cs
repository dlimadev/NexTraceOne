using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Domain.Graph.Errors;

namespace NexTraceOne.Catalog.Application.Graph.Features.IgnoreDiscoveredService;

/// <summary>
/// Feature: IgnoreDiscoveredService — marca um serviço descoberto como irrelevante.
/// Transição de estado: Pending → Ignored.
/// </summary>
public static class IgnoreDiscoveredService
{
    /// <summary>Comando para ignorar um serviço descoberto.</summary>
    public sealed record Command(
        Guid DiscoveredServiceId,
        string Reason) : ICommand<Response>;

    /// <summary>Valida a entrada.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.DiscoveredServiceId).NotEmpty();
            RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        }
    }

    /// <summary>Handler que marca como ignorado.</summary>
    public sealed class Handler(
        IDiscoveredServiceRepository discoveredServiceRepository,
        ICatalogGraphUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var discovered = await discoveredServiceRepository.GetByIdAsync(
                DiscoveredServiceId.From(request.DiscoveredServiceId), cancellationToken);
            if (discovered is null)
            {
                return CatalogGraphErrors.DiscoveredServiceNotFound(request.DiscoveredServiceId);
            }

            if (discovered.Status != DiscoveryStatus.Pending)
            {
                return CatalogGraphErrors.DiscoveredServiceAlreadyProcessed(request.DiscoveredServiceId, discovered.Status.ToString());
            }

            discovered.Ignore(request.Reason);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(discovered.Id.Value, "Ignored");
        }
    }

    /// <summary>Resposta do ignore.</summary>
    public sealed record Response(Guid DiscoveredServiceId, string NewStatus);
}
