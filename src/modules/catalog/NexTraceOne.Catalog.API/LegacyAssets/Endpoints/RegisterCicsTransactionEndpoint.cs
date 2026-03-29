using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.BuildingBlocks.Security.Extensions;

using RegisterCicsTransactionFeature = NexTraceOne.Catalog.Application.LegacyAssets.Features.RegisterCicsTransaction.RegisterCicsTransaction;

namespace NexTraceOne.Catalog.API.LegacyAssets.Endpoints;

/// <summary>
/// Endpoint para registo de uma nova transação CICS no catálogo legacy.
/// Route: POST /api/catalog/legacy/cics-transactions
/// </summary>
public static class RegisterCicsTransactionEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/cics-transactions", async (
            RegisterCicsTransactionFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToCreatedResult("/api/catalog/legacy/cics-transactions/{0}", localizer);
        }).RequirePermission("catalog:legacy-assets:write");
    }
}
