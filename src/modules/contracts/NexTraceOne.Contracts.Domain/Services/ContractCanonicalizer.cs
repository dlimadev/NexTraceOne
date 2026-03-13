using System.Text.Json;

namespace NexTraceOne.Contracts.Domain.Services;

/// <summary>
/// Serviço de domínio puro para canonicalização de conteúdo de contratos.
/// Normaliza JSON/YAML/XML para uma representação canônica determinística,
/// eliminando diferenças irrelevantes (espaçamento, ordem de chaves em JSON,
/// newlines, trailing whitespace) antes do cálculo de hash para assinatura.
/// Essencial para que assinaturas e fingerprints sejam estáveis e verificáveis.
/// </summary>
public static class ContractCanonicalizer
{
    /// <summary>
    /// Produz uma representação canônica do conteúdo do contrato.
    /// Para JSON: ordena as chaves recursivamente e remove espaços desnecessários.
    /// Para YAML/XML: normaliza newlines e trim de linhas.
    /// </summary>
    /// <param name="content">Conteúdo bruto do contrato.</param>
    /// <param name="format">Formato: "json", "yaml" ou "xml".</param>
    /// <returns>Conteúdo canonicalizado para hash estável.</returns>
    public static string Canonicalize(string content, string format)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        return format?.ToLowerInvariant() switch
        {
            "json" => CanonicalizeJson(content),
            "yaml" => CanonicalizeText(content),
            "xml" => CanonicalizeText(content),
            _ => CanonicalizeText(content)
        };
    }

    /// <summary>
    /// Canonicaliza JSON: parse e re-serializa com chaves ordenadas, sem indentação.
    /// Garante que JSONs semanticamente iguais produzam o mesmo hash.
    /// Limita o tamanho para evitar alocação excessiva de memória (DoS).
    /// </summary>
    private static string CanonicalizeJson(string content)
    {
        if (content.Length > MaxContentLength)
            return CanonicalizeText(content);

        try
        {
            using var doc = JsonDocument.Parse(content, SafeJsonOptions);
            var sortedElement = SortJsonElement(doc.RootElement);
            return JsonSerializer.Serialize(sortedElement, CanonicalJsonOptions);
        }
        catch (JsonException)
        {
            return CanonicalizeText(content);
        }
    }

    /// <summary>
    /// Canonicaliza texto genérico: normaliza newlines, remove linhas em branco
    /// redundantes e trailing whitespace por linha.
    /// </summary>
    private static string CanonicalizeText(string content)
    {
        var lines = content
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Split('\n')
            .Select(l => l.TrimEnd());

        return string.Join("\n", lines).Trim();
    }

    /// <summary>
    /// Ordena recursivamente as propriedades de um JsonElement do tipo Object.
    /// Arrays mantêm a ordem original; valores primitivos são preservados.
    /// </summary>
    private static object? SortJsonElement(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.Object => element
                .EnumerateObject()
                .OrderBy(p => p.Name, StringComparer.Ordinal)
                .ToDictionary(p => p.Name, p => SortJsonElement(p.Value)),

            JsonValueKind.Array => element
                .EnumerateArray()
                .Select(SortJsonElement)
                .ToList(),

            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.GetDecimal(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };

    private static readonly JsonSerializerOptions CanonicalJsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = null
    };

    private static readonly JsonDocumentOptions SafeJsonOptions = new()
    {
        MaxDepth = 64
    };

    /// <summary>Limite máximo de conteúdo para parsing JSON (10 MB) — proteção contra DoS.</summary>
    private const int MaxContentLength = 10_000_000;
}
