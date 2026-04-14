using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;

using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

using YamlDotNet.Serialization;

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
            var json = NormalizeToJson(specContent);
            using var doc = JsonDocument.Parse(json);
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
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning(
                "CanonicalModelBuilder: Failed to parse OpenAPI spec — {0}: {1}", ex.GetType().Name, ex.Message);
            return EmptyModel(ContractProtocol.OpenApi);
        }
    }

    /// <summary>
    /// Constrói modelo canônico a partir de especificação Swagger 2.0 em JSON.
    /// </summary>
    private static ContractCanonicalModel BuildFromSwagger(string specContent)
    {
        try
        {
            var json = NormalizeToJson(specContent);
            using var doc = JsonDocument.Parse(json);
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
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning(
                "CanonicalModelBuilder: Failed to parse Swagger spec — {0}: {1}", ex.GetType().Name, ex.Message);
            return EmptyModel(ContractProtocol.Swagger);
        }
    }

    /// <summary>
    /// Constrói modelo canônico a partir de especificação AsyncAPI 2.x/3.x em JSON.
    /// </summary>
    private static ContractCanonicalModel BuildFromAsyncApi(string specContent)
    {
        try
        {
            var json = NormalizeToJson(specContent);
            using var doc = JsonDocument.Parse(json);
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
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning(
                "CanonicalModelBuilder: Failed to parse AsyncAPI spec — {0}: {1}", ex.GetType().Name, ex.Message);
            return EmptyModel(ContractProtocol.AsyncApi);
        }
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
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning(
                "CanonicalModelBuilder: Failed to parse WSDL spec — {0}: {1}", ex.GetType().Name, ex.Message);
            return EmptyModel(ContractProtocol.Wsdl);
        }
    }

    // ── Helpers privados ─────────────────────────────────────────────

    /// <summary>
    /// Normaliza o conteúdo da spec para JSON quando está em formato YAML.
    /// Deteta JSON pelo primeiro caracter ('{' ou '[') e devolve o conteúdo original.
    /// Caso contrário, converte YAML→JSON usando YamlDotNet + conversão recursiva
    /// para System.Text.Json.Nodes.JsonNode que preserva tipos primitivos.
    /// </summary>
    private static string NormalizeToJson(string specContent)
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
    private static JsonNode? ToJsonNode(object? value) => value switch
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
    private static JsonNode? ResolveYamlScalar(string s) => s switch
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
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning(
                "CanonicalModelBuilder: Failed to parse WorkerService spec — {0}: {1}", ex.GetType().Name, ex.Message);
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
            if (path.Value.ValueKind != JsonValueKind.Object) continue;

            foreach (var method in path.Value.EnumerateObject())
            {
                if (method.Value.ValueKind != JsonValueKind.Object) continue;

                var methodName = method.Name.ToUpperInvariant();
                if (methodName is "PARAMETERS" or "SERVERS" or "$REF" or "SUMMARY" or "DESCRIPTION") continue;

                var opId = method.Value.TryGetProperty("operationId", out var oid) ? oid.GetString() ?? "" : $"{methodName} {path.Name}";
                var desc = method.Value.TryGetProperty("description", out var d) ? d.GetString() :
                           method.Value.TryGetProperty("summary", out var s) ? s.GetString() : null;
                var deprecated = method.Value.TryGetProperty("deprecated", out var dep) && dep.ValueKind == JsonValueKind.True;
                var tags = ExtractArrayStrings(method.Value, "tags");
                var inputParams = ExtractOperationParameters(method.Value);
                var requestBody = ExtractRequestBody(method.Value, root);
                var responses = ExtractResponses(method.Value, root);

                // OutputFields: flatten first 2xx response properties for backward compat
                var outputFields = responses
                    .Where(r => r.StatusCode.StartsWith('2'))
                    .SelectMany(r => r.Properties)
                    .ToList();

                ops.Add(new ContractOperation(opId, opId, desc, methodName, path.Name, inputParams, outputFields, deprecated, tags, requestBody, responses));
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
            if (path.Value.ValueKind != JsonValueKind.Object) continue;

            foreach (var method in path.Value.EnumerateObject())
            {
                if (method.Value.ValueKind != JsonValueKind.Object) continue;

                var methodName = method.Name.ToUpperInvariant();
                if (methodName is "PARAMETERS" or "$REF" or "SUMMARY" or "DESCRIPTION") continue;

                var opId = method.Value.TryGetProperty("operationId", out var oid) ? oid.GetString() ?? "" : $"{methodName} {path.Name}";
                var desc = method.Value.TryGetProperty("description", out var d) ? d.GetString() :
                           method.Value.TryGetProperty("summary", out var s) ? s.GetString() : null;
                var deprecated = method.Value.TryGetProperty("deprecated", out var dep) && dep.ValueKind == JsonValueKind.True;
                var tags = ExtractArrayStrings(method.Value, "tags");
                var inputParams = ExtractOperationParameters(method.Value);
                var requestBody = ExtractSwaggerRequestBody(method.Value, root);
                var responses = ExtractSwaggerResponses(method.Value, root);

                var outputFields = responses
                    .Where(r => r.StatusCode.StartsWith('2'))
                    .SelectMany(r => r.Properties)
                    .ToList();

                ops.Add(new ContractOperation(opId, opId, desc, methodName, path.Name, inputParams, outputFields, deprecated, tags, requestBody, responses));
            }
        }
        return ops;
    }

    /// <summary>
    /// Extrai request body de uma operação Swagger 2.0.
    /// No Swagger 2.0, o body param está em "parameters" com in:"body".
    /// </summary>
    private static ContractRequestBody? ExtractSwaggerRequestBody(JsonElement operation, JsonElement root)
    {
        if (!operation.TryGetProperty("parameters", out var pArr) || pArr.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var p in pArr.EnumerateArray())
        {
            if (p.ValueKind != JsonValueKind.Object) continue;
            if (!p.TryGetProperty("in", out var inProp) || inProp.GetString() != "body") continue;

            var required = p.TryGetProperty("required", out var req) && req.ValueKind == JsonValueKind.True;

            if (!p.TryGetProperty("schema", out var schema) || schema.ValueKind != JsonValueKind.Object)
                return new ContractRequestBody("application/json", required, []);

            string? schemaRef = null;
            if (schema.TryGetProperty("$ref", out var refProp))
            {
                schemaRef = refProp.GetString();
                var resolved = ResolveRef(root, schemaRef);
                if (resolved.ValueKind == JsonValueKind.Object)
                {
                    var props = ExtractSchemaProperties(resolved, root);
                    return new ContractRequestBody("application/json", required, props, schemaRef);
                }
                return new ContractRequestBody("application/json", required, [], schemaRef);
            }

            var properties = ExtractSchemaProperties(schema, root);
            return new ContractRequestBody("application/json", required, properties, schemaRef);
        }
        return null;
    }

    /// <summary>
    /// Extrai respostas de uma operação Swagger 2.0.
    /// </summary>
    private static List<ContractOperationResponse> ExtractSwaggerResponses(JsonElement operation, JsonElement root)
    {
        var responses = new List<ContractOperationResponse>();
        if (!operation.TryGetProperty("responses", out var resObj) || resObj.ValueKind != JsonValueKind.Object)
            return responses;

        foreach (var resp in resObj.EnumerateObject())
        {
            if (resp.Value.ValueKind != JsonValueKind.Object) continue;

            var statusCode = resp.Name;
            var description = resp.Value.TryGetProperty("description", out var d) ? d.GetString() : null;

            if (!resp.Value.TryGetProperty("schema", out var schema) || schema.ValueKind != JsonValueKind.Object)
            {
                responses.Add(new ContractOperationResponse(statusCode, description, null, []));
                continue;
            }

            string? schemaRef = null;
            if (schema.TryGetProperty("$ref", out var refProp))
            {
                schemaRef = refProp.GetString();
                var resolved = ResolveRef(root, schemaRef);
                if (resolved.ValueKind == JsonValueKind.Object)
                {
                    var props = ExtractSchemaProperties(resolved, root);
                    responses.Add(new ContractOperationResponse(statusCode, description, "application/json", props, schemaRef));
                    continue;
                }
                responses.Add(new ContractOperationResponse(statusCode, description, "application/json", [], schemaRef));
                continue;
            }

            var properties = ExtractSchemaProperties(schema, root);
            responses.Add(new ContractOperationResponse(statusCode, description, "application/json", properties, schemaRef));
        }

        return responses;
    }

    private static List<ContractOperation> ExtractAsyncApiOperations(JsonElement root)
    {
        var ops = new List<ContractOperation>();
        if (!root.TryGetProperty("channels", out var channels)) return ops;

        foreach (var channel in channels.EnumerateObject())
        {
            if (channel.Value.ValueKind != JsonValueKind.Object) continue;

            foreach (var prop in channel.Value.EnumerateObject())
            {
                if (prop.Value.ValueKind != JsonValueKind.Object) continue;

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
            if (schema.Value.ValueKind != JsonValueKind.Object) continue;

            var type = schema.Value.TryGetProperty("type", out var t) ? t.GetString() ?? "object" : "object";
            var desc = schema.Value.TryGetProperty("description", out var d) ? d.GetString() : null;
            var children = ExtractSchemaProperties(schema.Value, root);
            schemas.Add(new ContractSchemaElement(schema.Name, type, false, desc, Children: children.Count > 0 ? children : null));
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
                if (s.Value.ValueKind == JsonValueKind.Object && s.Value.TryGetProperty("url", out var url))
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
                if (t.ValueKind == JsonValueKind.Object && t.TryGetProperty("name", out var name))
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
    /// Extrai o request body de uma operação OpenAPI 3.x.
    /// Resolve o content type principal, required flag e propriedades do schema.
    /// </summary>
    private static ContractRequestBody? ExtractRequestBody(JsonElement operation, JsonElement root)
    {
        if (!operation.TryGetProperty("requestBody", out var rb) || rb.ValueKind != JsonValueKind.Object)
            return null;

        var required = rb.TryGetProperty("required", out var req) && req.ValueKind == JsonValueKind.True;

        if (!rb.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Object)
            return new ContractRequestBody("application/json", required, [], null);

        // Resolve primeiro media type (prioriza application/json)
        JsonElement mediaTypeElement = default;
        string contentType = "application/json";

        if (content.TryGetProperty("application/json", out var jsonMedia) && jsonMedia.ValueKind == JsonValueKind.Object)
        {
            mediaTypeElement = jsonMedia;
        }
        else
        {
            foreach (var mt in content.EnumerateObject())
            {
                if (mt.Value.ValueKind != JsonValueKind.Object) continue;
                contentType = mt.Name;
                mediaTypeElement = mt.Value;
                break;
            }
        }

        if (mediaTypeElement.ValueKind == JsonValueKind.Undefined)
            return new ContractRequestBody(contentType, required, [], null);

        var (properties, schemaRef) = ExtractSchemaFromMediaType(mediaTypeElement, root);
        return new ContractRequestBody(contentType, required, properties, schemaRef);
    }

    /// <summary>
    /// Extrai as respostas de uma operação OpenAPI 3.x.
    /// Cada resposta inclui status code, descrição, content type e propriedades do schema.
    /// </summary>
    private static List<ContractOperationResponse> ExtractResponses(JsonElement operation, JsonElement root)
    {
        var responses = new List<ContractOperationResponse>();
        if (!operation.TryGetProperty("responses", out var resObj) || resObj.ValueKind != JsonValueKind.Object)
            return responses;

        foreach (var resp in resObj.EnumerateObject())
        {
            if (resp.Value.ValueKind != JsonValueKind.Object) continue;

            var statusCode = resp.Name;
            var description = resp.Value.TryGetProperty("description", out var d) ? d.GetString() : null;

            if (!resp.Value.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Object)
            {
                // Resposta sem body (ex: 204 No Content)
                responses.Add(new ContractOperationResponse(statusCode, description, null, []));
                continue;
            }

            // Resolve primeiro media type (prioriza application/json)
            JsonElement mediaTypeElement = default;
            string contentType = "application/json";

            if (content.TryGetProperty("application/json", out var jsonMedia) && jsonMedia.ValueKind == JsonValueKind.Object)
            {
                mediaTypeElement = jsonMedia;
            }
            else
            {
                foreach (var mt in content.EnumerateObject())
                {
                    if (mt.Value.ValueKind != JsonValueKind.Object) continue;
                    contentType = mt.Name;
                    mediaTypeElement = mt.Value;
                    break;
                }
            }

            if (mediaTypeElement.ValueKind == JsonValueKind.Undefined)
            {
                responses.Add(new ContractOperationResponse(statusCode, description, contentType, []));
                continue;
            }

            var (properties, schemaRef) = ExtractSchemaFromMediaType(mediaTypeElement, root);
            responses.Add(new ContractOperationResponse(statusCode, description, contentType, properties, schemaRef));
        }

        return responses;
    }

    /// <summary>
    /// Extrai propriedades de um schema a partir de um media type element.
    /// Resolve $ref para schemas definidos em components/schemas ou definitions.
    /// </summary>
    private static (List<ContractSchemaElement> Properties, string? SchemaRef) ExtractSchemaFromMediaType(
        JsonElement mediaType, JsonElement root)
    {
        if (!mediaType.TryGetProperty("schema", out var schema) || schema.ValueKind != JsonValueKind.Object)
            return ([], null);

        // Resolve $ref
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
    private static List<ContractSchemaElement> ExtractSchemaProperties(JsonElement schema, JsonElement root)
    {
        var result = new List<ContractSchemaElement>();

        // Resolve $ref no próprio schema se necessário
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

        // Handle type: array with items
        if (schema.TryGetProperty("type", out var typeEl) && typeEl.GetString() == "array")
        {
            if (schema.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Object)
            {
                return ExtractSchemaProperties(items, root);
            }
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

            // Resolve children for nested objects
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
    private static JsonElement ResolveRef(JsonElement root, string? refPath)
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

    private static bool HasOpenApiExamples(JsonElement root)
    {
        if (!root.TryGetProperty("paths", out var paths)) return false;

        foreach (var path in paths.EnumerateObject())
        {
            if (path.Value.ValueKind != JsonValueKind.Object) continue;

            foreach (var method in path.Value.EnumerateObject())
            {
                if (method.Value.ValueKind != JsonValueKind.Object) continue;

                if (method.Value.TryGetProperty("requestBody", out var rb) && rb.ValueKind == JsonValueKind.Object)
                {
                    if (HasExamplesInContent(rb)) return true;
                }
                if (method.Value.TryGetProperty("responses", out var responses) && responses.ValueKind == JsonValueKind.Object)
                {
                    foreach (var resp in responses.EnumerateObject())
                    {
                        if (resp.Value.ValueKind != JsonValueKind.Object) continue;
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
