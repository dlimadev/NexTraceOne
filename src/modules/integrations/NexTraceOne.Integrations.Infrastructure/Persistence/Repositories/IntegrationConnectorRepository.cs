using Microsoft.EntityFrameworkCore;

using NexTraceOne.Integrations.Application.Abstractions;
using NexTraceOne.Integrations.Domain.Entities;
using NexTraceOne.Integrations.Domain.Enums;

namespace NexTraceOne.Integrations.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação do repositório de IntegrationConnectors usando EF Core.
/// Extraído de Governance.Infrastructure em P2.1 — owner correto: módulo Integrations.
/// </summary>
internal sealed class IntegrationConnectorRepository(IntegrationsDbContext context) : IIntegrationConnectorRepository
{
    public async Task<IReadOnlyList<IntegrationConnector>> ListAsync(
        ConnectorStatus? status,
        ConnectorHealth? health,
        string? connectorType,
        string? search,
        CancellationToken ct)
    {
        var query = context.IntegrationConnectors.AsQueryable();

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        if (health.HasValue)
            query = query.Where(c => c.Health == health.Value);

        if (!string.IsNullOrWhiteSpace(connectorType))
            query = query.Where(c => c.ConnectorType == connectorType);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c =>
                c.Name.Contains(search) ||
                c.Description!.Contains(search) ||
                c.Provider.Contains(search));

        return await query.OrderBy(c => c.Name).ToListAsync(ct);
    }

    public async Task<IntegrationConnector?> GetByIdAsync(IntegrationConnectorId id, CancellationToken ct)
        => await context.IntegrationConnectors.SingleOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IntegrationConnector?> GetByNameAsync(string name, CancellationToken ct)
        => await context.IntegrationConnectors.SingleOrDefaultAsync(c => c.Name == name, ct);

    public async Task AddAsync(IntegrationConnector connector, CancellationToken ct)
        => await context.IntegrationConnectors.AddAsync(connector, ct);

    public Task UpdateAsync(IntegrationConnector connector, CancellationToken ct)
    {
        context.IntegrationConnectors.Update(connector);
        return Task.CompletedTask;
    }

    public async Task<int> CountByStatusAsync(ConnectorStatus status, CancellationToken ct)
        => await context.IntegrationConnectors.CountAsync(c => c.Status == status, ct);

    public async Task<int> CountByHealthAsync(ConnectorHealth health, CancellationToken ct)
        => await context.IntegrationConnectors.CountAsync(c => c.Health == health, ct);
}
