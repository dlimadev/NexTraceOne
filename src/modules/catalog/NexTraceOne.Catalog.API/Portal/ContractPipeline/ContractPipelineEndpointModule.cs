using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using GenerateServerFeature = NexTraceOne.Catalog.Application.Portal.ContractPipeline.Features.GenerateServerFromContract.GenerateServerFromContract;
using GenerateMockFeature = NexTraceOne.Catalog.Application.Portal.ContractPipeline.Features.GenerateMockServer.GenerateMockServer;
using GeneratePostmanFeature = NexTraceOne.Catalog.Application.Portal.ContractPipeline.Features.GeneratePostmanCollection.GeneratePostmanCollection;
using GenerateTestsFeature = NexTraceOne.Catalog.Application.Portal.ContractPipeline.Features.GenerateContractTests.GenerateContractTests;
using GenerateSdkFeature = NexTraceOne.Catalog.Application.Portal.ContractPipeline.Features.GenerateClientSdkFromContract.GenerateClientSdkFromContract;
using OrchestrateFeature = NexTraceOne.Catalog.Application.Portal.ContractPipeline.Features.OrchestrateContractPipeline.OrchestrateContractPipeline;

namespace NexTraceOne.Catalog.API.Portal.ContractPipeline;

/// <summary>
/// Endpoints do Contract-to-Code Pipeline — geração de servidor, mock, Postman, testes e SDK cliente.
/// </summary>
public sealed class ContractPipelineEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/catalog/contracts/pipeline");

        // Gerar stubs de servidor (funcionalidade em preview — requer implementação pelo developer)
        group.MapPost("/server", async (
            GenerateServerFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            httpContext.Response.Headers["X-Feature-Preview"] = "true";
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("catalog:contracts:pipeline:read")
        .WithDescription("Gera stubs de servidor a partir de um contrato. PREVIEW: o código gerado contém stubs e TODOs que requerem implementação pelo developer. Não pronto para produção sem revisão.");

        // Gerar mock server
        group.MapPost("/mock-server", async (
            GenerateMockFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            httpContext.Response.Headers["X-Feature-Preview"] = "true";
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:contracts:pipeline:read");

        // Gerar coleção Postman
        group.MapPost("/postman", async (
            GeneratePostmanFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            httpContext.Response.Headers["X-Feature-Preview"] = "true";
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:contracts:pipeline:read");

        // Gerar testes de contrato
        group.MapPost("/tests", async (
            GenerateTestsFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            httpContext.Response.Headers["X-Feature-Preview"] = "true";
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:contracts:pipeline:read");

        // Gerar SDK cliente
        group.MapPost("/client-sdk", async (
            GenerateSdkFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            httpContext.Response.Headers["X-Feature-Preview"] = "true";
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:contracts:pipeline:read");

        // Orquestrar pipeline completo
        group.MapPost("/orchestrate", async (
            OrchestrateFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            httpContext.Response.Headers["X-Feature-Preview"] = "true";
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        }).RequirePermission("catalog:contracts:pipeline:read");
    }
}
