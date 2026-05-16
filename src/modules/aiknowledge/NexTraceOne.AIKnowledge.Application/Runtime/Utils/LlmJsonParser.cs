using System.Text.Json;

namespace NexTraceOne.AIKnowledge.Application.Runtime.Utils;

/// <summary>
/// Parser utilitário para extrair JSON válido de respostas de LLM.
/// Lida com markdown code blocks, texto envolvente, e JSON malformado leve.
/// </summary>
public static class LlmJsonParser
{
    private static readonly JsonSerializerOptions DefaultOptions = new(JsonSerializerOptions.Default)
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    /// <summary>
    /// Tenta extrair e deserializar um objeto JSON da resposta do LLM.
    /// </summary>
    public static bool TryParse<T>(string? response, out T? result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(response))
            return false;

        var json = ExtractJson(response);
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            result = JsonSerializer.Deserialize<T>(json, DefaultOptions);
            return result is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Extrai o conteúdo JSON puro de uma string que pode conter markdown ou texto envolvente.
    /// </summary>
    public static string? ExtractJson(string response)
    {
        var trimmed = response.Trim();

        // 1. Already pure JSON
        if (trimmed.StartsWith('{') && trimmed.EndsWith('}'))
            return trimmed;

        if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            return trimmed;

        // 2. Markdown code block ```json ... ```
        var codeBlockStart = trimmed.IndexOf("```json", StringComparison.OrdinalIgnoreCase);
        if (codeBlockStart >= 0)
        {
            var start = trimmed.IndexOf('\n', codeBlockStart);
            if (start < 0) start = codeBlockStart + "```json".Length;

            var end = trimmed.IndexOf("```", start, StringComparison.Ordinal);
            if (end > start)
                return trimmed[start..end].Trim();
        }

        // 3. Generic code block ``` ... ```
        codeBlockStart = trimmed.IndexOf("```", StringComparison.Ordinal);
        if (codeBlockStart >= 0)
        {
            var start = trimmed.IndexOf('\n', codeBlockStart);
            if (start < 0) start = codeBlockStart + 3;

            var end = trimmed.IndexOf("```", start, StringComparison.Ordinal);
            if (end > start)
                return trimmed[start..end].Trim();
        }

        // 4. Find first '{' or '[' and last '}' or ']'
        var firstBrace = trimmed.IndexOf('{');
        var firstBracket = trimmed.IndexOf('[');

        int startIdx;
        bool isArray;
        if (firstBrace >= 0 && (firstBracket < 0 || firstBrace < firstBracket))
        {
            startIdx = firstBrace;
            isArray = false;
        }
        else if (firstBracket >= 0)
        {
            startIdx = firstBracket;
            isArray = true;
        }
        else
        {
            return null;
        }

        var lastBrace = trimmed.LastIndexOf('}');
        var lastBracket = trimmed.LastIndexOf(']');
        int endIdx = isArray
            ? (lastBracket >= startIdx ? lastBracket : -1)
            : (lastBrace >= startIdx ? lastBrace : -1);

        if (endIdx >= startIdx)
            return trimmed[startIdx..(endIdx + 1)];

        return null;
    }
}
