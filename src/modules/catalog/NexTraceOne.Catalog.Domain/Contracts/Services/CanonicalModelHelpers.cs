using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

using YamlDotNet.Serialization;

namespace NexTraceOne.Catalog.Domain.Contracts.Services;

/// <summary>
/// Utilitários partilhados entre os builders de modelo canônico por protocolo.
/// Contém conversão YAML→JSON, resolução de refs, extração de schemas e helpers genéricos.
/// </summary>
internal static class CanonicalModelHelpers
{
    /// <summary>
    /// Normaliza o conteúdo da spec para JSON quando está em formato YAML.
    /// Deteta JSON pelo primeiro caracter ('{' ou '[') e devolve o conteúdo original.
    /// Caso contrário, converte YAML→JSON usando YamlDotNet + conversão recursiva
    /// para System.Text.Json.Nodes.JsonNode que preserva tipos primitivos.
    /// </summary>
    internal static string NormalizeToJson(string specContent)
    {
        var trimmed = specContent.AsSpan().TrimStart();
        if (trimmed.Length > 0 && (trimmed[0] == '{' || trimmed[0] == '['))
            return specContent;

        var deserializer = new DeserializerBuilder()
            .Build();

        var yamlObject = deserializer.Deserialize(new StringReader(specContent));
        if (yamlObject is null)
            return specContent;

        var jsonNode = ToJsonNode(yamlObject);
        return jsonNode?.ToJsonString() ?? "{}";
    }

    /// <summary>
    /// Converte recursivamente o grafo de objetos do YamlDotNet
    /// (Dictionary&lt;object,object&gt;, List&lt;object&gt;, escalares)
    /// para System.Text.Json.Nodes.JsonNode.
    /// YamlDotNet 16.x devolve escalares como strings por defeito ao desserializar
    /// sem tipo alvo, por isso é necessário resolver tipos YAML (bool, int, float, null)
    /// explicitamente para produzir JSON semanticamente correto.
    /// </summary>
    internal static JsonNode? ToJsonNode(object? value) => value switch
    {
        Dictionary<object, object> dict => new JsonObject(
            dict.Select(kvp => new KeyValuePair<string, JsonNode?>(
                kvp.Key?.ToString() ?? string.Empty,
                ToJsonNode(kvp.Value)))),
        List<object> list => new JsonArray(list.Select(ToJsonNode).ToArray()),
        bool b => JsonValue.Create(b),
        int i => JsonValue.Create(i),
        long l => JsonValue.Create(l),
        double d => JsonValue.Create(d),
        string s => ResolveYamlScalar(s),
        null => null,
        _ => JsonValue.Create(value.ToString())
    };

