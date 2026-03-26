using System.Text.Json;
using System.Xml.Linq;

using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

#pragma warning disable CA1031 // Captura genérica necessária para resiliência ao processar specs malformadas

namespace NexTraceOne.Catalog.Domain.Contracts.Services;

/// <summary>
/// Serviço de domínio responsável por construir o modelo canônico interno (ContractCanonicalModel)
/// a partir de qualquer especificação de contrato suportada (OpenAPI, Swagger, AsyncAPI, WSDL).
/// O modelo canônico é a representação normalizada que permite raciocinar sobre operações,
/// schemas, segurança e metadados de forma independente do protocolo original.
/// Para specs malformadas, retorna modelos parciais para não bloquear o pipeline.
/// </summary>
public static class CanonicalModelBuilder
{
    /// <summary>
    /// Constrói o modelo canônico a partir do conteúdo da spec e seu protocolo.
    /// Delega ao builder específico do protocolo.
    /// </summary>
    public static ContractCanonicalModel Build(string specContent, ContractProtocol protocol)
    {
        return protocol switch
        {
            ContractProtocol.OpenApi => BuildFromOpenApi(specContent),
            ContractProtocol.Swagger => BuildFromSwagger(specContent),
            ContractProtocol.AsyncApi => BuildFromAsyncApi(specContent),
            ContractProtocol.Wsdl => BuildFromWsdl(specContent),
            ContractProtocol.WorkerService => BuildFromWorkerService(specContent),
            _ => EmptyModel(protocol)
        };
    }

    /// <summary>
    /// Constrói modelo canônico a partir de especificação OpenAPI 3.x em JSON.
    /// </summary>
    private static ContractCanonicalModel BuildFromOpenApi(string specContent)
    {
        try
        {
            using var doc = JsonDocument.Parse(specContent);
            var root = doc.RootElement;

            var title = GetJsonString(root, "info", "title") ?? "Untitled API";
            var specVersion = GetJsonString(root, "info", "version") ?? "";
            var description = GetJsonString(root, "info", "description");

            var operations = ExtractOpenApiOperations(root);
            var schemas = ExtractJsonSchemas(root, "components", "schemas");
            var securitySchemes = ExtractSecuritySchemes(root);
            var servers = ExtractServers(root);
            var tags = ExtractTags(root);
            var hasExamples = HasOpenApiExamples(root);

            return new ContractCanonicalModel(
                ContractProtocol.OpenApi, title, specVersion, description,
                operations, schemas, securitySchemes, servers, tags,
                operations.Count, schemas.Count,
                securitySchemes.Count > 0, hasExamples,
                operations.Any(o => !string.IsNullOrWhiteSpace(o.Description)));
        }
        catch { return EmptyModel(ContractProtocol.OpenApi); }
    }

    /// <summary>
    /// Constrói modelo canônico a partir de especificação Swagger 2.0 em JSON.
    /// </summary>
    private static ContractCanonicalModel BuildFromSwagger(string specContent)
    {
        try
        {
            using var doc = JsonDocument.Parse(specContent);
            var root = doc.RootElement;

            var title = GetJsonString(root, "info", "title") ?? "Untitled API";
            var specVersion = GetJsonString(root, "info", "version") ?? "";
            var description = GetJsonString(root, "info", "description");

            var operations = ExtractSwaggerOperations(root);
            var schemas = ExtractJsonSchemas(root, "definitions");
            var securitySchemes = ExtractSwaggerSecuritySchemes(root);
            var tags = ExtractTags(root);

            return new ContractCanonicalModel(
                ContractProtocol.Swagger, title, specVersion, description,
                operations, schemas, securitySchemes, [], tags,
                operations.Count, schemas.Count,
                securitySchemes.Count > 0, false,
                operations.Any(o => !string.IsNullOrWhiteSpace(o.Description)));
        }
        catch { return EmptyModel(ContractProtocol.Swagger); }
    }

    /// <summary>
    /// Constrói modelo canônico a partir de especificação AsyncAPI 2.x/3.x em JSON.
    /// </summary>
    private static ContractCanonicalModel BuildFromAsyncApi(string specContent)
    {
        try
        {
            using var doc = JsonDocument.Parse(specContent);
            var root = doc.RootElement;

            var title = GetJsonString(root, "info", "title") ?? "Untitled Event API";
            var specVersion = GetJsonString(root, "asyncapi") ?? "";
            var description = GetJsonString(root, "info", "description");

            var operations = ExtractAsyncApiOperations(root);
            var schemas = ExtractJsonSchemas(root, "components", "schemas");
            var servers = ExtractAsyncApiServers(root);
            var tags = ExtractTags(root);

            return new ContractCanonicalModel(
                ContractProtocol.AsyncApi, title, specVersion, description,
                operations, schemas, [], servers, tags,
                operations.Count, schemas.Count,
                false, false,
                operations.Any(o => !string.IsNullOrWhiteSpace(o.Description)));
        }
        catch { return EmptyModel(ContractProtocol.AsyncApi); }
    }

