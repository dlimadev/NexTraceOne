using Microsoft.EntityFrameworkCore;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório de configuração SAML SSO.</summary>
internal sealed class SamlSsoConfigurationRepository(GovernanceDbContext context) : ISamlSsoConfigurationRepository
{
    public async Task<SamlSsoConfiguration?> GetActiveAsync(Guid? tenantId, CancellationToken ct)
        => await context.SamlSsoConfigurations
            .SingleOrDefaultAsync(c => c.TenantId == tenantId, ct);

    public async Task AddAsync(SamlSsoConfiguration config, CancellationToken ct)
        => await context.SamlSsoConfigurations.AddAsync(config, ct);

    public void Update(SamlSsoConfiguration config)
        => context.SamlSsoConfigurations.Update(config);
}
