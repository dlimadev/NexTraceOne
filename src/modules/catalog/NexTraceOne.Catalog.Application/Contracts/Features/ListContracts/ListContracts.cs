using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Features.ListContracts;

/// <summary>
/// Feature: ListContracts — lista contratos do catálogo com filtros opcionais.
/// Retorna a versão mais recente de cada contrato (por ApiAssetId distinto),
/// oferecendo a visão principal de governança de contratos do NexTraceOne.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class ListContracts
{
    /// <summary>Query de listagem do catálogo de contratos com filtros e paginação.</summary>
    public sealed record Query(
        ContractProtocol? Protocol,
        ContractLifecycleState? LifecycleState,
        string? SearchTerm,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Valida os parâmetros de listagem.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>Handler que lista contratos mais recentes por API asset.</summary>
    public sealed class Handler(IContractVersionRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var (items, totalCount) = await repository.ListLatestPerApiAssetAsync(
                request.Protocol,
                request.LifecycleState,
                request.SearchTerm,
                request.Page,
                request.PageSize,
                cancellationToken);

            var contracts = items
                .Select(v => new ContractListItem(
                    v.Id.Value,
                    v.ApiAssetId,
                    v.SemVer,
                    v.Protocol.ToString(),
                    v.LifecycleState.ToString(),
                    v.IsLocked,
                    v.Format,
                    v.ImportedFrom,
                    v.CreatedAt,
                    v.DeprecationDate,
                    v.Signature is not null))
                .ToList();

            return new Response(contracts, totalCount, request.Page, request.PageSize);
        }
    }

    /// <summary>Item de contrato na listagem do catálogo de governança.</summary>
    public sealed record ContractListItem(
        Guid VersionId,
        Guid ApiAssetId,
        string SemVer,
        string Protocol,
        string LifecycleState,
        bool IsLocked,
        string Format,
        string ImportedFrom,
        DateTimeOffset CreatedAt,
        DateTimeOffset? DeprecationDate,
        bool IsSigned);

    /// <summary>Resposta paginada da listagem de contratos.</summary>
    public sealed record Response(
        IReadOnlyList<ContractListItem> Items,
        int TotalCount,
        int Page,
        int PageSize);
}
