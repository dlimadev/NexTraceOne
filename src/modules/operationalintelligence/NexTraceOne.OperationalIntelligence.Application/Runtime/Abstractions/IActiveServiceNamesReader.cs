namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

/// <summary>
/// Abstracção para obtenção da lista de nomes de serviços activos do tenant.
///
/// Permite que o módulo OperationalIntelligence consulte a lista de serviços registados
/// no Catalog sem criar acoplamento directo entre módulos.
///
/// O <see cref="NullActiveServiceNamesReader"/> é o provider por defeito (honest-null pattern):
/// retorna lista vazia, indicando que o bridge para o Catalog ainda não está configurado.
/// Quando não há serviços conhecidos, os relatórios que precisam de "todos os serviços"
/// derivam a lista apenas dos dados operacionais disponíveis (experimentos, snapshots, etc.).
/// </summary>
public interface IActiveServiceNamesReader
{
    /// <summary>
    /// Lista os nomes de serviços activos no tenant.
    /// Retorna lista vazia se o bridge não estiver configurado.
    /// </summary>
    Task<IReadOnlyList<string>> ListActiveServiceNamesAsync(
        string tenantId,
        CancellationToken cancellationToken);
}
