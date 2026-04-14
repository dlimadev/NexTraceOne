using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Errors;

namespace NexTraceOne.Catalog.Application.Graph.Features.UpdateServiceOwnership;

/// <summary>
/// Feature: UpdateServiceOwnership — atualiza o ownership de um serviço existente.
/// Permite alterar equipa, owner técnico e owner de negócio.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class UpdateServiceOwnership
{
    /// <summary>Comando de atualização do ownership do serviço.</summary>
    public sealed record Command(
        Guid ServiceId,
        string TeamName,
        string TechnicalOwner,
        string BusinessOwner) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de atualização de ownership.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceId).NotEmpty();
            RuleFor(x => x.TeamName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.TechnicalOwner).MaximumLength(200);
            RuleFor(x => x.BusinessOwner).MaximumLength(200);
        }
    }

    /// <summary>Handler que atualiza o ownership de um serviço existente.</summary>
    public sealed class Handler(
        IServiceAssetRepository serviceAssetRepository,
        ICatalogGraphUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var service = await serviceAssetRepository.GetByIdAsync(
                ServiceAssetId.From(request.ServiceId), cancellationToken);

            if (service is null)
                return CatalogGraphErrors.ServiceAssetNotFoundById(request.ServiceId);

            service.UpdateOwnership(
                request.TeamName,
                request.TechnicalOwner,
                request.BusinessOwner);

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                service.Id.Value,
                service.Name,
                service.TeamName,
                service.TechnicalOwner,
                service.BusinessOwner);
        }
    }

    /// <summary>Resposta da atualização de ownership do serviço.</summary>
    public sealed record Response(
        Guid ServiceId,
        string Name,
        string TeamName,
        string TechnicalOwner,
        string BusinessOwner);
}
