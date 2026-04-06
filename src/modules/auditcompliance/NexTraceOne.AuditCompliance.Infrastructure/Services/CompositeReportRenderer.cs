using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AuditCompliance.Infrastructure.Services;

/// <summary>
/// Roteador de formato que delega para o renderer correto (JSON, PDF ou XLSX).
///
/// Estratégia multi-formato:
///   - JSON → JsonReportRenderer (serialização JSON com indentação)
///   - PDF  → PdfReportRenderer (QuestPDF — layout enterprise)
///   - XLSX → XlsxReportRenderer (ClosedXML — tabular com múltiplas sheets)
///
/// Persona: Auditor, Executive.
/// </summary>
public sealed class CompositeReportRenderer(IDateTimeProvider dateTimeProvider) : IReportRenderer
{
    private readonly JsonReportRenderer _jsonRenderer = new(dateTimeProvider);
    private readonly PdfReportRenderer _pdfRenderer = new(dateTimeProvider);
    private readonly XlsxReportRenderer _xlsxRenderer = new(dateTimeProvider);

    /// <inheritdoc />
    public Task<RenderedReport> RenderAsync(
        object report,
        string format,
        CancellationToken cancellationToken = default)
    {
        var renderer = ResolveRenderer(format);
        return renderer.RenderAsync(report, format, cancellationToken);
    }

    private IReportRenderer ResolveRenderer(string format) => format.ToUpperInvariant() switch
    {
        "PDF" => _pdfRenderer,
        "XLSX" => _xlsxRenderer,
        _ => _jsonRenderer
    };
}
