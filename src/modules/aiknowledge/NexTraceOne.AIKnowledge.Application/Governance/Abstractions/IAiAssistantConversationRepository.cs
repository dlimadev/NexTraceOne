using NexTraceOne.AiGovernance.Domain.Entities;

namespace NexTraceOne.AiGovernance.Application.Abstractions;

/// <summary>
/// Repositório de conversas do assistente de IA.
/// Suporta CRUD de conversas com filtros por utilizador e estado.
/// </summary>
public interface IAiAssistantConversationRepository
{
    /// <summary>Lista conversas com filtros opcionais de utilizador, estado ativo e limite.</summary>
    Task<IReadOnlyList<AiAssistantConversation>> ListAsync(
        string? userId,
        bool? isActive,
        int pageSize,
        CancellationToken ct);

    /// <summary>Obtém uma conversa pelo identificador fortemente tipado.</summary>
    Task<AiAssistantConversation?> GetByIdAsync(AiAssistantConversationId id, CancellationToken ct);

    /// <summary>Adiciona uma nova conversa para persistência.</summary>
    Task AddAsync(AiAssistantConversation conversation, CancellationToken ct);

    /// <summary>Atualiza uma conversa existente.</summary>
    Task UpdateAsync(AiAssistantConversation conversation, CancellationToken ct);

    /// <summary>Conta o total de conversas do utilizador.</summary>
    Task<int> CountAsync(string? userId, bool? isActive, CancellationToken ct);
}
