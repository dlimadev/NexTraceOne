using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;

using ListFeature = NexTraceOne.Configuration.Application.Features.ListServiceCustomFields.ListServiceCustomFields;
using CreateFeature = NexTraceOne.Configuration.Application.Features.CreateServiceCustomField.CreateServiceCustomField;
using DeleteFeature = NexTraceOne.Configuration.Application.Features.DeleteServiceCustomField.DeleteServiceCustomField;

namespace NexTraceOne.Configuration.API.Endpoints;

/// <summary>Endpoints de campos personalizados do catálogo de serviços.</summary>
public sealed class ServiceCustomFieldsEndpointModule
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/catalog/custom-fields").RequireAuthorization();

        group.MapGet("/", async (
            string tenantId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ListFeature.Query(tenantId), cancellationToken);
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

        group.MapDelete("/{fieldId:guid}", async (
            Guid fieldId,
            string tenantId,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DeleteFeature.Command(fieldId, tenantId), cancellationToken);
            return result.ToHttpResult(localizer);
        });
    }
}
