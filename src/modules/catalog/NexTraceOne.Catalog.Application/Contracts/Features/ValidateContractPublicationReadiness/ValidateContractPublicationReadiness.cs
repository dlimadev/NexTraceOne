using Ardalis.GuardClauses;
using FluentValidation;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.ConfigurationKeys;
using NexTraceOne.Catalog.Application.Contracts.Abstractions;
using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.Errors;
using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Features.ValidateContractPublicationReadiness;

/// <summary>
/// Feature: ValidateContractPublicationReadiness — valida se uma versão de contrato
/// cumpre os requisitos de publicação definidos via parametrização.
/// Consulta parâmetros:
///   - catalog.contract.validation.block_on_lint_errors
///   - catalog.contract.publication.require_examples
///   - catalog.contract.creation.approval_required
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class ValidateContractPublicationReadiness
{
    /// <summary>Query para validação de publicação de uma versão de contrato.</summary>
    public sealed record Query(
        Guid ContractVersionId) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ContractVersionId).NotEmpty();
        }
    }

    /// <summary>Handler que avalia requisitos de publicação configurados via parametrização.</summary>
    public sealed class Handler(
        IContractVersionRepository contractVersionRepository,
        IConfigurationResolutionService configService,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var version = await contractVersionRepository.GetDetailAsync(
                Domain.Contracts.Entities.ContractVersionId.From(request.ContractVersionId),
                cancellationToken);

            if (version is null)
                return ContractsErrors.ContractVersionNotFound(request.ContractVersionId.ToString());

            var checks = new List<PublicationCheck>();

            // ── Check: Block on lint errors ──────────────────────
            var lintBlockConfig = await configService.ResolveEffectiveValueAsync(
                CatalogConfigKeys.ContractValidationBlockOnLintErrors,
                ConfigurationScope.Tenant,
                null,
                cancellationToken);

            if (lintBlockConfig?.EffectiveValue == "true")
            {
                var hasLintErrors = version.RuleViolations.Count > 0;
                checks.Add(new PublicationCheck(
                    "lint_errors",
                    "Contract version must not have rule violations",
                    !hasLintErrors,
                    hasLintErrors
                        ? $"Contract has {version.RuleViolations.Count} rule violation(s) — fix before publishing"
                        : "No rule violations"));
            }

            // ── Check: Require examples (via artifacts) ──────────
            var examplesConfig = await configService.ResolveEffectiveValueAsync(
                CatalogConfigKeys.ContractPublicationRequireExamples,
                ConfigurationScope.Tenant,
                null,
                cancellationToken);

            if (examplesConfig?.EffectiveValue == "true")
            {
                // Check for examples in spec content (OpenAPI 'examples' or 'example' fields)
                var specHasExamples = version.SpecContent.Contains("\"example\"", StringComparison.OrdinalIgnoreCase)
                    || version.SpecContent.Contains("\"examples\"", StringComparison.OrdinalIgnoreCase)
                    || version.SpecContent.Contains("example:", StringComparison.OrdinalIgnoreCase)
                    || version.SpecContent.Contains("examples:", StringComparison.OrdinalIgnoreCase);

                checks.Add(new PublicationCheck(
                    "require_examples",
                    "Contract must include request/response examples",
                    specHasExamples,
                    specHasExamples ? "Examples present in specification" : "No examples found — add examples before publishing"));
            }

            // ── Check: Approval required ─────────────────────────
            var approvalConfig = await configService.ResolveEffectiveValueAsync(
                CatalogConfigKeys.ContractCreationApprovalRequired,
                ConfigurationScope.Tenant,
                null,
                cancellationToken);

            if (approvalConfig?.EffectiveValue == "true")
            {
                var isApproved = version.LifecycleState >= ContractLifecycleState.Approved;
                checks.Add(new PublicationCheck(
                    "approval_required",
                    "Contract version requires approval before publication",
                    isApproved,
                    isApproved
                        ? "Contract version is approved"
                        : $"Contract version is in '{version.LifecycleState}' state — must reach 'Approved' or higher"));
            }

            var allPassed = checks.Count == 0 || checks.All(c => c.Passed);

            return new Response(
                ContractVersionId: request.ContractVersionId,
                SemVer: version.SemVer,
                LifecycleState: version.LifecycleState.ToString(),
                IsReadyToPublish: allPassed,
                TotalChecks: checks.Count,
                PassedChecks: checks.Count(c => c.Passed),
                FailedChecks: checks.Count(c => !c.Passed),
                Checks: checks,
                EvaluatedAt: dateTimeProvider.UtcNow);
        }
    }

    /// <summary>Resposta da validação de publicação de contrato.</summary>
    public sealed record Response(
        Guid ContractVersionId,
        string SemVer,
        string LifecycleState,
        bool IsReadyToPublish,
        int TotalChecks,
        int PassedChecks,
        int FailedChecks,
        List<PublicationCheck> Checks,
        DateTimeOffset EvaluatedAt);

    /// <summary>Resultado individual de uma verificação de publicação.</summary>
    public sealed record PublicationCheck(
        string CheckId,
        string Description,
        bool Passed,
        string Message);
}
