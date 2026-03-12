using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Licensing.Application.Abstractions;
using NexTraceOne.Licensing.Domain.Entities;

namespace NexTraceOne.Licensing.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório de vínculos de hardware do módulo Licensing.
/// Implementado sobre EF Core com acesso direto à tabela licensing_hardware_bindings.
/// </summary>
internal sealed class HardwareBindingRepository(LicensingDbContext context)
    : RepositoryBase<HardwareBinding, HardwareBindingId>(context), IHardwareBindingRepository
{
    /// <summary>Obtém o vínculo de hardware associado a uma licença específica.</summary>
    public async Task<HardwareBinding?> GetByLicenseIdAsync(LicenseId licenseId, CancellationToken cancellationToken = default)
        => await DbSet
            .FirstOrDefaultAsync(
                hb => EF.Property<Guid>(hb, "LicenseId") == licenseId.Value,
                cancellationToken);

    /// <summary>Obtém o vínculo de hardware pelo fingerprint do hardware autorizado.</summary>
    public async Task<HardwareBinding?> GetByFingerprintAsync(string fingerprint, CancellationToken cancellationToken = default)
        => await DbSet
            .FirstOrDefaultAsync(hb => hb.Fingerprint == fingerprint, cancellationToken);
}