    /// <summary>
    /// Constrói modelo canônico a partir de especificação WSDL em XML.
    /// </summary>
    private static ContractCanonicalModel BuildFromWsdl(string specContent)
    {
        try
        {
            var xdoc = XDocument.Parse(specContent);
            var root = xdoc.Root;
            if (root is null) return EmptyModel(ContractProtocol.Wsdl);

            var wsdlNs = XNamespace.Get("http://schemas.xmlsoap.org/wsdl/");
            var title = root.Attribute("name")?.Value ?? "Untitled WSDL Service";

            var operations = ExtractWsdlOperations(root, wsdlNs);
            var schemas = ExtractWsdlSchemas(root);

            return new ContractCanonicalModel(
                ContractProtocol.Wsdl, title, "1.1", null,
                operations, schemas, [], [], [],
                operations.Count, schemas.Count,
                false, false,
                operations.Any(o => !string.IsNullOrWhiteSpace(o.Description)));
        }
        catch { return EmptyModel(ContractProtocol.Wsdl); }
    }

    // ── Helpers privados ─────────────────────────────────────────────

    /// <summary>
    /// Constrói modelo canônico a partir de especificação de Background Service Contract.
    /// Mapeia os metadados estruturais (inputs, outputs, trigger) para o modelo canônico
    /// de forma que scorecard, rule engine e evidências possam operar sobre o tipo.
    /// </summary>
    private static ContractCanonicalModel BuildFromWorkerService(string specContent)
    {
        try
        {
            var spec = BackgroundServiceSpecParser.Parse(specContent);

            // Mapeia inputs e outputs como "operações" canónicas para reutilizar o pipeline
            var operations = new List<ContractOperation>();

            // A operação canónica principal é o processo em background em si
            if (!string.IsNullOrWhiteSpace(spec.ServiceName))
            {
                var inputParams = spec.Inputs
                    .Select(kvp => new ContractSchemaElement(kvp.Key, kvp.Value, true))
                    .ToList();

                var description = spec.Category is { Length: > 0 }
                    ? $"{spec.TriggerType} background service in category '{spec.Category}'"
                    : $"{spec.TriggerType} background service";

                if (spec.ScheduleExpression is not null)
                    description += $". Schedule: {spec.ScheduleExpression}";

                operations.Add(new ContractOperation(
                    spec.ServiceName,
                    spec.ServiceName,
                    description,
                    spec.TriggerType,
                    spec.ServiceName,
                    inputParams,
                    [],
                    false,
                    string.IsNullOrWhiteSpace(spec.Category) ? [] : [spec.Category]));
            }

            var schemas = spec.Outputs
                .Select(kvp => new ContractSchemaElement(kvp.Key, kvp.Value, false))
                .ToList<ContractSchemaElement>();

            // Side effects são metadata — não há security schemes para worker services
            var hasDescriptions = operations.Any(o => !string.IsNullOrWhiteSpace(o.Description));
            var hasExamples = spec.Inputs.Count > 0 || spec.Outputs.Count > 0;

            return new ContractCanonicalModel(
                ContractProtocol.WorkerService,
                spec.ServiceName is { Length: > 0 } ? spec.ServiceName : "Unknown Worker",
                spec.TriggerType,
                spec.ScheduleExpression,
                operations.AsReadOnly(),
                schemas.AsReadOnly(),
                [], // Security schemes are not applicable to background services
                [], // No network servers for background services
                spec.Category is { Length: > 0 } ? [spec.Category] : [],
                operations.Count,
                schemas.Count,
                false, // HasSecurityDefinitions — not applicable for worker services
                hasExamples,
                hasDescriptions);
        }
        catch
        {
            return EmptyModel(ContractProtocol.WorkerService);
        }
    }

    private static ContractCanonicalModel EmptyModel(ContractProtocol protocol)
        => new(protocol, "Unknown", "", null, [], [], [], [], [], 0, 0, false, false, false);

    private static string? GetJsonString(JsonElement root, params string[] path)
    {
        var current = root;
        foreach (var key in path)
        {
            if (!current.TryGetProperty(key, out var next)) return null;
            current = next;
        }
        return current.ValueKind == JsonValueKind.String ? current.GetString() : current.ToString();
    }

