using MediatR;

using Microsoft.AspNetCore.Builder;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using NexTraceOne.OperationalIntelligence.Domain.Reliability.Enums;

using ListServiceReliabilityFeature = NexTraceOne.OperationalIntelligence.Application.Reliability.Features.ListServiceReliability.ListServiceReliability;
using GetServiceReliabilityDetailFeature = NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetServiceReliabilityDetail.GetServiceReliabilityDetail;
using GetTeamReliabilitySummaryFeature = NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetTeamReliabilitySummary.GetTeamReliabilitySummary;
using GetDomainReliabilitySummaryFeature = NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetDomainReliabilitySummary.GetDomainReliabilitySummary;
using GetServiceReliabilityTrendFeature = NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetServiceReliabilityTrend.GetServiceReliabilityTrend;
using GetTeamReliabilityTrendFeature = NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetTeamReliabilityTrend.GetTeamReliabilityTrend;
using GetServiceReliabilityCoverageFeature = NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetServiceReliabilityCoverage.GetServiceReliabilityCoverage;
using RegisterSloDefinitionFeature = NexTraceOne.OperationalIntelligence.Application.Reliability.Features.RegisterSloDefinition.RegisterSloDefinition;
using RegisterSlaDefinitionFeature = NexTraceOne.OperationalIntelligence.Application.Reliability.Features.RegisterSlaDefinition.RegisterSlaDefinition;
using GetErrorBudgetFeature = NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetErrorBudget.GetErrorBudget;
using GetBurnRateFeature = NexTraceOne.OperationalIntelligence.Application.Reliability.Features.GetBurnRate.GetBurnRate;

namespace NexTraceOne.OperationalIntelligence.API.Reliability.Endpoints.Endpoints;

/// <summary>
/// Registra endpoints Minimal API do módulo Reliability.
/// Fornece visões de confiabilidade por serviço, equipa e domínio,
/// tendências, cobertura operacional e resumos contextualizados.
///
/// Endpoints disponíveis:
/// - GET /api/v1/reliability/services                          → Lista serviços com resumo de confiabilidade
/// - GET /api/v1/reliability/services/{serviceId}              → Detalhe de confiabilidade do serviço
/// - GET /api/v1/reliability/services/{serviceId}/trend        → Tendência de confiabilidade do serviço
/// - GET /api/v1/reliability/services/{serviceId}/coverage     → Cobertura operacional do serviço
/// - GET /api/v1/reliability/teams/{teamId}/summary            → Resumo por equipa
/// - GET /api/v1/reliability/teams/{teamId}/trend              → Tendência por equipa
/// - GET /api/v1/reliability/domains/{domainId}/summary        → Resumo por domínio
/// - POST /api/v1/reliability/slos                             → Regista definição de SLO
/// - POST /api/v1/reliability/slas                             → Regista definição de SLA
/// - GET /api/v1/reliability/slos/{sloId}/error-budget         → Consulta error budget do SLO
/// - GET /api/v1/reliability/slos/{sloId}/burn-rate            → Consulta burn rate do SLO
///
/// Política de autorização:
/// - Leitura: "operations:reliability:read"
/// - Escrita: "operations:reliability:write"
/// </summary>
public sealed class ReliabilityEndpointModule
{
    /// <summary>Registra endpoints de Reliability no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/reliability").RequireRateLimiting("operations");

        // ── Lista de serviços com resumo de confiabilidade ──────────

        group.MapGet("/services", async (
            string? teamId,
            string? serviceId,
            string? domain,
            string? environment,
            ReliabilityStatus? status,
            string? serviceType,
            string? criticality,
            string? search,
            int page,
            int pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var query = new ListServiceReliabilityFeature.Query(
                teamId, serviceId, domain, environment, status,
                serviceType, criticality, search, page, pageSize);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("operations:reliability:read");

        // ── Detalhe de confiabilidade do serviço ────────────────────

        group.MapGet("/services/{serviceId}", async (
            string serviceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var query = new GetServiceReliabilityDetailFeature.Query(serviceId);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("operations:reliability:read");

        // ── Tendência de confiabilidade do serviço ──────────────────

        group.MapGet("/services/{serviceId}/trend", async (
            string serviceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var query = new GetServiceReliabilityTrendFeature.Query(serviceId);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("operations:reliability:read");

        // ── Cobertura operacional do serviço ────────────────────────

        group.MapGet("/services/{serviceId}/coverage", async (
            string serviceId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var query = new GetServiceReliabilityCoverageFeature.Query(serviceId);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("operations:reliability:read");

        // ── Resumo de confiabilidade por equipa ─────────────────────

        group.MapGet("/teams/{teamId}/summary", async (
            string teamId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var query = new GetTeamReliabilitySummaryFeature.Query(teamId);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("operations:reliability:read");

        // ── Tendência de confiabilidade por equipa ──────────────────

        group.MapGet("/teams/{teamId}/trend", async (
            string teamId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var query = new GetTeamReliabilityTrendFeature.Query(teamId);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("operations:reliability:read");

        // ── Resumo de confiabilidade por domínio ────────────────────

        group.MapGet("/domains/{domainId}/summary", async (
            string domainId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var query = new GetDomainReliabilitySummaryFeature.Query(domainId);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("operations:reliability:read");

        // ── P6.1: SLO / SLA / ErrorBudget / BurnRate ────────────────

        group.MapPost("/slos", async (
            RegisterSloDefinitionFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("operations:reliability:write");

        group.MapPost("/slas", async (
            RegisterSlaDefinitionFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("operations:reliability:write");

        group.MapGet("/slos/{sloId:guid}/error-budget", async (
            Guid sloId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var query = new GetErrorBudgetFeature.Query(sloId);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("operations:reliability:read");

        group.MapGet("/slos/{sloId:guid}/burn-rate", async (
            Guid sloId,
            BurnRateWindow window,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            var query = new GetBurnRateFeature.Query(sloId, window);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult(localizer);
        }).RequirePermission("operations:reliability:read");
    }
}
