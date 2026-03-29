using System.Globalization;

using NexTraceOne.BuildingBlocks.Core.Enums;
using NexTraceOne.Catalog.Domain.Contracts.ValueObjects;

namespace NexTraceOne.Catalog.Domain.LegacyAssets.Services;

/// <summary>
/// Serviço de domínio para diff semântico entre duas versões de copybook COBOL.
/// Detecta campos adicionados, removidos, alterados e classifica como breaking ou non-breaking.
/// Segue o mesmo padrão arquitetural do OpenApiDiffCalculator.
/// </summary>
public static class CopybookDiffCalculator
{
    /// <summary>
    /// Resultado estruturado do diff entre dois layouts de copybook.
    /// </summary>
    public sealed record CopybookDiffResult(
        IReadOnlyList<ChangeEntry> BreakingChanges,
        IReadOnlyList<ChangeEntry> AdditiveChanges,
        IReadOnlyList<ChangeEntry> NonBreakingChanges,
        ChangeLevel ChangeLevel);

    /// <summary>
    /// Computa o diff semântico entre dois layouts de copybook COBOL.
    /// Compara campos por nome, detectando adições, remoções, alterações de tipo/comprimento,
    /// e classifica o nível geral da mudança.
    /// </summary>
    /// <param name="baseLayout">Layout do copybook base (versão anterior).</param>
    /// <param name="targetLayout">Layout do copybook alvo (versão mais recente).</param>
    /// <returns>Resultado com listas de mudanças categorizadas e o nível de mudança calculado.</returns>
    public static CopybookDiffResult ComputeDiff(
        CopybookParsedLayout baseLayout,
        CopybookParsedLayout targetLayout)
    {
        ArgumentNullException.ThrowIfNull(baseLayout);
        ArgumentNullException.ThrowIfNull(targetLayout);

        var breaking = new List<ChangeEntry>();
        var additive = new List<ChangeEntry>();
        var nonBreaking = new List<ChangeEntry>();

        // Build lookup maps by field name (excluding 88-level conditions)
        var baseFields = BuildFieldMap(baseLayout.Fields);
        var targetFields = BuildFieldMap(targetLayout.Fields);

        // Detect removed fields — always breaking
        foreach (var (name, baseField) in baseFields)
        {
            if (!targetFields.ContainsKey(name))
            {
                breaking.Add(new ChangeEntry(
                    "FieldRemoved",
                    name,
                    null,
                    $"Field '{name}' (offset {baseField.Offset}, length {baseField.Length}) was removed.",
                    true));
            }
        }

        // Detect added fields — check if at end (additive) or in middle (breaking)
        var baseMaxOffset = baseFields.Count > 0
            ? baseFields.Values.Where(f => !f.IsRedefines).Max(f => f.Offset + f.Length)
            : 0;

        foreach (var (name, targetField) in targetFields)
        {
            if (baseFields.ContainsKey(name))
                continue;

            if (targetField.IsRedefines)
            {
                // REDEFINES added: non-breaking (overlay, doesn't shift offsets)
                nonBreaking.Add(new ChangeEntry(
                    "RedefinesAdded",
                    name,
                    null,
                    $"REDEFINES field '{name}' was added (overlays '{targetField.RedefinesTarget}').",
                    false));
            }
            else if (targetField.Offset >= baseMaxOffset)
            {
                // Added at end — additive, doesn't shift offsets
                additive.Add(new ChangeEntry(
                    "FieldAdded",
                    name,
                    null,
                    $"Field '{name}' was added at offset {targetField.Offset} (end of record).",
                    false));
            }
            else
            {
                // Added in middle — breaking because it shifts offsets
                breaking.Add(new ChangeEntry(
                    "FieldInserted",
                    name,
                    null,
                    $"Field '{name}' was inserted at offset {targetField.Offset} (shifts subsequent offsets).",
                    true));
            }
        }

        // Compare common fields — detect type, length, PIC, OCCURS changes
        foreach (var (name, baseField) in baseFields)
        {
            if (!targetFields.TryGetValue(name, out var targetField))
                continue;

            CompareField(name, baseField, targetField, breaking, nonBreaking);
        }

        // Compare 88-level condition values (non-breaking)
        Compare88Levels(baseLayout.Fields, targetLayout.Fields, nonBreaking);

        ChangeLevel changeLevel;
        if (breaking.Count > 0)
            changeLevel = ChangeLevel.Breaking;
        else if (additive.Count > 0)
            changeLevel = ChangeLevel.Additive;
        else
            changeLevel = ChangeLevel.NonBreaking;

        return new CopybookDiffResult(
            breaking.AsReadOnly(),
            additive.AsReadOnly(),
            nonBreaking.AsReadOnly(),
            changeLevel);
    }

