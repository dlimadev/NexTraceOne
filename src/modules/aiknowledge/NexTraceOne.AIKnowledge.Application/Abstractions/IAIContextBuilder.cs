using NexTraceOne.AIKnowledge.Domain.Orchestration.Context;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.AIKnowledge.Application.Abstractions;

/// <summary>
/// Abstração para construção do contexto de execução de IA (AiExecutionContext).
/// Garante que toda operação de IA receba um contexto completo, validado e autorizado.
///
/// O builder centraliza a lógica de:
/// - Resolução do tenant e ambiente ativos
/// - Determinação dos escopos de dados permitidos para o usuário e persona
/// - Vinculação ao módulo e release quando aplicável
/// - Definição da janela de tempo adequada ao contexto
///
/// Princípio: nenhuma operação de IA deve ser iniciada sem este contexto.
/// O backend controla o escopo — o frontend não pode expandir permissões de IA.
/// </summary>
public interface IAIContextBuilder
{
    /// <summary>
    /// Constrói o contexto de execução de IA para a requisição atual.
    /// Resolve automaticamente o tenant e ambiente do contexto de segurança corrente.
    /// </summary>
    /// <param name="moduleContext">Módulo ou funcionalidade que está acionando a IA.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task<AiExecutionContext> BuildAsync(
        string moduleContext,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Constrói o contexto de execução de IA para um tenant e ambiente específicos.
    /// Usado em cenários onde o contexto não vem da requisição HTTP (ex.: background jobs).
    /// </summary>
    /// <param name="tenantId">Identificador do tenant.</param>
    /// <param name="environmentId">Identificador do ambiente.</param>
    /// <param name="moduleContext">Módulo ou funcionalidade que está acionando a IA.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task<AiExecutionContext> BuildForAsync(
        TenantId tenantId,
        EnvironmentId environmentId,
        string moduleContext,
        CancellationToken cancellationToken = default);
}
