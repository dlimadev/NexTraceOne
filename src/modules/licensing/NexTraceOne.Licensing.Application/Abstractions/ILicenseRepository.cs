using NexTraceOne.Licensing.Domain.Entities;

namespace NexTraceOne.Licensing.Application.Abstractions;

/// <summary>
/// Repositório de licenças do módulo Licensing.
/// </summary>
public interface ILicenseRepository
{
    /// <summary>Obtém uma licença pelo identificador.</summary>
    Task<License?> GetByIdAsync(LicenseId id, CancellationToken cancellationToken);

    /// <summary>Obtém uma licença pela chave pública.</summary>
    Task<License?> GetByLicenseKeyAsync(string licenseKey, CancellationToken cancellationToken);

    /// <summary>Adiciona uma nova licença para persistência.</summary>
    void Add(License license);
}
