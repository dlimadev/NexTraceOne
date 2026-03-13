using NexTraceOne.Licensing.Domain.Entities;

namespace NexTraceOne.Licensing.Application.Abstractions;

/// <summary>
/// Repositório de vínculos de hardware do módulo Licensing.
/// Permite consultas diretas em bindings sem carregar o aggregate License completo.
/// </summary>
public interface IHardwareBindingRepository
{
    /// <summary>Obtém um vínculo de hardware pelo identificador único.</summary>
    Task<HardwareBinding?> GetByIdAsync(HardwareBindingId id, CancellationToken cancellationToken = default);

    /// <summary>Obtém o vínculo de hardware associado a uma licença específica.</summary>
    Task<HardwareBinding?> GetByLicenseIdAsync(LicenseId licenseId, CancellationToken cancellationToken = default);

    /// <summary>Obtém o vínculo de hardware pelo fingerprint do hardware autorizado.</summary>
    Task<HardwareBinding?> GetByFingerprintAsync(string fingerprint, CancellationToken cancellationToken = default);

    /// <summary>Adiciona um novo vínculo de hardware para persistência.</summary>
    void Add(HardwareBinding hardwareBinding);

    /// <summary>Atualiza um vínculo de hardware existente.</summary>
    void Update(HardwareBinding hardwareBinding);
}
