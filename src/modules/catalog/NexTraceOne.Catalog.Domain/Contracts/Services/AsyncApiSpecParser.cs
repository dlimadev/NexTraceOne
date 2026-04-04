using System.Text.Json;

#pragma warning disable CA1031 // Captura genérica necessária para resiliência ao processar specs malformadas

namespace NexTraceOne.Catalog.Domain.Contracts.Services;

/// <summary>
/// Parser responsável pela extração estruturada de dados de especificações AsyncAPI em JSON.
/// AsyncAPI modela contratos event-driven: em vez de paths/métodos HTTP,
/// define "channels" (tópicos, filas, streams) com operações "publish" e/ou "subscribe".
/// Cada canal pode ter mensagens tipadas em "components.messages".
/// Em caso de specs malformadas, retorna estruturas vazias para não bloquear o diff.
/// </summary>
public static class AsyncApiSpecParser
{
    /// <summary>
    /// Tipos de operação válidos em channels AsyncAPI.
    /// "publish" indica que a aplicação envia mensagens ao canal;
    /// "subscribe" indica que a aplicação recebe mensagens do canal.
    /// </summary>
    private static readonly HashSet<string> ValidOperations = new(StringComparer.OrdinalIgnoreCase)
    {
        "PUBLISH", "SUBSCRIBE"
    };

    /// <summary>
    /// Extrai mapa de canais e seus tipos de operação a partir de uma especificação AsyncAPI em JSON.
    /// Cada canal pode ter operações "publish" e/ou "subscribe", análogas aos métodos HTTP em REST.
    /// Retorna um dicionário cujas chaves são os nomes dos canais (ex: "user/signedup")
    /// e cujos valores são os conjuntos de operações definidas.
    /// </summary>
    /// <param name="specContent">Conteúdo JSON da spec AsyncAPI.</param>
    /// <returns>Dicionário canal → conjunto de tipos de operação (case-insensitive).</returns>
    public static Dictionary<string, HashSet<string>> ExtractChannelsAndOperations(string specContent)
    {
        var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var doc = JsonDocument.Parse(specContent);
            if (doc.RootElement.TryGetProperty("channels", out var channels))
            {
                foreach (var channel in channels.EnumerateObject())
                {
                    var operations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var prop in channel.Value.EnumerateObject())
                    {
                        var op = prop.Name.ToUpperInvariant();
                        if (ValidOperations.Contains(op))
                            operations.Add(op);
                    }
                    result[channel.Name] = operations;
                }
            }
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Trace.TraceWarning(
                "AsyncApiSpecParser: Failed to parse AsyncAPI channels — {0}", ex.Message);
        }
        return result;
    }

    /// <summary>
    /// Extrai o schema da mensagem de uma operação específica dentro de um canal AsyncAPI.
    /// Navega pela estrutura "channels.{channel}.{operation}.message.payload.properties"
    /// e extrai os campos com indicação de obrigatoriedade via "required" array do payload.
    /// Retorna dicionário campo → obrigatório (bool).
    /// </summary>
    /// <param name="specContent">Conteúdo JSON da spec AsyncAPI.</param>
    /// <param name="channel">Nome do canal (ex: "user/signedup").</param>
    /// <param name="operation">Tipo de operação (ex: "publish" ou "subscribe").</param>
    /// <returns>Dicionário nome do campo → indicador de obrigatoriedade (case-insensitive).</returns>
    public static Dictionary<string, bool> ExtractMessageSchema(string specContent, string channel, string operation)
    {
        var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var doc = JsonDocument.Parse(specContent);
            if (doc.RootElement.TryGetProperty("channels", out var channels)
                && channels.TryGetProperty(channel, out var channelEl)
                && channelEl.TryGetProperty(operation.ToLowerInvariant(), out var operationEl)
                && operationEl.TryGetProperty("message", out var message)
                && message.TryGetProperty("payload", out var payload)
                && payload.TryGetProperty("properties", out var properties))
            {
                // Extrai lista de campos obrigatórios do array "required" do payload
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
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Trace.TraceWarning(
                "AsyncApiSpecParser: Failed to parse message schema for channel '{0}' operation '{1}' — {2}",
                channel, operation, ex.Message);
        }
        return result;
    }
}
