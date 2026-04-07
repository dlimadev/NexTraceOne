using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;

using ListFeature = NexTraceOne.Configuration.Application.Features.ListTaxonomyCategories.ListTaxonomyCategories;
using CreateFeature = NexTraceOne.Configuration.Application.Features.CreateTaxonomyCategory.CreateTaxonomyCategory;

namespace NexTraceOne.Configuration.API.Endpoints;

/// <summary>Endpoints de taxonomias de classificação por tenant.</summary>
public sealed class TaxonomiesEndpointModule
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/taxonomies").RequireAuthorization();

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
    }
}
