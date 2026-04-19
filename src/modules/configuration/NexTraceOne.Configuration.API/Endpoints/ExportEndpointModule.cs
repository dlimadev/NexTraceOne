using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;
using NexTraceOne.Configuration.Application.Features.ExportData;

namespace NexTraceOne.Configuration.API.Endpoints;

/// <summary>
/// Endpoint de exportação genérica de dados.
/// Suporta exportação de contratos, releases e eventos de auditoria nos formatos CSV e JSON.
/// </summary>
public sealed class ExportEndpointModule
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/export").RequireAuthorization();

        // POST /api/v1/export
        // Body: { entity: string, format: "csv"|"json", columns?: string[], filtersJson?: string }
        group.MapPost("/", async (
            ExportRequest request,
            ISender mediator,
            IErrorLocalizer localizer,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Entity))
                return Results.BadRequest(new { error = "Entity is required." });

            var supportedFormats = new[] { "csv", "json" };
            var format = request.Format?.ToLowerInvariant() ?? "csv";
            if (!supportedFormats.Contains(format))
                return Results.BadRequest(new { error = $"Format must be one of: {string.Join(", ", supportedFormats)}." });

            var command = new ExportData.Command(request.Entity, format, request.Columns);

            var validator = new ExportData.Validator();
            var validation = validator.Validate(command);
            if (!validation.IsValid)
                return Results.BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

            var result = await mediator.Send(command, ct);
            if (result.IsFailure)
                return Results.BadRequest(new { error = result.Error.Message });

            return Results.File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
        });
    }

    private sealed record ExportRequest(
        string Entity,
        string? Format,
        string[]? Columns,
        string? FiltersJson);
}
