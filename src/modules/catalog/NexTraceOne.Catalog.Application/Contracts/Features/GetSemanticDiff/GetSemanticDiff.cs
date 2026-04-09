using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetSemanticDiff;

/// <summary>
/// Feature: GetSemanticDiff — obtém um resultado de diff semântico por identificador.
/// Retorna o relatório completo com sumário, classificação, consumidores afetados e mitigação.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class GetSemanticDiff
{
    /// <summary>Query para obter um resultado de diff semântico por Id.</summary>
    public sealed record Query(Guid SemanticDiffResultId) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.SemanticDiffResultId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que obtém o resultado de diff semântico por Id.
    /// </summary>
    public sealed class Handler(ISemanticDiffResultRepository repository)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var result = await repository.GetByIdAsync(
                SemanticDiffResultId.From(request.SemanticDiffResultId), cancellationToken);

            if (result is null)
                return ContractsErrors.SemanticDiffResultNotFound(request.SemanticDiffResultId.ToString());

            return new Response(
                result.Id.Value,
                result.ContractVersionFromId,
                result.ContractVersionToId,
                result.NaturalLanguageSummary,
                result.Classification,
                result.AffectedConsumers,
                result.MitigationSuggestions,
                result.CompatibilityScore,
                result.GeneratedByModel,
                result.GeneratedAt,
                result.TenantId);
        }
    }

    /// <summary>Resposta completa de um resultado de diff semântico.</summary>
    public sealed record Response(
        Guid SemanticDiffResultId,
        string ContractVersionFromId,
        string ContractVersionToId,
        string NaturalLanguageSummary,
        SemanticDiffClassification Classification,
        string? AffectedConsumers,
        string? MitigationSuggestions,
        int CompatibilityScore,
        string GeneratedByModel,
        DateTimeOffset GeneratedAt,
        string? TenantId);
}
