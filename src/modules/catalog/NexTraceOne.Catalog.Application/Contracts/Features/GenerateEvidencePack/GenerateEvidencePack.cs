using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Errors;
using NexTraceOne.Catalog.Domain.Contracts.Services;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

namespace NexTraceOne.Catalog.Application.Contracts.Features.GenerateEvidencePack;

/// <summary>
/// Feature: GenerateEvidencePack — gera o pacote de evidências técnicas para workflow.
/// Agrega diff semântico, scorecard, regras violadas e avaliação de impacto
/// em um único pacote auditável para decisões de governança.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class GenerateEvidencePack
{
    /// <summary>Limiar de risco acima do qual aprovação de workflow é obrigatória.</summary>
    private const decimal WorkflowApprovalRiskThreshold = 0.5m;
    /// <summary>Query para geração do evidence pack de uma versão de contrato.</summary>
    public sealed record Query(
        Guid ContractVersionId,
        string GeneratedBy) : IQuery<Response>;

    /// <summary>Valida a entrada da query de geração de evidence pack.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ContractVersionId).NotEmpty();
            RuleFor(x => x.GeneratedBy).NotEmpty().MaximumLength(500);
        }
    }

    /// <summary>
    /// Handler que orquestra a geração do evidence pack completo.
    /// Carrega a versão, computa o modelo canônico, avalia regras e scorecard,
    /// e agrega tudo em um pacote de evidências para workflow.
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

            // Usa o último diff computado para dados de breaking changes
            var latestDiff = version.Diffs.OrderByDescending(d => d.ComputedAt).FirstOrDefault();
            var changeLevel = latestDiff?.ChangeLevel ?? ChangeLevel.NonBreaking;
            var breakingCount = latestDiff?.BreakingChanges.Count ?? 0;
            var additiveCount = latestDiff?.AdditiveChanges.Count ?? 0;
            var nonBreakingCount = latestDiff?.NonBreakingChanges.Count ?? 0;
            var recommendedVersion = latestDiff?.SuggestedSemVer ?? version.SemVer;

            var requiresApproval = changeLevel == ChangeLevel.Breaking || scorecard.RiskScore > WorkflowApprovalRiskThreshold;
            var requiresNotification = changeLevel == ChangeLevel.Breaking;

            var executiveSummary = BuildExecutiveSummary(version, changeLevel, breakingCount, scorecard.OverallScore);
            var technicalSummary = BuildTechnicalSummary(version, canonicalModel, violations.Count, scorecard);

            var evidencePack = ContractEvidencePack.Create(
                version.Id,
                version.ApiAssetId,
                version.Protocol,
                version.SemVer,
                changeLevel,
                breakingCount,
                additiveCount,
                nonBreakingCount,
                recommendedVersion,
                scorecard.OverallScore,
                scorecard.RiskScore,
                violations.Count,
                requiresApproval,
                requiresNotification,
                executiveSummary,
                technicalSummary,
                [],
                dateTimeProvider.UtcNow,
                request.GeneratedBy);

            return new Response(
                evidencePack.Id.Value,
                request.ContractVersionId,
                version.ApiAssetId,
                version.Protocol.ToString(),
                version.SemVer,
                changeLevel,
                breakingCount,
                additiveCount,
                nonBreakingCount,
                recommendedVersion,
                scorecard.OverallScore,
                scorecard.RiskScore,
                violations.Count,
                requiresApproval,
                requiresNotification,
                executiveSummary,
                technicalSummary);
        }

        private static string BuildExecutiveSummary(
            ContractVersion version, ChangeLevel changeLevel, int breakingCount, decimal overallScore)
        {
            var levelDesc = changeLevel switch
            {
                ChangeLevel.Breaking => $"BREAKING change with {breakingCount} incompatible modification(s)",
                ChangeLevel.Additive => "Additive change with new capabilities",
                _ => "Non-breaking change with minor adjustments"
            };

            return $"Contract {version.SemVer} ({version.Protocol}): {levelDesc}. " +
                   $"Overall quality score: {overallScore:P0}.";
        }

        private static string BuildTechnicalSummary(
            ContractVersion version,
            ContractCanonicalModel model,
            int ruleViolationCount,
            ContractScorecard scorecard)
        {
            return $"Protocol: {version.Protocol}. " +
                   $"Operations: {model.OperationCount}. " +
                   $"Schemas: {model.SchemaCount}. " +
                   $"Security: {(model.HasSecurityDefinitions ? "defined" : "MISSING")}. " +
                   $"Rule violations: {ruleViolationCount}. " +
                   $"Quality: {scorecard.QualityScore:F2}, " +
                   $"Completeness: {scorecard.CompletenessScore:F2}, " +
                   $"Compatibility: {scorecard.CompatibilityScore:F2}, " +
                   $"Risk: {scorecard.RiskScore:F2}.";
        }
    }

    /// <summary>Resposta do evidence pack de uma versão de contrato.</summary>
    public sealed record Response(
        Guid EvidencePackId,
        Guid ContractVersionId,
        Guid ApiAssetId,
        string Protocol,
        string SemVer,
        ChangeLevel ChangeLevel,
        int BreakingChangeCount,
        int AdditiveChangeCount,
        int NonBreakingChangeCount,
        string RecommendedVersion,
        decimal OverallScore,
        decimal RiskScore,
        int RuleViolationCount,
        bool RequiresWorkflowApproval,
        bool RequiresChangeNotification,
        string ExecutiveSummary,
        string TechnicalSummary);
}
