using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using AutoRegisterFeature = NexTraceOne.Catalog.Application.Portal.Features.AutoRegisterScaffoldedService.AutoRegisterScaffoldedService;
using PushToRepositoryFeature = NexTraceOne.Catalog.Application.Portal.Features.PushToRepository.PushToRepository;

namespace NexTraceOne.Catalog.API.Portal.Endpoints.Endpoints;

/// <summary>
/// Endpoints da Fase 5 — Preview, Export &amp; Catalog Registration.
/// Permite registar serviços scaffoldados automaticamente no catálogo e exportar ficheiros para repositórios Git.
/// </summary>
public sealed class ScaffoldExportEndpointModule
{
    /// <summary>Registra endpoints no roteador do ASP.NET Core.</summary>
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/catalog/scaffold");

        // Registo automático de serviço scaffoldado no catálogo
        group.MapPost("/register", async (
            AutoRegisterFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("catalog:services:write")
        .WithName("AutoRegisterScaffoldedService")
        .WithSummary("Automatically register a scaffolded service in the Service Catalog.");

        // Exportação para repositório Git (instruções + comandos prontos)
        group.MapPost("/push-to-repo", async (
            PushToRepositoryFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        })
        .RequirePermission("catalog:services:write")
        .WithName("PushToRepository")
        .WithSummary("Generate Git commands to push scaffolded files to a repository.");
    }
}
