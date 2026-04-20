using System.Text.Json;

#pragma warning disable CA1031 // Captura genérica necessária para resiliência ao processar specs malformadas

namespace NexTraceOne.Catalog.Domain.Contracts.Services;

/// <summary>
/// Parser responsável pela extração estruturada de dados de especificações AsyncAPI em JSON.
/// Suporta AsyncAPI 2.x e 3.x.
/// AsyncAPI 2.x: channels contêm operações "publish"/"subscribe" directamente.
/// AsyncAPI 3.x: secção "operations" top-level com "action": "send"/"receive" e referência ao canal via $ref.
/// Em caso de specs malformadas, retorna estruturas vazias para não bloquear o diff.
/// </summary>
public static class AsyncApiSpecParser
{
    /// <summary>Tipos de operação válidos em AsyncAPI 2.x channels.</summary>
    private static readonly HashSet<string> ValidOperations2x = new(StringComparer.OrdinalIgnoreCase)
    {
        "PUBLISH", "SUBSCRIBE"
    };

    /// <summary>Tipos de acção válidos em AsyncAPI 3.x operations (mapeados para terminologia interna).</summary>
    private static readonly HashSet<string> ValidActions3x = new(StringComparer.OrdinalIgnoreCase)
    {
        "SEND", "RECEIVE"
    };

    /// <summary>
    /// Detecta se a spec é AsyncAPI 3.x (versão >= "3.").
    /// </summary>
    public static bool IsAsyncApi3x(JsonElement root)
    {
        if (root.TryGetProperty("asyncapi", out var ver) && ver.GetString() is { Length: > 0 } v)
            return v.StartsWith("3.", StringComparison.Ordinal);
        return false;
    }

