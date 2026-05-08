using System.Text.Json;
using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Domain.Contracts.Services;

/// <summary>
/// Parser de especificações de Data Contract Schema.
/// Extrai metadados estruturados do JSON de schema de colunas — nome, tipo, nullable e
/// classificação PII — para alimentar o motor de compliance e histórico de versões.
///
/// Formato esperado (array de colunas):
/// <code>
/// [
///   { "name": "id",    "type": "uuid",    "nullable": false, "pii": "None"   },
///   { "name": "email", "type": "varchar", "nullable": false, "pii": "High"   },
///   { "name": "salary","type": "decimal", "nullable": true,  "pii": "Critical" }
/// ]
/// </code>
///
/// O campo PII aceita "pii" ou "piiClassification" (case-insensitive).
/// Valores aceites: None, Low, Medium, High, Critical.
///
/// Referência: CC-03, ADR-007.
/// </summary>
#pragma warning disable CA1031 // Captura genérica necessária para resiliência ao processar specs malformadas
public static class DataContractSpecParser
{
    /// <summary>
    /// Metadados extraídos de um schema de Data Contract.
    /// </summary>
    public sealed record DataContractSpec(
        int ColumnCount,
        PiiClassification MaxPiiClassification,
        IReadOnlyList<ColumnSpec> Columns);

    /// <summary>
    /// Metadados de uma coluna individual no schema.
    /// </summary>
    public sealed record ColumnSpec(
        string Name,
        string Type,
        bool Nullable,
        PiiClassification PiiClassification);

    /// <summary>
    /// Faz parse do JSON de schema de colunas.
    /// Retorna spec vazia para conteúdo inválido ou malformado (resiliência ao pipeline).
    /// </summary>
    public static DataContractSpec Parse(string schemaJson)
    {
        if (string.IsNullOrWhiteSpace(schemaJson) || schemaJson.Trim() is "[]" or "{}")
            return EmptySpec();

        try
        {
            using var doc = JsonDocument.Parse(schemaJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return EmptySpec();

            var columns = new List<ColumnSpec>();
            var maxPii = PiiClassification.None;

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var name = TryGetString(element, "name") ?? string.Empty;
                var type = TryGetString(element, "type") ?? "unknown";
                var nullable = TryGetBool(element, "nullable");
                var pii = ParsePiiClassification(element);

                if (pii > maxPii)
                    maxPii = pii;

                columns.Add(new ColumnSpec(name, type, nullable, pii));
            }

            return new DataContractSpec(columns.Count, maxPii, columns.AsReadOnly());
        }
        catch
        {
            return EmptySpec();
        }
    }

    /// <summary>Retorna spec vazia para schemas ausentes ou inválidos.</summary>
    public static DataContractSpec EmptySpec() =>
        new(0, PiiClassification.None, Array.Empty<ColumnSpec>());

    private static PiiClassification ParsePiiClassification(JsonElement element)
    {
        // Accept both "pii" and "piiClassification" property names
        var found = element.TryGetProperty("piiClassification", out var prop)
                 || element.TryGetProperty("pii", out prop);

        if (!found) return PiiClassification.None;

        var raw = prop.GetString();
        return Enum.TryParse<PiiClassification>(raw, ignoreCase: true, out var result)
            ? result
            : PiiClassification.None;
    }

    private static string? TryGetString(JsonElement element, string key)
    {
        if (element.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString();
        return null;
    }

    private static bool TryGetBool(JsonElement element, string key)
    {
        if (element.TryGetProperty(key, out var prop))
            return prop.ValueKind == JsonValueKind.True;
        return false;
    }
}
#pragma warning restore CA1031
