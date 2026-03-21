namespace NexTraceOne.BuildingBlocks.Application.Integrations;

/// <summary>
/// Implementação nula de IIntegrationContextResolver.
/// Retorna null para todas as consultas — sem bindings configurados.
///
/// Registrada como padrão em BuildingBlocks para permitir que o sistema
/// inicie sem bindings configurados (desenvolvimento, testes unitários).
///
/// Em produção e ambientes reais, uma implementação concreta que lê
/// do banco de dados deve ser registrada pelo módulo de integrações.
/// </summary>
internal sealed class NullIntegrationContextResolver : IIntegrationContextResolver
{
    /// <inheritdoc />
    public Task<IntegrationBindingDescriptor?> ResolveAsync(
        string integrationType,
        Guid tenantId,
        Guid? environmentId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IntegrationBindingDescriptor?>(null);

    /// <inheritdoc />
    public Task<IReadOnlyList<IntegrationBindingDescriptor>> ListActiveBindingsAsync(
        Guid tenantId,
        Guid? environmentId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<IntegrationBindingDescriptor>>([]);

    /// <inheritdoc />
    public Task<bool> HasActiveBindingAsync(
        string integrationType,
        Guid tenantId,
        Guid? environmentId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(false);
}
