using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NexTraceOne.BuildingBlocks.Observability.HealthChecks;

/// <summary>
/// Escritor JSON para respostas de health check do ASP.NET Core.
/// Fornece saída estruturada com estado geral, resultados individuais e duração total.
/// </summary>
public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    /// <summary>Escreve o relatório de health check como JSON na resposta HTTP.</summary>
    public static Task WriteResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var response = new HealthCheckResponse(
            Status: report.Status.ToString(),
            TotalDurationMs: report.TotalDuration.TotalMilliseconds,
            Checks: report.Entries.Select(entry => new HealthCheckEntry(
                Name: entry.Key,
                Status: entry.Value.Status.ToString(),
                Description: entry.Value.Description,
                DurationMs: entry.Value.Duration.TotalMilliseconds,
                Tags: entry.Value.Tags.Any() ? entry.Value.Tags.ToArray() : null,
                Exception: entry.Value.Exception?.Message
            )).ToArray());

        return context.Response.WriteAsJsonAsync(response, JsonOptions);
    }

    private sealed record HealthCheckResponse(
        string Status,
        double TotalDurationMs,
        IReadOnlyList<HealthCheckEntry> Checks);

    private sealed record HealthCheckEntry(
        string Name,
        string Status,
        string? Description,
        double DurationMs,
        string[]? Tags,
        string? Exception);
}
