using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

using ClassifyBreakingChangeFeature = NexTraceOne.Catalog.Application.Contracts.Features.ClassifyBreakingChange.ClassifyBreakingChange;
using ParseSpecPreviewFeature = NexTraceOne.Catalog.Application.Contracts.Features.ParseSpecPreview.ParseSpecPreview;
using ComputeSemanticDiffFeature = NexTraceOne.Catalog.Application.Contracts.Features.ComputeSemanticDiff.ComputeSemanticDiff;
using CreateContractVersionFeature = NexTraceOne.Catalog.Application.Contracts.Features.CreateContractVersion.CreateContractVersion;
using DeprecateContractVersionFeature = NexTraceOne.Catalog.Application.Contracts.Features.DeprecateContractVersion.DeprecateContractVersion;
using ExportContractFeature = NexTraceOne.Catalog.Application.Contracts.Features.ExportContract.ExportContract;
using GetContractHistoryFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetContractHistory.GetContractHistory;
using GetContractVersionDetailFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetContractVersionDetail.GetContractVersionDetail;
using GetContractsSummaryFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetContractsSummary.GetContractsSummary;
using ImportContractFeature = NexTraceOne.Catalog.Application.Contracts.Features.ImportContract.ImportContract;
using ListContractsFeature = NexTraceOne.Catalog.Application.Contracts.Features.ListContracts.ListContracts;
using ListContractsByServiceFeature = NexTraceOne.Catalog.Application.Contracts.Features.ListContractsByService.ListContractsByService;
using LockContractVersionFeature = NexTraceOne.Catalog.Application.Contracts.Features.LockContractVersion.LockContractVersion;
using SignContractVersionFeature = NexTraceOne.Catalog.Application.Contracts.Features.SignContractVersion.SignContractVersion;
using SuggestSemanticVersionFeature = NexTraceOne.Catalog.Application.Contracts.Features.SuggestSemanticVersion.SuggestSemanticVersion;
using TransitionLifecycleStateFeature = NexTraceOne.Catalog.Application.Contracts.Features.TransitionLifecycleState.TransitionLifecycleState;
using ListRuleViolationsFeature = NexTraceOne.Catalog.Application.Contracts.Features.ListRuleViolations.ListRuleViolations;
using SearchContractsFeature = NexTraceOne.Catalog.Application.Contracts.Features.SearchContracts.SearchContracts;
using SyncContractsFeature = NexTraceOne.Catalog.Application.Contracts.Features.SyncContracts.SyncContracts;
using ValidateContractIntegrityFeature = NexTraceOne.Catalog.Application.Contracts.Features.ValidateContractIntegrity.ValidateContractIntegrity;
using VerifySignatureFeature = NexTraceOne.Catalog.Application.Contracts.Features.VerifySignature.VerifySignature;
using GenerateScorecardFeature = NexTraceOne.Catalog.Application.Contracts.Features.GenerateScorecard.GenerateScorecard;
using GenerateEvidencePackFeature = NexTraceOne.Catalog.Application.Contracts.Features.GenerateEvidencePack.GenerateEvidencePack;
using EvaluateContractRulesFeature = NexTraceOne.Catalog.Application.Contracts.Features.EvaluateContractRules.EvaluateContractRules;
using GetCompatibilityAssessmentFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetCompatibilityAssessment.GetCompatibilityAssessment;
using RegisterContractDeploymentFeature = NexTraceOne.Catalog.Application.Contracts.Features.RegisterContractDeployment.RegisterContractDeployment;
using GetContractDeploymentsFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetContractDeployments.GetContractDeployments;
using SearchCanonicalEntitiesFeature = NexTraceOne.Catalog.Application.Contracts.Features.SearchCanonicalEntities.SearchCanonicalEntities;
using ListCanonicalEntityVersionsFeature = NexTraceOne.Catalog.Application.Contracts.Features.ListCanonicalEntityVersions.ListCanonicalEntityVersions;
using DiffCanonicalEntityVersionsFeature = NexTraceOne.Catalog.Application.Contracts.Features.DiffCanonicalEntityVersions.DiffCanonicalEntityVersions;
using GetCanonicalEntityUsagesFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetCanonicalEntityUsages.GetCanonicalEntityUsages;
using GetCanonicalEntityImpactFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetCanonicalEntityImpact.GetCanonicalEntityImpact;
using PropagateCanonicalEntityChangeFeature = NexTraceOne.Catalog.Application.Contracts.Features.PropagateCanonicalEntityChange.PropagateCanonicalEntityChange;
using GenerateMockConfigurationFeature = NexTraceOne.Catalog.Application.Contracts.Features.GenerateMockConfiguration.GenerateMockConfiguration;
using EvaluateDesignGuidelinesFeature = NexTraceOne.Catalog.Application.Contracts.Features.EvaluateDesignGuidelines.EvaluateDesignGuidelines;
using ExportContractMultiFormatFeature = NexTraceOne.Catalog.Application.Contracts.Features.ExportContractMultiFormat.ExportContractMultiFormat;
using RegisterConsumerExpectationFeature = NexTraceOne.Catalog.Application.Contracts.Features.RegisterConsumerExpectation.RegisterConsumerExpectation;
using GetContractConsumerExpectationsFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetContractConsumerExpectations.GetContractConsumerExpectations;
using VerifyProviderCompatibilityFeature = NexTraceOne.Catalog.Application.Contracts.Features.VerifyProviderCompatibility.VerifyProviderCompatibility;
using InferDependenciesFromContractsFeature = NexTraceOne.Catalog.Application.Contracts.Features.InferDependenciesFromContracts.InferDependenciesFromContracts;
using GenerateSemanticChangelogFeature = NexTraceOne.Catalog.Application.Contracts.Features.GenerateSemanticChangelog.GenerateSemanticChangelog;
using ComputeContractHealthDashboardFeature = NexTraceOne.Catalog.Application.Contracts.Features.ComputeContractHealthDashboard.ComputeContractHealthDashboard;
using SuggestSchemaFromContextFeature = NexTraceOne.Catalog.Application.Contracts.Features.SuggestSchemaFromContext.SuggestSchemaFromContext;
using InitiateContractDeprecationFeature = NexTraceOne.Catalog.Application.Contracts.Features.InitiateContractDeprecation.InitiateContractDeprecation;
using GetDeprecationProgressFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetDeprecationProgress.GetDeprecationProgress;
using DetectContractDriftFeature = NexTraceOne.Catalog.Application.Contracts.Features.DetectContractDrift.DetectContractDrift;
using GetContractHealthTimelineFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetContractHealthTimeline.GetContractHealthTimeline;
using GetCanonicalEntityImpactCascadeFeature = NexTraceOne.Catalog.Application.Contracts.Features.GetCanonicalEntityImpactCascade.GetCanonicalEntityImpactCascade;
using ValidatePublicationReadinessFeature = NexTraceOne.Catalog.Application.Contracts.Features.ValidateContractPublicationReadiness.ValidateContractPublicationReadiness;

