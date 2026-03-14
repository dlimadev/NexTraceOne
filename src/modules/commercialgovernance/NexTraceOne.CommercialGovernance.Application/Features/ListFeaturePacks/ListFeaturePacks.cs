using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.CommercialCatalog.Application.Abstractions;

namespace NexTraceOne.CommercialCatalog.Application.Features.ListFeaturePacks;

/// <summary>
/// Feature: ListFeaturePacks — query para listar pacotes de funcionalidades.
///
/// Usada pelo backoffice para exibir pacotes cadastrados e suas capabilities.
/// Suporta filtro opcional por estado ativo.
///
/// Permissão requerida: licensing:vendor:featurepack:read
/// </summary>
public static class ListFeaturePacks
{
    /// <summary>Query para listar pacotes com filtro opcional por estado ativo.</summary>
    public sealed record Query(bool? ActiveOnly = null) : IQuery<Response>;

    /// <summary>Valida os parâmetros da query (nenhuma regra obrigatória).</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
    }

    /// <summary>
    /// Handler que retorna uma lista de pacotes de funcionalidades com seus itens.
    /// </summary>
    public sealed class Handler(
        IFeaturePackRepository featurePackRepository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var packs = await featurePackRepository.ListAsync(request.ActiveOnly, cancellationToken);

            var items = packs.Select(fp => new FeaturePackItem(
                fp.Id.Value,
                fp.Code,
                fp.Name,
                fp.Description,
                fp.IsActive,
                fp.Items.Select(i => new FeaturePackCapability(
                    i.CapabilityCode,
                    i.CapabilityName,
                    i.DefaultLimit)).ToList())).ToList();

            return new Response(items);
        }
    }

    /// <summary>Capability dentro de um pacote, para listagem.</summary>
    public sealed record FeaturePackCapability(
        string CapabilityCode,
        string CapabilityName,
        int? DefaultLimit);

    /// <summary>Item resumido de pacote de funcionalidades para listagem.</summary>
    public sealed record FeaturePackItem(
        Guid FeaturePackId,
        string Code,
        string Name,
        string? Description,
        bool IsActive,
        IReadOnlyList<FeaturePackCapability> Capabilities);

    /// <summary>Resposta da listagem de pacotes de funcionalidades.</summary>
    public sealed record Response(IReadOnlyList<FeaturePackItem> Items);
}
