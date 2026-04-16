namespace NexTraceOne.ApiHost.Preflight.Checks;

/// <summary>
/// Contrato para um check individual de preflight.
/// Cada implementação é responsável por uma única verificação de infraestrutura,
/// respeitando o Princípio de Responsabilidade Única (SRP).
/// O resultado pode conter múltiplos <see cref="PreflightCheckResult"/> (ex.: check de múltiplas portas).
/// </summary>
public interface IPreflightCheck
{
    /// <summary>
    /// Executa o check e retorna um ou mais resultados.
    /// Nunca lança excepção — erros internos devem ser capturados e reportados como Warning.
    /// </summary>
    Task<IReadOnlyList<PreflightCheckResult>> RunAsync(CancellationToken ct = default);
}
