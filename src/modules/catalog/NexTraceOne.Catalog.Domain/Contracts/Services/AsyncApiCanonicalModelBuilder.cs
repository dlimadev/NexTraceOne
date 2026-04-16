using System.Text.Json;

using NexTraceOne.Catalog.Domain.Contracts.Enums;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

#pragma warning disable CA1031

namespace NexTraceOne.Catalog.Domain.Contracts.Services;

/// <summary>
/// Constrói o modelo canônico a partir de especificações AsyncAPI 2.x/3.x (JSON ou YAML).
/// </summary>
internal static class AsyncApiCanonicalModelBuilder
{
    /// <summary>
    /// Constrói modelo canônico a partir de especificação AsyncAPI 2.x/3.x em JSON ou YAML.
    /// </summary>
    internal static ContractCanonicalModel Build(string specContent)
    {
        try
        {
            var json = CanonicalModelHelpers.NormalizeToJson(specContent);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var title = CanonicalModelHelpers.GetJsonString(root, "info", "title") ?? "Untitled Event API";
            var specVersion = CanonicalModelHelpers.GetJsonString(root, "asyncapi") ?? "";
            var description = CanonicalModelHelpers.GetJsonString(root, "info", "description");

            var operations = ExtractAsyncApiOperations(root);
            var schemas = CanonicalModelHelpers.ExtractJsonSchemas(root, "components", "schemas");
            var servers = ExtractAsyncApiServers(root);
            var tags = CanonicalModelHelpers.ExtractTags(root);

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
            return CanonicalModelHelpers.EmptyModel(ContractProtocol.AsyncApi);
        }
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
                var tags = CanonicalModelHelpers.ExtractArrayStrings(prop.Value, "tags");

                ops.Add(new ContractOperation(opId, opId, desc, opType, channel.Name, [], [], false, tags));
            }
        }
        return ops;
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
}
