using NexTraceOne.Licensing.Contracts.DTOs;

namespace NexTraceOne.Licensing.Contracts.ServiceInterfaces;

/// <summary>
/// Interface pública do módulo Licensing.
/// Outros módulos que precisarem de dados deste módulo devem usar
/// este contrato — nunca acessar o DbContext ou repositórios diretamente.
///
/// Decisão de design:
/// - Retorna DTOs do Contracts layer para manter isolamento.
/// - Métodos são read-only (queries) para minimizar acoplamento.
/// - Mutations no módulo de licensing são feitas via MediatR (CQRS).
/// </summary>
public interface ILicensingModule
{
    /// <summary>Obtém o estado atual da licença pela chave pública.</summary>
    Task<LicenseStatusDto?> GetLicenseStatusAsync(string licenseKey, CancellationToken cancellationToken);

    /// <summary>Verifica se uma capability está habilitada para a licença informada.</summary>
    Task<bool> HasCapabilityAsync(string licenseKey, string capabilityCode, CancellationToken cancellationToken);

    /// <summary>Valida se a licença está apta para execução no hardware atual.</summary>
    Task<bool> VerifyLicenseAsync(string licenseKey, CancellationToken cancellationToken);

    /// <summary>Verifica se a licença é do tipo trial.</summary>
    Task<bool> IsTrialAsync(string licenseKey, CancellationToken cancellationToken);

    /// <summary>Obtém o health score da licença (0.0 a 1.0).</summary>
    Task<decimal> GetHealthScoreAsync(string licenseKey, CancellationToken cancellationToken);
}
