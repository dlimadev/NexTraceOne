namespace NexTraceOne.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Abstração para acesso ao ambiente ativo na requisição atual.
/// Segue o mesmo padrão de ICurrentTenant e ICurrentUser.
///
/// Implementado pelo IdentityAccess.Infrastructure como adapter sobre EnvironmentContextAccessor.
/// Disponível em BuildingBlocks.Application para uso pelos módulos operacionais
/// sem depender diretamente do módulo IdentityAccess.
///
/// O ambiente pode não estar resolvido em requisições globais que não passam
/// o header X-Environment-Id. Módulos operacionais devem verificar IsResolved
/// antes de usar o EnvironmentId.
/// </summary>
public interface ICurrentEnvironment
{
    /// <summary>Identificador do ambiente ativo. Guid.Empty se não resolvido.</summary>
    Guid EnvironmentId { get; }

    /// <summary>Indica se o ambiente foi resolvido e validado para esta requisição.</summary>
    bool IsResolved { get; }

    /// <summary>Indica se o ambiente ativo tem comportamento similar à produção.</summary>
    bool IsProductionLike { get; }
}
