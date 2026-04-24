namespace NexTraceOne.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Contrato de persistência e consulta das mensagens Dead Letter Queue.
/// </summary>
public interface IDeadLetterRepository
{
    /// <summary>Persiste uma mensagem que esgotou todas as tentativas de entrega.</summary>
    Task SaveAsync(DeadLetterMessage message, CancellationToken ct = default);

    /// <summary>Lista mensagens DLQ com suporte a paginação e filtros por tenant/status.</summary>
    Task<DeadLetterPage> ListAsync(
        Guid? tenantId,
        DlqMessageStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>Retorna uma mensagem DLQ pelo seu Id, ou null se não existir.</summary>
    Task<DeadLetterMessage?> FindByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Persiste alterações a uma mensagem DLQ existente (status, reprocessedAt, etc.).</summary>
    Task UpdateAsync(DeadLetterMessage message, CancellationToken ct = default);
}

/// <summary>Resultado paginado de listagem de mensagens DLQ.</summary>
public sealed record DeadLetterPage(
    IReadOnlyList<DeadLetterMessage> Items,
    int Total,
    int Page,
    int PageSize);
