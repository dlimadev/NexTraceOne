using System.Text.Json;

using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.Catalog.Application.Contracts.Generation;

using YamlDotNet.Serialization;

namespace NexTraceOne.Catalog.Infrastructure.Contracts.Generation;

/// <summary>
/// Parser de OpenAPI baseado em System.Text.Json. Suporta especificações OpenAPI 3.x em
/// formato JSON e YAML (o YAML é convertido para JSON via YamlDotNet antes do parsing).
///
/// Isolado atrás de <see cref="IOpenApiContractParser"/>: pode ser substituído por um parser
/// dedicado de OpenAPI sem impactar o gerador nem a feature.
/// </summary>
internal sealed class OpenApiContractParser : IOpenApiContractParser
{
    public Result<OpenApiContractModel> Parse(string specContent)
    {
        if (string.IsNullOrWhiteSpace(specContent))
            return Error.Validation("Contract.Empty", "The OpenAPI specification is empty.");

        string json;
        try
        {
            json = LooksLikeJson(specContent) ? specContent : ConvertYamlToJson(specContent);
        }
        catch (YamlDotNet.Core.YamlException ex)
        {
            return Error.Validation("Contract.InvalidYaml", $"Invalid OpenAPI YAML: {ex.Message}");
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var title = TryGetString(root, "info", "title") ?? "API";
            var schemas = ParseSchemas(root);
            var operations = ParseOperations(root);

            return Result<OpenApiContractModel>.Success(
                new OpenApiContractModel(title, schemas, operations));
        }
        catch (JsonException ex)
        {
            return Error.Validation("Contract.InvalidSpec", $"Invalid OpenAPI document: {ex.Message}");
        }
    }

    private static bool LooksLikeJson(string content)
    {
        var trimmed = content.AsSpan().TrimStart();
        return trimmed.Length > 0 && (trimmed[0] == '{' || trimmed[0] == '[');
    }

    /// <summary>Converte YAML em JSON preservando a estrutura (mapas→objetos, sequências→arrays).</summary>
    private static string ConvertYamlToJson(string yaml)
    {
        var graph = new DeserializerBuilder().Build().Deserialize(new StringReader(yaml));
        if (graph is null)
            return "{}";

        return new SerializerBuilder().JsonCompatible().Build().Serialize(graph);
    }

    // ── Schemas ──────────────────────────────────────────────────────────

    private static IReadOnlyList<SchemaModel> ParseSchemas(JsonElement root)
    {
        var result = new List<SchemaModel>();

        if (!root.TryGetProperty("components", out var components)
            || !components.TryGetProperty("schemas", out var schemas)
            || schemas.ValueKind != JsonValueKind.Object)
            return result;

        foreach (var schema in schemas.EnumerateObject())
        {
            var required = ExtractRequired(schema.Value);
            var properties = new List<PropertyModel>();

            if (schema.Value.TryGetProperty("properties", out var props)
                && props.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in props.EnumerateObject())
                {
                    properties.Add(new PropertyModel(
                        Name: prop.Name,
                        NeutralType: NeutralType(prop.Value),
                        Required: required.Contains(prop.Name)));
                }
            }

            result.Add(new SchemaModel(schema.Name, properties));
        }

