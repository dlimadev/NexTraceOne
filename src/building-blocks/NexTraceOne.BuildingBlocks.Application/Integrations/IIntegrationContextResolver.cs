namespace NexTraceOne.BuildingBlocks.Application.Integrations;

/// <summary>
/// Resolve o binding de integração correto para o contexto tenant + ambiente atual.
///
/// PAPEL CENTRAL: Garante que cada módulo operacional use o binding certo
/// para o contexto certo — sem hardcoding de URLs, brokers ou configurações
/// que variam entre tenants e ambientes.
///
/// REGRA DE SEGURANÇA:
/// O resolver NUNCA retorna um binding de produção para uma operação marcada como não-produtiva,
/// a menos que o tenant/ambiente tenha política explícita de sandbox controlado.
///
/// IMPLEMENTAÇÃO:
/// A implementação concreta lê IntegrationBindingDescriptor da base de dados
/// (tabela de bindings por tenant/ambiente) e aplica as políticas de segurança.
/// A implementação padrão (stub) é registrada em BuildingBlocks para desenvolvimento.
/// Módulos que precisam de bindings reais registram uma implementação concreta.
/// </summary>
public interface IIntegrationContextResolver
{
    /// <summary>
    /// Resolve o binding ativo para um tipo de integração no contexto atual.
    /// Retorna null se não houver binding configurado para o contexto.
    /// </summary>
    Task<IntegrationBindingDescriptor?> ResolveAsync(
        string integrationType,
        Guid tenantId,
        Guid? environmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista todos os bindings ativos para um tenant e ambiente.
    /// Usados para inventário e análise de integrações ativas.
    /// </summary>
    Task<IReadOnlyList<IntegrationBindingDescriptor>> ListActiveBindingsAsync(
        Guid tenantId,
        Guid? environmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se existe binding ativo de um tipo para o contexto fornecido.
    /// </summary>
    Task<bool> HasActiveBindingAsync(
        string integrationType,
        Guid tenantId,
        Guid? environmentId,
        CancellationToken cancellationToken = default);
}
