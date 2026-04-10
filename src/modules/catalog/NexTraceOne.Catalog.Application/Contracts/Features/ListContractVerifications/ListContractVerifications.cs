using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts.Features.ListContractVerifications;

/// <summary>
/// Feature: ListContractVerifications — lista verificações de contrato com filtros
/// por serviço ou ativo de API, suportando paginação.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class ListContractVerifications
{
    /// <summary>Query de listagem de verificações de contrato com filtros e paginação.</summary>
    public sealed record Query(
        string? ServiceName,
        string? ApiAssetId,
        int Page = 1,
        int PageSize = 20) : IQuery<Response>;

    /// <summary>Valida os parâmetros de listagem de verificações.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        }
    }

    /// <summary>
    /// Handler que lista verificações de contrato filtradas por serviço ou ativo de API.
    /// Prioriza filtro por serviço quando ambos são informados.
    /// </summary>
    public sealed class Handler(IContractVerificationRepository repository)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            if (!string.IsNullOrWhiteSpace(request.ServiceName))
            {
                var items = await repository.ListByServiceAsync(
                    request.ServiceName, request.Page, request.PageSize, cancellationToken);

                var summaries = items.Select(v => new VerificationSummary(
                    v.Id.Value,
                    v.ApiAssetId,
                    v.ServiceName,
                    v.Status.ToString(),
                    v.BreakingChangesCount,
                    v.SourceSystem,
                    v.CommitSha,
                    v.VerifiedAt)).ToList();

                return new Response(summaries, summaries.Count);
            }

            if (!string.IsNullOrWhiteSpace(request.ApiAssetId))
            {
                var items = await repository.ListByApiAssetAsync(
                    request.ApiAssetId, cancellationToken);

                var paged = items
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(v => new VerificationSummary(
                        v.Id.Value,
                        v.ApiAssetId,
                        v.ServiceName,
                        v.Status.ToString(),
                        v.BreakingChangesCount,
                        v.SourceSystem,
                        v.CommitSha,
                        v.VerifiedAt))
                    .ToList();

                return new Response(paged, items.Count);
            }

            return new Response([], 0);
        }
    }

    /// <summary>Resumo de uma verificação de contrato para listagem.</summary>
    public sealed record VerificationSummary(
        Guid VerificationId,
        string ApiAssetId,
        string ServiceName,
        string Status,
        int BreakingChangesCount,
        string SourceSystem,
        string? CommitSha,
        DateTimeOffset VerifiedAt);

    /// <summary>Resposta paginada da listagem de verificações de contrato.</summary>
    public sealed record Response(
        IReadOnlyList<VerificationSummary> Items,
        int TotalCount);
}
