using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GetSchemaEvolutionAdvice;

/// <summary>
/// Feature: GetSchemaEvolutionAdvice — obtém uma análise de evolução de schema por identificador.
/// Retorna o relatório completo com score, campos afetados, consumidores e recomendações.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class GetSchemaEvolutionAdvice
{
    /// <summary>Query para obter uma análise de evolução de schema por Id.</summary>
    public sealed record Query(Guid AdviceId) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.AdviceId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que obtém a análise de evolução de schema por Id.
    /// </summary>
    public sealed class Handler(ISchemaEvolutionAdviceRepository repository)
        : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var advice = await repository.GetByIdAsync(
                SchemaEvolutionAdviceId.From(request.AdviceId), cancellationToken);

            if (advice is null)
                return ContractsErrors.SchemaEvolutionAdviceNotFound(request.AdviceId.ToString());

            return new Response(
                advice.Id.Value,
                advice.ApiAssetId,
                advice.ContractName,
                advice.SourceVersion,
                advice.TargetVersion,
                advice.CompatibilityLevel,
                advice.CompatibilityScore,
                advice.FieldsAdded,
                advice.FieldsRemoved,
                advice.FieldsModified,
                advice.FieldsInUseByConsumers,
                advice.AffectedConsumers,
                advice.AffectedConsumerCount,
                advice.RecommendedStrategy,
                advice.StrategyDetails,
                advice.Recommendations,
                advice.Warnings,
                advice.AnalyzedAt,
                advice.AnalyzedByAgentName);
        }
    }

    /// <summary>Resposta completa de uma análise de evolução de schema.</summary>
    public sealed record Response(
        Guid AdviceId,
        Guid ApiAssetId,
        string ContractName,
        string SourceVersion,
        string TargetVersion,
        CompatibilityLevel CompatibilityLevel,
        int CompatibilityScore,
        string? FieldsAdded,
        string? FieldsRemoved,
        string? FieldsModified,
        string? FieldsInUseByConsumers,
        string? AffectedConsumers,
        int AffectedConsumerCount,
        MigrationStrategy RecommendedStrategy,
        string? StrategyDetails,
        string? Recommendations,
        string? Warnings,
        DateTimeOffset AnalyzedAt,
        string? AnalyzedByAgentName);
}
