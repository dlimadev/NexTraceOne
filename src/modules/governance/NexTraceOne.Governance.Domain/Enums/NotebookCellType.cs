namespace NexTraceOne.Governance.Domain.Enums;

/// <summary>
/// Tipo de célula de uma Notebook operacional.
/// Wave V3.4 — AI-assisted Dashboard Creation &amp; Notebook Mode.
/// </summary>
public enum NotebookCellType
{
    /// <summary>Célula de narrativa em Markdown.</summary>
    Markdown = 0,

    /// <summary>Célula de query NQL com resultado executável.</summary>
    Query = 1,

    /// <summary>Célula de widget de dashboard embutido.</summary>
    Widget = 2,

    /// <summary>Célula de ação (acionar runbook, abrir incidente, anotar change).</summary>
    Action = 3,

    /// <summary>Célula de prompt AI com resposta gerada.</summary>
    Ai = 4,
}
