using System.Text.Json;

using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

#pragma warning disable CA1031

namespace NexTraceOne.Catalog.Domain.Contracts.Services;

/// <summary>
/// Constrói o modelo canônico a partir de especificações Swagger 2.0 (JSON ou YAML).
/// </summary>
internal static class SwaggerCanonicalModelBuilder
{
    /// <summary>
    /// Constrói modelo canônico a partir de especificação Swagger 2.0 em JSON ou YAML.
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

            var operations = ExtractSwaggerOperations(root);
            var schemas = CanonicalModelHelpers.ExtractJsonSchemas(root, "definitions");
            var securitySchemes = ExtractSwaggerSecuritySchemes(root);
            var tags = CanonicalModelHelpers.ExtractTags(root);

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
            return CanonicalModelHelpers.EmptyModel(ContractProtocol.Swagger);
        }
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
                var tags = CanonicalModelHelpers.ExtractArrayStrings(method.Value, "tags");
                var inputParams = CanonicalModelHelpers.ExtractOperationParameters(method.Value);
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
                var resolved = CanonicalModelHelpers.ResolveRef(root, schemaRef);
                if (resolved.ValueKind == JsonValueKind.Object)
                {
                    var props = CanonicalModelHelpers.ExtractSchemaProperties(resolved, root);
                    return new ContractRequestBody("application/json", required, props, schemaRef);
                }
                return new ContractRequestBody("application/json", required, [], schemaRef);
            }

            var properties = CanonicalModelHelpers.ExtractSchemaProperties(schema, root);
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
                var resolved = CanonicalModelHelpers.ResolveRef(root, schemaRef);
                if (resolved.ValueKind == JsonValueKind.Object)
                {
                    var props = CanonicalModelHelpers.ExtractSchemaProperties(resolved, root);
                    responses.Add(new ContractOperationResponse(statusCode, description, "application/json", props, schemaRef));
                    continue;
                }
                responses.Add(new ContractOperationResponse(statusCode, description, "application/json", [], schemaRef));
                continue;
            }

            var properties = CanonicalModelHelpers.ExtractSchemaProperties(schema, root);
            responses.Add(new ContractOperationResponse(statusCode, description, "application/json", properties, schemaRef));
        }

        return responses;
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
}
