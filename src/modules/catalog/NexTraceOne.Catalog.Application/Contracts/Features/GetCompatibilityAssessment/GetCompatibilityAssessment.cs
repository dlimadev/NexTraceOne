using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Contracts.Application.Abstractions;
using NexTraceOne.Contracts.Domain.Entities;
using NexTraceOne.Contracts.Domain.Errors;
using NexTraceOne.Contracts.Domain.Services;
using NexTraceOne.Contracts.Domain.ValueObjects;

namespace NexTraceOne.Contracts.Application.Features.GetCompatibilityAssessment;

/// <summary>
/// Feature: GetCompatibilityAssessment — avalia a compatibilidade entre duas versões de contrato.
/// Combina diff semântico, scorecard e análise de risco para gerar uma avaliação
/// completa de compatibilidade com recomendações de versão e readiness para workflow.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class GetCompatibilityAssessment
{
    /// <summary>Limiar de risco acima do qual aprovação de workflow é obrigatória.</summary>
    private const decimal WorkflowApprovalRiskThreshold = 0.5m;
    /// <summary>Query para avaliação de compatibilidade entre duas versões de contrato.</summary>
    public sealed record Query(Guid BaseVersionId, Guid TargetVersionId) : IQuery<Response>;

    /// <summary>Valida a entrada da query de compatibilidade.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.BaseVersionId).NotEmpty();
            RuleFor(x => x.TargetVersionId).NotEmpty();
        }
    }

    /// <summary>
    /// Handler que orquestra a avaliação de compatibilidade entre versões.
    /// Computa diff semântico, modelo canônico, scorecard e regras para gerar
    /// avaliação completa com recomendação de versão e readiness para workflow.
    /// </summary>
    public sealed class Handler(
        IContractVersionRepository repository,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var baseVersion = await repository.GetByIdAsync(
                ContractVersionId.From(request.BaseVersionId), cancellationToken);
            if (baseVersion is null)
                return ContractsErrors.ContractVersionNotFound(request.BaseVersionId.ToString());

            var targetVersion = await repository.GetByIdAsync(
                ContractVersionId.From(request.TargetVersionId), cancellationToken);
            if (targetVersion is null)
                return ContractsErrors.ContractVersionNotFound(request.TargetVersionId.ToString());

            // Computa diff semântico
            var diffResult = ContractDiffCalculator.ComputeDiff(
                baseVersion.SpecContent, targetVersion.SpecContent, targetVersion.Protocol);

            // Constrói modelo canônico e avalia regras da versão alvo
            var canonicalModel = CanonicalModelBuilder.Build(targetVersion.SpecContent, targetVersion.Protocol);
            var violations = ContractRuleEngine.Evaluate(
                targetVersion.Id, canonicalModel, targetVersion.SemVer, targetVersion.Protocol);
            var scorecard = ContractScorecardCalculator.Compute(
                targetVersion.Id, canonicalModel, targetVersion.Protocol, violations.Count, dateTimeProvider.UtcNow);

            // Calcula versão recomendada
            var baseSemVer = SemanticVersion.Parse(baseVersion.SemVer);
            var recommendedVersion = baseSemVer is null
                ? baseVersion.SemVer
                : diffResult.ChangeLevel switch
                {
                    ChangeLevel.Breaking => baseSemVer.BumpMajor().ToString(),
                    ChangeLevel.Additive => baseSemVer.BumpMinor().ToString(),
                    _ => baseSemVer.BumpPatch().ToString()
                };

            var isBackwardCompatible = diffResult.ChangeLevel != ChangeLevel.Breaking;
            var requiresApproval = diffResult.ChangeLevel == ChangeLevel.Breaking || scorecard.RiskScore > WorkflowApprovalRiskThreshold;
            var requiresNotification = diffResult.ChangeLevel == ChangeLevel.Breaking;

            var summary = BuildSummary(diffResult, scorecard, isBackwardCompatible);

            return new Response(
                request.BaseVersionId,
                request.TargetVersionId,
                diffResult.ChangeLevel,
                isBackwardCompatible,
                recommendedVersion,
                scorecard.RiskScore,
                diffResult.BreakingChanges.Count,
                diffResult.AdditiveChanges.Count,
                diffResult.NonBreakingChanges.Count,
                requiresApproval,
                requiresNotification,
                summary,
                targetVersion.Protocol.ToString(),
                scorecard.OverallScore);
        }

        private static string BuildSummary(
            OpenApiDiffCalculator.DiffResult diffResult,
            ContractScorecard scorecard,
            bool isBackwardCompatible)
        {
            var compat = isBackwardCompatible ? "backward compatible" : "NOT backward compatible";
            return $"Change is {compat}. " +
                   $"Breaking: {diffResult.BreakingChanges.Count}, " +
                   $"Additive: {diffResult.AdditiveChanges.Count}, " +
                   $"NonBreaking: {diffResult.NonBreakingChanges.Count}. " +
                   $"Overall score: {scorecard.OverallScore:F2}, Risk: {scorecard.RiskScore:F2}.";
        }
    }

    /// <summary>Resposta da avaliação de compatibilidade entre versões de contrato.</summary>
    public sealed record Response(
        Guid BaseVersionId,
        Guid TargetVersionId,
        ChangeLevel ChangeLevel,
        bool IsBackwardCompatible,
        string RecommendedVersion,
        decimal RiskScore,
        int BreakingChangeCount,
        int AdditiveChangeCount,
        int NonBreakingChangeCount,
        bool RequiresWorkflowApproval,
        bool RequiresChangeNotification,
        string Summary,
        string Protocol,
        decimal OverallScore);
}
