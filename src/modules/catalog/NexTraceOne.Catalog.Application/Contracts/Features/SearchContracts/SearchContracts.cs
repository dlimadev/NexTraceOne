using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Domain.Results;
using NexTraceOne.Contracts.Application.Abstractions;
using NexTraceOne.Contracts.Domain.Enums;

namespace NexTraceOne.Contracts.Application.Features.SearchContracts;

/// <summary>
/// Feature: SearchContracts — pesquisa e filtra versões de contrato por protocolo,
/// estado de ciclo de vida, ativo de API e texto livre no SemVer.
/// Estrutura VSA: Query + Validator + Handler + Response em um único arquivo.
/// </summary>
public static class SearchContracts
{
    /// <summary>
    /// Query de pesquisa de contratos com filtros opcionais e paginação.
    /// </summary>
    public sealed record Query(
        ContractProtocol? Protocol,
        ContractLifecycleState? LifecycleState,
        Guid? ApiAssetId,
        string? SearchTerm,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>
    /// Valida os parâmetros de pesquisa, garantindo paginação dentro dos limites permitidos.
    /// </summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1);

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100);
        }
    }

    /// <summary>
    /// Handler que executa a pesquisa de versões de contrato com filtros e paginação.
    /// Delega ao repositório a construção da query filtrada.
    /// </summary>
    public sealed class Handler(IContractVersionRepository repository) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var (items, totalCount) = await repository.SearchAsync(
                request.Protocol,
                request.LifecycleState,
                request.ApiAssetId,
                request.SearchTerm,
                request.Page,
                request.PageSize,
                cancellationToken);

            var summaries = items
                .Select(v => new ContractVersionSummary(
                    v.Id.Value,
                    v.ApiAssetId,
                    v.SemVer,
                    v.Protocol,
                    v.LifecycleState,
                    v.IsLocked,
                    v.CreatedAt,
                    v.ImportedFrom))
                .ToList()
                .AsReadOnly();

            return new Response(summaries, totalCount, request.Page, request.PageSize);
        }
    }

    /// <summary>
    /// Resumo de uma versão de contrato retornado na pesquisa.
    /// </summary>
    public sealed record ContractVersionSummary(
        Guid VersionId,
        Guid ApiAssetId,
        string SemVer,
        ContractProtocol Protocol,
        ContractLifecycleState LifecycleState,
        bool IsLocked,
        DateTimeOffset CreatedAt,
        string ImportedFrom);

    /// <summary>
    /// Resposta paginada da pesquisa de contratos.
    /// </summary>
    public sealed record Response(
        IReadOnlyList<ContractVersionSummary> Items,
        int TotalCount,
        int Page,
        int PageSize);
}
