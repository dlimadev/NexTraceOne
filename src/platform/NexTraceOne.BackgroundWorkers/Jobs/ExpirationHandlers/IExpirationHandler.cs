using NexTraceOne.Identity.Infrastructure.Persistence;

namespace NexTraceOne.BackgroundWorkers.Jobs.ExpirationHandlers;

/// <summary>
/// Contrato para handlers especializados de expiração do módulo Identity.
/// Cada implementação processa um único tipo de entidade expirável,
/// garantindo separação de responsabilidades e facilidade de teste.
/// O IdentityExpirationJob orquestra a execução sequencial de todos os handlers.
/// </summary>
public interface IExpirationHandler
{
    /// <summary>
    /// Processa um lote de entidades expiradas do tipo específico.
    /// Cada handler é responsável por: consultar entidades pendentes,
    /// aplicar a expiração via método de domínio e registrar SecurityEvent para auditoria.
    /// </summary>
    /// <param name="dbContext">DbContext do módulo Identity para acesso às entidades.</param>
    /// <param name="now">Momento atual (UTC) para comparação de prazos.</param>
    /// <param name="batchSize">Tamanho máximo do lote para limitar pressão no banco.</param>
    /// <param name="cancellationToken">Token de cancelamento para interrupção cooperativa.</param>
    /// <returns>Quantidade de entidades processadas no lote.</returns>
    Task<int> HandleAsync(
        IdentityDbContext dbContext,
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken);
}