    /// <summary>
    /// Extrai mapa de canais e seus tipos de operação a partir de uma especificação AsyncAPI em JSON.
    /// Suporta AsyncAPI 2.x (publish/subscribe por canal) e 3.x (operations top-level com send/receive).
    /// </summary>
    /// <param name="specContent">Conteúdo JSON da spec AsyncAPI.</param>
    /// <returns>Dicionário canal → conjunto de tipos de operação (case-insensitive).</returns>
    public static Dictionary<string, HashSet<string>> ExtractChannelsAndOperations(string specContent)
    {
        var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var doc = JsonDocument.Parse(specContent);
            var root = doc.RootElement;

            if (IsAsyncApi3x(root))
                ExtractChannelsAndOperations3x(root, result);
            else
                ExtractChannelsAndOperations2x(root, result);
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Trace.TraceWarning(
                "AsyncApiSpecParser: Failed to parse AsyncAPI channels — {0}", ex.Message);
        }
        return result;
    }

    /// <summary>AsyncAPI 2.x: extrai publish/subscribe directamente dos channels.</summary>
    private static void ExtractChannelsAndOperations2x(
        JsonElement root,
        Dictionary<string, HashSet<string>> result)
    {
        if (!root.TryGetProperty("channels", out var channels)) return;

        foreach (var channel in channels.EnumerateObject())
        {
            var operations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in channel.Value.EnumerateObject())
            {
                var op = prop.Name.ToUpperInvariant();
                if (ValidOperations2x.Contains(op))
                    operations.Add(op);
            }
            result[channel.Name] = operations;
        }
    }

    /// <summary>
    /// AsyncAPI 3.x: extrai canais e acções a partir da secção top-level "operations".
    /// Cada operation tem "action" (send/receive) e "channel.$ref" apontando para um canal.
    /// Mapeia "send" → "PUBLISH" e "receive" → "SUBSCRIBE" para manter compatibilidade interna.
    /// </summary>
    private static void ExtractChannelsAndOperations3x(
        JsonElement root,
        Dictionary<string, HashSet<string>> result)
    {
        // Inicializar todos os canais definidos (mesmo sem operações explícitas)
        if (root.TryGetProperty("channels", out var channels))
        {
            foreach (var channel in channels.EnumerateObject())
            {
                if (!result.ContainsKey(channel.Name))
                    result[channel.Name] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        if (!root.TryGetProperty("operations", out var operations)) return;

        foreach (var op in operations.EnumerateObject())
        {
            if (op.Value.ValueKind != JsonValueKind.Object) continue;

            // Obter a acção: "send" ou "receive"
            if (!op.Value.TryGetProperty("action", out var actionEl)) continue;
            var action = actionEl.GetString()?.ToUpperInvariant();
            if (!ValidActions3x.Contains(action ?? string.Empty)) continue;

            // Mapear acção para terminologia interna (compatível com 2.x)
            var mappedOp = action == "SEND" ? "PUBLISH" : "SUBSCRIBE";

            // Resolver referência ao canal: "channel": {"$ref": "#/channels/channelName"}
            if (!op.Value.TryGetProperty("channel", out var channelRef)) continue;
            var channelName = Resolve3xRef(channelRef);
            if (string.IsNullOrEmpty(channelName)) continue;

            if (!result.TryGetValue(channelName, out var ops))
            {
                ops = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                result[channelName] = ops;
            }
            ops.Add(mappedOp);
        }
    }

    /// <summary>
    /// Extrai o schema da mensagem de uma operação específica dentro de um canal AsyncAPI.
    /// Suporta AsyncAPI 2.x e 3.x.
    /// </summary>
    /// <param name="specContent">Conteúdo JSON da spec AsyncAPI.</param>
    /// <param name="channel">Nome do canal.</param>
    /// <param name="operation">Tipo de operação ("publish", "subscribe", "send" ou "receive").</param>
    /// <returns>Dicionário nome do campo → indicador de obrigatoriedade (case-insensitive).</returns>
    public static Dictionary<string, bool> ExtractMessageSchema(string specContent, string channel, string operation)
    {
        var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var doc = JsonDocument.Parse(specContent);
            var root = doc.RootElement;

            if (IsAsyncApi3x(root))
                ExtractMessageSchema3x(root, channel, operation, result);
            else
                ExtractMessageSchema2x(root, channel, operation, result);
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Trace.TraceWarning(
                "AsyncApiSpecParser: Failed to parse message schema for channel '{0}' operation '{1}' — {2}",
                channel, operation, ex.Message);
        }
        return result;
    }

    /// <summary>AsyncAPI 2.x: navega channels.{channel}.{operation}.message.payload.properties.</summary>
    private static void ExtractMessageSchema2x(
        JsonElement root,
        string channel,
        string operation,
        Dictionary<string, bool> result)
    {
        if (!root.TryGetProperty("channels", out var channels)
            || !channels.TryGetProperty(channel, out var channelEl)
            || !channelEl.TryGetProperty(operation.ToLowerInvariant(), out var operationEl)
            || !operationEl.TryGetProperty("message", out var message)
            || !message.TryGetProperty("payload", out var payload)
            || !payload.TryGetProperty("properties", out var properties))
            return;

        ExtractSchemaProperties(payload, properties, result);
    }

    /// <summary>
    /// AsyncAPI 3.x: procura a mensagem nas referências do canal e depois em components.messages.
    /// Navega channels.{channel}.messages → resolve $ref → components.messages.{name}.payload.properties.
    /// </summary>
    private static void ExtractMessageSchema3x(
        JsonElement root,
        string channel,
        string operation,
        Dictionary<string, bool> result)
    {
        // Tentar obter a mensagem directamente do canal
        if (!root.TryGetProperty("channels", out var channels)
            || !channels.TryGetProperty(channel, out var channelEl)) return;

        if (channelEl.TryGetProperty("messages", out var channelMessages))
        {
            foreach (var msg in channelMessages.EnumerateObject())
            {
                JsonElement payload;
                if (msg.Value.TryGetProperty("$ref", out var refEl))
                {
                    // Resolver referência para components.messages
                    var refName = ResolveComponentRef(refEl.GetString(), "messages");
                    if (refName is not null
                        && root.TryGetProperty("components", out var comps)
                        && comps.TryGetProperty("messages", out var compMsgs)
                        && compMsgs.TryGetProperty(refName, out var resolved)
                        && resolved.TryGetProperty("payload", out payload)
                        && payload.TryGetProperty("properties", out var props))
                    {
                        ExtractSchemaProperties(payload, props, result);
                        return;
                    }
                }
                else if (msg.Value.TryGetProperty("payload", out payload)
                    && payload.TryGetProperty("properties", out var directProps))
                {
                    ExtractSchemaProperties(payload, directProps, result);
                    return;
                }
            }
        }
    }

    /// <summary>Extrai propriedades de um schema com marcação de obrigatoriedade.</summary>
    private static void ExtractSchemaProperties(
        JsonElement payload,
        JsonElement properties,
        Dictionary<string, bool> result)
    {
        var requiredFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (payload.TryGetProperty("required", out var requiredArray)
            && requiredArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var req in requiredArray.EnumerateArray())
            {
                var fieldName = req.GetString();
                if (!string.IsNullOrEmpty(fieldName))
                    requiredFields.Add(fieldName);
            }
        }

        foreach (var prop in properties.EnumerateObject())
        {
            if (!string.IsNullOrEmpty(prop.Name))
                result[prop.Name] = requiredFields.Contains(prop.Name);
        }
    }

    /// <summary>
    /// Resolve uma referência $ref de channel AsyncAPI 3.x para o nome do canal.
    /// Exemplo: {"$ref": "#/channels/userSignedUp"} → "userSignedUp".
    /// </summary>
    private static string? Resolve3xRef(JsonElement refEl)
    {
        if (refEl.TryGetProperty("$ref", out var r) && r.GetString() is { Length: > 0 } refStr)
        {
            // #/channels/channelName
            var parts = refStr.Split('/');
            return parts.Length >= 3 && parts[1].Equals("channels", StringComparison.OrdinalIgnoreCase)
                ? parts[2]
                : null;
        }
        return null;
    }

    /// <summary>
    /// Resolve uma referência $ref de componente AsyncAPI 3.x.
    /// Exemplo: "#/components/messages/UserSignedUp" com section="messages" → "UserSignedUp".
    /// </summary>
    private static string? ResolveComponentRef(string? refStr, string section)
    {
        if (string.IsNullOrEmpty(refStr)) return null;
        var parts = refStr.Split('/');
        // #/components/{section}/{name}
        return parts.Length >= 4
            && parts[1].Equals("components", StringComparison.OrdinalIgnoreCase)
            && parts[2].Equals(section, StringComparison.OrdinalIgnoreCase)
            ? parts[3]
            : null;
    }
}
