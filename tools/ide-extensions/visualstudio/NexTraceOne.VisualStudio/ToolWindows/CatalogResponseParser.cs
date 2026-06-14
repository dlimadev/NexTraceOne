using System;
using System.Collections.Generic;
using System.Text.Json;

namespace NexTraceOne.VisualStudio.ToolWindows;

/// <summary>
/// Parses JSON catalog responses into the DTOs used by the Service Catalog tool window.
/// Kept separate from the WPF control so it can be unit-tested without Visual Studio dependencies.
/// </summary>
internal static class CatalogResponseParser
{
    /// <summary>
    /// Parses a catalog JSON response. Supports both a raw array and a wrapped { "items": [...] } shape.
    /// </summary>
    public static IReadOnlyList<CatalogServiceDto> Parse(string json)
    {
        var result = new List<CatalogServiceDto>();
        if (string.IsNullOrWhiteSpace(json))
            return result;

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var items = root.ValueKind == JsonValueKind.Array
            ? root
            : (root.TryGetProperty("items", out var arr) ? arr : default);

        if (items.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var el in items.EnumerateArray())
        {
            result.Add(new CatalogServiceDto
            {
                Name = GetString(el, "name") ?? "(unknown)",
                TeamName = GetString(el, "teamName"),
                Domain = GetString(el, "domain"),
                Type = GetString(el, "type"),
                Language = GetString(el, "language"),
                Status = GetString(el, "status"),
                Description = GetString(el, "description")
            });
        }

        return result;
    }

    private static string? GetString(JsonElement el, string property)
    {
        return el.TryGetProperty(property, out var p) && p.ValueKind == JsonValueKind.String
            ? p.GetString()
            : null;
    }
}
