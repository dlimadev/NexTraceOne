using System.Text.Json;

#pragma warning disable CA1031 // Captura genérica necessária para resiliência ao processar specs malformadas

namespace NexTraceOne.Catalog.Domain.Contracts.Services;

/// <summary>
/// Serviço de domínio responsável pela extração de metadados estruturados de specs AsyncAPI
/// para popular EventContractDetail e EventDraftMetadata.
/// Extrai: título, versão AsyncAPI, defaultContentType, channels com operações,
/// mensagens/schemas e servidores/brokers.
/// Delega a extração de channels ao AsyncApiSpecParser existente.
/// </summary>
public static class AsyncApiMetadataExtractor
{
    /// <summary>Resultado da extração de metadados de uma spec AsyncAPI.</summary>
    public sealed record AsyncApiMetadata(
        string Title,
        string AsyncApiVersion,
        string DefaultContentType,
        string ChannelsJson,
        string MessagesJson,
        string ServersJson);

    /// <summary>
    /// Extrai os metadados estruturados relevantes de uma especificação AsyncAPI (JSON ou YAML).
    /// Retorna valores padrão seguros quando a spec está malformada ou incompleta.
    /// </summary>
    /// <param name="specContent">Conteúdo JSON da especificação AsyncAPI.</param>
    /// <param name="fallbackTitle">Título de fallback quando não encontrado na spec.</param>
    public static AsyncApiMetadata Extract(string specContent, string fallbackTitle = "EventService")
    {
        try
        {
            using var doc = JsonDocument.Parse(specContent);
            var root = doc.RootElement;

            var title = ExtractTitle(root, fallbackTitle);
            var asyncApiVersion = ExtractAsyncApiVersion(root);
            var defaultContentType = ExtractDefaultContentType(root);
            var channelsJson = SerializeChannels(AsyncApiSpecParser.ExtractChannelsAndOperations(specContent));
            var messagesJson = ExtractMessagesJson(root);
            var serversJson = ExtractServersJson(root);

            return new AsyncApiMetadata(title, asyncApiVersion, defaultContentType, channelsJson, messagesJson, serversJson);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning(
                "AsyncApiMetadataExtractor: Failed to extract metadata — {0}: {1}", ex.GetType().Name, ex.Message);
            return BuildDefault(fallbackTitle);
        }
    }

    private static AsyncApiMetadata BuildDefault(string title) =>
        new(title, "2.6.0", "application/json", "{}", "{}", "{}");

    private static string ExtractTitle(JsonElement root, string fallback)
    {
        if (root.TryGetProperty("info", out var info)
            && info.TryGetProperty("title", out var titleEl)
            && titleEl.GetString() is { Length: > 0 } t)
            return t;
        return fallback;
    }

    private static string ExtractAsyncApiVersion(JsonElement root)
    {
        if (root.TryGetProperty("asyncapi", out var v) && v.GetString() is { Length: > 0 } ver)
            return ver;
        return "2.6.0";
    }

    private static string ExtractDefaultContentType(JsonElement root)
    {
        if (root.TryGetProperty("defaultContentType", out var ct) && ct.GetString() is { Length: > 0 } c)
            return c;
        return "application/json";
    }

    private static string SerializeChannels(Dictionary<string, HashSet<string>> channelsMap)
    {
        if (channelsMap.Count == 0)
            return "{}";

        var serializable = channelsMap.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.OrderBy(op => op).ToList());

        return JsonSerializer.Serialize(serializable);
    }

    private static string ExtractMessagesJson(JsonElement root)
    {
        try
        {
            // AsyncAPI 2.x: components.messages
            if (!root.TryGetProperty("components", out var components)
                || !components.TryGetProperty("messages", out var messages))
                return "{}";

            var result = new Dictionary<string, List<string>>();
            foreach (var msg in messages.EnumerateObject())
            {
                var fields = new List<string>();
                if (msg.Value.TryGetProperty("payload", out var payload)
                    && payload.TryGetProperty("properties", out var properties))
                {
                    foreach (var prop in properties.EnumerateObject())
                        fields.Add(prop.Name);
                }
                result[msg.Name] = fields;
            }
            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning(
                "AsyncApiMetadataExtractor: Failed to extract messages JSON — {0}: {1}", ex.GetType().Name, ex.Message);
            return "{}";
        }
    {
        try
        {
            if (!root.TryGetProperty("servers", out var servers))
                return "{}";

            var result = new Dictionary<string, string>();
            foreach (var server in servers.EnumerateObject())
            {
                var url = server.Value.TryGetProperty("url", out var urlEl)
                    ? urlEl.GetString() ?? string.Empty
                    : string.Empty;
                result[server.Name] = url;
            }
            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning(
                "AsyncApiMetadataExtractor: Failed to extract servers JSON — {0}: {1}", ex.GetType().Name, ex.Message);
            return "{}";
        }