        return result;
    }

    private static HashSet<string> ExtractRequired(JsonElement schema)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        if (schema.TryGetProperty("required", out var req) && req.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in req.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                    set.Add(item.GetString()!);
            }
        }
        return set;
    }

    // ── Operations ─────────────────────────────────────────────────────────

    private static readonly string[] HttpMethods = ["get", "post", "put", "delete", "patch"];

    private static IReadOnlyList<OperationModel> ParseOperations(JsonElement root)
    {
        var result = new List<OperationModel>();

        if (!root.TryGetProperty("paths", out var paths) || paths.ValueKind != JsonValueKind.Object)
            return result;

        foreach (var path in paths.EnumerateObject())
        {
            if (path.Value.ValueKind != JsonValueKind.Object)
                continue;

            foreach (var method in HttpMethods)
            {
                if (!path.Value.TryGetProperty(method, out var op) || op.ValueKind != JsonValueKind.Object)
                    continue;

                var operationId = TryGetStringValue(op, "operationId")
                    ?? $"{method}{path.Name}";
                var tag = ExtractFirstTag(op);
                var summary = TryGetStringValue(op, "summary");

                result.Add(new OperationModel(
                    Method: method.ToUpperInvariant(),
                    Path: path.Name,
                    OperationId: operationId,
                    Tag: tag,
                    RequestSchemaName: ExtractRequestSchema(op),
                    ResponseSchemaName: ExtractResponseSchema(op),
                    Summary: summary));
            }
        }

        return result;
    }

    private static string? ExtractFirstTag(JsonElement op)
    {
        if (op.TryGetProperty("tags", out var tags)
            && tags.ValueKind == JsonValueKind.Array)
        {
            foreach (var tag in tags.EnumerateArray())
            {
                if (tag.ValueKind == JsonValueKind.String)
                    return tag.GetString();
            }
        }
        return null;
    }

    private static string? ExtractRequestSchema(JsonElement op)
    {
        if (op.TryGetProperty("requestBody", out var body)
            && body.TryGetProperty("content", out var content))
            return FirstContentSchemaRef(content);
        return null;
    }

    private static string? ExtractResponseSchema(JsonElement op)
    {
        if (!op.TryGetProperty("responses", out var responses)
            || responses.ValueKind != JsonValueKind.Object)
            return null;

        // Preferir 200, depois 201, depois a primeira resposta disponível.
        foreach (var code in new[] { "200", "201" })
        {
            if (responses.TryGetProperty(code, out var resp)
                && resp.TryGetProperty("content", out var content))
            {
                var refName = FirstContentSchemaRef(content);
                if (refName is not null)
                    return refName;
            }
        }

        foreach (var resp in responses.EnumerateObject())
        {
            if (resp.Value.TryGetProperty("content", out var content))
            {
                var refName = FirstContentSchemaRef(content);
                if (refName is not null)
                    return refName;
            }
        }

        return null;
    }

    private static string? FirstContentSchemaRef(JsonElement content)
    {
        if (content.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var media in content.EnumerateObject())
        {
            if (media.Value.TryGetProperty("schema", out var schema))
            {
                var neutral = NeutralType(schema);
                if (neutral.StartsWith("ref:", StringComparison.Ordinal))
                    return neutral["ref:".Length..];
                if (neutral.StartsWith("array:ref:", StringComparison.Ordinal))
                    return neutral["array:ref:".Length..];
            }
        }
        return null;
    }

    // ── Mapeamento para o tipo neutro ───────────────────────────────────────

    private static string NeutralType(JsonElement schema)
    {
        if (schema.TryGetProperty("$ref", out var refEl) && refEl.ValueKind == JsonValueKind.String)
            return "ref:" + LastSegment(refEl.GetString()!);

        var type = TryGetStringValue(schema, "type");
        var format = TryGetStringValue(schema, "format");

        return type switch
        {
            "array" => "array:" + (schema.TryGetProperty("items", out var items)
                ? NeutralType(items)
                : "object"),
            "integer" => format == "int64" ? "long" : "integer",
            "number" => "number",
            "boolean" => "boolean",
            "string" => format switch
            {
                "date-time" => "date-time",
                "date" => "date",
                "uuid" => "uuid",
                _ => "string"
            },
            _ => "object"
        };
    }

    private static string LastSegment(string reference)
    {
        var idx = reference.LastIndexOf('/');
        return idx >= 0 && idx < reference.Length - 1 ? reference[(idx + 1)..] : reference;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static string? TryGetString(JsonElement element, string parent, string child)
        => element.TryGetProperty(parent, out var p) ? TryGetStringValue(p, child) : null;

    private static string? TryGetStringValue(JsonElement element, string property)
        => element.TryGetProperty(property, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;
}
