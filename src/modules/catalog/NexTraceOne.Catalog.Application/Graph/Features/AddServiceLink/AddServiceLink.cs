using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;
using NexTraceOne.Catalog.Domain.Graph.Enums;
using NexTraceOne.Catalog.Domain.Graph.Errors;

namespace NexTraceOne.Catalog.Application.Graph.Features.AddServiceLink;

/// <summary>
/// Feature: AddServiceLink — adiciona um link categorizado a um serviço do catálogo.
/// Estrutura VSA: Command + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class AddServiceLink
{
    /// <summary>Comando para adicionar um link a um serviço.</summary>
    public sealed record Command(
        Guid ServiceAssetId,
        string Category,
        string Title,
        string Url,
        string? Description = null,
        string? IconHint = null,
        int SortOrder = 0) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de adição de link.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ServiceAssetId).NotEmpty();
            RuleFor(x => x.Category).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
            RuleFor(x => x.Url).NotEmpty().MaximumLength(2000);
            RuleFor(x => x.Description).MaximumLength(1000);
            RuleFor(x => x.IconHint).MaximumLength(100);
        }
    }

    /// <summary>Handler que adiciona um link a um serviço existente.</summary>
    public sealed class Handler(
        IServiceAssetRepository serviceAssetRepository,
        IServiceLinkRepository serviceLinkRepository,
        ICatalogGraphUnitOfWork unitOfWork) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var serviceId = ServiceAssetId.From(request.ServiceAssetId);
            var service = await serviceAssetRepository.GetByIdAsync(serviceId, cancellationToken);
            if (service is null)
                return CatalogGraphErrors.ServiceAssetNotFoundById(request.ServiceAssetId);

            if (!Enum.TryParse<LinkCategory>(request.Category, ignoreCase: true, out var category))
                category = LinkCategory.Other;

            var link = ServiceLink.Create(
                serviceId,
                category,
                request.Title,
                request.Url,
                request.Description,
                request.IconHint,
                request.SortOrder);

            serviceLinkRepository.Add(link);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                link.Id.Value,
                link.ServiceAssetId.Value,
                link.Category.ToString(),
                link.Title,
                link.Url,
                link.Description,
                link.IconHint,
                link.SortOrder,
                link.CreatedAt);
        }
    }

    /// <summary>Resposta da adição de um link.</summary>
    public sealed record Response(
        Guid LinkId,
        Guid ServiceAssetId,
        string Category,
        string Title,
        string Url,
        string Description,
        string IconHint,
        int SortOrder,
        DateTimeOffset CreatedAt);
}
