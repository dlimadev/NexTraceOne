using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Features.ListSchemaEvolutionAdvices;

/// <summary>
/// Feature: ListSchemaEvolutionAdvices — lista análises de evolução de schema com filtro opcional por API Asset.
/// Permite visualizar o histórico de análises de compatibilidade de um contrato.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class ListSchemaEvolutionAdvices
{
    /// <summary>Query de listagem de análises de evolução de schema com filtro opcional.</summary>
    public sealed record Query(Guid? ApiAssetId = null) : IQuery<Response>;

    /// <summary>Valida a entrada da query de listagem.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            When(x => x.ApiAssetId.HasValue, () =>
                RuleFor(x => x.ApiAssetId!.Value).NotEmpty());
        }
    }

    /// <summary>
    /// Handler que lista análises de evolução de schema, opcionalmente filtradas por API Asset.
    /// </summary>
    public sealed class Handler(ISchemaEvolutionAdviceRepository repository)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var items = await repository.ListByApiAssetAsync(request.ApiAssetId, cancellationToken);

            var advices = items.Select(a => new AdviceListItem(
                a.Id.Value,
                a.ApiAssetId,
                a.ContractName,
                a.SourceVersion,
                a.TargetVersion,
                a.CompatibilityLevel,
                a.CompatibilityScore,
                a.RecommendedStrategy,
                a.AffectedConsumerCount,
                a.AnalyzedAt,
                a.AnalyzedByAgentName)).ToList();

            return new Response(advices, advices.Count);
        }
    }

    /// <summary>Item resumido de uma análise de evolução de schema.</summary>
    public sealed record AdviceListItem(
        Guid AdviceId,
        Guid ApiAssetId,
        string ContractName,
        string SourceVersion,
        string TargetVersion,
        CompatibilityLevel CompatibilityLevel,
        int CompatibilityScore,
        MigrationStrategy RecommendedStrategy,
        int AffectedConsumerCount,
        DateTimeOffset AnalyzedAt,
        string? AnalyzedByAgentName);

    /// <summary>Resposta da listagem de análises de evolução de schema.</summary>
    public sealed record Response(
        IReadOnlyList<AdviceListItem> Items,
        int TotalCount);
}
