namespace NexTraceOne.Governance.Application.Abstractions;

/// <summary>
/// Abstração para leitura de registos de auditoria de chamadas HTTP externas.
/// A implementação concreta consulta o IObservabilityProvider (Elastic/ClickHouse) quando configurado.
/// Fallback gracioso para lista vazia quando a stack de observabilidade não está disponível.
/// </summary>
public interface IHttpAuditReader
{
    /// <summary>
    /// Consulta registos de auditoria HTTP com filtros opcionais.
    /// Retorna página paginada de entradas de auditoria.
    /// Quando o backend não está disponível, retorna página vazia sem lançar exceção.
    /// </summary>
    Task<HttpAuditPage> QueryAsync(HttpAuditFilter filter, CancellationToken cancellationToken = default);
}

/// <summary>Filtros para consulta de auditoria HTTP externa.</summary>
/// <param name="Destination">Filtrar por destino (hostname ou URL parcial).</param>
/// <param name="Context">Filtrar por contexto do serviço de origem.</param>
/// <param name="From">Início do intervalo temporal.</param>
/// <param name="To">Fim do intervalo temporal.</param>
/// <param name="Page">Número de página (1-based).</param>
/// <param name="PageSize">Tamanho da página.</param>
public sealed record HttpAuditFilter(
    string? Destination,
    string? Context,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int Page,
    int PageSize);

/// <summary>Página de resultados de auditoria HTTP.</summary>
/// <param name="Entries">Entradas de auditoria na página atual.</param>
/// <param name="Total">Total de entradas correspondentes ao filtro.</param>
/// <param name="IsLiveData">Indica se os dados provêm de fonte real (vs. resultado de fallback vazio).</param>
public sealed record HttpAuditPage(
    IReadOnlyList<HttpAuditEntry> Entries,
    int Total,
    bool IsLiveData = false);

/// <summary>Entrada de auditoria de chamada HTTP externa.</summary>
/// <param name="Id">Identificador único (trace ID ou span ID).</param>
/// <param name="Destination">Destino da chamada (hostname + path).</param>
/// <param name="Method">Método HTTP (GET, POST, etc.).</param>
/// <param name="StatusCode">Código de resposta HTTP.</param>
/// <param name="DurationMs">Duração em milissegundos.</param>
/// <param name="Context">Serviço ou contexto de origem da chamada.</param>
/// <param name="OccurredAt">Momento da chamada.</param>
public sealed record HttpAuditEntry(
    string Id,
    string Destination,
    string Method,
    int StatusCode,
    long DurationMs,
    string Context,
    DateTimeOffset OccurredAt);

