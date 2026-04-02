using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;
using ListIntegrationConnectorsFeature = NexTraceOne.Integrations.Application.Features.ListIntegrationConnectors.ListIntegrationConnectors;
using GetIntegrationFilterOptionsFeature = NexTraceOne.Integrations.Application.Features.GetIntegrationFilterOptions.GetIntegrationFilterOptions;
using GetIntegrationConnectorFeature = NexTraceOne.Integrations.Application.Features.GetIntegrationConnector.GetIntegrationConnector;
using ListIngestionSourcesFeature = NexTraceOne.Integrations.Application.Features.ListIngestionSources.ListIngestionSources;
using ListIngestionExecutionsFeature = NexTraceOne.Integrations.Application.Features.ListIngestionExecutions.ListIngestionExecutions;
using GetIngestionHealthFeature = NexTraceOne.Integrations.Application.Features.GetIngestionHealth.GetIngestionHealth;
using GetIngestionFreshnessFeature = NexTraceOne.Integrations.Application.Features.GetIngestionFreshness.GetIngestionFreshness;
using RetryConnectorFeature = NexTraceOne.Integrations.Application.Features.RetryConnector.RetryConnector;
using ReprocessExecutionFeature = NexTraceOne.Integrations.Application.Features.ReprocessExecution.ReprocessExecution;

namespace NexTraceOne.Integrations.API.Endpoints;

/// <summary>
/// Endpoints do Integration Hub &amp; Ingestion Maturity — gestão de conectores, fontes de ingestão,
/// execuções, saúde e frescura do pipeline de dados.
///
/// Módulo nativo de Integrations. Ownership real do módulo Integrations.
/// Os handlers consomem NexTraceOne.Integrations.Application.Abstractions e NexTraceOne.Integrations.Domain.
/// As rotas /api/v1/integrations e /api/v1/ingestion pertencem ao módulo Integrations.
/// </summary>
public sealed class IntegrationHubEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var integrations = app.MapGroup("/api/v1/integrations");
        var ingestion = app.MapGroup("/api/v1/ingestion");

        integrations.MapGet("/connectors", async (
            string? connectorType,
            string? status,
            string? environment,
            string? search,
            int? page,
            int? pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new ListIntegrationConnectorsFeature.Query(
                connectorType, status, environment, search,
                page ?? 1, pageSize ?? 20);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("integrations:read");

        integrations.MapGet("/filter-options", async (
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new GetIntegrationFilterOptionsFeature.Query(), cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("integrations:read");

        integrations.MapGet("/connectors/{id}", async (
            string id,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetIntegrationConnectorFeature.Query(id);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("integrations:read");

        ingestion.MapGet("/sources", async (
            Guid? connectorId,
            string? dataDomain,
            string? trustLevel,
            string? status,
            int? page,
            int? pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new ListIngestionSourcesFeature.Query(
                connectorId, dataDomain, trustLevel, status,
                page ?? 1, pageSize ?? 20);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("integrations:read");

        ingestion.MapGet("/executions", async (
            Guid? connectorId,
            Guid? sourceId,
            string? resultFilter,
            DateTimeOffset? from,
            DateTimeOffset? to,
            int? page,
            int? pageSize,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new ListIngestionExecutionsFeature.Query(
                connectorId, sourceId, resultFilter, from, to,
                page ?? 1, pageSize ?? 20);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("integrations:read");

        integrations.MapGet("/health", async (
            Guid? connectorId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetIngestionHealthFeature.Query(connectorId);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("integrations:read");

        ingestion.MapGet("/freshness", async (
            string? dataDomain,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new GetIngestionFreshnessFeature.Query(dataDomain);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("integrations:read");

        integrations.MapPost("/connectors/{id}/retry", async (
            string id,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new RetryConnectorFeature.Command(id);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("integrations:write");

        ingestion.MapPost("/executions/{id}/reprocess", async (
            string id,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var command = new ReprocessExecutionFeature.Command(id);
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("integrations:write");
    }
}
