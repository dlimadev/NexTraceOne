namespace NexTraceOne.Configuration.Application.Abstractions;

/// <summary>
/// Serviço de seed de definições de feature flags da plataforma.
/// A execução é idempotente: apenas insere definições que ainda não existem.
/// Deve ser executado em todos os ambientes (Development, Staging, Production).
/// </summary>
public interface IFeatureFlagDefinitionSeeder
{
    /// <summary>
    /// Insere as definições de feature flags padrão da plataforma se ainda não existirem.
    /// Retorna um <see cref="SeedingResult"/> com o número de definições inseridas e ignoradas.
    /// </summary>
    Task<SeedingResult> SeedAsync(CancellationToken cancellationToken = default);
}
