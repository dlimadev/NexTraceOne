namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Estado do ciclo de vida de uma Notebook.
/// Wave V3.4 — AI-assisted Dashboard Creation &amp; Notebook Mode.
/// </summary>
public enum NotebookStatus
{
    /// <summary>Notebook em elaboração (não partilhada).</summary>
    Draft = 0,

    /// <summary>Notebook publicada (visível conforme SharingPolicy).</summary>
    Published = 1,

    /// <summary>Notebook arquivada (somente leitura).</summary>
    Archived = 2,
}
