using Microsoft.EntityFrameworkCore;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>Repositório EF Core para PersonaHomeConfiguration (V3.10).</summary>
public sealed class PersonaHomeConfigurationRepository(GovernanceDbContext db)
    : IPersonaHomeConfigurationRepository
{
    public async Task<PersonaHomeConfiguration?> GetByUserPersonaAsync(
        string userId, string persona, string tenantId, CancellationToken ct = default)
        => await db.PersonaHomeConfigurations
            .FirstOrDefaultAsync(c => c.UserId == userId
                                   && c.Persona == persona.ToLowerInvariant()
                                   && c.TenantId == tenantId, ct);

    public async Task AddAsync(PersonaHomeConfiguration config, CancellationToken ct = default)
        => await db.PersonaHomeConfigurations.AddAsync(config, ct);

    public Task UpdateAsync(PersonaHomeConfiguration config, CancellationToken ct = default)
    {
        db.PersonaHomeConfigurations.Update(config);
        return Task.CompletedTask;
    }
}