namespace NexTraceOne.Catalog.API.Contracts.Endpoints.Endpoints;

/// <summary>
/// Registra todos os endpoints Minimal API do módulo Contracts.
/// Descoberto automaticamente pelo ApiHost via assembly scanning.
/// Suporta multi-protocolo: OpenAPI, Swagger, WSDL, AsyncAPI.
/// Inclui lifecycle management, assinatura digital e verificação de integridade.
///
/// Política de autorização:
/// - Endpoints de leitura (history, detail, export, classification, search, verify, validate)
///   exigem "contracts:read".
/// - Endpoints de escrita (import, create version, lock, lifecycle, deprecate, sign, sync)
///   exigem "contracts:write".
/// - Endpoint de importação exige "contracts:import" para manter distinção granular.
/// </summary>
public sealed class ContractsEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/contracts");

        // ── Contract Catalog (Governança) ───────────────────────

        group.MapGet("/list", async (
            ContractProtocol? protocol,
            ContractLifecycleState? lifecycleState,
            string? searchTerm,
            int? page,
            int? pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListContractsFeature.Query(
                protocol, lifecycleState, searchTerm, page ?? 1, pageSize ?? 20), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        group.MapGet("/summary", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetContractsSummaryFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        group.MapGet("/by-service/{serviceId:guid}", async (
            Guid serviceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListContractsByServiceFeature.Query(serviceId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        // ── Import & Versioning ─────────────────────────────────

        group.MapPost("/", async (
            ImportContractFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult("/api/v1/contracts/{0}", localizer);
        }).RequirePermission("contracts:import");

        group.MapPost("/versions", async (
            CreateContractVersionFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult("/api/v1/contracts/{0}", localizer);
        }).RequirePermission("contracts:write");

        // ── Diff & Classification ───────────────────────────────

        group.MapPost("/diff", async (
            ComputeSemanticDiffFeature.Query query,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        group.MapGet("/{contractVersionId:guid}/classification", async (
            Guid contractVersionId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ClassifyBreakingChangeFeature.Query(contractVersionId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        group.MapGet("/suggest-version", async (
            Guid apiAssetId,
            ChangeLevel changeLevel,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new SuggestSemanticVersionFeature.Query(apiAssetId, changeLevel), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        // ── History & Detail ────────────────────────────────────

        group.MapGet("/history/{apiAssetId:guid}", async (
            Guid apiAssetId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetContractHistoryFeature.Query(apiAssetId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        group.MapGet("/{contractVersionId:guid}/detail", async (
            Guid contractVersionId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetContractVersionDetailFeature.Query(contractVersionId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        group.MapGet("/{contractVersionId:guid}/export", async (
            Guid contractVersionId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ExportContractFeature.Query(contractVersionId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        // ── Lifecycle ───────────────────────────────────────────

        group.MapPost("/{contractVersionId:guid}/lock", async (
            Guid contractVersionId,
            LockContractVersionFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { ContractVersionId = contractVersionId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:write");

        group.MapPost("/{contractVersionId:guid}/lifecycle", async (
            Guid contractVersionId,
            TransitionLifecycleStateFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { ContractVersionId = contractVersionId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:write");

        group.MapPost("/{contractVersionId:guid}/deprecate", async (
            Guid contractVersionId,
            DeprecateContractVersionFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updatedCommand = command with { ContractVersionId = contractVersionId };
            var result = await sender.Send(updatedCommand, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:write");

        // ── Signing & Integrity ─────────────────────────────────

        group.MapPost("/{contractVersionId:guid}/sign", async (
            Guid contractVersionId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new SignContractVersionFeature.Command(contractVersionId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:write");

        group.MapGet("/{contractVersionId:guid}/verify", async (
            Guid contractVersionId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new VerifySignatureFeature.Query(contractVersionId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        // ── Search & Filtering ──────────────────────────────────────

        group.MapGet("/search", async (
            ContractProtocol? protocol,
            ContractLifecycleState? lifecycleState,
            Guid? apiAssetId,
            string? searchTerm,
            int? page,
            int? pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new SearchContractsFeature.Query(
                protocol, lifecycleState, apiAssetId, searchTerm, page ?? 1, pageSize ?? 20), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        // ── Rule Violations ─────────────────────────────────────────

        group.MapGet("/{contractVersionId:guid}/violations", async (
            Guid contractVersionId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListRuleViolationsFeature.Query(contractVersionId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        // ── Validation & Integrity ───────────────────────────────────

        /// <summary>
        /// Valida a integridade estrutural de uma versão de contrato conforme seu protocolo.
        /// Retorna contagem de paths/channels/portTypes e version extraída do spec.
        /// </summary>
        group.MapGet("/{contractVersionId:guid}/validate", async (
            Guid contractVersionId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ValidateContractIntegrityFeature.Query(contractVersionId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        // ── Scorecard, Evidence Pack, Rules & Compatibility ────────────

        group.MapGet("/{contractVersionId:guid}/scorecard", async (
            Guid contractVersionId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GenerateScorecardFeature.Query(contractVersionId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        group.MapGet("/{contractVersionId:guid}/evidence-pack", async (
            Guid contractVersionId,
            string? generatedBy,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GenerateEvidencePackFeature.Query(contractVersionId, generatedBy ?? string.Empty), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        group.MapGet("/{contractVersionId:guid}/rules", async (
            Guid contractVersionId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new EvaluateContractRulesFeature.Query(contractVersionId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        group.MapPost("/compatibility", async (
            GetCompatibilityAssessmentFeature.Query query,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        // ── Deployments ──────────────────────────────────────────────

        group.MapPost("/{contractVersionId:guid}/deployments", async (
            Guid contractVersionId,
            RegisterContractDeploymentFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updated = command with { ContractVersionId = contractVersionId };
            var result = await sender.Send(updated, cancellationToken);
            return result.ToCreatedResult("/api/v1/contracts/{0}/deployments", localizer);
        }).RequirePermission("contracts:write");

        group.MapGet("/{contractVersionId:guid}/deployments", async (
            Guid contractVersionId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetContractDeploymentsFeature.Query(contractVersionId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        // ── Preview (Contract Studio Editor) ─────────────────────────

        group.MapPost("/parse-preview", async (
            ParseSpecPreviewFeature.Query query,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        // ── External Inbound (CI/CD Integration) ─────────────────────

        /// <summary>
        /// Endpoint de sincronização em lote para sistemas externos (CI/CD, API Gateways).
        /// Permite importar múltiplos contratos em uma única requisição autenticada sistema-a-sistema.
        /// Máximo de 50 itens por lote; retorna resultado por item com erros não-bloqueantes.
        /// Exige permissão de importação para controle de acesso granular.
        /// </summary>
        group.MapPost("/sync", async (
            SyncContractsFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:import");

        // ── Canonical Entities ───────────────────────────────────────────

        var canonicalGroup = app.MapGroup("/api/v1/contracts/canonical-entities");

        canonicalGroup.MapGet("/", async (
            string? searchTerm,
            string? domain,
            string? category,
            string? state,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 20) =>
        {
            var result = await sender.Send(
                new SearchCanonicalEntitiesFeature.Query(searchTerm, domain, category, page, pageSize),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        canonicalGroup.MapGet("/search", async (
            string? searchTerm,
            string? domain,
            string? category,
            int? page,
            int? pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new SearchCanonicalEntitiesFeature.Query(
                searchTerm, domain, category, page ?? 1, pageSize ?? 20), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        canonicalGroup.MapGet("/{entityId:guid}/versions", async (
            Guid entityId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ListCanonicalEntityVersionsFeature.Query(entityId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        canonicalGroup.MapGet("/{entityId:guid}/versions/diff", async (
            Guid entityId,
            string fromVersion,
            string toVersion,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new DiffCanonicalEntityVersionsFeature.Query(entityId, fromVersion, toVersion), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        canonicalGroup.MapGet("/{entityId:guid}/usages", async (
            Guid entityId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetCanonicalEntityUsagesFeature.Query(entityId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        canonicalGroup.MapGet("/{entityId:guid}/impact", async (
            Guid entityId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetCanonicalEntityImpactFeature.Query(entityId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        canonicalGroup.MapGet("/{entityId:guid}/impact/cascade", async (
            Guid entityId,
            int? maxDepth,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetCanonicalEntityImpactCascadeFeature.Query(entityId, maxDepth ?? 2), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        canonicalGroup.MapPost("/{entityId:guid}/propagate", async (
            Guid entityId,
            PropagateCanonicalEntityChangeFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updated = command with { CanonicalEntityId = entityId };
            var result = await sender.Send(updated, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:write");

        // ── Mock, Design Guidelines, Multi-Format Export ─────────────────

        group.MapGet("/{versionId:guid}/mock-config", async (
            Guid versionId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GenerateMockConfigurationFeature.Query(versionId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        group.MapGet("/{versionId:guid}/design-guidelines", async (
            Guid versionId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new EvaluateDesignGuidelinesFeature.Query(versionId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        group.MapGet("/{versionId:guid}/export-format", async (
            Guid versionId,
            string format,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ExportContractMultiFormatFeature.Query(versionId, format ?? "openapi-yaml"), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        // ── Consumer Expectations (CDCT) ─────────────────────────────────

        group.MapPost("/{apiAssetId:guid}/consumer-expectations", async (
            Guid apiAssetId,
            RegisterConsumerExpectationFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updated = command with { ApiAssetId = apiAssetId };
            var result = await sender.Send(updated, cancellationToken);
            return result.ToCreatedResult("/api/v1/contracts/{0}/consumer-expectations", localizer);
        }).RequirePermission("contracts:write");

        group.MapGet("/{apiAssetId:guid}/consumer-expectations", async (
            Guid apiAssetId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetContractConsumerExpectationsFeature.Query(apiAssetId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        group.MapGet("/{apiAssetId:guid}/versions/{versionId:guid}/cdct-verify", async (
            Guid apiAssetId,
            Guid versionId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new VerifyProviderCompatibilityFeature.Query(apiAssetId, versionId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        // ── Phase 6 — Intelligence & Governance ──────────────────────────

        group.MapPost("/{serviceId:guid}/infer-dependencies", async (
            Guid serviceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new InferDependenciesFromContractsFeature.Command(serviceId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        group.MapGet("/{apiAssetId:guid}/semantic-changelog", async (
            Guid apiAssetId,
            string? fromVersion,
            string? toVersion,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GenerateSemanticChangelogFeature.Query(apiAssetId, fromVersion, toVersion), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        group.MapGet("/health-dashboard", async (
            string? domain,
            string? contractType,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 20) =>
        {
            var result = await sender.Send(
                new ComputeContractHealthDashboardFeature.Query(domain, contractType, page <= 0 ? 1 : page, pageSize <= 0 ? 20 : pageSize),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        group.MapGet("/suggest-schema", async (
            string method,
            string path,
            string? domain,
            Guid? serviceAssetId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new SuggestSchemaFromContextFeature.Query(method, path, domain, serviceAssetId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        group.MapPost("/{apiAssetId:guid}/deprecate", async (
            Guid apiAssetId,
            InitiateContractDeprecationFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var updated = command with { ApiAssetId = apiAssetId };
            var result = await sender.Send(updated, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:write");

        group.MapGet("/{apiAssetId:guid}/deprecation-progress", async (
            Guid apiAssetId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetDeprecationProgressFeature.Query(apiAssetId), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        // ── Contract Drift & Health Timeline ────────────────────────────

        group.MapPost("/{apiAssetId:guid}/drift", async (
            Guid apiAssetId,
            IReadOnlyList<DetectContractDriftFeature.ObservedTraceOperation> observedOperations,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new DetectContractDriftFeature.Query(apiAssetId, observedOperations ?? []),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        group.MapGet("/{apiAssetId:guid}/health/timeline", async (
            Guid apiAssetId,
            int? maxVersions,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new GetContractHealthTimelineFeature.Query(apiAssetId, maxVersions ?? 20),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");

        // ── Publication Readiness (Parameterization v2.0) ──────────────

        group.MapGet("/{contractVersionId:guid}/publication-readiness", async (
            Guid contractVersionId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ValidatePublicationReadinessFeature.Query(contractVersionId),
                cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("contracts:read");
    }
}
