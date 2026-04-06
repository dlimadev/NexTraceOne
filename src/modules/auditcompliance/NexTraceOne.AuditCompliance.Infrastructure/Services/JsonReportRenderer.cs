using System.Text;
using System.Text.Json;

using NexTraceOne.AuditCompliance.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Application.Abstractions;

namespace NexTraceOne.AuditCompliance.Infrastructure.Services;

/// <summary>
/// Implementação de IReportRenderer que serializa o relatório como JSON formatado.
///
/// Usado como formato padrão e fallback quando o formato solicitado não é PDF/XLSX.
///
/// Persona: Auditor, Executive.
/// </summary>
public sealed class JsonReportRenderer(IDateTimeProvider dateTimeProvider) : IReportRenderer
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
        // JSON é o formato padrão para auditoria.
        var json = JsonSerializer.Serialize(report, SerializerOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        var now = dateTimeProvider.UtcNow;

        return Task.FromResult(new RenderedReport(
            bytes,
            "application/json",
            $"audit-report-{now:yyyyMMdd-HHmmss}.json"));
    }
}
