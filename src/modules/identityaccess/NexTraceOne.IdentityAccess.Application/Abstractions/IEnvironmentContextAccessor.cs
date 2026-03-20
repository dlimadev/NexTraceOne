using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Abstração para acesso ao contexto de ambiente ativo na requisição atual.
/// Complementa ICurrentTenant para formar o contexto mínimo obrigatório:
/// TenantId + EnvironmentId.
///
/// O ambiente ativo é resolvido pelo middleware de contexto a partir do JWT,
/// header ou parâmetro de rota, e deve ser validado contra os ambientes
/// do tenant ativo.
///
/// Princípio: nenhuma operação com escopo operacional (observabilidade, incidentes,
/// mudanças, IA) deve ser executada sem contexto de ambiente explícito.
/// </summary>
public interface IEnvironmentContextAccessor
{
    /// <summary>Identificador do ambiente ativo.</summary>
    EnvironmentId EnvironmentId { get; }

    /// <summary>Perfil operacional do ambiente ativo.</summary>
    EnvironmentProfile Profile { get; }

    /// <summary>Indica se o ambiente ativo tem comportamento similar à produção.</summary>
    bool IsProductionLike { get; }

    /// <summary>Indica se o contexto de ambiente está resolvido e disponível.</summary>
    bool IsResolved { get; }
}
