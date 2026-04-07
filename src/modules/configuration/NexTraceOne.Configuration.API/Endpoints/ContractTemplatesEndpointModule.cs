using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;

using ListFeature = NexTraceOne.Configuration.Application.Features.ListContractTemplates.ListContractTemplates;
using CreateFeature = NexTraceOne.Configuration.Application.Features.CreateContractTemplate.CreateContractTemplate;
using DeleteFeature = NexTraceOne.Configuration.Application.Features.DeleteContractTemplate.DeleteContractTemplate;

namespace NexTraceOne.Configuration.API.Endpoints;

/// <summary>Endpoints de templates de contrato customizáveis por tenant.</summary>
public sealed class ContractTemplatesEndpointModule
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/contract-templates").RequireAuthorization();

        group.MapGet("/", async (
            string? type,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListFeature.Query(type), cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapPost("/", async (
            CreateFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DeleteFeature.Command(id), cancellationToken);
            return result.ToHttpResult(localizer);
        });
    }
}
