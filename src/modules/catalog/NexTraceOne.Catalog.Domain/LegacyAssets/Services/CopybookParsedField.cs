namespace NexTraceOne.Catalog.Domain.LegacyAssets.Services;

/// <summary>
/// Campo individual resultante do parsing de um copybook COBOL.
/// Representa um campo com nível, nome, PIC clause, tipo, offset e comprimento.
/// </summary>
public sealed record CopybookParsedField(
    int Level,
    string Name,
    string? PicClause,
    string DataType,
    int Offset,
    int Length,
    int? DecimalPositions,
    bool IsGroup,
    int? OccursCount,
    bool IsRedefines,
    string? RedefinesTarget,
    IReadOnlyList<string>? ConditionValues);
