namespace NexTraceOne.Catalog.Domain.LegacyAssets.Services;

/// <summary>
/// Resultado completo do parsing de um copybook COBOL.
/// Contém os campos estruturados e metadados do layout.
/// </summary>
public sealed record CopybookParsedLayout(
    string CopybookName,
    IReadOnlyList<CopybookParsedField> Fields,
    int TotalLength,
    string RecordFormat);
