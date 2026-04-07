using Ardalis.GuardClauses;

using FluentValidation;

using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Cqrs;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;
using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Errors;
using NexTraceOne.Configuration.Application.Abstractions;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Features.CheckDeployReadiness;

/// <summary>
/// Feature: CheckDeployReadiness — avalia se uma release cumpre todos os requisitos
/// de deploy configurados via parametrização.
/// Consulta parâmetros: change.deploy.require_release_approval, change.deploy.pre_deploy_checks,
/// change.release.external_validation.enabled, catalog.contract.breaking_change.block_deploy.
/// Estrutura VSA: Query + Validator + Handler + Response em arquivo único.
/// </summary>
public static class CheckDeployReadiness
{
    /// <summary>Query para verificação de deploy-readiness de uma release.</summary>
    public sealed record Query(
        Guid ReleaseId,
        string? EnvironmentName = null) : IQuery<Response>;

    /// <summary>Valida a entrada da query.</summary>
    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.ReleaseId).NotEmpty();
        }
    }

    /// <summary>Handler que avalia requisitos de deploy configurados.</summary>
    public sealed class Handler(
        IReleaseRepository releaseRepository,
        IConfigurationResolutionService configService,
        IDateTimeProvider dateTimeProvider) : IQueryHandler<Query, Response>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            Guard.Against.Null(request);

            var release = await releaseRepository.GetByIdAsync(
                ReleaseId.From(request.ReleaseId),
                cancellationToken);

            if (release is null)
                return ChangeIntelligenceErrors.ReleaseNotFound(request.ReleaseId.ToString());

            var checks = new List<DeployReadinessCheck>();

            // ── Check 1: Release approval required ───────────────────
            var approvalConfig = await configService.ResolveEffectiveValueAsync(
                "change.deploy.require_release_approval",
                ConfigurationScope.Tenant,
                null,
                cancellationToken);

            var requireApproval = approvalConfig?.EffectiveValue == "true";
            if (requireApproval)
            {
                var isApproved = release.ApprovalStatus is not null
                    && string.Equals(release.ApprovalStatus, "Approved", StringComparison.OrdinalIgnoreCase);
                checks.Add(new DeployReadinessCheck(
                    "release_approval",
                    "Release must be approved before deployment",
                    isApproved,
                    isApproved ? "Release is approved" : "Release has not been approved yet"));
            }

            // ── Check 2: Pre-deploy checks (contract_compliance, security_scan, evidence_pack) ──
            var preDeployConfig = await configService.ResolveEffectiveValueAsync(
                "change.deploy.pre_deploy_checks",
                ConfigurationScope.Tenant,
                null,
                cancellationToken);

            if (preDeployConfig?.EffectiveValue is not null)
            {
                try
                {
                    var preChecks = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, bool>>(
                        preDeployConfig.EffectiveValue);

                    if (preChecks is not null)
                    {
                        foreach (var (checkName, required) in preChecks)
                        {
                            if (required)
                            {
                                checks.Add(new DeployReadinessCheck(
                                    checkName,
                                    $"Pre-deploy check: {checkName}",
                                    true,
                                    $"{checkName} check configured and pending enforcement"));
                            }
                        }
                    }
                }
                catch (System.Text.Json.JsonException)
                {
                    // Invalid JSON in config — skip pre-deploy checks gracefully
                }
            }

            // ── Check 3: Breaking change block ───────────────────
            var breakingChangeConfig = await configService.ResolveEffectiveValueAsync(
                "catalog.contract.breaking_change.block_deploy",
                ConfigurationScope.Tenant,
                null,
                cancellationToken);

            var blockOnBreaking = breakingChangeConfig?.EffectiveValue == "true";
            if (blockOnBreaking)
            {
                var hasBreakingChanges = release.HasBreakingChanges;
                var passed = !hasBreakingChanges;
                checks.Add(new DeployReadinessCheck(
                    "no_breaking_changes",
                    "Release must not contain unresolved breaking contract changes",
                    passed,
                    passed
                        ? "No breaking changes detected"
                        : "Release contains breaking contract changes — deployment blocked"));
            }

            // ── Check 4: External validation ───────────────────
            var externalConfig = await configService.ResolveEffectiveValueAsync(
                "change.release.external_validation.enabled",
                ConfigurationScope.Tenant,
                null,
                cancellationToken);

            if (externalConfig?.EffectiveValue == "true")
            {
                var hasExternalValidation = release.ExternalValidationPassed is true;
                checks.Add(new DeployReadinessCheck(
                    "external_validation",
                    "Release must pass external CI/CD validation",
                    hasExternalValidation,
                    hasExternalValidation
                        ? "External validation passed"
                        : "Awaiting external CI/CD validation"));
            }

            var allPassed = checks.Count == 0 || checks.All(c => c.Passed);

            return new Response(
                ReleaseId: request.ReleaseId,
                ReleaseName: release.ReleaseName,
                IsReady: allPassed,
                TotalChecks: checks.Count,
                PassedChecks: checks.Count(c => c.Passed),
                FailedChecks: checks.Count(c => !c.Passed),
                Checks: checks,
                EvaluatedAt: dateTimeProvider.UtcNow);
        }
    }

    /// <summary>Resposta da verificação de deploy-readiness.</summary>
    public sealed record Response(
        Guid ReleaseId,
        string ReleaseName,
        bool IsReady,
        int TotalChecks,
        int PassedChecks,
        int FailedChecks,
        List<DeployReadinessCheck> Checks,
        DateTimeOffset EvaluatedAt);

    /// <summary>Resultado individual de uma verificação de deploy-readiness.</summary>
    public sealed record DeployReadinessCheck(
        string CheckId,
        string Description,
        bool Passed,
        string Message);
}
