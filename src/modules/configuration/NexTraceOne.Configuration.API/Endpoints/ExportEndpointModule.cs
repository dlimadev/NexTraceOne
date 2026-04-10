using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;

namespace NexTraceOne.Configuration.API.Endpoints;

/// <summary>
/// [PREVIEW] Endpoint de exportação genérica de dados.
/// Retorna status "not_implemented" até a geração real de ficheiros via Quartz job estar disponível.
/// A implementação completa incluirá geração de ficheiros CSV/JSON/PDF com suporte a streaming.
/// </summary>
public sealed class ExportEndpointModule
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/export").RequireAuthorization();

        // POST /api/v1/export
        // Body: { entity: string, format: "csv"|"json"|"pdf", columns?: string[], filtersJson?: string }
        group.MapPost("/", async (
            ExportRequest request,
            IErrorLocalizer localizer,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Entity))
                return Results.BadRequest(new { error = "Entity is required." });

            var supportedFormats = new[] { "csv", "json", "pdf" };
            var format = request.Format?.ToLowerInvariant() ?? "csv";
            if (!supportedFormats.Contains(format))
                return Results.BadRequest(new { error = $"Format must be one of: {string.Join(", ", supportedFormats)}." });

            // Sinalizar explicitamente que a funcionalidade ainda não está implementada.
            // A geração real de ficheiros via Quartz job está no roadmap.
            httpContext.Response.Headers["X-Feature-Preview"] = "true";
            return Results.Json(new
            {
                status = "not_implemented",
                isPreview = true,
                entity = request.Entity,
                format,
                message = $"Export of '{request.Entity}' as {format.ToUpperInvariant()} is not yet available. " +
                          "This feature is planned for a future release."
            }, statusCode: StatusCodes.Status501NotImplemented);
        });
    }

    private sealed record ExportRequest(
        string Entity,
        string? Format,
        string[]? Columns,
        string? FiltersJson);
}
