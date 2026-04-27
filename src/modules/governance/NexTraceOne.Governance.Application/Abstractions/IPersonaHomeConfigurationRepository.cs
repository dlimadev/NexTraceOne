using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Abstractions;

/// <summary>Repositório para PersonaHomeConfiguration (V3.10 — Persona-first Experience).</summary>
public interface IPersonaHomeConfigurationRepository
{
    Task<PersonaHomeConfiguration?> GetByUserPersonaAsync(string userId, string persona, string tenantId, CancellationToken ct = default);
    Task AddAsync(PersonaHomeConfiguration config, CancellationToken ct = default);
    Task UpdateAsync(PersonaHomeConfiguration config, CancellationToken ct = default);
}