    /// <summary>
    /// Resolve um escalar YAML (devolvido como string pelo YamlDotNet) para o
    /// JsonNode com o tipo JSON correto (boolean, number, null ou string).
    /// Segue as regras de resolução do YAML Core Schema (RFC 9512 §10.3).
    /// </summary>
    internal static JsonNode? ResolveYamlScalar(string s) => s switch
    {
        "true" or "True" or "TRUE" => JsonValue.Create(true),
        "false" or "False" or "FALSE" => JsonValue.Create(false),
        "null" or "Null" or "NULL" or "~" or "" => null,
        _ when long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l) =>
            l is >= int.MinValue and <= int.MaxValue ? JsonValue.Create((int)l) : JsonValue.Create(l),
        _ when double.TryParse(s, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var d) =>
            JsonValue.Create(d),
        _ => JsonValue.Create(s)
    };

    internal static ContractCanonicalModel EmptyModel(ContractProtocol protocol)
        => new(protocol, "Unknown", "", null, [], [], [], [], [], 0, 0, false, false, false);

    internal static string? GetJsonString(JsonElement root, params string[] path)
    {
        var current = root;
        foreach (var key in path)
        {
            if (!current.TryGetProperty(key, out var next)) return null;
            current = next;
        }
        return current.ValueKind == JsonValueKind.String ? current.GetString() : current.ToString();
    }

    internal static List<ContractSchemaElement> ExtractJsonSchemas(JsonElement root, params string[] path)
    {
        var schemas = new List<ContractSchemaElement>();
        var current = root;
        foreach (var key in path)
        {
            if (!current.TryGetProperty(key, out var next)) return schemas;
            current = next;
        }

        foreach (var schema in current.EnumerateObject())
        {
            if (schema.Value.ValueKind != JsonValueKind.Object) continue;

            var type = schema.Value.TryGetProperty("type", out var t) ? t.GetString() ?? "object" : "object";
            var desc = schema.Value.TryGetProperty("description", out var d) ? d.GetString() : null;
            var children = ExtractSchemaProperties(schema.Value, root);
            schemas.Add(new ContractSchemaElement(schema.Name, type, false, desc, Children: children.Count > 0 ? children : null));
        }
        return schemas;
    }

    internal static List<string> ExtractTags(JsonElement root)
    {
        var tags = new List<string>();
        if (root.TryGetProperty("tags", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var t in arr.EnumerateArray())
            {
                if (t.ValueKind == JsonValueKind.Object && t.TryGetProperty("name", out var name))
                    tags.Add(name.GetString() ?? "");
            }
        }
        return tags;
    }

    internal static List<string> ExtractArrayStrings(JsonElement element, string property)
    {
        var result = new List<string>();
        if (element.TryGetProperty(property, out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in arr.EnumerateArray())
            {
                var val = item.GetString();
                if (!string.IsNullOrEmpty(val)) result.Add(val);
            }
        }
        return result;
    }

    internal static List<ContractSchemaElement> ExtractOperationParameters(JsonElement operation)
    {
        var parameters = new List<ContractSchemaElement>();
        if (!operation.TryGetProperty("parameters", out var pArr) || pArr.ValueKind != JsonValueKind.Array)
            return parameters;

        foreach (var p in pArr.EnumerateArray())
        {
            if (p.ValueKind != JsonValueKind.Object) continue;

            var name = p.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
            var required = p.TryGetProperty("required", out var r) && r.ValueKind == JsonValueKind.True;
            var schema = p.TryGetProperty("schema", out var s) && s.ValueKind == JsonValueKind.Object ? s : default;
            var type = schema.ValueKind != JsonValueKind.Undefined && schema.TryGetProperty("type", out var t) ? t.GetString() ?? "string" : "string";
            var desc = p.TryGetProperty("description", out var d) ? d.GetString() : null;
            var deprecated = p.TryGetProperty("deprecated", out var dep) && dep.ValueKind == JsonValueKind.True;

            parameters.Add(new ContractSchemaElement(name, type, required, desc, IsDeprecated: deprecated));
        }
        return parameters;
    }

    /// <summary>
    /// Extrai propriedades de um schema a partir de um media type element.
    /// Resolve $ref para schemas definidos em components/schemas ou definitions.
    /// </summary>
    internal static (List<ContractSchemaElement> Properties, string? SchemaRef) ExtractSchemaFromMediaType(
        JsonElement mediaType, JsonElement root)
    {
        if (!mediaType.TryGetProperty("schema", out var schema) || schema.ValueKind != JsonValueKind.Object)
            return ([], null);

        if (schema.TryGetProperty("$ref", out var refProp))
        {
            var refPath = refProp.GetString();
            var resolved = ResolveRef(root, refPath);
            if (resolved.ValueKind == JsonValueKind.Object)
            {
                var props = ExtractSchemaProperties(resolved, root);
                return (props, refPath);
            }
            return ([], refPath);
        }

        var properties = ExtractSchemaProperties(schema, root);
        return (properties, null);
    }

    /// <summary>
    /// Extrai propriedades de um schema JSON (object com "properties").
    /// Suporta também array items com propriedades.
    /// </summary>
    internal static List<ContractSchemaElement> ExtractSchemaProperties(JsonElement schema, JsonElement root)
    {
        var result = new List<ContractSchemaElement>();

        if (schema.TryGetProperty("$ref", out var refProp))
        {
            var resolved = ResolveRef(root, refProp.GetString());
            if (resolved.ValueKind == JsonValueKind.Object)
                return ExtractSchemaProperties(resolved, root);
            return result;
        }

        var requiredFields = new HashSet<string>(StringComparer.Ordinal);
        if (schema.TryGetProperty("required", out var reqArr) && reqArr.ValueKind == JsonValueKind.Array)
        {
            foreach (var r in reqArr.EnumerateArray())
            {
                var val = r.GetString();
                if (!string.IsNullOrEmpty(val)) requiredFields.Add(val);
            }
        }

        if (schema.TryGetProperty("type", out var typeEl) && typeEl.GetString() == "array")
        {
            if (schema.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Object)
                return ExtractSchemaProperties(items, root);
            return result;
        }

        if (!schema.TryGetProperty("properties", out var props) || props.ValueKind != JsonValueKind.Object)
            return result;

        foreach (var prop in props.EnumerateObject())
        {
            if (prop.Value.ValueKind != JsonValueKind.Object) continue;

            var propType = prop.Value.TryGetProperty("type", out var pt) ? pt.GetString() ?? "string" : "string";
            var desc = prop.Value.TryGetProperty("description", out var pd) ? pd.GetString() : null;
            var format = prop.Value.TryGetProperty("format", out var pf) ? pf.GetString() : null;
            var defaultVal = prop.Value.TryGetProperty("default", out var dv) ? dv.ToString() : null;
            var deprecated = prop.Value.TryGetProperty("deprecated", out var dp) && dp.ValueKind == JsonValueKind.True;
            var isRequired = requiredFields.Contains(prop.Name);

            List<ContractSchemaElement>? children = null;
            if (propType == "object")
            {
                var nested = ExtractSchemaProperties(prop.Value, root);
                if (nested.Count > 0) children = nested;
            }
            else if (propType == "array" && prop.Value.TryGetProperty("items", out var itemsEl) && itemsEl.ValueKind == JsonValueKind.Object)
            {
                var nested = ExtractSchemaProperties(itemsEl, root);
                if (nested.Count > 0) children = nested;
                var itemType = itemsEl.TryGetProperty("type", out var it) ? it.GetString() : null;
                propType = itemType != null ? $"array<{itemType}>" : "array";
            }

            result.Add(new ContractSchemaElement(prop.Name, propType, isRequired, desc, format, defaultVal, deprecated, children));
        }

        return result;
    }

    /// <summary>
    /// Resolve uma referência JSON ($ref) dentro do documento root.
    /// Suporta referências no formato "#/components/schemas/Name" e "#/definitions/Name".
    /// </summary>
    internal static JsonElement ResolveRef(JsonElement root, string? refPath)
    {
        if (string.IsNullOrEmpty(refPath) || !refPath.StartsWith("#/"))
            return default;

        var segments = refPath[2..].Split('/');
        var current = root;
        foreach (var segment in segments)
        {
            if (current.ValueKind != JsonValueKind.Object) return default;
            if (!current.TryGetProperty(segment, out var next)) return default;
            current = next;
        }
        return current;
    }
}
