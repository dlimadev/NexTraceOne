using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Features.AnalyzeSchemaEvolution;

/// <summary>
/// Feature: AnalyzeSchemaEvolution — cria uma análise de evolução de schema entre duas versões de contrato.
/// Produz relatório estruturado com score de compatibilidade, campos afetados,
/// consumidores impactados e estratégia de migração recomendada.
/// Estrutura VSA: Command + Validator + Handler + Response em arquivo único.
/// </summary>
public static class AnalyzeSchemaEvolution
{
    /// <summary>Comando para analisar a evolução de schema entre duas versões de contrato.</summary>
    public sealed record Command(
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
        string? AnalyzedByAgentName = null,
        Guid? TenantId = null) : ICommand<Response>;

    /// <summary>Valida a entrada do comando de análise de evolução de schema.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.ContractName).NotEmpty().MaximumLength(500);
            RuleFor(x => x.SourceVersion).NotEmpty().MaximumLength(50);
            RuleFor(x => x.TargetVersion).NotEmpty().MaximumLength(50);
            RuleFor(x => x.CompatibilityLevel).IsInEnum();
            RuleFor(x => x.CompatibilityScore).InclusiveBetween(0, 100);
            RuleFor(x => x.AffectedConsumerCount).GreaterThanOrEqualTo(0);
            RuleFor(x => x.RecommendedStrategy).IsInEnum();
        }
    }

    /// <summary>
    /// Handler que cria e persiste uma análise de evolução de schema.
    /// Delega a criação ao factory method SchemaEvolutionAdvice.Analyze.
    /// </summary>
    public sealed class Handler(
        ISchemaEvolutionAdviceRepository repository,
        IContractsUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var advice = SchemaEvolutionAdvice.Analyze(
                request.ApiAssetId,
                request.ContractName,
                request.SourceVersion,
                request.TargetVersion,
                request.CompatibilityLevel,
                request.CompatibilityScore,
                request.FieldsAdded,
                request.FieldsRemoved,
                request.FieldsModified,
                request.FieldsInUseByConsumers,
                request.AffectedConsumers,
                request.AffectedConsumerCount,
                request.RecommendedStrategy,
                request.StrategyDetails,
                request.Recommendations,
                request.Warnings,
                dateTimeProvider.UtcNow,
                request.AnalyzedByAgentName,
                request.TenantId);

            await repository.AddAsync(advice, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                advice.Id.Value,
                advice.ApiAssetId,
                advice.ContractName,
                advice.SourceVersion,
                advice.TargetVersion,
                advice.CompatibilityLevel,
                advice.CompatibilityScore,
                advice.RecommendedStrategy,
                advice.AffectedConsumerCount,
                advice.AnalyzedAt);
        }
    }

    /// <summary>Resposta da análise de evolução de schema.</summary>
    public sealed record Response(
        Guid AdviceId,
        Guid ApiAssetId,
        string ContractName,
        string SourceVersion,
        string TargetVersion,
        CompatibilityLevel CompatibilityLevel,
        int CompatibilityScore,
        MigrationStrategy RecommendedStrategy,
        int AffectedConsumerCount,
        DateTimeOffset AnalyzedAt);
}
