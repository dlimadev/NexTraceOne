using System.Text;
using System.Text.Json;

using NexTraceOne.AuditCompliance.Application.Abstractions;

namespace NexTraceOne.AuditCompliance.Infrastructure.Services;

/// <summary>
/// Implementação padrão de IReportRenderer que serializa o relatório como JSON formatado.
///
/// Estratégia multi-formato:
///   - JSON → serialização JSON com indentação (provider padrão, sem dependências externas)
///   - PDF  → stub que devolve JSON até QuestPDF ser integrado (IReportRenderer abstrai a transição)
///   - XLSX → stub que devolve JSON até ClosedXML ser integrado (IReportRenderer abstrai a transição)
///
/// A interface IReportRenderer garante que quando os adapters de PDF/XLSX forem
/// implementados, basta registar um novo IReportRenderer no DI sem alterar handlers.
///
/// Persona: Auditor, Executive.
/// </summary>
public sealed class JsonReportRenderer : IReportRenderer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <inheritdoc />
    public Task<RenderedReport> RenderAsync(
        object report,
        string format,
        CancellationToken cancellationToken = default)
    {
        // Todos os formatos recebem JSON estruturado enquanto adapters PDF/XLSX não existem.
        // O campo Format na resposta identifica o formato solicitado para auditores.
        var json = JsonSerializer.Serialize(report, SerializerOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        var (contentType, fileName) = format.ToUpperInvariant() switch
        {
            "PDF"  => ("application/json", "audit-report.json"),   // placeholder até QuestPDF
            "XLSX" => ("application/json", "audit-report.json"),   // placeholder até ClosedXML
            _      => ("application/json", "audit-report.json")
        };

        return Task.FromResult(new RenderedReport(bytes, contentType, fileName));
    }
}
