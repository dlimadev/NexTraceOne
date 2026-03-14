using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using CreateFreezeWindowFeature = NexTraceOne.ChangeIntelligence.Application.Features.CreateFreezeWindow.CreateFreezeWindow;
using CheckFreezeConflictFeature = NexTraceOne.ChangeIntelligence.Application.Features.CheckFreezeConflict.CheckFreezeConflict;

namespace NexTraceOne.ChangeIntelligence.API.Endpoints;

/// <summary>
/// Endpoints de gestão de janelas de freeze.
/// Permite criar janelas de freeze para restringir ou elevar risco de mudanças
/// em períodos críticos, e verificar conflitos antes de submeter releases.
/// </summary>
internal static class FreezeEndpoints
{
    /// <summary>
    /// Mapeia os endpoints de freeze num grupo dedicado separado do grupo de releases.
    /// </summary>
    internal static void Map(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/freeze-windows");

        group.MapPost("/", async (
            CreateFreezeWindowFeature.Command command,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(localizer);
        });

        group.MapGet("/check", async (
            [AsParameters] CheckFreezeConflictRequest request,
            ISender sender,
            IErrorLocalizer localizer,
            CancellationToken cancellationToken) =>
        {
            var query = new CheckFreezeConflictFeature.Query(request.At, request.Environment);
            var result = await sender.Send(query, cancellationToken);
            return result.ToHttpResult(localizer);
        });
    }
}

/// <summary>Parâmetros de query string para verificação de conflito de freeze.</summary>
internal sealed record CheckFreezeConflictRequest(DateTimeOffset At, string? Environment);
