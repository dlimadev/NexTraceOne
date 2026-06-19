using System;
using System.Text.Json;

namespace NexTraceOne.VisualStudio.ToolWindows;

/// <summary>
/// Extracts the assistant text from the /api/v1/ai/ide/query JSON response.
/// Supports several common property names used by different model backends.
/// </summary>
internal static class IdeQueryResponseParser
{
    private static readonly string[] CandidateKeys = ["content", "output", "message", "response", "result"];

    /// <summary>
    /// Returns the assistant message text, or the raw body if no known field is found.
    /// </summary>
    public static string ExtractMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return body;

        try
        {
            using var doc = JsonDocument.Parse(body);
            foreach (var key in CandidateKeys)
            {
                if (doc.RootElement.TryGetProperty(key, out var prop) &&
                    prop.ValueKind == JsonValueKind.String)
                {
                    var value = prop.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                        return value!;
                }
            }
        }
        catch
        {
            // Fallback to raw body on any parse error.
        }

        return body;
    }
}
