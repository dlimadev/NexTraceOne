using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

using ClassifyBreakingChangeFeature = NexTraceOne.Catalog.Application.Contracts.Features.ClassifyBreakingChange.ClassifyBreakingChange;
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
    }
}
