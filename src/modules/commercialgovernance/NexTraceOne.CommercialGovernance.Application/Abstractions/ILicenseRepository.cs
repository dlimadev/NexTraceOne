using NexTraceOne.Licensing.Domain.Entities;

namespace NexTraceOne.Licensing.Application.Abstractions;

/// <summary>
/// Repositório de licenças do módulo Licensing.
/// Inclui operações de leitura e escrita para o aggregate License.
/// </summary>
public interface ILicenseRepository
{
    /// <summary>Obtém uma licença pelo identificador.</summary>
    Task<License?> GetByIdAsync(LicenseId id, CancellationToken cancellationToken);

    /// <summary>Obtém uma licença pela chave pública.</summary>
    Task<License?> GetByLicenseKeyAsync(string licenseKey, CancellationToken cancellationToken);

    /// <summary>Lista todas as licenças com paginação. Usada por vendor ops.</summary>
    Task<(IReadOnlyList<License> Items, int TotalCount)> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Adiciona uma nova licença para persistência.</summary>
    void Add(License license);
}
