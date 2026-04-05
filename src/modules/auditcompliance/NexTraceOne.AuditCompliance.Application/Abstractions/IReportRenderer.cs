namespace NexTraceOne.AuditCompliance.Application.Abstractions;

/// <summary>
/// Porta de rendering de relatórios de auditoria para diferentes formatos (JSON, PDF, XLSX).
///
/// Arquitetura:
///   - Esta interface fica na Application Layer e permite que o handler produza
///     o relatório independente do formato final.
///   - A implementação concreta fica na Infrastructure Layer.
///   - Em ambiente sem biblioteca de rendering externa (QuestPDF, ClosedXML),
///     o provider padrão devolve JSON serializado.
///   - Quando QuestPDF/ClosedXML estiverem disponíveis, novos adapters podem ser
///     registados no DI sem alterar o handler.
///
/// Persona: Auditor, Executive.
/// Valor: desacopla lógica de negócio (recolha + assinatura) de lógica de apresentação.
/// </summary>
public interface IReportRenderer
{
    /// <summary>
    /// Renderiza o relatório no formato solicitado.
    /// </summary>
    /// <param name="report">Dados estruturados do relatório (já com assinatura SHA-256).</param>
    /// <param name="format">Formato de saída (JSON, PDF, XLSX).</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Bytes do relatório renderizado e o content-type correspondente.</returns>
    Task<RenderedReport> RenderAsync(
        object report,
        string format,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Resultado de um relatório renderizado: bytes + content-type.
/// </summary>
/// <param name="Content">Bytes do conteúdo renderizado.</param>
/// <param name="ContentType">MIME type para o cabeçalho HTTP Content-Type.</param>
/// <param name="FileName">Nome de ficheiro sugerido para download.</param>
public sealed record RenderedReport(
    byte[] Content,
    string ContentType,
    string FileName);