    private static List<ContractOperation> ExtractOpenApiOperations(JsonElement root)
    {
        var ops = new List<ContractOperation>();
        if (!root.TryGetProperty("paths", out var paths)) return ops;

        foreach (var path in paths.EnumerateObject())
        {
            foreach (var method in path.Value.EnumerateObject())
            {
                var methodName = method.Name.ToUpperInvariant();
                if (methodName is "PARAMETERS" or "SERVERS" or "$REF") continue;

                var opId = method.Value.TryGetProperty("operationId", out var oid) ? oid.GetString() ?? "" : $"{methodName} {path.Name}";
                var desc = method.Value.TryGetProperty("description", out var d) ? d.GetString() :
                           method.Value.TryGetProperty("summary", out var s) ? s.GetString() : null;
                var deprecated = method.Value.TryGetProperty("deprecated", out var dep) && dep.GetBoolean();
                var tags = ExtractArrayStrings(method.Value, "tags");
                var inputParams = ExtractOperationParameters(method.Value);

                ops.Add(new ContractOperation(opId, opId, desc, methodName, path.Name, inputParams, [], deprecated, tags));
            }
        }
        return ops;
    }

    private static List<ContractOperation> ExtractSwaggerOperations(JsonElement root)
    {
        var ops = new List<ContractOperation>();
        if (!root.TryGetProperty("paths", out var paths)) return ops;

        foreach (var path in paths.EnumerateObject())
        {
            foreach (var method in path.Value.EnumerateObject())
            {
                var methodName = method.Name.ToUpperInvariant();
                if (methodName is "PARAMETERS" or "$REF") continue;

                var opId = method.Value.TryGetProperty("operationId", out var oid) ? oid.GetString() ?? "" : $"{methodName} {path.Name}";
                var desc = method.Value.TryGetProperty("description", out var d) ? d.GetString() :
                           method.Value.TryGetProperty("summary", out var s) ? s.GetString() : null;
                var deprecated = method.Value.TryGetProperty("deprecated", out var dep) && dep.GetBoolean();
                var tags = ExtractArrayStrings(method.Value, "tags");

                ops.Add(new ContractOperation(opId, opId, desc, methodName, path.Name, [], [], deprecated, tags));
            }
        }
        return ops;
    }

    private static List<ContractOperation> ExtractAsyncApiOperations(JsonElement root)
    {
        var ops = new List<ContractOperation>();
        if (!root.TryGetProperty("channels", out var channels)) return ops;

        foreach (var channel in channels.EnumerateObject())
        {
            foreach (var prop in channel.Value.EnumerateObject())
            {
                var opType = prop.Name.ToUpperInvariant();
                if (opType is not ("PUBLISH" or "SUBSCRIBE")) continue;

                var opId = prop.Value.TryGetProperty("operationId", out var oid) ? oid.GetString() ?? "" : $"{opType} {channel.Name}";
                var desc = prop.Value.TryGetProperty("description", out var d) ? d.GetString() :
                           prop.Value.TryGetProperty("summary", out var s) ? s.GetString() : null;
                var tags = ExtractArrayStrings(prop.Value, "tags");

                ops.Add(new ContractOperation(opId, opId, desc, opType, channel.Name, [], [], false, tags));
            }
        }
        return ops;
    }

    private static List<ContractOperation> ExtractWsdlOperations(XElement root, XNamespace wsdlNs)
    {
        var ops = new List<ContractOperation>();
        var portTypes = root.Descendants(wsdlNs + "portType").Concat(root.Descendants("portType"));

        foreach (var pt in portTypes)
        {
            var ptName = pt.Attribute("name")?.Value ?? "";
            var operations = pt.Elements(wsdlNs + "operation").Concat(pt.Elements("operation"));

            foreach (var op in operations)
            {
                var name = op.Attribute("name")?.Value ?? "";
                var doc = op.Element(wsdlNs + "documentation")?.Value ?? op.Element("documentation")?.Value;
                ops.Add(new ContractOperation($"{ptName}.{name}", name, doc, "SOAP", ptName, [], [], false));
            }
        }
        return ops;
    }

