namespace NexTraceOne.Catalog.Domain.Contracts.Enums;

/// <summary>
/// Formato de saída de uma entrada de changelog de contrato.
/// </summary>
public enum ChangelogFormat
{
    /// <summary>Formato Markdown.</summary>
    Markdown = 0,

    /// <summary>Formato JSON estruturado.</summary>
    Json = 1,

    /// <summary>Ambos os formatos (Markdown e JSON).</summary>
    Both = 2
}
