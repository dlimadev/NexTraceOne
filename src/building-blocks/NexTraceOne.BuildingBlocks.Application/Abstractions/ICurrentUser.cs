namespace NexTraceOne.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Abstração para acesso ao usuário autenticado no contexto da requisição.
/// Implementada pelo projeto Security como HttpContextCurrentUser.
/// </summary>
public interface ICurrentUser
{
    /// <summary>Id único do usuário autenticado.</summary>
    string Id { get; }
    /// <summary>Nome de exibição do usuário.</summary>
    string Name { get; }
    /// <summary>Email do usuário autenticado.</summary>
    string Email { get; }
    /// <summary>
    /// Persona explícita do utilizador, lida do claim JWT <c>x-nxt-persona</c>.
    /// Quando presente, tem prioridade sobre a inferência por permissões.
    /// Valores esperados: Engineer, TechLead, Architect, Product, Executive, PlatformAdmin, Auditor.
    /// </summary>
    string? Persona { get; }
    /// <summary>Indica se há um usuário autenticado no contexto atual.</summary>
    bool IsAuthenticated { get; }
    /// <summary>Verifica se o usuário possui a permissão especificada.</summary>
    bool HasPermission(string permission);
}
