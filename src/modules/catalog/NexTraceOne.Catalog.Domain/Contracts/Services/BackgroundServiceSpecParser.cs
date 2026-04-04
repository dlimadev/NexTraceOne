using System.Text.Json;

namespace NexTraceOne.Catalog.Domain.Contracts.Services;

/// <summary>
/// Parser de especificações de Background Service Contracts.
/// Extrai os metadados estruturados do JSON de spec de um worker/job/scheduler
/// para permitir diff semântico, canonical model e rule engine.
///
/// Formato esperado da spec (opcional — pode vir de SpecContent ou ser construído
/// a partir dos campos do BackgroundServiceContractDetail):
/// <code>
/// {
///   "serviceName": "OrderExpirationJob",
///   "category": "Job",
///   "triggerType": "Cron",
///   "scheduleExpression": "0 * * * *",
///   "timeoutExpression": "PT30M",
///   "allowsConcurrency": false,
///   "inputs": { "orderId": "Guid|The order to expire" },
///   "outputs": { "expiredCount": "int|Number of orders expired" },
///   "sideEffects": ["Writes to order_history table", "Publishes OrderExpired event"]
/// }
/// </code>
/// </summary>
#pragma warning disable CA1031 // Captura genérica necessária para resiliência ao processar specs malformadas
public static class BackgroundServiceSpecParser
{
    /// <summary>
    /// Metadados extraídos de uma especificação de Background Service Contract.
    /// </summary>
    public sealed record BackgroundServiceSpec(
        string ServiceName,
        string Category,
        string TriggerType,
        string? ScheduleExpression,
        string? TimeoutExpression,
        bool AllowsConcurrency,
        IReadOnlyDictionary<string, string> Inputs,
        IReadOnlyDictionary<string, string> Outputs,
        IReadOnlyList<string> SideEffects);

    /// <summary>
    /// Faz parse do conteúdo JSON de uma spec de background service.
    /// Retorna spec vazia para conteúdo inválido ou malformado (resiliência ao pipeline).
    /// </summary>
    public static BackgroundServiceSpec Parse(string specContent)
    {
        if (string.IsNullOrWhiteSpace(specContent) || specContent.Trim() == "{}")
            return EmptySpec();

        try
        {
            using var doc = JsonDocument.Parse(specContent);
            var root = doc.RootElement;

            var serviceName = TryGetString(root, "serviceName") ?? string.Empty;
            var category = TryGetString(root, "category") ?? string.Empty;
            var triggerType = TryGetString(root, "triggerType") ?? "OnDemand";
            var schedule = TryGetString(root, "scheduleExpression");
            var timeout = TryGetString(root, "timeoutExpression");
            var allowsConcurrency = TryGetBool(root, "allowsConcurrency");

            var inputs = TryGetDictionary(root, "inputs");
            var outputs = TryGetDictionary(root, "outputs");
            var sideEffects = TryGetStringList(root, "sideEffects");

            return new BackgroundServiceSpec(
                serviceName, category, triggerType, schedule, timeout,
                allowsConcurrency, inputs, outputs, sideEffects);
        }
        catch
        {
            System.Diagnostics.Trace.TraceWarning("BackgroundServiceSpecParser: Failed to parse spec content — returning empty spec.");
            return EmptySpec();
        }
    }

    /// <summary>Retorna spec vazia para specs inválidas ou ausentes.</summary>
    public static BackgroundServiceSpec EmptySpec() =>
        new(string.Empty, string.Empty, "OnDemand", null, null, false,
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            Array.Empty<string>());

    private static string? TryGetString(JsonElement root, string key)
    {
        if (root.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString();
        return null;
    }

    private static bool TryGetBool(JsonElement root, string key)
    {
        if (root.TryGetProperty(key, out var prop))
            return prop.ValueKind == JsonValueKind.True;
        return false;
    }

    private static IReadOnlyDictionary<string, string> TryGetDictionary(JsonElement root, string key)
    {
        if (!root.TryGetProperty(key, out var prop) || prop.ValueKind != JsonValueKind.Object)
            return new Dictionary<string, string>();

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in prop.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.String)
                result[property.Name] = property.Value.GetString() ?? string.Empty;
        }
        return result;
    }

    private static IReadOnlyList<string> TryGetStringList(JsonElement root, string key)
    {
        if (!root.TryGetProperty(key, out var prop) || prop.ValueKind != JsonValueKind.Array)
            return Array.Empty<string>();

        return prop.EnumerateArray()
            .Where(e => e.ValueKind == JsonValueKind.String)
            .Select(e => e.GetString() ?? string.Empty)
            .ToList()
            .AsReadOnly();
    }
}
#pragma warning restore CA1031
