using System.Text.Json;

using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

#pragma warning disable CA1031

namespace NexTraceOne.Catalog.Domain.Contracts.Services;

/// <summary>
/// Constrói o modelo canônico a partir de especificações OpenAPI 3.x (JSON ou YAML).
/// </summary>
internal static class OpenApiCanonicalModelBuilder
{
    /// <summary>
    /// Constrói modelo canônico a partir de especificação OpenAPI 3.x em JSON ou YAML.
    /// </summary>
    internal static ContractCanonicalModel Build(string specContent)
    {
        try
        {
            var json = CanonicalModelHelpers.NormalizeToJson(specContent);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var title = CanonicalModelHelpers.GetJsonString(root, "info", "title") ?? "Untitled API";
            var specVersion = CanonicalModelHelpers.GetJsonString(root, "info", "version") ?? "";
            var description = CanonicalModelHelpers.GetJsonString(root, "info", "description");

            var operations = ExtractOpenApiOperations(root);
            var schemas = CanonicalModelHelpers.ExtractJsonSchemas(root, "components", "schemas");
            var securitySchemes = ExtractSecuritySchemes(root);
            var servers = ExtractServers(root);
            var tags = CanonicalModelHelpers.ExtractTags(root);
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
            return CanonicalModelHelpers.EmptyModel(ContractProtocol.OpenApi);
        }
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
                var tags = CanonicalModelHelpers.ExtractArrayStrings(method.Value, "tags");
                var inputParams = CanonicalModelHelpers.ExtractOperationParameters(method.Value);
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

        var (properties, schemaRef) = CanonicalModelHelpers.ExtractSchemaFromMediaType(mediaTypeElement, root);
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

            var (properties, schemaRef) = CanonicalModelHelpers.ExtractSchemaFromMediaType(mediaTypeElement, root);
            responses.Add(new ContractOperationResponse(statusCode, description, contentType, properties, schemaRef));
        }

        return responses;
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
