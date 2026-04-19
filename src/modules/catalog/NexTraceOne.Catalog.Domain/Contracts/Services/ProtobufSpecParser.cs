using System.Text.RegularExpressions;

#pragma warning disable CA1031 // Captura genérica necessária para resiliência ao processar specs malformadas

namespace NexTraceOne.Catalog.Domain.Contracts.Services;

/// <summary>
/// Parser responsável pela extração estruturada de dados de especificações Protocol Buffers (.proto).
/// Extrai messages, enums, services e RPCs usando análise textual com regex.
/// Não depende de nenhuma biblioteca de parse de .proto para manter o domínio sem dependências externas.
/// Em caso de specs malformadas, retorna estruturas vazias para não bloquear o diff.
/// </summary>
public static class ProtobufSpecParser
{
    private static readonly Regex MessageRegex = new(
        @"^\s*message\s+(\w+)\s*\{([^{}]*(?:\{[^{}]*\}[^{}]*)*)\}",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex EnumRegex = new(
        @"^\s*enum\s+(\w+)\s*\{([^}]*)\}",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex ServiceRegex = new(
        @"^\s*service\s+(\w+)\s*\{([^}]*)\}",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex RpcRegex = new(
        @"^\s*rpc\s+(\w+)\s*\(",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex FieldRegex = new(
        @"^\s*(?:optional|required|repeated)?\s+\w+\s+(\w+)\s*=\s*(\d+)\s*;",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex EnumValueRegex = new(
        @"^\s*([A-Z_][A-Z0-9_]*)\s*=\s*\d+\s*;",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex SyntaxRegex = new(
        @"syntax\s*=\s*""(proto[23])""",
        RegexOptions.Compiled);

    /// <summary>
    /// Extrai mapa de messages e seus campos (com respectivos números de campo) do .proto.
    /// Retorna dicionário messageName → dicionário fieldName → fieldNumber.
    /// O número de campo é crítico em Protobuf: reutilizá-lo para tipos diferentes é breaking.
    /// </summary>
    /// <param name="protoContent">Conteúdo do ficheiro .proto.</param>
    /// <returns>Dicionário messageName → (fieldName → fieldNumber).</returns>
    public static Dictionary<string, Dictionary<string, int>> ExtractMessages(string protoContent)
    {
        var result = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(protoContent))
            return result;

        try
        {
            foreach (Match match in MessageRegex.Matches(protoContent))
            {
                var messageName = match.Groups[1].Value;
                var body = match.Groups[2].Value;

                var fields = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (Match fieldMatch in FieldRegex.Matches(body))
                {
                    var fieldName = fieldMatch.Groups[1].Value;
                    if (int.TryParse(fieldMatch.Groups[2].Value, out var fieldNumber))
                        fields[fieldName] = fieldNumber;
                }

                result[messageName] = fields;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning(
                "ProtobufSpecParser: Failed to extract messages — {0}", ex.Message);
        }

        return result;
    }

    /// <summary>
    /// Extrai mapa de services e seus RPCs do .proto.
    /// Retorna dicionário serviceName → conjunto de nomes de RPC.
    /// </summary>
    /// <param name="protoContent">Conteúdo do ficheiro .proto.</param>
    /// <returns>Dicionário serviceName → conjunto de nomes de RPC.</returns>
    public static Dictionary<string, HashSet<string>> ExtractServices(string protoContent)
    {
        var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(protoContent))
            return result;

        try
        {
            foreach (Match match in ServiceRegex.Matches(protoContent))
            {
                var serviceName = match.Groups[1].Value;
                var body = match.Groups[2].Value;

                var rpcs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (Match rpcMatch in RpcRegex.Matches(body))
                    rpcs.Add(rpcMatch.Groups[1].Value);

                result[serviceName] = rpcs;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning(
                "ProtobufSpecParser: Failed to extract services — {0}", ex.Message);
        }

        return result;
    }

    /// <summary>
    /// Extrai mapa de enums e seus valores do .proto.
    /// Retorna dicionário enumName → conjunto de nomes de valores.
    /// </summary>
    /// <param name="protoContent">Conteúdo do ficheiro .proto.</param>
    /// <returns>Dicionário enumName → conjunto de nomes de valores enum.</returns>
    public static Dictionary<string, HashSet<string>> ExtractEnums(string protoContent)
    {
        var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(protoContent))
            return result;

        try
        {
            foreach (Match match in EnumRegex.Matches(protoContent))
            {
                var enumName = match.Groups[1].Value;
                var body = match.Groups[2].Value;

                var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (Match valueMatch in EnumValueRegex.Matches(body))
                    values.Add(valueMatch.Groups[1].Value);

                result[enumName] = values;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning(
                "ProtobufSpecParser: Failed to extract enums — {0}", ex.Message);
        }

        return result;
    }

    /// <summary>
    /// Extrai a versão de sintaxe do .proto (proto2 ou proto3).
    /// Retorna null se não encontrada.
    /// </summary>
    public static string? ExtractSyntaxVersion(string protoContent)
    {
        if (string.IsNullOrWhiteSpace(protoContent))
            return null;

        var match = SyntaxRegex.Match(protoContent);
        return match.Success ? match.Groups[1].Value : null;
    }
}
