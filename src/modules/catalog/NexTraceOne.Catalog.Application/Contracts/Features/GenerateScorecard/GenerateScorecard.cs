using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Errors;
using NexTraceOne.Catalog.Domain.Contracts.Services;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GenerateScorecard;

/// <summary>
/// Feature: GenerateScorecard — gera o scorecard técnico de uma versão de contrato.
/// Constrói o modelo canônico, avalia regras determinísticas e calcula scores
/// de qualidade, completude, compatibilidade e risco técnico.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class GenerateScorecard
{
    /// <summary>Query para geração do scorecard técnico de uma versão de contrato.</summary>
    public sealed record Query(Guid ContractVersionId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de geração de scorecard.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ContractVersionId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que orquestra a geração do scorecard técnico.
    /// Carrega a versão do contrato, constrói o modelo canônico, avalia regras
    /// e calcula os scores em cada dimensão.
    /// </summary>
    public sealed class Handler(
        IContractVersionRepository repository,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var version = await repository.GetByIdAsync(
                ContractVersionId.From(request.ContractVersionId), cancellationToken);
            if (version is null)
                return ContractsErrors.ContractVersionNotFound(request.ContractVersionId.ToString());

            var canonicalModel = CanonicalModelBuilder.Build(version.SpecContent, version.Protocol);
            var violations = ContractRuleEngine.Evaluate(version.Id, canonicalModel, version.SemVer, version.Protocol);
            var scorecard = ContractScorecardCalculator.Compute(
                version.Id, canonicalModel, version.Protocol, violations.Count, dateTimeProvider.UtcNow);

            return new Response(
                scorecard.Id.Value,
                request.ContractVersionId,
                scorecard.QualityScore,
                scorecard.CompletenessScore,
                scorecard.CompatibilityScore,
                scorecard.RiskScore,
                scorecard.OverallScore,
                scorecard.OperationCount,
                scorecard.SchemaCount,
                scorecard.HasSecurityDefinitions,
                scorecard.HasExamples,
                scorecard.HasDescriptions,
                scorecard.QualityJustification,
                scorecard.CompletenessJustification,
                scorecard.CompatibilityJustification,
                scorecard.RiskJustification);
        }
    }

    /// <summary>Resposta do scorecard técnico de uma versão de contrato.</summary>
    public sealed record Response(
        Guid ScorecardId,
        Guid ContractVersionId,
        decimal QualityScore,
        decimal CompletenessScore,
        decimal CompatibilityScore,
        decimal RiskScore,
        decimal OverallScore,
        int OperationCount,
        int SchemaCount,
        bool HasSecurityDefinitions,
        bool HasExamples,
        bool HasDescriptions,
        string QualityJustification,
        string CompletenessJustification,
        string CompatibilityJustification,
        string RiskJustification);
}
