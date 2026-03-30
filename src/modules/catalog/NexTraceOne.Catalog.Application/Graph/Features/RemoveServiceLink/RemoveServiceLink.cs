using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Errors;

namespace NexTraceOne.Catalog.Application.Graph.Features.RemoveServiceLink;

/// <summary>
/// Feature: RemoveServiceLink — remove um link de um serviço do catálogo.
/// Estrutura VSA: Command + Validator + Handler em um único arquivo.
/// </summary>
public static class RemoveServiceLink
{
    /// <summary>Comando para remover um link de serviço.</summary>
    public sealed record Command(Guid ServiceAssetId, Guid LinkId) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de remoção de link.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceAssetId).NotEmpty();
            RuleFor(x => x.LinkId).NotEmpty();
        }
    }

    /// <summary>Handler que remove um link de um serviço.</summary>
    public sealed class Handler(
        IServiceLinkRepository serviceLinkRepository,
        ICatalogGraphUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var linkId = ServiceLinkId.From(request.LinkId);
            var link = await serviceLinkRepository.GetByIdAsync(linkId, cancellationToken);

            if (link is null)
                return CatalogGraphErrors.ServiceLinkNotFound(request.LinkId);

            if (link.ServiceAssetId.Value != request.ServiceAssetId)
                return CatalogGraphErrors.ServiceLinkNotFound(request.LinkId);

            serviceLinkRepository.Remove(link);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(request.LinkId);
        }
    }

    /// <summary>Resposta da remoção de um link.</summary>
    public sealed record Response(Guid LinkId);
}
