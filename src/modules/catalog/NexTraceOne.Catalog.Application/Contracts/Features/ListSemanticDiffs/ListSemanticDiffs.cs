using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Features.ListSemanticDiffs;

/// <summary>
/// Feature: ListSemanticDiffs — lista resultados de diff semântico que envolvam uma versão de contrato.
/// Permite visualizar o histórico de análises semânticas de uma versão (como origem ou destino).
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class ListSemanticDiffs
{
    /// <summary>Query de listagem de resultados de diff semântico por versão de contrato.</summary>
    public sealed record Query(string ContractVersionId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de listagem.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ContractVersionId).NotEmpty().MaximumLength(200);
        }
    }

    /// <summary>
    /// Handler que lista resultados de diff semântico envolvendo uma versão de contrato.
    /// </summary>
    public sealed class Handler(ISemanticDiffResultRepository repository)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var items = await repository.ListByContractVersionAsync(request.ContractVersionId, cancellationToken);

            var diffs = items.Select(r => new SemanticDiffListItem(
                r.Id.Value,
                r.ContractVersionFromId,
                r.ContractVersionToId,
                r.NaturalLanguageSummary,
                r.Classification,
                r.CompatibilityScore,
                r.GeneratedByModel,
                r.GeneratedAt)).ToList();

            return new Response(diffs, diffs.Count);
        }
    }

    /// <summary>Item resumido de um resultado de diff semântico.</summary>
    public sealed record SemanticDiffListItem(
        Guid SemanticDiffResultId,
        string ContractVersionFromId,
        string ContractVersionToId,
        string NaturalLanguageSummary,
        SemanticDiffClassification Classification,
        int CompatibilityScore,
        string GeneratedByModel,
        DateTimeOffset GeneratedAt);

    /// <summary>Resposta da listagem de resultados de diff semântico.</summary>
    public sealed record Response(
        IReadOnlyList<SemanticDiffListItem> Items,
        int TotalCount);
}
