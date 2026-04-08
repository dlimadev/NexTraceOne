using Microsoft.EntityFrameworkCore;

using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Persistence.Repositories;

/// <summary>
/// Repositório EF Core para gráficos customizados (CustomChart).
/// Isolamento total: acessa apenas RuntimeIntelligenceDbContext — sem acesso cross-module.
/// </summary>
internal sealed class CustomChartRepository(RuntimeIntelligenceDbContext context)
    : RepositoryBase<CustomChart, CustomChartId>(context), ICustomChartRepository
{
    /// <summary>Busca um gráfico customizado pelo seu identificador, filtrando por tenant.</summary>
    public async Task<CustomChart?> GetByIdAsync(CustomChartId id, string tenantId, CancellationToken cancellationToken)
        => await context.CustomCharts
            .SingleOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId, cancellationToken);

    /// <summary>Lista gráficos customizados de um utilizador no tenant, ordenados por data de criação descendente.</summary>
    public async Task<IReadOnlyList<CustomChart>> ListByUserAsync(string userId, string tenantId, CancellationToken cancellationToken)
        => await context.CustomCharts
            .Where(c => c.UserId == userId && c.TenantId == tenantId)
            .OrderByDescending(c => c.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    /// <summary>Adiciona um novo gráfico customizado.</summary>
    public async Task AddAsync(CustomChart chart, CancellationToken cancellationToken)
    {
        await context.CustomCharts.AddAsync(chart, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Atualiza um gráfico customizado existente.</summary>
    public async Task UpdateAsync(CustomChart chart, CancellationToken cancellationToken)
    {
        context.CustomCharts.Update(chart);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Remove logicamente um gráfico customizado pelo seu identificador.</summary>
    public async Task DeleteAsync(CustomChartId id, CancellationToken cancellationToken)
    {
        var chart = await context.CustomCharts.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (chart is not null)
        {
            chart.SoftDelete();
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
