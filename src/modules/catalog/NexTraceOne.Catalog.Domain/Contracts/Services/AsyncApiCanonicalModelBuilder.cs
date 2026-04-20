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
        return AsyncApiSpecParser.IsAsyncApi3x(root)
            ? ExtractAsyncApiOperations3x(root)
            : ExtractAsyncApiOperations2x(root);
    }

    /// <summary>AsyncAPI 2.x: publish/subscribe inline nos channels.</summary>
    private static List<ContractOperation> ExtractAsyncApiOperations2x(JsonElement root)
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

    /// <summary>
    /// AsyncAPI 3.x: operações no objecto top-level "operations" com "action": "send"/"receive"
    /// e referência ao canal via "channel.$ref".
    /// Mapeia "send" → "PUBLISH", "receive" → "SUBSCRIBE" para manter compatibilidade interna.
    /// </summary>
    private static List<ContractOperation> ExtractAsyncApiOperations3x(JsonElement root)
    {
        var ops = new List<ContractOperation>();
        if (!root.TryGetProperty("operations", out var operations)) return ops;

        foreach (var op in operations.EnumerateObject())
        {
            if (op.Value.ValueKind != JsonValueKind.Object) continue;

            if (!op.Value.TryGetProperty("action", out var actionEl)) continue;
            var action = actionEl.GetString()?.ToUpperInvariant();
            var opType = action switch
            {
                "SEND" => "PUBLISH",
                "RECEIVE" => "SUBSCRIBE",
                _ => null
            };
            if (opType is null) continue;

            // Resolver canal via $ref
            var channelName = string.Empty;
            if (op.Value.TryGetProperty("channel", out var chanRef)
                && chanRef.TryGetProperty("$ref", out var refEl)
                && refEl.GetString() is { Length: > 0 } refStr)
            {
                var parts = refStr.Split('/');
                if (parts.Length >= 3 && parts[1].Equals("channels", StringComparison.OrdinalIgnoreCase))
                    channelName = parts[2];
            }

            var opId = op.Name;
            var desc = op.Value.TryGetProperty("description", out var d) ? d.GetString() :
                       op.Value.TryGetProperty("summary", out var s) ? s.GetString() : null;
            var tags = CanonicalModelHelpers.ExtractArrayStrings(op.Value, "tags");

            ops.Add(new ContractOperation(opId, opId, desc, opType, channelName, [], [], false, tags));
        }
        return ops;
    }

    private static List<string> ExtractAsyncApiServers(JsonElement root)
    {
        var servers = new List<string>();
        if (!root.TryGetProperty("servers", out var svrs) || svrs.ValueKind != JsonValueKind.Object)
            return servers;

        var is3x = AsyncApiSpecParser.IsAsyncApi3x(root);

        foreach (var s in svrs.EnumerateObject())
        {
            if (s.Value.ValueKind != JsonValueKind.Object) continue;

            if (is3x)
            {
                // AsyncAPI 3.x: "host" + "protocol"
                var host = s.Value.TryGetProperty("host", out var hostEl) ? hostEl.GetString() ?? "" : "";
                var protocol = s.Value.TryGetProperty("protocol", out var protEl) ? protEl.GetString() ?? "" : "";
                if (!string.IsNullOrEmpty(host))
                    servers.Add(string.IsNullOrEmpty(protocol) ? host : $"{protocol}://{host}");
            }
            else
            {
                // AsyncAPI 2.x: "url"
                if (s.Value.TryGetProperty("url", out var url))
                    servers.Add(url.GetString() ?? "");
            }
        }
        return servers;
    }
}
