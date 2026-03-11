using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Application.Abstractions;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;

namespace NexTraceOne.Contracts.Infrastructure.Persistence;

/// <summary>
/// DbContext do módulo Contracts.
/// Herda de NexTraceDbContextBase: RLS, auditoria, Outbox, criptografia, soft-delete.
/// REGRA: Outros módulos NUNCA referenciam este DbContext. Comunicação via Integration Events.
/// </summary>
public sealed class ContractsDbContext(
    DbContextOptions<ContractsDbContext> options,
    ICurrentTenant tenant,
    ICurrentUser user,
    IDateTimeProvider clock)
    : NexTraceDbContextBase(options, tenant, user, clock)
{
    // TODO: Adicionar DbSet<T> para cada entidade do módulo

    protected override System.Reflection.Assembly ConfigurationsAssembly
        => typeof(ContractsDbContext).Assembly;
}
