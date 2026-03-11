namespace NexTraceOne.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Abstração da Unidade de Trabalho para coordenar o commit de mudanças.
/// Implementada pelos DbContexts de cada módulo.
/// REGRA: Não chamar CommitAsync() manualmente — o base handler cuida disso.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Persiste todas as mudanças pendentes. Dispara interceptors.</summary>
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}
