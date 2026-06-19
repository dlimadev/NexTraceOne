using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Writers;

namespace NexTrace.Sdk.Clients;

/// <summary>
/// Filtro de especificação OpenAPI para geração dirigida de clientes consumidores.
/// Permite reter apenas os paths ou operationIds solicitados antes de enviar ao gerador de código.
/// </summary>
internal static class OpenApiSpecFilter
{
    /// <summary>
    /// Aplica filtros de rotas/operationIds a um spec OpenAPI.
    /// Retorna null se não for possível fazer parse ou se não houver filtros.
    /// </summary>
    public static string? Apply(string specContent, IReadOnlyList<string>? filters)
    {
        if (filters is null || filters.Count == 0)
            return null;

        var filterSet = filters
            .Select(f => f.Trim().TrimStart('/'))
            .Where(f => !string.IsNullOrEmpty(f))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (filterSet.Count == 0)
            return null;

        var isYaml = IsYaml(specContent);
        var isV2 = IsOpenApiV2(specContent);

        var reader = new OpenApiStringReader();
        var document = reader.Read(specContent, out var diagnostic);

        if (document is null || diagnostic.Errors.Count > 0)
            return null;

        FilterPathsAndOperations(document, filterSet);

        if (document.Paths.Count == 0)
            return null;

        RemoveOrphanedSchemas(document, isV2);

        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, Encoding.UTF8);

        if (isYaml)
        {
            if (isV2)
                document.SerializeAsV2(new OpenApiYamlWriter(writer));
            else
                document.SerializeAsV3(new OpenApiYamlWriter(writer));
        }
        else
        {
            if (isV2)
                document.SerializeAsV2(new OpenApiJsonWriter(writer));
            else
                document.SerializeAsV3(new OpenApiJsonWriter(writer));
        }

        writer.Flush();
        var bytes = stream.ToArray();
        // Encoding.UTF8 includes a BOM by default; strip it so callers and JSON parsers see clean text.
        return Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF');
    }

    private static void FilterPathsAndOperations(OpenApiDocument document, HashSet<string> filterSet)
    {
        var pathsToRemove = new List<string>();
        foreach (var path in document.Paths)
        {
            if (filterSet.Contains(path.Key.TrimStart('/')))
                continue;

            var hasMatchingOperation = path.Value.Operations
                .Any(op => !string.IsNullOrWhiteSpace(op.Value.OperationId) &&
                           filterSet.Contains(op.Value.OperationId));

            if (!hasMatchingOperation)
            {
                pathsToRemove.Add(path.Key);
                continue;
            }

            // Remove only non-matching operations within the path.
            var operationsToRemove = path.Value.Operations
                .Where(op => string.IsNullOrWhiteSpace(op.Value.OperationId) ||
                             !filterSet.Contains(op.Value.OperationId))
                .Select(op => op.Key)
                .ToList();

            foreach (var operation in operationsToRemove)
                path.Value.Operations.Remove(operation);
        }

        foreach (var path in pathsToRemove)
            document.Paths.Remove(path);
    }

    private static void RemoveOrphanedSchemas(OpenApiDocument document, bool isV2)
    {
        if (document.Components?.Schemas is null || document.Components.Schemas.Count == 0)
            return;

        var referencedSchemas = CollectReferencedSchemaIds(document, isV2);
        var orphaned = document.Components.Schemas
            .Keys
            .Where(key => !referencedSchemas.Contains(key))
            .ToList();

        foreach (var key in orphaned)
            document.Components.Schemas.Remove(key);
    }

    private static HashSet<string> CollectReferencedSchemaIds(OpenApiDocument document, bool isV2)
    {
        // Serialize the filtered document as JSON (always JSON for easy parsing) and collect $ref values.
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, Encoding.UTF8);

        if (isV2)
            document.SerializeAsV2(new OpenApiJsonWriter(writer));
        else
            document.SerializeAsV3(new OpenApiJsonWriter(writer));

        writer.Flush();
        var json = Encoding.UTF8.GetString(stream.ToArray()).TrimStart('\uFEFF');

        var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var doc = JsonDocument.Parse(json);
        CollectRefs(doc.RootElement, referenced);
        return referenced;
    }

    private static void CollectRefs(JsonElement element, HashSet<string> referenced)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (property.NameEquals("$ref") && property.Value.ValueKind == JsonValueKind.String)
                    {
                        var refValue = property.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(refValue))
                        {
                            var schemaName = ExtractSchemaNameFromRef(refValue);
                            if (!string.IsNullOrEmpty(schemaName))
                                referenced.Add(schemaName);
                        }
                    }
                    else
                    {
                        CollectRefs(property.Value, referenced);
                    }
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    CollectRefs(item, referenced);
                break;
        }
    }

    private static string? ExtractSchemaNameFromRef(string refValue)
    {
        const string schemasPrefix = "#/components/schemas/";
        const string v2SchemasPrefix = "#/definitions/";

        if (refValue.StartsWith(schemasPrefix, StringComparison.OrdinalIgnoreCase))
            return refValue[schemasPrefix.Length..];

        if (refValue.StartsWith(v2SchemasPrefix, StringComparison.OrdinalIgnoreCase))
            return refValue[v2SchemasPrefix.Length..];

        return null;
    }

    private static bool IsYaml(string content)
    {
        var trimmed = content.TrimStart();
        if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
            return false;

        return true;
    }

    private static bool IsOpenApiV2(string content)
    {
        // Swagger 2.0 specs use the "swagger" property; OpenAPI 3.x uses "openapi".
        return content.Contains("\"swagger\"", StringComparison.OrdinalIgnoreCase) ||
               content.Contains("swagger:", StringComparison.OrdinalIgnoreCase);
    }
}
