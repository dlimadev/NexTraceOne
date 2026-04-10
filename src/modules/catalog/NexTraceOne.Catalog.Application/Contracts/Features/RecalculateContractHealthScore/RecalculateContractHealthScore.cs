using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Errors;

namespace NexTraceOne.Catalog.Application.Contracts.Features.RecalculateContractHealthScore;

/// <summary>
/// Feature: RecalculateContractHealthScore — recalcula o score de saúde de um contrato (API Asset).
/// Avalia 6 dimensões de qualidade com base nos dados reais das versões do contrato
/// e persiste o resultado para consulta rápida no catálogo e em promotion gates.
/// Estrutura VSA: Command + Validator + Handler + Response em arquivo único.
/// </summary>
public static class RecalculateContractHealthScore
{
    /// <summary>Command para recalcular o score de saúde de um contrato.</summary>
    public sealed record Command(
        Guid ApiAssetId,
        int DegradationThreshold = 50) : ICommand<Response>;

    /// <summary>Valida a entrada do command de recálculo de score de saúde.</summary>
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ApiAssetId).NotEmpty();
            RuleFor(x => x.DegradationThreshold).InclusiveBetween(0, 100);
        }
    }

    /// <summary>
    /// Handler que orquestra o recálculo do score de saúde de um contrato.
    /// Carrega as versões do contrato, calcula cada dimensão a partir dos dados reais
    /// e persiste o score consolidado via repositório + UnitOfWork.
    /// </summary>
    public sealed class Handler(
        IContractHealthScoreRepository healthScoreRepository,
        IContractVersionRepository contractVersionRepository,
        IContractsUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var versions = await contractVersionRepository.ListByApiAssetAsync(
                request.ApiAssetId, cancellationToken);

            if (versions.Count == 0)
                return ContractsErrors.NoVersionsForHealthScore(request.ApiAssetId.ToString());

            var breakingScore = ComputeBreakingChangeFrequencyScore(versions);
            var consumerScore = ComputeConsumerImpactScore(versions);
            var reviewScore = ComputeReviewRecencyScore(versions);
            var exampleScore = ComputeExampleCoverageScore(versions);
            var policyScore = ComputePolicyComplianceScore(versions);
            var docScore = ComputeDocumentationScore(versions);

            var now = dateTimeProvider.UtcNow;

            var existing = await healthScoreRepository.GetByApiAssetIdAsync(
                request.ApiAssetId, cancellationToken);

            if (existing is not null)
            {
                existing.Recalculate(
                    breakingScore, consumerScore, reviewScore,
                    exampleScore, policyScore, docScore,
                    request.DegradationThreshold, now);

                await healthScoreRepository.UpdateAsync(existing, cancellationToken);
            }
            else
            {
                existing = ContractHealthScore.Create(
                    request.ApiAssetId,
                    breakingScore, consumerScore, reviewScore,
                    exampleScore, policyScore, docScore,
                    request.DegradationThreshold, now);

                await healthScoreRepository.AddAsync(existing, cancellationToken);
            }

            await unitOfWork.CommitAsync(cancellationToken);

            return new Response(
                existing.Id.Value,
                existing.ApiAssetId,
                existing.OverallScore,
                existing.BreakingChangeFrequencyScore,
                existing.ConsumerImpactScore,
                existing.ReviewRecencyScore,
                existing.ExampleCoverageScore,
                existing.PolicyComplianceScore,
                existing.DocumentationScore,
                existing.IsDegraded,
                existing.DegradationThreshold,
                existing.CalculatedAt);
        }

        /// <summary>
        /// Calcula score de frequência de breaking changes.
        /// Menos breaking changes (major bumps) = score mais alto.
        /// </summary>
        private static int ComputeBreakingChangeFrequencyScore(IReadOnlyList<ContractVersion> versions)
        {
            if (versions.Count <= 1)
                return 100;

            var ordered = versions.OrderBy(v => v.CreatedAt).ToList();
            var majorBumps = 0;

            for (var i = 1; i < ordered.Count; i++)
            {
                var prevMajor = ExtractMajorVersion(ordered[i - 1].SemVer);
                var curMajor = ExtractMajorVersion(ordered[i].SemVer);
                if (curMajor > prevMajor)
                    majorBumps++;
            }

            var ratio = (double)majorBumps / (ordered.Count - 1);
            return Math.Clamp((int)Math.Round((1.0 - ratio) * 100), 0, 100);
        }

        /// <summary>
        /// Calcula score de impacto nos consumidores.
        /// Contratos com consumer expectations registados indicam menor risco.
        /// Heurística: presença de referências a schemas e especificações bem estruturadas.
        /// </summary>
        private static int ComputeConsumerImpactScore(IReadOnlyList<ContractVersion> versions)
        {
            var latest = versions.OrderByDescending(v => v.CreatedAt).First();

            var hasSchemas = latest.SpecContent.Contains("#/components/schemas/", StringComparison.OrdinalIgnoreCase);
            var hasResponses = latest.SpecContent.Contains("\"responses\":", StringComparison.OrdinalIgnoreCase)
                               || latest.SpecContent.Contains("responses:", StringComparison.OrdinalIgnoreCase);

            var score = 50;
            if (hasSchemas) score += 25;
            if (hasResponses) score += 25;

            return Math.Clamp(score, 0, 100);
        }

        /// <summary>
        /// Calcula score de recência da última revisão.
        /// Versões mais recentes indicam contrato activamente mantido.
        /// </summary>
        private static int ComputeReviewRecencyScore(IReadOnlyList<ContractVersion> versions)
        {
            var latest = versions.OrderByDescending(v => v.CreatedAt).First();
            var isApproved = latest.LifecycleState is ContractLifecycleState.Approved
                or ContractLifecycleState.Locked;
            var isNotDeprecated = latest.LifecycleState is not ContractLifecycleState.Deprecated
                and not ContractLifecycleState.Sunset
                and not ContractLifecycleState.Retired;

            var score = 40;
            if (isApproved) score += 30;
            if (isNotDeprecated) score += 30;

            return Math.Clamp(score, 0, 100);
        }

        /// <summary>
        /// Calcula score de cobertura de exemplos no contrato.
        /// </summary>
        private static int ComputeExampleCoverageScore(IReadOnlyList<ContractVersion> versions)
        {
            var latest = versions.OrderByDescending(v => v.CreatedAt).First();

            var hasExamples = latest.SpecContent.Contains("\"example\":", StringComparison.OrdinalIgnoreCase)
                              || latest.SpecContent.Contains("example:", StringComparison.OrdinalIgnoreCase);
            var hasExamplesPlural = latest.SpecContent.Contains("\"examples\":", StringComparison.OrdinalIgnoreCase)
                                   || latest.SpecContent.Contains("examples:", StringComparison.OrdinalIgnoreCase);

            if (hasExamples && hasExamplesPlural)
                return 100;
            if (hasExamples || hasExamplesPlural)
                return 60;

            return 0;
        }

        /// <summary>
        /// Calcula score de conformidade com políticas.
        /// Usa violações de regras como indicador inverso.
        /// </summary>
        private static int ComputePolicyComplianceScore(IReadOnlyList<ContractVersion> versions)
        {
            var latest = versions.OrderByDescending(v => v.CreatedAt).First();
            var violationCount = latest.RuleViolations.Count;

            return violationCount switch
            {
                0 => 100,
                <= 2 => 80,
                <= 5 => 60,
                <= 10 => 40,
                _ => 20
            };
        }

        /// <summary>
        /// Calcula score de documentação do contrato.
        /// Verifica presença de descrições, info, e conteúdo estruturado.
        /// </summary>
        private static int ComputeDocumentationScore(IReadOnlyList<ContractVersion> versions)
        {
            var latest = versions.OrderByDescending(v => v.CreatedAt).First();
            var score = 0;

            if (latest.SpecContent.Contains("\"description\":", StringComparison.OrdinalIgnoreCase)
                || latest.SpecContent.Contains("description:", StringComparison.OrdinalIgnoreCase))
                score += 40;

            if (latest.SpecContent.Contains("\"info\":", StringComparison.OrdinalIgnoreCase)
                || latest.SpecContent.Contains("info:", StringComparison.OrdinalIgnoreCase))
                score += 30;

            if (latest.SpecContent.Contains("\"summary\":", StringComparison.OrdinalIgnoreCase)
                || latest.SpecContent.Contains("summary:", StringComparison.OrdinalIgnoreCase))
                score += 30;

            return Math.Clamp(score, 0, 100);
        }

        private static int ExtractMajorVersion(string semVer)
        {
            var dot = semVer.IndexOf('.', StringComparison.Ordinal);
            if (dot < 0) return 0;
            return int.TryParse(semVer[..dot], out var major) ? major : 0;
        }
    }

    /// <summary>Resposta do recálculo do score de saúde de um contrato.</summary>
    public sealed record Response(
        Guid HealthScoreId,
        Guid ApiAssetId,
        int OverallScore,
        int BreakingChangeFrequencyScore,
        int ConsumerImpactScore,
        int ReviewRecencyScore,
        int ExampleCoverageScore,
        int PolicyComplianceScore,
        int DocumentationScore,
        bool IsDegraded,
        int DegradationThreshold,
        DateTimeOffset CalculatedAt);
}