    private static List<ContractSchemaElement> ExtractJsonSchemas(JsonElement root, params string[] path)
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
            var type = schema.Value.TryGetProperty("type", out var t) ? t.GetString() ?? "object" : "object";
            var desc = schema.Value.TryGetProperty("description", out var d) ? d.GetString() : null;
            schemas.Add(new ContractSchemaElement(schema.Name, type, false, desc));
        }
        return schemas;
    }

    private static List<ContractSchemaElement> ExtractWsdlSchemas(XElement root)
    {
        var schemas = new List<ContractSchemaElement>();
        var xsdNs = XNamespace.Get("http://www.w3.org/2001/XMLSchema");
        var elements = root.Descendants(xsdNs + "element").Concat(root.Descendants("element"));

        // Limite defensivo para evitar consumo excessivo de memória em WSDLs com schemas muito extensos
        foreach (var el in elements.Take(50))
        {
            var name = el.Attribute("name")?.Value;
            if (!string.IsNullOrEmpty(name))
            {
                var type = el.Attribute("type")?.Value ?? "complex";
                schemas.Add(new ContractSchemaElement(name, type, false));
            }
        }
        return schemas;
    }

    private static List<string> ExtractSecuritySchemes(JsonElement root)
    {
        var schemes = new List<string>();
        if (root.TryGetProperty("components", out var c) && c.TryGetProperty("securitySchemes", out var ss))
        {
            foreach (var s in ss.EnumerateObject())
                schemes.Add(s.Name);
        }
        return schemes;
    }

    private static List<string> ExtractSwaggerSecuritySchemes(JsonElement root)
    {
        var schemes = new List<string>();
        if (root.TryGetProperty("securityDefinitions", out var sd))
        {
            foreach (var s in sd.EnumerateObject())
                schemes.Add(s.Name);
        }
        return schemes;
    }

    private static List<string> ExtractServers(JsonElement root)
    {
        var servers = new List<string>();
        if (root.TryGetProperty("servers", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var s in arr.EnumerateArray())
            {
                if (s.TryGetProperty("url", out var url))
                    servers.Add(url.GetString() ?? "");
            }
        }
        return servers;
    }

    private static List<string> ExtractAsyncApiServers(JsonElement root)
    {
        var servers = new List<string>();
        if (root.TryGetProperty("servers", out var svrs) && svrs.ValueKind == JsonValueKind.Object)
        {
            foreach (var s in svrs.EnumerateObject())
            {
                if (s.Value.TryGetProperty("url", out var url))
                    servers.Add(url.GetString() ?? "");
            }
        }
        return servers;
    }

    private static List<string> ExtractTags(JsonElement root)
    {
        var tags = new List<string>();
        if (root.TryGetProperty("tags", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var t in arr.EnumerateArray())
            {
                if (t.TryGetProperty("name", out var name))
                    tags.Add(name.GetString() ?? "");
            }
        }
        return tags;
    }

    private static List<string> ExtractArrayStrings(JsonElement element, string property)
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

    private static List<ContractSchemaElement> ExtractOperationParameters(JsonElement operation)
    {
        var parameters = new List<ContractSchemaElement>();
        if (!operation.TryGetProperty("parameters", out var pArr) || pArr.ValueKind != JsonValueKind.Array)
            return parameters;

        foreach (var p in pArr.EnumerateArray())
        {
            var name = p.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
            var required = p.TryGetProperty("required", out var r) && r.GetBoolean();
            var schema = p.TryGetProperty("schema", out var s) ? s : default;
            var type = schema.ValueKind != JsonValueKind.Undefined && schema.TryGetProperty("type", out var t) ? t.GetString() ?? "string" : "string";
            var desc = p.TryGetProperty("description", out var d) ? d.GetString() : null;
            var deprecated = p.TryGetProperty("deprecated", out var dep) && dep.GetBoolean();

            parameters.Add(new ContractSchemaElement(name, type, required, desc, IsDeprecated: deprecated));
        }
        return parameters;
    }

    private static bool HasOpenApiExamples(JsonElement root)
    {
        if (!root.TryGetProperty("paths", out var paths)) return false;

        foreach (var path in paths.EnumerateObject())
        {
            foreach (var method in path.Value.EnumerateObject())
            {
                if (method.Value.TryGetProperty("requestBody", out var rb))
                {
                    if (HasExamplesInContent(rb)) return true;
                }
                if (method.Value.TryGetProperty("responses", out var responses))
                {
                    foreach (var resp in responses.EnumerateObject())
                    {
                        if (HasExamplesInContent(resp.Value)) return true;
                    }
                }
            }
        }
        return false;
    }

    private static bool HasExamplesInContent(JsonElement element)
    {
        if (element.TryGetProperty("content", out var content))
        {
            foreach (var mediaType in content.EnumerateObject())
            {
                if (mediaType.Value.TryGetProperty("example", out _) ||
                    mediaType.Value.TryGetProperty("examples", out _))
                    return true;
            }
        }
        return false;
    }
}
