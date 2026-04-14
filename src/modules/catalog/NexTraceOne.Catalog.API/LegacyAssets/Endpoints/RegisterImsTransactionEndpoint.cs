using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using RegisterImsTransactionFeature = NexTraceOne.Catalog.Application.LegacyAssets.Features.RegisterImsTransaction.RegisterImsTransaction;

namespace NexTraceOne.Catalog.API.LegacyAssets.Endpoints;

/// <summary>
/// Endpoint para registo de uma nova transação IMS no catálogo legacy.
/// Route: POST /api/catalog/legacy/ims-transactions
/// </summary>
public static class RegisterImsTransactionEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/ims-transactions", async (
            RegisterImsTransactionFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult(r => $"/api/catalog/legacy/ims-transactions/{r.Id}", localizer);
        }).RequirePermission("catalog:legacy-assets:write");
    }
}
