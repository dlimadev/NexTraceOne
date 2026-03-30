namespace NexTraceOne.OperationalIntelligence.Contracts.Runtime.ServiceInterfaces;

// IMPLEMENTATION STATUS: Implemented — RuntimeIntelligenceModule (Infrastructure).

/// <summary>
/// Interface pública do módulo RuntimeIntelligence.
/// Outros módulos que precisarem de dados de runtime devem usar este contrato —
/// nunca acessar o DbContext ou repositórios diretamente.
/// Garante isolamento de base de dados entre serviços.
/// </summary>
public interface IRuntimeIntelligenceModule
{
    /// <summary>
    /// Obtém o status de saúde atual de um serviço e ambiente.
    /// Retorna null se nenhum snapshot de runtime foi capturado.
    /// </summary>
    Task<string?> GetCurrentHealthStatusAsync(string serviceName, string environment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém o score de observabilidade (0.0 a 1.0) para um serviço.
    /// Retorna null se o perfil de observabilidade ainda não foi avaliado.
    /// </summary>
    Task<decimal?> GetObservabilityScoreAsync(string serviceName, string environment, CancellationToken cancellationToken = default);
}
