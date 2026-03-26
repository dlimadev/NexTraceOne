namespace NexTraceOne.Configuration.Application.Abstractions;

/// <summary>
/// Resultado de uma execução do seeder de definições de configuração.
/// Permite rastrear quantas definições foram inseridas versus já existiam.
/// </summary>
public sealed record SeedingResult(int Added, int Skipped)
{
    /// <summary>Total de definições processadas (inseridas + ignoradas).</summary>
    public int Total => Added + Skipped;

    /// <summary>Indica se a execução foi o primeiro seed (nenhuma definição existia).</summary>
    public bool IsFirstRun => Skipped == 0 && Added > 0;

    /// <summary>Indica se todas as definições já existiam (sem alterações).</summary>
    public bool IsNoOp => Added == 0;
}

/// <summary>
/// Serviço de seed de definições de configuração da plataforma.
/// A execução é idempotente: apenas insere definições que ainda não existem.
/// Deve ser executado em todos os ambientes (Development, Staging, Production).
/// </summary>
public interface IConfigurationDefinitionSeeder
{
    /// <summary>
    /// Insere as definições de configuração padrão da plataforma se ainda não existirem.
    /// Retorna um <see cref="SeedingResult"/> com o número de definições inseridas e ignoradas.
    /// </summary>
    Task<SeedingResult> SeedAsync(CancellationToken cancellationToken = default);
}
