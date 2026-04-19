using Microsoft.EntityFrameworkCore;
using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>Implementação EF Core do repositório de agendas de ambientes não produtivos.</summary>
internal sealed class NonProdScheduleRepository(GovernanceDbContext context) : INonProdScheduleRepository
{
    public async Task<IReadOnlyList<NonProdSchedule>> ListAllAsync(CancellationToken ct)
        => await context.NonProdSchedules.AsNoTracking().OrderBy(s => s.EnvironmentName).ToListAsync(ct);

    public async Task<NonProdSchedule?> GetByEnvironmentIdAsync(string environmentId, CancellationToken ct)
        => await context.NonProdSchedules.SingleOrDefaultAsync(s => s.EnvironmentId == environmentId, ct);

    public async Task AddAsync(NonProdSchedule schedule, CancellationToken ct)
        => await context.NonProdSchedules.AddAsync(schedule, ct);

    public void Update(NonProdSchedule schedule)
        => context.NonProdSchedules.Update(schedule);
}
