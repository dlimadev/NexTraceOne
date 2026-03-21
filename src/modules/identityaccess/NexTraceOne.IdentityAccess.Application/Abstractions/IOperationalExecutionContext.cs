using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.Enums;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Contrato do contexto operacional de execução combinado.
/// Agrega TenantId + EnvironmentId + User + Profile em um único ponto de acesso.
///
/// Este é o contexto que todos os handlers operacionais (observabilidade, incidentes,
/// mudanças, IA) devem usar para operar dentro do escopo correto.
///
/// Resolução: populado pelo EnvironmentResolutionMiddleware após autenticação
/// e resolução do tenant (TenantResolutionMiddleware).
/// </summary>
public interface IOperationalExecutionContext
{
    /// <summary>Id do usuário autenticado.</summary>
    string UserId { get; }

    /// <summary>Nome do usuário autenticado.</summary>
    string UserName { get; }

    /// <summary>Email do usuário autenticado.</summary>
    string UserEmail { get; }

    /// <summary>Identificador do tenant ativo.</summary>
    TenantId TenantId { get; }

    /// <summary>Identificador do ambiente ativo.</summary>
    EnvironmentId EnvironmentId { get; }

    /// <summary>Perfil operacional do ambiente ativo.</summary>
    EnvironmentProfile EnvironmentProfile { get; }

    /// <summary>Indica se o ambiente ativo tem comportamento similar à produção.</summary>
    bool IsProductionLikeEnvironment { get; }

    /// <summary>Contexto combinado de tenant e ambiente, para uso em serviços de domínio.</summary>
    TenantEnvironmentContext TenantEnvironmentContext { get; }

    /// <summary>Indica se o contexto operacional completo foi resolvido com sucesso.</summary>
    bool IsFullyResolved { get; }

    /// <summary>
    /// Indica se o contexto tem pelo menos o contexto de tenant resolvido
    /// (sem necessariamente ter o ambiente).
    /// </summary>
    bool HasTenantContext { get; }
}
