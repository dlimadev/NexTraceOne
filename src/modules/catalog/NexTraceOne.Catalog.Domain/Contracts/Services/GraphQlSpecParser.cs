using System.Text.RegularExpressions;

#pragma warning disable CA1031 // Captura genérica necessária para resiliência ao processar specs malformadas

namespace NexTraceOne.Catalog.Domain.Contracts.Services;

/// <summary>
/// Parser responsável pela extração estruturada de dados de especificações GraphQL SDL.
/// GraphQL define contratos via Schema Definition Language (SDL): types, interfaces, enums,
/// inputs e root types (Query, Mutation, Subscription) com os seus campos.
/// A análise é baseada em regex sobre o texto SDL para não exigir uma dependência de parse completa.
/// Em caso de specs malformadas, retorna estruturas vazias para não bloquear o diff.
/// </summary>
public static class GraphQlSpecParser
{
    private static readonly Regex TypeDefinitionRegex = new(
        @"^\s*type\s+(\w+)\s*(?:implements[^{]*)?\{([^}]*)\}",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex InterfaceDefinitionRegex = new(
        @"^\s*interface\s+(\w+)\s*\{([^}]*)\}",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex EnumDefinitionRegex = new(
        @"^\s*enum\s+(\w+)\s*\{([^}]*)\}",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex InputDefinitionRegex = new(
        @"^\s*input\s+(\w+)\s*\{([^}]*)\}",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex FieldNameRegex = new(
        @"^\s*(\w+)\s*(?:\([^)]*\))?\s*:",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex EnumValueRegex = new(
        @"^\s*([A-Z_][A-Z0-9_]*)\s*$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly HashSet<string> RootTypeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Query", "Mutation", "Subscription"
    };

    /// <summary>
    /// Extrai mapa de tipos e seus campos a partir de uma especificação GraphQL SDL.
    /// Inclui todos os <c>type</c>, <c>interface</c> e <c>input</c> definidos.
    /// Retorna dicionário typeName → conjunto de nomes de campo.
    /// </summary>
    /// <param name="schemaContent">Conteúdo SDL do schema GraphQL.</param>
    /// <returns>Dicionário typeName → conjunto de campos (case-insensitive).</returns>
    public static Dictionary<string, HashSet<string>> ExtractTypesAndFields(string schemaContent)
    {
        var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(schemaContent))
            return result;

        try
        {
            ExtractDefinitions(schemaContent, TypeDefinitionRegex, result);
            ExtractDefinitions(schemaContent, InterfaceDefinitionRegex, result);
            ExtractDefinitions(schemaContent, InputDefinitionRegex, result);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning(
                "GraphQlSpecParser: Failed to parse GraphQL SDL — {0}", ex.Message);
        }

        return result;
    }

    /// <summary>
    /// Extrai os valores de todos os enums definidos no schema GraphQL.
    /// Retorna dicionário enumName → conjunto de valores do enum.
    /// </summary>
    /// <param name="schemaContent">Conteúdo SDL do schema GraphQL.</param>
    /// <returns>Dicionário enumName → conjunto de valores enum.</returns>
    public static Dictionary<string, HashSet<string>> ExtractEnums(string schemaContent)
    {
        var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(schemaContent))
            return result;

        try
        {
            foreach (Match match in EnumDefinitionRegex.Matches(schemaContent))
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
                "GraphQlSpecParser: Failed to extract enums — {0}", ex.Message);
        }

        return result;
    }

    /// <summary>
    /// Indica se um tipo é um root type (Query, Mutation, Subscription).
    /// Os root types merecem tratamento especial no diff pois impactam diretamente o contrato.
    /// </summary>
    public static bool IsRootType(string typeName) =>
        RootTypeNames.Contains(typeName);

    // ── Privados ─────────────────────────────────────────────────────────────

    private static void ExtractDefinitions(
        string content,
        Regex regex,
        Dictionary<string, HashSet<string>> result)
    {
        foreach (Match match in regex.Matches(content))
        {
            var typeName = match.Groups[1].Value;
            var body = match.Groups[2].Value;

            var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match fieldMatch in FieldNameRegex.Matches(body))
                fields.Add(fieldMatch.Groups[1].Value);

            result[typeName] = fields;
        }
    }
}
