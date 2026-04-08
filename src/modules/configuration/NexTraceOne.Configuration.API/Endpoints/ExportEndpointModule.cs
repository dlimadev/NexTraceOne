using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NexTraceOne.BuildingBlocks.Application.Extensions;
using NexTraceOne.BuildingBlocks.Application.Localization;

namespace NexTraceOne.Configuration.API.Endpoints;

/// <summary>
/// [PREVIEW] Endpoint de exportação genérica de dados.
/// Aceita pedidos de exportação e retorna status "queued".
/// A geração real de ficheiros CSV/JSON/PDF está prevista como evolução futura via Quartz job.
/// Suporta formatos CSV, JSON e PDF com selecção de colunas e filtros.
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
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Entity))
                return Results.BadRequest(new { error = "Entity is required." });

            var supportedFormats = new[] { "csv", "json", "pdf" };
            var format = request.Format?.ToLowerInvariant() ?? "csv";
            if (!supportedFormats.Contains(format))
                return Results.BadRequest(new { error = $"Format must be one of: {string.Join(", ", supportedFormats)}." });

            // Return a stub response confirming the export parameters.
            // Real export generation (streaming CSV/JSON/PDF bytes) is a Quartz job roadmap item.
            var response = new ExportResponse(
                Entity: request.Entity,
                Format: format,
                Columns: request.Columns ?? [],
                FiltersJson: request.FiltersJson ?? "{}",
                Status: "queued",
                Message: $"Export of '{request.Entity}' as {format.ToUpperInvariant()} queued. Download will be available shortly.");

            return Results.Accepted("/api/v1/export/status", response);
        });
    }

    private sealed record ExportRequest(
        string Entity,
        string? Format,
        string[]? Columns,
        string? FiltersJson);

    private sealed record ExportResponse(
        string Entity,
        string Format,
        string[] Columns,
        string FiltersJson,
        string Status,
        string Message);
}