    /// <summary>
    /// Constrói mapa de campos por nome (excluindo 88-level conditions).
    /// </summary>
    private static Dictionary<string, CopybookParsedField> BuildFieldMap(
        IReadOnlyList<CopybookParsedField> fields)
    {
        var map = new Dictionary<string, CopybookParsedField>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in fields)
        {
            if (field.Level == 88)
                continue;
            // Use first occurrence (some names may repeat in REDEFINES scenarios)
            map.TryAdd(field.Name, field);
        }
        return map;
    }

    /// <summary>
    /// Compara dois campos com o mesmo nome e detecta mudanças de tipo, comprimento, offset e OCCURS.
    /// </summary>
    private static void CompareField(
        string name,
        CopybookParsedField baseField,
        CopybookParsedField targetField,
        List<ChangeEntry> breaking,
        List<ChangeEntry> nonBreaking)
    {
        // Type changed
        if (!string.Equals(baseField.DataType, targetField.DataType, StringComparison.OrdinalIgnoreCase))
        {
            breaking.Add(new ChangeEntry(
                "FieldTypeChanged",
                name,
                null,
                $"Field '{name}' type changed from '{baseField.DataType}' to '{targetField.DataType}'.",
                true));
        }

        // Length changed — only for non-group fields (group lengths derive from children)
        if (baseField.Length != targetField.Length && !baseField.IsGroup && !targetField.IsGroup)
        {
            breaking.Add(new ChangeEntry(
                "FieldLengthChanged",
                name,
                null,
                $"Field '{name}' length changed from {baseField.Length} to {targetField.Length} bytes.",
                true));
        }

        // OCCURS count changed
        if (baseField.OccursCount != targetField.OccursCount)
        {
            breaking.Add(new ChangeEntry(
                "OccursCountChanged",
                name,
                null,
                $"Field '{name}' OCCURS count changed from {baseField.OccursCount?.ToString(CultureInfo.InvariantCulture) ?? "none"} to {targetField.OccursCount?.ToString(CultureInfo.InvariantCulture) ?? "none"}.",
                true));
        }

        // PIC clause changed but type and length same — non-breaking cosmetic change
        if (!string.Equals(baseField.PicClause, targetField.PicClause, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(baseField.DataType, targetField.DataType, StringComparison.OrdinalIgnoreCase) &&
            baseField.Length == targetField.Length)
        {
            nonBreaking.Add(new ChangeEntry(
                "PicClauseChanged",
                name,
                null,
                $"Field '{name}' PIC clause changed from '{baseField.PicClause}' to '{targetField.PicClause}' (compatible).",
                false));
        }

        // Offset changed — only for non-group fields (group offsets aren't independently meaningful)
        if (baseField.Offset != targetField.Offset &&
            !baseField.IsRedefines && !targetField.IsRedefines &&
            !baseField.IsGroup && !targetField.IsGroup)
        {
            breaking.Add(new ChangeEntry(
                "FieldOffsetChanged",
                name,
                null,
                $"Field '{name}' offset changed from {baseField.Offset} to {targetField.Offset}.",
                true));
        }
    }

    /// <summary>
    /// Compara condition values de campos 88-level entre versões.
    /// Alterações em 88-level são consideradas non-breaking.
    /// </summary>
    private static void Compare88Levels(
        IReadOnlyList<CopybookParsedField> baseFields,
        IReadOnlyList<CopybookParsedField> targetFields,
        List<ChangeEntry> nonBreaking)
    {
        var baseConditions = baseFields
            .Where(f => f.Level == 88)
            .ToDictionary(f => f.Name, f => f.ConditionValues, StringComparer.OrdinalIgnoreCase);

        var targetConditions = targetFields
            .Where(f => f.Level == 88)
            .ToDictionary(f => f.Name, f => f.ConditionValues, StringComparer.OrdinalIgnoreCase);

        foreach (var (name, baseValues) in baseConditions)
        {
            if (!targetConditions.TryGetValue(name, out var targetValues))
            {
                nonBreaking.Add(new ChangeEntry(
                    "ConditionRemoved",
                    name,
                    null,
                    $"Condition name '{name}' was removed.",
                    false));
                continue;
            }

            if (!ConditionValuesEqual(baseValues, targetValues))
            {
                nonBreaking.Add(new ChangeEntry(
                    "ConditionValueChanged",
                    name,
                    null,
                    $"Condition '{name}' values changed.",
                    false));
            }
        }

        foreach (var name in targetConditions.Keys.Except(baseConditions.Keys, StringComparer.OrdinalIgnoreCase))
        {
            nonBreaking.Add(new ChangeEntry(
                "ConditionAdded",
                name,
                null,
                $"Condition name '{name}' was added.",
                false));
        }
    }

    private static bool ConditionValuesEqual(
        IReadOnlyList<string>? left,
        IReadOnlyList<string>? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        if (left.Count != right.Count) return false;

        for (var i = 0; i < left.Count; i++)
        {
            if (!string.Equals(left[i], right[i], StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }
}
