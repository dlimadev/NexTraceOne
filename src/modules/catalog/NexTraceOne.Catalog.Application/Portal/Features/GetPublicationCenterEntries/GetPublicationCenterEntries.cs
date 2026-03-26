using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Portal.Abstractions;
using NexTraceOne.Catalog.Domain.Portal.Enums;

namespace NexTraceOne.Catalog.Application.Portal.Features.GetPublicationCenterEntries;

/// <summary>
/// Feature: GetPublicationCenterEntries — lista as entradas do Publication Center com filtros opcionais.
/// Usada pela página de gestão de publicações onde os TechLeads e Platform Admins
/// veem o estado de publicação de todos os contratos no Developer Portal.
/// Estrutura VSA: Query + Validator + Handler + Response em ficheiro único.
/// </summary>
public static class GetPublicationCenterEntries
{
    /// <summary>Query para listar entradas do Publication Center.</summary>
    public sealed record Query(
        string? StatusFilter = null,
        Guid? ApiAssetId = null,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Valida os parâmetros da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>
    /// Handler que lista entradas do Publication Center.
    /// Suporta filtro por status e por ApiAsset.
    /// </summary>
    public sealed class Handler(IContractPublicationEntryRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            ContractPublicationStatus? statusFilter = null;
            if (request.StatusFilter is not null
                && Enum.TryParse<ContractPublicationStatus>(request.StatusFilter, ignoreCase: true, out var parsed))
            {
                statusFilter = parsed;
            }

            var entries = await repository.ListAsync(statusFilter, request.ApiAssetId, request.Page, request.PageSize, cancellationToken);

            var items = entries.Select(e => new PublicationEntryItem(
                PublicationEntryId: e.Id.Value,
                ContractVersionId: e.ContractVersionId,
                ApiAssetId: e.ApiAssetId,
                ContractTitle: e.ContractTitle,
                SemVer: e.SemVer,
                Status: e.Status.ToString(),
                Visibility: e.Visibility.ToString(),
                PublishedBy: e.PublishedBy,
                PublishedAt: e.PublishedAt,
                WithdrawnAt: e.WithdrawnAt,
                WithdrawalReason: e.WithdrawalReason,
                ReleaseNotes: e.ReleaseNotes)).ToList();

            return new Response(items, items.Count);
        }
    }

    /// <summary>Item de uma entrada do Publication Center.</summary>
    public sealed record PublicationEntryItem(
        Guid PublicationEntryId,
        Guid ContractVersionId,
        Guid ApiAssetId,
        string ContractTitle,
        string SemVer,
        string Status,
        string Visibility,
        string PublishedBy,
        DateTimeOffset? PublishedAt,
        DateTimeOffset? WithdrawnAt,
        string? WithdrawalReason,
        string? ReleaseNotes);

    /// <summary>Resposta com a lista paginada de entradas do Publication Center.</summary>
    public sealed record Response(IReadOnlyList<PublicationEntryItem> Items, int TotalCount);
}
